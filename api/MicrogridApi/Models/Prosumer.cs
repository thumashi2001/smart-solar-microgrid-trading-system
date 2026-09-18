using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MicrogridApi.Models;

public class Prosumer
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string Nic { get; set; } = string.Empty; // primary key
    public string FullName { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string Status { get; set; } = "active"; // active | deactivated | pendingActivation
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}