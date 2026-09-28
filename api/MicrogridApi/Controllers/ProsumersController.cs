// =============================================================================
// File: ProsumersController.cs
// Description: Prosumer management API (register, list, update, delete,
//              activate/deactivate, change password). NIC is the primary key.
// Author: Thumashi (Component 1)
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MicrogridApi.Data;
using MicrogridApi.Models;

namespace MicrogridApi.Controllers;

[ApiController]
[Route("api/prosumers")]
public class ProsumersController : ControllerBase
{
    private readonly MongoDbContext _db;

    // Creates the controller with the MongoDB context.
    public ProsumersController(MongoDbContext db)
    {
        _db = db;
    }

    // GET: api/prosumers - returns every prosumer (used by the web admin page).
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var prosumers = await _db.Prosumers.Find(_ => true).ToListAsync();
        return Ok(prosumers);
    }

    // POST: api/prosumers/register - self registration from the mobile app.
    // Rejects empty details and duplicate NICs; new accounts start as pendingActivation.
    [HttpPost("register")]
    public async Task<IActionResult> Register(Prosumer newProsumer)
    {
        if (string.IsNullOrWhiteSpace(newProsumer.Nic) ||
            string.IsNullOrWhiteSpace(newProsumer.FullName) ||
            string.IsNullOrWhiteSpace(newProsumer.Email) ||
            string.IsNullOrWhiteSpace(newProsumer.Phone) ||
            string.IsNullOrWhiteSpace(newProsumer.PasswordHash))
        {
            return BadRequest("NIC, full name, email, phone and password are required.");
        }

        var existing = await _db.Prosumers.Find(p => p.Nic == newProsumer.Nic).FirstOrDefaultAsync();
        if (existing != null) return BadRequest("A prosumer with this NIC already exists.");

        newProsumer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newProsumer.PasswordHash);
        newProsumer.Status = "pendingActivation";
        await _db.Prosumers.InsertOneAsync(newProsumer);
        return Ok(newProsumer);
    }

    // PUT: api/prosumers/{nic} - updates a prosumer's profile.
    // Keeps the old password hash when no new password is supplied.
    [HttpPut("{nic}")]
    public async Task<IActionResult> Update(string nic, Prosumer updatedProsumer)
    {
        var existing = await _db.Prosumers.Find(p => p.Nic == nic).FirstOrDefaultAsync();
        if (existing == null) return NotFound();

        if (!string.IsNullOrEmpty(updatedProsumer.PasswordHash))
        {
            updatedProsumer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updatedProsumer.PasswordHash);
        }
        else
        {
            updatedProsumer.PasswordHash = existing.PasswordHash;
        }

        updatedProsumer.Id = existing.Id;
        updatedProsumer.Nic = nic;
        updatedProsumer.CreatedAt = existing.CreatedAt;

        await _db.Prosumers.ReplaceOneAsync(p => p.Nic == nic, updatedProsumer);
        return Ok(updatedProsumer);
    }

    // DELETE: api/prosumers/{nic} - permanently removes a prosumer.
    [HttpDelete("{nic}")]
    public async Task<IActionResult> Delete(string nic)
    {
        var result = await _db.Prosumers.DeleteOneAsync(p => p.Nic == nic);
        if (result.DeletedCount == 0) return NotFound();
        return Ok(new { message = "Prosumer deleted." });
    }

    // PATCH: api/prosumers/{nic}/deactivate - blocks the account from logging in.
    [HttpPatch("{nic}/deactivate")]
    public async Task<IActionResult> Deactivate(string nic)
    {
        var update = Builders<Prosumer>.Update.Set(p => p.Status, "deactivated");
        var result = await _db.Prosumers.UpdateOneAsync(p => p.Nic == nic, update);
        if (result.MatchedCount == 0) return NotFound();
        return Ok(new { message = "Prosumer deactivated." });
    }

    // PATCH: api/prosumers/{nic}/reactivate - approves a pending account or restores a deactivated one.
    [HttpPatch("{nic}/reactivate")]
    public async Task<IActionResult> Reactivate(string nic)
    {
        var update = Builders<Prosumer>.Update.Set(p => p.Status, "active");
        var result = await _db.Prosumers.UpdateOneAsync(p => p.Nic == nic, update);
        if (result.MatchedCount == 0) return NotFound();
        return Ok(new { message = "Prosumer reactivated." });
    }

    // Request body for the change-password endpoint.
    public class ChangePasswordRequest
    {
        public string NewPassword { get; set; } = "";
    }

    // PATCH: api/prosumers/{nic}/change-password - updates only the password hash.
    [HttpPatch("{nic}/change-password")]
    public async Task<IActionResult> ChangePassword(string nic, ChangePasswordRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest("New password is required.");

        var hashed = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        var update = Builders<Prosumer>.Update.Set(p => p.PasswordHash, hashed);
        var result = await _db.Prosumers.UpdateOneAsync(p => p.Nic == nic, update);
        if (result.MatchedCount == 0) return NotFound();
        return Ok(new { message = "Password changed." });
    }
}