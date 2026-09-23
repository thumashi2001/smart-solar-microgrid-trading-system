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

    public SlotsController(MongoDbContext db)
    {
        _db = db;
    }

    // GET: api/slots
    // Returns slots with optional filtering by stationId and date.
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

    // GET: api/slots/{id}
    // Returns one slot using its MongoDB ID.
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

    // POST: api/slots
    // Creates a new slot.
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

        await _db.EnergyBookingSlots.InsertOneAsync(newSlot);

        return CreatedAtAction(
            nameof(GetById),
            new { id = newSlot.Id },
            newSlot
        );
    }

    // PUT: api/slots/{id}
    // Updates slot schedule and status. Strictly prevents updating Capacity and Availability.
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

            // SAFEGUARD: Do not allow schedule changes if active reservations exist
            var activeReservationsExist = await _db.EnergyReservations
                .CountDocumentsAsync(r => r.SlotId == existingSlot.SlotId && (r.Status == "Pending" || r.Status == "Approved")) > 0;

            if (activeReservationsExist)
            {
                return BadRequest(new { message = "Cannot modify the slot schedule because active reservations exist for this slot." });
            }
        }

        // Apply safe updates
        var updateDefinitionBuilder = Builders<EnergyBookingSlot>.Update
            .Set(s => s.UpdatedAt, DateTime.UtcNow);

        if (scheduleChanging)
        {
            updateDefinitionBuilder = updateDefinitionBuilder
                .Set(s => s.Date, newDate)
                .Set(s => s.StartTime, newStartTime)
                .Set(s => s.EndTime, newEndTime);
            
            existingSlot.Date = newDate;
            existingSlot.StartTime = newStartTime;
            existingSlot.EndTime = newEndTime;
        }

        if (!string.IsNullOrWhiteSpace(request.Status))
        {
            updateDefinitionBuilder = updateDefinitionBuilder.Set(s => s.Status, request.Status);
            existingSlot.Status = request.Status;
        }

        await _db.EnergyBookingSlots.UpdateOneAsync(
            s => s.Id == id,
            updateDefinitionBuilder
        );

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
