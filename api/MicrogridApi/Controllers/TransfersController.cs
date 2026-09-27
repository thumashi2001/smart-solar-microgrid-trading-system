// =============================================================================
// File: TransfersController.cs
// Description: Grid Operator verify and complete endpoints for energy transfers.
// Author: Suwani (Component 4)
// =============================================================================

using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MicrogridApi.Dtos.Transfers;
using MicrogridApi.Services;

namespace MicrogridApi.Controllers;

[ApiController]
[Route("api/transfers")]
[Authorize(Roles = "GridOperator")]
public class TransfersController : ControllerBase
{
    private readonly TransferService _transfers;

    // Creates the transfers controller with the transfer domain service.
    public TransfersController(TransferService transfers)
    {
        _transfers = transfers;
    }

    // POST: api/transfers/verify
    // Verifies a scanned QR transaction reference without completing the transfer.
    [HttpPost("verify")]
    public async Task<IActionResult> Verify(
        [FromBody] VerifyTransferRequest request,
        CancellationToken cancellationToken)
    {
        var result = await _transfers.VerifyAsync(request.TransactionReference, cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new { message = result.ErrorMessage });
        }

        return Ok(result.Response);
    }

    // PATCH: api/transfers/{id}/complete
    // {id} is the business ReservationId. Completes atomically from approved -> completed.
    [HttpPatch("{id}/complete")]
    public async Task<IActionResult> Complete(string id, CancellationToken cancellationToken)
    {
        var operatorId = User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? User.FindFirstValue("sub");
        var operatorName = User.FindFirstValue(ClaimTypes.Name) ?? "GridOperator";

        if (string.IsNullOrWhiteSpace(operatorId))
        {
            return Unauthorized(new { message = "Authenticated operator identity is missing." });
        }

        // Identity is derived from the JWT principal only — never from the request body.
        var result = await _transfers.CompleteAsync(id, operatorId, operatorName, cancellationToken);
        if (!result.Success)
        {
            return StatusCode(result.StatusCode, new
            {
                message = result.ErrorMessage,
                status = result.CurrentStatus
            });
        }

        return Ok(result.Response);
    }
}
