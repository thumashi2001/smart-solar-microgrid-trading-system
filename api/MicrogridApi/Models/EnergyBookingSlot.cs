// =============================================================================
// File: EnergyBookingSlot.cs
// Description: Provisional booking-slot document compatible with Viman's proposed
//              energyBookingSlots collection. Used by Suwani seed only — not CRUD.
// Author: Suwani (Component 4)
// =============================================================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MicrogridApi.Models;

/// <summary>
/// Slot statuses use lowercase to match users/nodes: active | inactive | booked.
/// StationId stores MicrogridNode.NodeId (not Mongo ObjectId).
/// </summary>
public class EnergyBookingSlot
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string SlotId { get; set; } = string.Empty;

    /// <summary>Application-level station id (MicrogridNode.NodeId).</summary>
    public string StationId { get; set; } = string.Empty;

    /// <summary>UTC calendar date of the slot (date portion).</summary>
    public DateTime Date { get; set; }

    public string StartTime { get; set; } = string.Empty;

    public string EndTime { get; set; } = string.Empty;

    public int Capacity { get; set; }

    public int Availability { get; set; }

    /// <summary>active | inactive | booked</summary>
    public string Status { get; set; } = "active";

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
