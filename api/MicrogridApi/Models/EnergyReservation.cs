using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MicrogridApi.Models;

public class EnergyReservation
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string ReservationId { get; set; } = string.Empty;

    public string ProsumerNic { get; set; } = string.Empty; // references Prosumer.Nic

    public string StationId { get; set; } = string.Empty; // references MicrogridNode.NodeId

    public string SlotId { get; set; } = string.Empty; // references EnergyBookingSlot.SlotId

    public string Status { get; set; } = "Pending"; // "Pending", "Approved", "Cancelled", "Completed"

    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

    public string TransactionReference { get; set; } = string.Empty; // For QR/operator flow
}
