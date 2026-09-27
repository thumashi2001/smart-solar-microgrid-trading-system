// =============================================================================
// File: SuwaniDevFixturesController.cs
// Description: Development-only Suwani seed/fixture endpoints (not Production).
// Author: Suwani (Component 4)
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using MicrogridApi.Services;

namespace MicrogridApi.Controllers;

[ApiController]
[Route("api/dev/suwani-fixtures")]
public class SuwaniDevFixturesController : ControllerBase
{
    private readonly SuwaniDevSeedService _seedService;
    private readonly IHostEnvironment _env;

    // Creates the development fixture controller.
    public SuwaniDevFixturesController(SuwaniDevSeedService seedService, IHostEnvironment env)
    {
        _seedService = seedService;
        _env = env;
    }

    // POST: api/dev/suwani-fixtures/seed
    // Idempotent Suwani development seed (users, stations, slot, approved reservation).
    [HttpPost("seed")]
    public Task<IActionResult> Seed(CancellationToken cancellationToken) =>
        RunSeedAsync(cancellationToken);

    // POST: api/dev/suwani-fixtures/approved-reservation
    // Compatibility alias — runs the same idempotent seed as /seed.
    [HttpPost("approved-reservation")]
    public Task<IActionResult> CreateApprovedReservationFixture(
        CancellationToken cancellationToken) =>
        RunSeedAsync(cancellationToken);

    // Executes seed only when ASPNETCORE_ENVIRONMENT is Development.
    private async Task<IActionResult> RunSeedAsync(CancellationToken cancellationToken)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        var result = await _seedService.SeedAsync(cancellationToken);

        return Ok(new
        {
            warning = result.Warning,
            firstRun = result.FirstRun,
            created = result.Created,
            updated = result.Updated,
            backofficeEmail = result.BackofficeEmail,
            backofficeUserId = result.BackofficeUserId,
            operatorEmail = result.OperatorEmail,
            operatorUserId = result.OperatorUserId,
            prosumerNic = result.ProsumerNic,
            stationNodeIds = result.StationNodeIds,
            slotId = result.SlotId,
            reservationId = result.ReservationId,
            transactionReference = result.TransactionReference,
            reservationStatus = result.ReservationStatus
        });
    }
}
