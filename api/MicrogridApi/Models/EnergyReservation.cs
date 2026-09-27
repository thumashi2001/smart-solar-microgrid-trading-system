using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MicrogridApi.Models;

/// <summary>
/// Shared reservation document (Viman CRUD + Suwani QR/transfer).
/// Statuses: Pending | Approved | Cancelled | Completed (PascalCase, Viman contract).
/// StationId stores MicrogridNode.NodeId (not Mongo ObjectId).
/// TransactionReference is an opaque QR credential issued by Suwani's API when approved.
/// </summary>
public class EnergyReservation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string ReservationId { get; set; } = string.Empty;

    public string ProsumerNic { get; set; } = string.Empty; // references Prosumer.Nic

    public string StationId { get; set; } = string.Empty; // references MicrogridNode.NodeId

    public string SlotId { get; set; } = string.Empty; // references EnergyBookingSlot.SlotId

    /// <summary>Pending | Approved | Cancelled | Completed</summary>
    public string Status { get; set; } = "Pending";

    /// <summary>
    /// Opaque server-issued QR reference. Empty until QR is issued for an Approved reservation.
    /// </summary>
    public string TransactionReference { get; set; } = string.Empty;

    public DateTime? TransactionReferenceIssuedAt { get; set; }

    public string? CompletedByOperatorId { get; set; }

    public string? CompletedByOperatorName { get; set; }

    public DateTime? CompletedAt { get; set; }

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
}
