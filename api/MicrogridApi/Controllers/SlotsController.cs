// ============================================================================
// File: SlotsController.cs
// Component: Component 2 - Energy Reservation & Slot Management
// Description: Manages energy booking slots, including schedule updates,
//              optimistic concurrency safeguards, and status transitions.
// ============================================================================

using Microsoft.AspNetCore.Mvc;
using MongoDB.Bson;
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
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
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> GetById(string id)
    {
        if (!ObjectId.TryParse(id, out _))
        {
            return NotFound(new { message = "Slot not found." });
        }

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
    [ProducesResponseType(StatusCodes.Status201Created)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
    [ProducesResponseType(StatusCodes.Status503ServiceUnavailable)]
    public async Task<IActionResult> Create(CreateSlotRequest request)
    {
        if (!MongoDbIndexConfigurator.IndexesVerified && !await MongoDbIndexConfigurator.EnsureIndexesVerifiedAsync(_db))
        {
            return StatusCode(503, new { message = "Database unique indexes have not been verified. Write operations are disabled until database readiness is established." });
        }

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
            catch (MongoWriteException ex) when (ex.WriteError.Category == ServerErrorCategory.DuplicateKey)
            {
                // Duplicate key detected:
                // Attempts 0 and 1: continue loop to generate a fresh SlotId.
                // Final attempt (attempt 2): loop exits cleanly with createdSlot == null to return 409 Conflict.
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
    /// Updates slot schedule and status using atomic optimistic concurrency.
    /// Strictly prevents updating Capacity and Availability.
    /// Eliminates unsafe post-check and rollback patterns by atomically validating
    /// availability and timestamp invariants during the update operation.
    /// </summary>
    [HttpPut("{id}")]
    [ProducesResponseType(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status400BadRequest)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    [ProducesResponseType(StatusCodes.Status409Conflict)]
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

            // SAFEGUARD: Do not allow schedule changes if active reservations exist or capacity is consumed
            var activeReservationsExist = await _db.EnergyReservations
                .CountDocumentsAsync(r => r.SlotId == existingSlot.SlotId && (r.Status == "Pending" || r.Status == "Approved")) > 0;

            if (activeReservationsExist || existingSlot.Availability < existingSlot.Capacity)
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

        // Optimistic concurrency filter:
        // If schedule is changing, we atomically verify BOTH the timestamp (UpdatedAt) AND that Availability == Capacity,
        // guaranteeing that no in-flight or concurrent booking has decremented slot availability during the update window.
        // If only status is changing, we verify UpdatedAt.
        var filterBuilder = Builders<EnergyBookingSlot>.Filter;
        var slotFilter = filterBuilder.And(
            filterBuilder.Eq(s => s.Id, id),
            filterBuilder.Eq(s => s.UpdatedAt, existingSlot.UpdatedAt)
        );

        if (scheduleChanging)
        {
            slotFilter &= filterBuilder.Eq(s => s.Availability, existingSlot.Capacity);
        }

        var updateResult = await _db.EnergyBookingSlots.UpdateOneAsync(
            slotFilter,
            updateDefinitionBuilder
        );

        if (updateResult.ModifiedCount == 0)
        {
            // The slot was modified concurrently or a booking consumed availability in the race window.
            // No partial state was written. Return 409 Conflict.
            return Conflict(new { message = "The slot was concurrently modified by another request. Please refresh and try again." });
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
