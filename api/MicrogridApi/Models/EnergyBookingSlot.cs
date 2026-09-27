using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MicrogridApi.Models;

/// <summary>
/// Shared booking-slot document (Viman slot CRUD + Suwani seed compatibility).
/// Statuses: Available | Unavailable (Viman contract).
/// StationId stores MicrogridNode.NodeId (not Mongo ObjectId).
/// </summary>
public class EnergyBookingSlot
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string SlotId { get; set; } = string.Empty;

    public string StationId { get; set; } = string.Empty; // references MicrogridNode.NodeId

    public DateTime Date { get; set; }

    public string StartTime { get; set; } = string.Empty; // "HH:mm" format, e.g. "09:00"

    public string EndTime { get; set; } = string.Empty; // "HH:mm" format, e.g. "12:00"

    public int Capacity { get; set; } // maximum number of reservations for this slot

    public int Availability { get; set; } // remaining reservation spaces (0 <= Availability <= Capacity)

    /// <summary>Available | Unavailable</summary>
    public string Status { get; set; } = "Available";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
