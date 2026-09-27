// =============================================================================
// File: SuwaniQaV1FixturesController.cs
// Description: Development-only SUWANI-QA-V1 additive seed endpoint (not Production).
// Author: Suwani (Component 4)
// =============================================================================

using Microsoft.AspNetCore.Mvc;
using MicrogridApi.Services;

namespace MicrogridApi.Controllers;

[ApiController]
[Route("api/dev/suwani-qa-v1")]
public class SuwaniQaV1FixturesController : ControllerBase
{
    private readonly SuwaniQaV1SeedService _seedService;
    private readonly IHostEnvironment _env;

    public SuwaniQaV1FixturesController(SuwaniQaV1SeedService seedService, IHostEnvironment env)
    {
        _seedService = seedService;
        _env = env;
    }

    // POST: api/dev/suwani-qa-v1/seed
    // Create-if-missing QA fixtures. Does not call the broad SuwaniDevSeedService.
    [HttpPost("seed")]
    public async Task<IActionResult> Seed(CancellationToken cancellationToken)
    {
        if (!_env.IsDevelopment())
        {
            return NotFound();
        }

        var result = await _seedService.SeedAsync(cancellationToken);

        return Ok(new
        {
            marker = result.Marker,
            warning = result.Warning,
            createdCount = result.Created.Count,
            skippedCount = result.Skipped.Count,
            created = result.Created,
            skipped = result.Skipped,
            notes = result.Notes,
            accounts = result.Accounts,
            reservations = result.Reservations
        });
    }
}
