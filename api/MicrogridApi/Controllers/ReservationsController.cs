// ============================================================================
// File: ReservationsController.cs
// Component: Component 2 - Energy Reservation & Slot Management
// Description: Manages energy booking reservations, including concurrency controls,
//              atomic availability exchange, duplicate key retries, and cancellation integrity.
// ============================================================================

using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MicrogridApi.Data;
using MicrogridApi.Models;
using MicrogridApi.Dtos;

namespace MicrogridApi.Controllers;

[ApiController]
[Route("api/reservations")]
public class ReservationsController : ControllerBase
{
    private readonly MongoDbContext _db;

    // AUTHORIZATION CONTRACT:
    // Prosumers may create reservations and view/modify/cancel only their own reservations.
    // ProsumerNic ownership must be verified against authenticated claims once Component 1 JWT is active.
    // Backoffice and GridOperator roles have elevated administrative access to all reservations.
    // Integration Hook: Endpoints will be annotated with [Authorize] once Component 1 delivers the shared JWT middleware.

    public ReservationsController(MongoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET: api/reservations
    /// Returns all reservations.
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var reservations = await _db.EnergyReservations
            .Find(_ => true)
            .ToListAsync();

        return Ok(reservations);
    }

    /// <summary>
    /// GET: api/reservations/{id}
    /// Returns one reservation using its MongoDB ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var reservation = await _db.EnergyReservations
            .Find(r => r.Id == id)
            .FirstOrDefaultAsync();

        if (reservation == null)
        {
            return NotFound(new { message = "Reservation not found." });
        }

        return Ok(reservation);
    }

    /// <summary>
    /// GET: api/reservations/history/{prosumerNic}
    /// Returns reservation history for a prosumer.
    /// </summary>
    [HttpGet("history/{prosumerNic}")]
    public async Task<IActionResult> GetHistory(string prosumerNic)
    {
        var reservations = await _db.EnergyReservations
            .Find(r => r.ProsumerNic == prosumerNic)
            .ToListAsync();

        return Ok(reservations);
    }

    /// <summary>
    /// POST: api/reservations
    /// Creates a new reservation with atomic schedule validation, collision retry, and safe compensation.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateReservationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.ProsumerNic) ||
            string.IsNullOrWhiteSpace(request.StationId) ||
            string.IsNullOrWhiteSpace(request.SlotId))
        {
            return BadRequest(new { message = "ProsumerNic, StationId, and SlotId are required." });
        }

        // 1. Find and validate Prosumer
        var prosumer = await _db.Prosumers
            .Find(p => p.Nic == request.ProsumerNic)
            .FirstOrDefaultAsync();

        if (prosumer == null)
        {
            return NotFound(new { message = "Prosumer not found." });
        }

        if (prosumer.Status != "active")
        {
            return BadRequest(new { message = "Prosumer is not active." });
        }

        // 2. Find and validate Station
        var station = await _db.MicrogridNodes
            .Find(s => s.NodeId == request.StationId)
            .FirstOrDefaultAsync();

        if (station == null)
        {
            return NotFound(new { message = "Station not found." });
        }

        if (station.Status != "active")
        {
            return BadRequest(new { message = "Station is not active." });
        }

        // 3. Find and validate Slot
        var slot = await _db.EnergyBookingSlots
            .Find(s => s.SlotId == request.SlotId)
            .FirstOrDefaultAsync();

        if (slot == null)
        {
            return NotFound(new { message = "Slot not found." });
        }

        if (slot.StationId != request.StationId)
        {
            return BadRequest(new { message = "Slot does not belong to the selected station." });
        }

        if (slot.Status != "Available")
        {
            return BadRequest(new { message = "Slot is not available for booking." });
        }

        if (slot.Availability <= 0)
        {
            return BadRequest(new { message = "Slot has no remaining availability." });
        }

        // 4. 7-day booking window calculation
        if (!TimeSpan.TryParse(slot.StartTime, out var startTimeSpan))
        {
            return BadRequest(new { message = "Invalid slot start time format." });
        }

        var slotStartDateTime = slot.Date.Date + startTimeSpan;
        var now = DateTime.UtcNow;
        var maxAllowedDateTime = now.AddDays(7);

        if (slotStartDateTime < now)
        {
            return BadRequest(new { message = "Cannot create a reservation for a slot in the past." });
        }

        if (slotStartDateTime > maxAllowedDateTime)
        {
            return BadRequest(new { message = "Reservation must be scheduled within 7 days from now." });
        }

        // 5. Concurrency protection: Atomically validate slot schedule and decrement availability by 1.
        // We include StationId, Date, StartTime, EndTime, and Status in the atomic filter to prevent
        // booking if an operator concurrently altered the slot schedule.
        var filter = Builders<EnergyBookingSlot>.Filter.And(
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, request.SlotId),
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.StationId, request.StationId),
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.Date, slot.Date),
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.StartTime, slot.StartTime),
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.EndTime, slot.EndTime),
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.Status, "Available"),
            Builders<EnergyBookingSlot>.Filter.Gt(s => s.Availability, 0)
        );

        var update = Builders<EnergyBookingSlot>.Update
            .Inc(s => s.Availability, -1)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        var options = new FindOneAndUpdateOptions<EnergyBookingSlot>
        {
            ReturnDocument = ReturnDocument.After
        };

        var updatedSlot = await _db.EnergyBookingSlots.FindOneAndUpdateAsync(filter, update, options);

        if (updatedSlot == null)
        {
            return BadRequest(new { message = "Slot has no remaining availability or schedule changed concurrently." });
        }

        // 6. Create Reservation with collision-resilient insertion loop
        const int maxRetries = 3;
        EnergyReservation? createdReservation = null;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            var newReservation = new EnergyReservation
            {
                ReservationId = $"RES-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                ProsumerNic = request.ProsumerNic,
                StationId = request.StationId,
                SlotId = request.SlotId,
                Status = "Pending",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                TransactionReference = ""
            };

            try
            {
                await _db.EnergyReservations.InsertOneAsync(newReservation);
                createdReservation = newReservation;
                break;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey && attempt < maxRetries - 1)
            {
                // Unique ReservationId collision: retry with a newly generated ID
            }
            catch (Exception)
            {
                // Concurrency-safe compensation: restore availability ONLY if availability < slot.Capacity
                // to preserve the invariant: 0 <= Availability <= Capacity
                var rollbackFilter = Builders<EnergyBookingSlot>.Filter.And(
                    Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, request.SlotId),
                    Builders<EnergyBookingSlot>.Filter.Lt(s => s.Availability, slot.Capacity)
                );

                var rollbackUpdate = Builders<EnergyBookingSlot>.Update
                    .Inc(s => s.Availability, 1)
                    .Set(s => s.UpdatedAt, DateTime.UtcNow);

                await _db.EnergyBookingSlots.UpdateOneAsync(rollbackFilter, rollbackUpdate);
                throw;
            }
        }

        if (createdReservation == null)
        {
            // All retry attempts encountered duplicate keys: compensate slot availability
            var rollbackFilter = Builders<EnergyBookingSlot>.Filter.And(
                Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, request.SlotId),
                Builders<EnergyBookingSlot>.Filter.Lt(s => s.Availability, slot.Capacity)
            );

            var rollbackUpdate = Builders<EnergyBookingSlot>.Update
                .Inc(s => s.Availability, 1)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            await _db.EnergyBookingSlots.UpdateOneAsync(rollbackFilter, rollbackUpdate);

            return Conflict(new { message = "Failed to create reservation due to an ID collision. Please try again." });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdReservation.Id },
            createdReservation
        );
    }

    /// <summary>
    /// PUT: api/reservations/{id}
    /// Updates an existing reservation using optimistic concurrency and safe two-phase availability exchange.
    /// Eliminates dangerous decrement rollbacks by updating reservation state before releasing old slot space.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, UpdateReservationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StationId) ||
            string.IsNullOrWhiteSpace(request.SlotId))
        {
            return BadRequest(new { message = "StationId and SlotId are required." });
        }

        // 1. Find reservation
        var existingReservation = await _db.EnergyReservations
            .Find(r => r.Id == id)
            .FirstOrDefaultAsync();

        if (existingReservation == null)
        {
            return NotFound(new { message = "Reservation not found." });
        }

        // Terminal Reservation Update Protection:
        // Completed and Cancelled reservations must not be modified, and availability must not be exchanged.
        if (existingReservation.Status == "Cancelled")
        {
            return BadRequest(new { message = "Cancelled reservations cannot be modified." });
        }

        if (existingReservation.Status == "Completed")
        {
            return BadRequest(new { message = "Completed reservations cannot be modified." });
        }

        if (existingReservation.Status != "Pending" && existingReservation.Status != "Approved")
        {
            return BadRequest(new { message = "Reservation is not in a modifiable status." });
        }

        // Optimization: If slot is unchanged and station matches, return immediately (no-op).
        // If slot matches but station does not match, reject the request as invalid.
        if (existingReservation.SlotId == request.SlotId)
        {
            if (existingReservation.StationId == request.StationId)
            {
                return Ok(existingReservation);
            }

            return BadRequest(new { message = "Slot does not belong to the requested station." });
        }

        // 2. Find current slot and calculate 12-hour rule
        var currentSlot = await _db.EnergyBookingSlots
            .Find(s => s.SlotId == existingReservation.SlotId)
            .FirstOrDefaultAsync();

        if (currentSlot == null)
        {
            return NotFound(new { message = "Current slot not found." });
        }

        if (!TimeSpan.TryParse(currentSlot.StartTime, out var currentStartTimeSpan))
        {
            return BadRequest(new { message = "Invalid current slot start time format." });
        }

        var currentSlotStartDateTime = currentSlot.Date.Date + currentStartTimeSpan;
        var now = DateTime.UtcNow;

        if (currentSlotStartDateTime - now < TimeSpan.FromHours(12))
        {
            return BadRequest(new { message = "Modification requires at least 12 hours' notice." });
        }

        // 3. Find and validate requested new slot
        var newSlot = await _db.EnergyBookingSlots
            .Find(s => s.SlotId == request.SlotId)
            .FirstOrDefaultAsync();

        if (newSlot == null)
        {
            return NotFound(new { message = "New slot not found." });
        }

        if (newSlot.StationId != request.StationId)
        {
            return BadRequest(new { message = "New slot does not belong to the requested station." });
        }

        if (newSlot.Status != "Available")
        {
            return BadRequest(new { message = "New slot is not available for booking." });
        }

        if (newSlot.Availability <= 0)
        {
            return BadRequest(new { message = "New slot has no remaining availability." });
        }

        // 4. Validate new slot 7-day rule
        if (!TimeSpan.TryParse(newSlot.StartTime, out var newStartTimeSpan))
        {
            return BadRequest(new { message = "Invalid new slot start time format." });
        }

        var newSlotStartDateTime = newSlot.Date.Date + newStartTimeSpan;
        var maxAllowedDateTime = now.AddDays(7);

        if (newSlotStartDateTime < now)
        {
            return BadRequest(new { message = "Cannot modify reservation to a slot in the past." });
        }

        if (newSlotStartDateTime > maxAllowedDateTime)
        {
            return BadRequest(new { message = "New slot must be scheduled within 7 days from now." });
        }

        // 5. Atomic Availability Exchange:
        // Re-ordered operation sequence:
        //   Step A: Atomically reserve new slot space.
        //   Step B: Optimistically update reservation document (verifying original state & timestamp).
        //           If reservation update fails, compensate ONLY new slot (+1). Old slot was never touched!
        //   Step C: Release old slot space (+1).
        // This design completely eliminates dangerous decrement rollbacks on the old slot.

        // Step A: Decrement New Slot with atomic schedule verification
        var newSlotFilter = Builders<EnergyBookingSlot>.Filter.And(
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, request.SlotId),
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.StationId, request.StationId),
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.Date, newSlot.Date),
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.StartTime, newSlot.StartTime),
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.EndTime, newSlot.EndTime),
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.Status, "Available"),
            Builders<EnergyBookingSlot>.Filter.Gt(s => s.Availability, 0)
        );

        var newSlotUpdate = Builders<EnergyBookingSlot>.Update
            .Inc(s => s.Availability, -1)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        var updatedNewSlot = await _db.EnergyBookingSlots.FindOneAndUpdateAsync(
            newSlotFilter,
            newSlotUpdate,
            new FindOneAndUpdateOptions<EnergyBookingSlot> { ReturnDocument = ReturnDocument.After }
        );

        if (updatedNewSlot == null)
        {
            return BadRequest(new { message = "New slot has no remaining availability or schedule changed concurrently." });
        }

        // Step B: Optimistic Conditional Update on Reservation Document
        var reservationFilter = Builders<EnergyReservation>.Filter.And(
            Builders<EnergyReservation>.Filter.Eq(r => r.Id, id),
            Builders<EnergyReservation>.Filter.Eq(r => r.SlotId, existingReservation.SlotId),
            Builders<EnergyReservation>.Filter.Eq(r => r.StationId, existingReservation.StationId),
            Builders<EnergyReservation>.Filter.In(r => r.Status, new[] { "Pending", "Approved" }),
            Builders<EnergyReservation>.Filter.Eq(r => r.UpdatedAt, existingReservation.UpdatedAt)
        );

        var reservationUpdate = Builders<EnergyReservation>.Update
            .Set(r => r.StationId, request.StationId)
            .Set(r => r.SlotId, request.SlotId)
            .Set(r => r.UpdatedAt, DateTime.UtcNow);

        EnergyReservation? updatedReservation;

        try
        {
            updatedReservation = await _db.EnergyReservations.FindOneAndUpdateAsync(
                reservationFilter,
                reservationUpdate,
                new FindOneAndUpdateOptions<EnergyReservation> { ReturnDocument = ReturnDocument.After }
            );
        }
        catch (Exception)
        {
            // Compensate new slot decrement ONLY (safely restore +1)
            var rollbackNewSlotFilter = Builders<EnergyBookingSlot>.Filter.And(
                Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, request.SlotId),
                Builders<EnergyBookingSlot>.Filter.Lt(s => s.Availability, newSlot.Capacity)
            );

            await _db.EnergyBookingSlots.UpdateOneAsync(
                rollbackNewSlotFilter,
                Builders<EnergyBookingSlot>.Update.Inc(s => s.Availability, 1).Set(s => s.UpdatedAt, DateTime.UtcNow)
            );
            throw;
        }

        if (updatedReservation == null)
        {
            // Reservation was concurrently modified, cancelled, or completed.
            // Safely compensate the new slot decrement (restore +1). Old slot was never touched.
            var rollbackNewSlotFilter = Builders<EnergyBookingSlot>.Filter.And(
                Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, request.SlotId),
                Builders<EnergyBookingSlot>.Filter.Lt(s => s.Availability, newSlot.Capacity)
            );

            await _db.EnergyBookingSlots.UpdateOneAsync(
                rollbackNewSlotFilter,
                Builders<EnergyBookingSlot>.Update.Inc(s => s.Availability, 1).Set(s => s.UpdatedAt, DateTime.UtcNow)
            );

            return Conflict(new { message = "The reservation was concurrently modified or cancelled. Please refresh and try again." });
        }

        // Step C: Increment Old Slot availability (release space)
        var oldSlotFilter = Builders<EnergyBookingSlot>.Filter.And(
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, currentSlot.SlotId),
            Builders<EnergyBookingSlot>.Filter.Lt(s => s.Availability, currentSlot.Capacity)
        );

        var oldSlotUpdate = Builders<EnergyBookingSlot>.Update
            .Inc(s => s.Availability, 1)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        await _db.EnergyBookingSlots.UpdateOneAsync(oldSlotFilter, oldSlotUpdate);

        return Ok(updatedReservation);
    }

    /// <summary>
    /// DELETE: api/reservations/{id}
    /// Cancels an existing reservation with optimistic concurrency, slot integrity, and non-misleading status reporting.
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        // 1. Find reservation
        var existingReservation = await _db.EnergyReservations
            .Find(r => r.Id == id)
            .FirstOrDefaultAsync();

        if (existingReservation == null)
        {
            return NotFound(new { message = "Reservation not found." });
        }

        // 2. Prevent invalid state transitions.
        // Only Pending and Approved reservations can be cancelled.
        if (existingReservation.Status != "Pending" && existingReservation.Status != "Approved")
        {
            if (existingReservation.Status == "Cancelled")
            {
                return BadRequest(new { message = "Reservation is already cancelled." });
            }

            if (existingReservation.Status == "Completed")
            {
                return BadRequest(new { message = "Completed reservations cannot be cancelled." });
            }

            return BadRequest(new { message = $"Reservation in status '{existingReservation.Status}' cannot be cancelled." });
        }

        // 3. Find current slot and calculate 12-hour rule
        var currentSlot = await _db.EnergyBookingSlots
            .Find(s => s.SlotId == existingReservation.SlotId)
            .FirstOrDefaultAsync();

        if (currentSlot == null)
        {
            return NotFound(new { message = "Associated slot not found. Cannot safely cancel." });
        }

        if (!TimeSpan.TryParse(currentSlot.StartTime, out var currentStartTimeSpan))
        {
            return BadRequest(new { message = "Invalid slot start time format." });
        }

        var slotStartDateTime = currentSlot.Date.Date + currentStartTimeSpan;
        var now = DateTime.UtcNow;

        if (slotStartDateTime - now < TimeSpan.FromHours(12))
        {
            return BadRequest(new { message = "Cancellation requires at least 12 hours' notice." });
        }

        // 4. Atomic Reservation Update (Logical Cancellation) with optimistic concurrency.
        // Validates expected SlotId and UpdatedAt so concurrent modification or cancellation is safely detected.
        var reservationFilter = Builders<EnergyReservation>.Filter.And(
            Builders<EnergyReservation>.Filter.Eq(r => r.Id, id),
            Builders<EnergyReservation>.Filter.Eq(r => r.SlotId, existingReservation.SlotId),
            Builders<EnergyReservation>.Filter.In(r => r.Status, new[] { "Pending", "Approved" }),
            Builders<EnergyReservation>.Filter.Eq(r => r.UpdatedAt, existingReservation.UpdatedAt)
        );

        var reservationUpdate = Builders<EnergyReservation>.Update
            .Set(r => r.Status, "Cancelled")
            .Set(r => r.UpdatedAt, DateTime.UtcNow);

        var updatedReservation = await _db.EnergyReservations.FindOneAndUpdateAsync(
            reservationFilter,
            reservationUpdate,
            new FindOneAndUpdateOptions<EnergyReservation> { ReturnDocument = ReturnDocument.After }
        );

        if (updatedReservation == null)
        {
            return Conflict(new { message = "Reservation was modified or cancelled concurrently. Cancellation aborted." });
        }

        // 5. Atomic Slot Availability Restoration
        // Conditionally increments availability ONLY for the exact slot confirmed by the cancellation.
        var slotFilter = Builders<EnergyBookingSlot>.Filter.And(
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, existingReservation.SlotId),
            Builders<EnergyBookingSlot>.Filter.Lt(s => s.Availability, currentSlot.Capacity)
        );

        var slotUpdate = Builders<EnergyBookingSlot>.Update
            .Inc(s => s.Availability, 1)
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        var oldSlotResult = await _db.EnergyBookingSlots.UpdateOneAsync(slotFilter, slotUpdate);

        if (oldSlotResult.ModifiedCount == 0)
        {
            // Do not report 200 OK success if slot restoration failed.
            // Return 500 Internal Server Error with the cancelled reservation details.
            return StatusCode(500, new { 
                message = "Reservation was cancelled, but slot availability could not be restored due to a database anomaly.", 
                reservation = updatedReservation 
            });
        }

        return Ok(new { 
            message = "Reservation cancelled successfully.", 
            reservation = updatedReservation 
        });
    }
}
