// ============================================================================
// File: SlotsController.cs
// Component: Component 2 - Energy Reservation & Slot Management
// Description: Manages energy booking slots, including schedule updates,
//              concurrency safeguards, and status transitions.
// ============================================================================

using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MicrogridApi.Data;
using MicrogridApi.Models;
using MicrogridApi.Dtos;

namespace MicrogridApi.Controllers;

[ApiController]
[Route("api/slots")]
public class SlotsController : ControllerBase
{
    private readonly MongoDbContext _db;

    // AUTHORIZATION CONTRACT:
    // Slot mutations (Create, Update) are restricted to Backoffice and GridOperator roles.
    // Prosumers are strictly prohibited from creating or altering slot schedules.
    // Integration Hook: Once Component 1 configures the ASP.NET Core JWT authentication scheme on dev,
    // endpoints will be secured with [Authorize(Roles = "Backoffice,GridOperator")].

    public SlotsController(MongoDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// GET: api/slots
    /// Returns slots with optional filtering by stationId and calendar date (YYYY-MM-DD).
    /// </summary>
    [HttpGet]
    public async Task<IActionResult> GetAll([FromQuery] string? stationId, [FromQuery] string? date)
    {
        var filterBuilder = Builders<EnergyBookingSlot>.Filter;
        var filter = filterBuilder.Empty;

        if (!string.IsNullOrWhiteSpace(stationId))
        {
            filter &= filterBuilder.Eq(s => s.StationId, stationId);
        }

        if (!string.IsNullOrWhiteSpace(date))
        {
            if (!DateTime.TryParseExact(date, "yyyy-MM-dd", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var parsedDate))
            {
                return BadRequest(new { message = "Invalid date format. Expected YYYY-MM-DD." });
            }

            // Match the exact date portion (ignoring time if stored with time)
            filter &= filterBuilder.Eq(s => s.Date, parsedDate.Date);
        }

        var slots = await _db.EnergyBookingSlots
            .Find(filter)
            .ToListAsync();

        return Ok(slots);
    }

    /// <summary>
    /// GET: api/slots/{id}
    /// Returns one slot using its MongoDB ID.
    /// </summary>
    [HttpGet("{id}")]
    public async Task<IActionResult> GetById(string id)
    {
        var slot = await _db.EnergyBookingSlots
            .Find(s => s.Id == id)
            .FirstOrDefaultAsync();

        if (slot == null)
        {
            return NotFound(new { message = "Slot not found." });
        }

        return Ok(slot);
    }

    /// <summary>
    /// POST: api/slots
    /// Creates a new energy booking slot with collision retry and database-level uniqueness.
    /// </summary>
    [HttpPost]
    public async Task<IActionResult> Create(CreateSlotRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.StationId))
        {
            return BadRequest(new { message = "StationId is required." });
        }

        if (request.Date == default || request.Date == DateTime.MinValue || request.Date.Year <= 1)
        {
            return BadRequest(new { message = "Date is required." });
        }

        if (request.Capacity <= 0)
        {
            return BadRequest(new { message = "Capacity must be greater than 0." });
        }

        if (!TryParseStrictTime(request.StartTime, out var parsedStartTime) || !TryParseStrictTime(request.EndTime, out var parsedEndTime))
        {
            return BadRequest(new { message = "Invalid time format for StartTime or EndTime. Expected format is HH:mm." });
        }

        if (parsedEndTime <= parsedStartTime)
        {
            return BadRequest(new { message = "EndTime must be after StartTime." });
        }

        // Validate that the referenced station exists and is active
        var station = await _db.MicrogridNodes
            .Find(s => s.NodeId == request.StationId)
            .FirstOrDefaultAsync();

        if (station == null)
        {
            return NotFound(new { message = "Station not found." });
        }

        if (station.Status != "active")
        {
            return BadRequest(new { message = "Cannot create slots for an inactive station." });
        }

        // Collision-resilient insertion loop for SlotId
        const int maxRetries = 3;
        EnergyBookingSlot? createdSlot = null;

        for (int attempt = 0; attempt < maxRetries; attempt++)
        {
            var newSlot = new EnergyBookingSlot
            {
                SlotId = $"SLOT-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                StationId = request.StationId,
                Date = request.Date.Date, // store pure date component
                StartTime = request.StartTime,
                EndTime = request.EndTime,
                Capacity = request.Capacity,
                Availability = request.Capacity, // Availability initially represents the full booking capacity
                Status = "Available",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                await _db.EnergyBookingSlots.InsertOneAsync(newSlot);
                createdSlot = newSlot;
                break;
            }
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey && attempt < maxRetries - 1)
            {
                // SlotId collision: retry with a newly generated SlotId
            }
        }

        if (createdSlot == null)
        {
            return Conflict(new { message = "Failed to create slot due to a unique key collision. Please try again." });
        }

        return CreatedAtAction(
            nameof(GetById),
            new { id = createdSlot.Id },
            createdSlot
        );
    }

    /// <summary>
    /// PUT: api/slots/{id}
    /// Updates slot schedule and status using optimistic concurrency and active-reservation rollback.
    /// Strictly prevents updating Capacity and Availability.
    /// </summary>
    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, UpdateSlotRequest request)
    {
        var existingSlot = await _db.EnergyBookingSlots
            .Find(s => s.Id == id)
            .FirstOrDefaultAsync();

        if (existingSlot == null)
        {
            return NotFound(new { message = "Slot not found." });
        }

        // Validate Status
        if (!string.IsNullOrWhiteSpace(request.Status) && 
            request.Status != "Available" && 
            request.Status != "Unavailable")
        {
            return BadRequest(new { message = "Status must be 'Available' or 'Unavailable'." });
        }

        bool scheduleChanging = false;
        var newDate = existingSlot.Date;
        var newStartTime = existingSlot.StartTime;
        var newEndTime = existingSlot.EndTime;

        if (request.Date.HasValue)
        {
            if (request.Date.Value == default || request.Date.Value == DateTime.MinValue || request.Date.Value.Year <= 1)
            {
                return BadRequest(new { message = "Date is required." });
            }

            if (request.Date.Value.Date != existingSlot.Date.Date)
            {
                newDate = request.Date.Value.Date;
                scheduleChanging = true;
            }
        }

        if (!string.IsNullOrWhiteSpace(request.StartTime) && request.StartTime != existingSlot.StartTime)
        {
            newStartTime = request.StartTime;
            scheduleChanging = true;
        }

        if (!string.IsNullOrWhiteSpace(request.EndTime) && request.EndTime != existingSlot.EndTime)
        {
            newEndTime = request.EndTime;
            scheduleChanging = true;
        }

        if (scheduleChanging)
        {
            // Validate new time formats
            if (!TryParseStrictTime(newStartTime, out var parsedStartTime) || 
                !TryParseStrictTime(newEndTime, out var parsedEndTime))
            {
                return BadRequest(new { message = "Invalid time format for StartTime or EndTime. Expected format is HH:mm." });
            }

            if (parsedEndTime <= parsedStartTime)
            {
                return BadRequest(new { message = "EndTime must be after StartTime." });
            }

            // SAFEGUARD (Pre-check): Do not allow schedule changes if active reservations exist
            var activeReservationsExist = await _db.EnergyReservations
                .CountDocumentsAsync(r => r.SlotId == existingSlot.SlotId && (r.Status == "Pending" || r.Status == "Approved")) > 0;

            if (activeReservationsExist)
            {
                return BadRequest(new { message = "Cannot modify the slot schedule because active reservations exist for this slot." });
            }
        }

        // Apply safe updates with optimistic concurrency
        var updateDefinitionBuilder = Builders<EnergyBookingSlot>.Update
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        if (scheduleChanging)
        {
            updateDefinitionBuilder = updateDefinitionBuilder
                .Set(s => s.Date, newDate)
                .Set(s => s.StartTime, newStartTime)
                .Set(s => s.EndTime, newEndTime);
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            updateDefinitionBuilder = updateDefinitionBuilder.Set(s => s.Status, request.Status);
        }

        // Optimistic concurrency filter: ensures the slot was not modified concurrently
        var slotFilter = Builders<EnergyBookingSlot>.Filter.And(
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.Id, id),
            Builders<EnergyBookingSlot>.Filter.Eq(s => s.UpdatedAt, existingSlot.UpdatedAt)
        );

        var updateResult = await _db.EnergyBookingSlots.UpdateOneAsync(
            slotFilter,
            updateDefinitionBuilder
        );

        if (updateResult.ModifiedCount == 0)
        {
            return Conflict(new { message = "The slot was concurrently modified by another request. Please refresh and try again." });
        }

        // SAFEGUARD (Post-check): Verify that no active reservation was committed concurrently during the update window
        if (scheduleChanging)
        {
            var postCheckActiveReservations = await _db.EnergyReservations
                .CountDocumentsAsync(r => r.SlotId == existingSlot.SlotId && (r.Status == "Pending" || r.Status == "Approved")) > 0;

            if (postCheckActiveReservations)
            {
                // Revert schedule change immediately to protect the active reservation from schedule corruption
                await _db.EnergyBookingSlots.UpdateOneAsync(
                    s => s.Id == id,
                    Builders<EnergyBookingSlot>.Update
                        .Set(s => s.Date, existingSlot.Date)
                        .Set(s => s.StartTime, existingSlot.StartTime)
                        .Set(s => s.EndTime, existingSlot.EndTime)
                        .Set(s => s.UpdatedAt, DateTime.UtcNow)
                );

                return BadRequest(new { message = "Cannot modify the slot schedule because active reservations exist for this slot." });
            }
        }

        existingSlot.Date = newDate;
        existingSlot.StartTime = newStartTime;
        existingSlot.EndTime = newEndTime;
        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            existingSlot.Status = request.Status;
        }
        existingSlot.UpdatedAt = DateTime.UtcNow;

        return Ok(existingSlot);
    }

    private static bool TryParseStrictTime(string? timeStr, out TimeSpan timeSpan)
    {
        timeSpan = default;
        if (string.IsNullOrWhiteSpace(timeStr))
            return false;

        if (System.Text.RegularExpressions.Regex.IsMatch(timeStr, @"^([01]\d|2[0-3]):([0-5]\d)$") &&
            DateTime.TryParseExact(timeStr, "HH:mm", System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.None, out var dt))
        {
            timeSpan = dt.TimeOfDay;
            return true;
        }

        return false;
    }
}
