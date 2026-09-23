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

        // TODO: Phase 3 - Implement deep validation:
        // 1. Prosumer exists and is active.
        // 2. Station exists and is active.
        // 3. Slot exists, is active/available, belongs to station.
        // 4. 7-day booking window rule.
        // 5. Slot availability check (> 0).
        // 6. Decrease slot availability by 1.

        var newReservation = new EnergyReservation
        {
            ReservationId = $"RES-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
            ProsumerNic = request.ProsumerNic,
            StationId = request.StationId,
            SlotId = request.SlotId,
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow
        };

        await _db.EnergyReservations.InsertOneAsync(newReservation);

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
