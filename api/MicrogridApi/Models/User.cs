// =============================================================================
// File: User.cs
// Description: MongoDB document model for Backoffice and Grid Operator user
//              accounts, stored in the "users" collection.
// Author: Thumashi (Component 1)
// =============================================================================

using MongoDB.Bson;
using MongoDB.Bson.Serialization.Attributes;

namespace MicrogridApi.Models;

public class User
{
    [BsonId]
    [BsonRepresentation(BsonType.ObjectId)]
    public string? Id { get; set; }

    public string UserId { get; set; } = Guid.NewGuid().ToString();
    public string Email { get; set; } = string.Empty;
    public string PasswordHash { get; set; } = string.Empty;
    public string FullName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty; // "Backoffice" or "GridOperator"
    public string Status { get; set; } = "active";   // "active" or "deactivated"
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}