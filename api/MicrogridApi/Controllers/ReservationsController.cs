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

    public ReservationsController(MongoDbContext db)
    {
        _db = db;
    }

    // GET: api/reservations
    // Returns all reservations.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var reservations = await _db.EnergyReservations
            .Find(_ => true)
            .ToListAsync();

        return Ok(reservations);
    }

    // GET: api/reservations/{id}
    // Returns one reservation using its MongoDB ID.
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

    // GET: api/reservations/history/{prosumerNic}
    // Returns reservation history for a prosumer.
    [HttpGet("history/{prosumerNic}")]
    public async Task<IActionResult> GetHistory(string prosumerNic)
    {
        var reservations = await _db.EnergyReservations
            .Find(r => r.ProsumerNic == prosumerNic)
            .ToListAsync();

        return Ok(reservations);
    }

    // POST: api/reservations
    // Creates a new reservation.
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

        // 5. Concurrency protection: Atomically check availability > 0 and decrement by 1.
        // We use FindOneAndUpdateAsync to ensure that even if multiple requests arrive simultaneously,
        // only one can consume a given availability unit. Capacity remains untouched.
        var filter = Builders<EnergyBookingSlot>.Filter.And(
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, request.SlotId),
            Builders<EnergyBookingSlot>.Filter.Gt(s => s.Availability, 0),
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.Status, "Available")
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
            // If updatedSlot is null, it means either the slot was deleted OR availability dropped to 0 concurrently.
            return BadRequest(new { message = "Slot has no remaining availability or could not be booked due to high concurrency." });
        }

        // 6. Create Reservation
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
        }
        catch (Exception)
        {
            // In a production system with replica sets, we would use a MongoDB transaction.
            // For this standalone setup, if reservation creation fails, we must manually rollback the availability decrement to avoid partial state.
            var rollbackUpdate = Builders<EnergyBookingSlot>.Update
                .Inc(s => s.Availability, 1)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);
            
            await _db.EnergyBookingSlots.UpdateOneAsync(
                Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, request.SlotId), 
                rollbackUpdate
            );
            
            throw; // Re-throw to return 500 error after rollback
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = newReservation.Id },
            newReservation
        );
    }

    // PUT: api/reservations/{id}
    // Updates an existing reservation.
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

        // Optimization: If slot is unchanged, return immediately
        if (existingReservation.SlotId == request.SlotId)
        {
            return Ok(existingReservation);
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

        // 5. Atomic Availability Exchange
        // Since we are not using MongoDB multi-document transactions (standalone assumption),
        // we use a safe sequence: decrement new slot -> increment old slot -> update reservation.
        // If anything fails, we roll back the previous steps.

        // Step A: Decrement New Slot
        var newSlotFilter = Builders<EnergyBookingSlot>.Filter.And(
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, request.SlotId),
            Builders<EnergyBookingSlot>.Filter.Gt(s => s.Availability, 0),
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.Status, "Available")
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
            return BadRequest(new { message = "New slot has no remaining availability or could not be booked due to concurrency." });
        }

        // Step B: Increment Old Slot
        try
        {
            var oldSlotUpdate = Builders<EnergyBookingSlot>.Update
                .Inc(s => s.Availability, 1)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            var oldSlotResult = await _db.EnergyBookingSlots.UpdateOneAsync(
                Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, currentSlot.SlotId),
                oldSlotUpdate
            );

            if (oldSlotResult.ModifiedCount == 0)
            {
                // The old slot was not found/modified. We must rollback the new slot decrement.
                await _db.EnergyBookingSlots.UpdateOneAsync(
                    Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, request.SlotId),
                    Builders<EnergyBookingSlot>.Update.Inc(s => s.Availability, 1)
                );
                return BadRequest(new { message = "Failed to update current slot availability." });
            }
        }
        catch (Exception)
        {
            // Rollback New Slot decrement
            await _db.EnergyBookingSlots.UpdateOneAsync(
                Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, request.SlotId),
                Builders<EnergyBookingSlot>.Update.Inc(s => s.Availability, 1)
            );
            throw;
        }

        // Step C: Update Reservation
        existingReservation.StationId = request.StationId;
        existingReservation.SlotId = request.SlotId;
        existingReservation.UpdatedAt = DateTime.UtcNow;
        // Status remains unchanged

        try
        {
            await _db.EnergyReservations.ReplaceOneAsync(
                r => r.Id == id,
                existingReservation
            );
        }
        catch (Exception)
        {
            // Rollback both slots
            // 1. Rollback new slot (safely add 1 back)
            await _db.EnergyBookingSlots.UpdateOneAsync(
                Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, request.SlotId),
                Builders<EnergyBookingSlot>.Update.Inc(s => s.Availability, 1)
            );
            
            // 2. Rollback old slot (safely decrement ONLY IF Availability > 0 to prevent invariant violation)
            // We use a conditional update to ensure we don't accidentally push it below 0 
            // if another operation concurrently claimed the released availability.
            var oldSlotRollbackFilter = Builders<EnergyBookingSlot>.Filter.And(
                Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, currentSlot.SlotId),
                Builders<EnergyBookingSlot>.Filter.Gt(s => s.Availability, 0)
            );
            
            await _db.EnergyBookingSlots.UpdateOneAsync(
                oldSlotRollbackFilter,
                Builders<EnergyBookingSlot>.Update.Inc(s => s.Availability, -1)
            );
            throw;
        }

        return Ok(existingReservation);
    }

    // DELETE: api/reservations/{id}
    // Cancels an existing reservation.
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

        // 2. Prevent invalid state transitions (including double cancellation)
        if (existingReservation.Status == "Cancelled")
        {
            return BadRequest(new { message = "Reservation is already cancelled." });
        }

        if (existingReservation.Status == "Completed")
        {
            return BadRequest(new { message = "Completed reservations cannot be cancelled." });
        }

        // 3. Find the current slot and calculate 12-hour rule
        var currentSlot = await _db.EnergyBookingSlots
            .Find(s => s.SlotId == existingReservation.SlotId)
            .FirstOrDefaultAsync();

        if (currentSlot == null)
        {
            // If the slot doesn't exist, we can't calculate the 12-hour rule or restore availability safely.
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

        // 4. Atomic Reservation Update (Logical Cancellation)
        // By checking the status in the filter, we ensure that if two cancellation requests arrive at the 
        // exact same millisecond, only ONE will succeed in updating the status.
        var reservationFilter = Builders<EnergyReservation>.Filter.And(
            Builders<EnergyReservation>.Filter.Eq(r => r.Id, id),
            Builders<EnergyReservation>.Filter.Ne(r => r.Status, "Cancelled"),
            Builders<EnergyReservation>.Filter.Ne(r => r.Status, "Completed")
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
            return BadRequest(new { message = "Reservation could not be cancelled. It may have already been cancelled concurrently." });
        }

        // 5. Atomic Slot Availability Restoration
        // We conditionally increment availability ONLY if it is currently less than the known capacity.
        // Because 'Capacity' is immutable per project rules, using currentSlot.Capacity is safe from staleness.
        // This guarantees the invariant: 0 <= Availability <= Capacity
        bool slotRestored = false;
        try
        {
            var slotFilter = Builders<EnergyBookingSlot>.Filter.And(
                Builders<EnergyBookingSlot>.Filter.Eq(s => s.SlotId, currentSlot.SlotId),
                Builders<EnergyBookingSlot>.Filter.Lt(s => s.Availability, currentSlot.Capacity)
            );

            var slotUpdate = Builders<EnergyBookingSlot>.Update
                .Inc(s => s.Availability, 1)
                .Set(s => s.UpdatedAt, DateTime.UtcNow);

            var oldSlotResult = await _db.EnergyBookingSlots.UpdateOneAsync(slotFilter, slotUpdate);

            if (oldSlotResult.ModifiedCount > 0)
            {
                slotRestored = true;
            }
        }
        catch (Exception)
        {
            // If the database fails catastrophically during the slot increment, the reservation
            // remains cancelled. Compensating a failed cancellation back to "Pending" because 
            // the slot availability couldn't update is unsafe and violates user expectations.
            // The reservation is successfully cancelled, but availability is "lost" due to this DB anomaly.
            // The exception is thrown to return a 500 error to the client, indicating a partial failure.
            throw; 
        }

        if (!slotRestored)
        {
            // ModifiedCount == 0 means the slot was deleted concurrently OR the availability was already 
            // magically at capacity (data anomaly). 
            // The logical cancellation succeeded, so we return 200 OK but include an explanatory message.
            return Ok(new { 
                message = "Reservation cancelled successfully, but slot availability could not be automatically restored due to an anomaly.", 
                reservation = updatedReservation 
            });
        }

        return Ok(new { 
            message = "Reservation cancelled successfully.", 
            reservation = updatedReservation 
        });
    }
}
