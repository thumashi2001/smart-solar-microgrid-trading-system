using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MicrogridApi.Data;
using MicrogridApi.Models;

namespace MicrogridApi.Controllers;

[ApiController]
[Route("api/users")]
public class UsersController : ControllerBase
{
    private readonly MongoDbContext _db;

    public UsersController(MongoDbContext db)
    {
        _db = db;
    }

    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.Users.Find(_ => true).ToListAsync();
        return Ok(users);
    }

    [HttpPost]
    public async Task<IActionResult> Create(User newUser)
    {
        newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newUser.PasswordHash);
        await _db.Users.InsertOneAsync(newUser);
        return Ok(newUser);
    }

    [HttpPut("{id}")]
    public async Task<IActionResult> Update(string id, User updatedUser)
    {
        var existing = await _db.Users.Find(u => u.Id == id).FirstOrDefaultAsync();
        if (existing == null) return NotFound();

        if (!string.IsNullOrEmpty(updatedUser.PasswordHash))
        {
            updatedUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(updatedUser.PasswordHash);
        }
        else
        {
            updatedUser.PasswordHash = existing.PasswordHash;
        }

        updatedUser.Id = id;
        updatedUser.CreatedAt = existing.CreatedAt;

        await _db.Users.ReplaceOneAsync(u => u.Id == id, updatedUser);
        return Ok(updatedUser);
    }

    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _db.Users.DeleteOneAsync(u => u.Id == id);
        if (result.DeletedCount == 0) return NotFound();
        return Ok(new { message = "User deleted." });
    }

    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(string id)
    {
        var update = Builders<User>.Update.Set(u => u.Status, "deactivated");
        var result = await _db.Users.UpdateOneAsync(u => u.Id == id, update);
        if (result.MatchedCount == 0) return NotFound();
        return Ok(new { message = "User deactivated." });
    }

    [HttpPatch("{id}/reactivate")]
    public async Task<IActionResult> Reactivate(string id)
    {
        var update = Builders<User>.Update.Set(u => u.Status, "active");
        var result = await _db.Users.UpdateOneAsync(u => u.Id == id, update);
        if (result.MatchedCount == 0) return NotFound();
        return Ok(new { message = "User reactivated." });
    }
}