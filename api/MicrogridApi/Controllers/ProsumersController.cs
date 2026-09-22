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

    public ProsumersController(MongoDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var prosumers = await _db.Prosumers.Find(_ => true).ToListAsync();
        return Ok(prosumers);
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register(Prosumer newProsumer)
    {
        var existing = await _db.Prosumers.Find(p => p.Nic == newProsumer.Nic).FirstOrDefaultAsync();
        if (existing != null) return BadRequest("A prosumer with this NIC already exists.");

        newProsumer.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newProsumer.PasswordHash);
        newProsumer.Status = "pendingActivation";
        await _db.Prosumers.InsertOneAsync(newProsumer);
        return Ok(newProsumer);
    }

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

    [HttpDelete("{nic}")]
    public async Task<IActionResult> Delete(string nic)
    {
        var result = await _db.Prosumers.DeleteOneAsync(p => p.Nic == nic);
        if (result.DeletedCount == 0) return NotFound();
        return Ok(new { message = "Prosumer deleted." });
    }

    [HttpPatch("{nic}/deactivate")]
    public async Task<IActionResult> Deactivate(string nic)
    {
        var update = Builders<Prosumer>.Update.Set(p => p.Status, "deactivated");
        var result = await _db.Prosumers.UpdateOneAsync(p => p.Nic == nic, update);
        if (result.MatchedCount == 0) return NotFound();
        return Ok(new { message = "Prosumer deactivated." });
    }

    [HttpPatch("{nic}/reactivate")]
    public async Task<IActionResult> Reactivate(string nic)
    {
        var update = Builders<Prosumer>.Update.Set(p => p.Status, "active");
        var result = await _db.Prosumers.UpdateOneAsync(p => p.Nic == nic, update);
        if (result.MatchedCount == 0) return NotFound();
        return Ok(new { message = "Prosumer reactivated." });
    }

    public class ChangePasswordRequest
    {
        public string NewPassword { get; set; } = "";
    }

    [HttpPatch("{nic}/change-password")]
    public async Task<IActionResult> ChangePassword(string nic, ChangePasswordRequest request)
    {
        var hashed = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        var update = Builders<Prosumer>.Update.Set(p => p.PasswordHash, hashed);
        var result = await _db.Prosumers.UpdateOneAsync(p => p.Nic == nic, update);
        if (result.MatchedCount == 0) return NotFound();
        return Ok(new { message = "Password changed." });
    }
}