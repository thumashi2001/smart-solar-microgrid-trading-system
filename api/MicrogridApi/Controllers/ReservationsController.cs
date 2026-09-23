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

        var existingReservation = await _db.EnergyReservations
            .Find(r => r.Id == id)
            .FirstOrDefaultAsync();

        if (existingReservation == null)
        {
            return NotFound(new { message = "Reservation not found." });
        }

        // TODO: Phase 3 - Implement deep validation:
        // 1. 12-hour rule for updating.
        // 2. If slot changed: restore old slot availability, decrease new slot availability.
        // 3. Validate new station and new slot exist/active/available.

        existingReservation.StationId = request.StationId;
        existingReservation.SlotId = request.SlotId;
        existingReservation.UpdatedAt = DateTime.UtcNow;

        await _db.EnergyReservations.ReplaceOneAsync(
            r => r.Id == id,
            existingReservation
        );

        return Ok(existingReservation);
    }

    // DELETE: api/reservations/{id}
    // Cancels an existing reservation.
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var existingReservation = await _db.EnergyReservations
            .Find(r => r.Id == id)
            .FirstOrDefaultAsync();

        if (existingReservation == null)
        {
            return NotFound(new { message = "Reservation not found." });
        }

        if (existingReservation.Status == "Cancelled")
        {
            return BadRequest(new { message = "Reservation is already cancelled." });
        }

        // TODO: Phase 3 - Implement deep validation:
        // 1. 12-hour rule for cancelling.
        // 2. Restore slot availability by 1.

        var update = Builders<EnergyReservation>.Update
            .Set(r => r.Status, "Cancelled")
            .Set(r => r.UpdatedAt, DateTime.UtcNow);

        await _db.EnergyReservations.UpdateOneAsync(
            r => r.Id == id,
            update
        );

        return Ok(new { message = "Reservation cancelled successfully." });
    }
}
