// =============================================================================
// File: SuwaniQaV1SeedResult.cs
// Description: Result payload for SUWANI-QA-V1 additive seed.
// Author: Suwani (Component 4)
// =============================================================================

namespace MicrogridApi.Services;

public sealed class SuwaniQaV1SeedResult
{
    public string Marker { get; init; } = SuwaniQaV1Constants.Marker;
    public string Warning { get; init; } =
        "Development-only SUWANI-QA-V1 fixtures. Create-if-missing; never resets Completed/Cancelled or passwords.";

    public List<string> Created { get; init; } = new();
    public List<string> Skipped { get; init; } = new();
    public List<string> Notes { get; init; } = new();

    public Dictionary<string, string> Accounts { get; init; } = new();
    public Dictionary<string, string> Reservations { get; init; } = new();
}
