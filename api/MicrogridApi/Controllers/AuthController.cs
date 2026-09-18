using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MicrogridApi.Data;
using MicrogridApi.Dtos;

namespace MicrogridApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly MongoDbContext _db;

    public AuthController(MongoDbContext db)
    {
        _db = db;
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login(LoginRequest request)
    {
        // Try matching a Backoffice/GridOperator user by email
        var user = await _db.Users.Find(u => u.Email == request.Identifier).FirstOrDefaultAsync();
        if (user != null)
        {
            if (user.Status == "deactivated")
                return Unauthorized("This account has been deactivated.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
                return Unauthorized("Invalid credentials.");

            var token = Guid.NewGuid().ToString(); // simple placeholder token for now

            return Ok(new LoginResponse
            {
                Token = token,
                Role = user.Role,
                FullName = user.FullName
            });
        }

        // Otherwise, try matching a Prosumer by NIC
        var prosumer = await _db.Prosumers.Find(p => p.Nic == request.Identifier).FirstOrDefaultAsync();
        if (prosumer != null)
        {
            if (prosumer.Status != "active")
                return Unauthorized("This account is not active.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, prosumer.PasswordHash))
                return Unauthorized("Invalid credentials.");

            var token = Guid.NewGuid().ToString();

            return Ok(new LoginResponse
            {
                Token = token,
                Role = "Prosumer",
                FullName = prosumer.FullName
            });
        }

        return Unauthorized("Invalid credentials.");
    }
}