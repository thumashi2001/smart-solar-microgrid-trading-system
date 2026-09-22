// =============================================================================
// File: EnergyReservation.cs
// Description: Provisional reservation document compatible with Viman's proposed
//              energyReservation collection. Suwani owns QR/transfer fields only.
// Author: Suwani (Component 4)
// =============================================================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MicrogridApi.Models;

/// <summary>
/// Canonical statuses (lowercase to match existing users/nodes): pending, approved,
/// cancelled, completed.
/// StationId stores the application NodeId (e.g. NODE-XXXXXXXX), not Mongo ObjectId.
/// TransactionReference is a high-entropy opaque QR credential issued by the API.
/// </summary>
public class EnergyReservation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string ReservationId { get; set; } = Guid.NewGuid().ToString("N");

    public string ProsumerNic { get; set; } = string.Empty;

    /// <summary>Application-level station identifier (MicrogridNode.NodeId).</summary>
    public string StationId { get; set; } = string.Empty;

    public string SlotId { get; set; } = string.Empty;

    /// <summary>pending | approved | cancelled | completed</summary>
    public string Status { get; set; } = "pending";

    /// <summary>
    /// Opaque server-issued QR reference. Never derived from sequential counters alone.
    /// </summary>
    public string? TransactionReference { get; set; }

    public DateTime? TransactionReferenceIssuedAt { get; set; }

    public string? CompletedByOperatorId { get; set; }

    public string? CompletedByOperatorName { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
