// =============================================================================
// File: UsersController.cs
// Description: Backoffice and Grid Operator user management API (create, list,
//              update, delete, activate/deactivate). Used by the web admin app.
// Author: Thumashi (Component 1)
// =============================================================================

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

    // Creates the controller with the MongoDB context.
    public UsersController(MongoDbContext db)
    {
        _db = db;
    }

    // GET: api/users - returns every Backoffice and Grid Operator user.
    [HttpGet]
    public async Task<IActionResult> GetAll()
    {
        var users = await _db.Users.Find(_ => true).ToListAsync();
        return Ok(users);
    }

    // POST: api/users - creates a new Backoffice or Grid Operator account.
    [HttpPost]
    public async Task<IActionResult> Create(User newUser)
    {
        newUser.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newUser.PasswordHash);
        await _db.Users.InsertOneAsync(newUser);
        return Ok(newUser);
    }

    // PUT: api/users/{id} - updates a user's details.
    // Keeps the old password hash when no new password is supplied.
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

    // DELETE: api/users/{id} - permanently removes a user.
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(string id)
    {
        var result = await _db.Users.DeleteOneAsync(u => u.Id == id);
        if (result.DeletedCount == 0) return NotFound();
        return Ok(new { message = "User deleted." });
    }

    // PATCH: api/users/{id}/deactivate - blocks the account from logging in.
    [HttpPatch("{id}/deactivate")]
    public async Task<IActionResult> Deactivate(string id)
    {
        var update = Builders<User>.Update.Set(u => u.Status, "deactivated");
        var result = await _db.Users.UpdateOneAsync(u => u.Id == id, update);
        if (result.MatchedCount == 0) return NotFound();
        return Ok(new { message = "User deactivated." });
    }

    // PATCH: api/users/{id}/reactivate - restores a deactivated account.
    [HttpPatch("{id}/reactivate")]
    public async Task<IActionResult> Reactivate(string id)
    {
        var update = Builders<User>.Update.Set(u => u.Status, "active");
        var result = await _db.Users.UpdateOneAsync(u => u.Id == id, update);
        if (result.MatchedCount == 0) return NotFound();
        return Ok(new { message = "User reactivated." });
    }
}