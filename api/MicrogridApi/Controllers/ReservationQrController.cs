// =============================================================================
// File: ReservationQrController.cs
// Description: Minimal QR issuance/read for approved reservations (Suwani ownership).
// Author: Suwani (Component 4)
// =============================================================================

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MicrogridApi.Services;

namespace MicrogridApi.Controllers;

[ApiController]
[Route("api/reservations")]
[Authorize]
public class ReservationQrController : ControllerBase
{
    private readonly TransferService _transfers;

    // Creates the QR controller using shared transfer/QR service logic.
    public ReservationQrController(TransferService transfers)
    {
        _transfers = transfers;
    }

    // GET: api/reservations/{reservationId}/qr
    // Returns opaque QR payload for an approved reservation. Reuses existing reference.
    [HttpGet("{reservationId}/qr")]
    public async Task<IActionResult> GetQr(string reservationId, CancellationToken cancellationToken)
    {
        var role = User.FindFirstValue(ClaimTypes.Role) ?? string.Empty;
        var subject = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub")
            ?? string.Empty;

        // Prosumers may be identified by NIC stored in the "identifier" claim.
        var identifier = User.FindFirstValue("identifier") ?? subject;

        var callerKey = role == "Prosumer" ? identifier : subject;

        var result = await _transfers.IssueQrAsync(
            reservationId,
            callerKey,
            role,
            cancellationToken);

        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });
        }

        return Ok(result.Response);
    }
}
