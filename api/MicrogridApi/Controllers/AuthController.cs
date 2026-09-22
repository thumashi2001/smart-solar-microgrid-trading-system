// =============================================================================
// File: AuthController.cs
// Description: Login endpoint; now issues validated JWTs (compat LoginRequest/Response).
// Author: Thumashi (Component 1) — JWT issuance added by Suwani for protected transfers
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using MicrogridApi.Auth;
using MicrogridApi.Data;
using MicrogridApi.Dtos;

namespace MicrogridApi.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController : ControllerBase
{
    private readonly MongoDbContext _db;
    private readonly TokenService _tokens;

    // Creates the auth controller with MongoDB and JWT token issuance.
    public AuthController(MongoDbContext db, TokenService tokens)
    {
        _db = db;
        _tokens = tokens;
    }

    // POST: api/auth/login
    // Authenticates Backoffice/GridOperator by email or Prosumer by NIC.
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

            var token = _tokens.CreateToken(
                subjectId: user.UserId,
                role: user.Role,
                fullName: user.FullName,
                emailOrNic: user.Email);

            return Ok(new LoginResponse
            {
                Token = token,
                Role = user.Role,
                FullName = user.FullName
            });
        }

        // Otherwise, try matching a Prosumer by NIC & Email
        var prosumer = await _db.Prosumers.Find(p => p.Nic == request.Identifier || p.Email == request.Identifier).FirstOrDefaultAsync();
        if (prosumer != null)
        {
            if (prosumer.Status != "active")
                return Unauthorized("This account is not active.");

            if (!BCrypt.Net.BCrypt.Verify(request.Password, prosumer.PasswordHash))
                return Unauthorized("Invalid credentials.");

            var token = _tokens.CreateToken(
                subjectId: prosumer.Nic,
                role: "Prosumer",
                fullName: prosumer.FullName,
                emailOrNic: prosumer.Nic);

            return Ok(new LoginResponse
            {
                Token = token,
                Role = "Prosumer",
                FullName = prosumer.FullName,
                Nic = prosumer.Nic
            });
        }

        return Unauthorized("Invalid credentials.");
    }
}
