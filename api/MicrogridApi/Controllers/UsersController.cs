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