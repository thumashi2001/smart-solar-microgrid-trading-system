// =============================================================================
// File: SuwaniSeedResult.cs
// Description: Safe seed response payload (no passwords, hashes, or secrets).
// Author: Suwani (Component 4)
// =============================================================================

namespace MicrogridApi.Services;

public class SuwaniSeedResult
{
    public string Warning { get; init; } =
        "Development seed only. Credentials are for local testing — never use in production.";

    public bool FirstRun { get; init; }

    public IReadOnlyList<string> Created { get; init; } = Array.Empty<string>();
    public IReadOnlyList<string> Updated { get; init; } = Array.Empty<string>();

    public string BackofficeEmail { get; init; } = string.Empty;
    public string BackofficeUserId { get; init; } = string.Empty;

    public string OperatorEmail { get; init; } = string.Empty;
    public string OperatorUserId { get; init; } = string.Empty;

    public string ProsumerNic { get; init; } = string.Empty;

    public IReadOnlyList<string> StationNodeIds { get; init; } = Array.Empty<string>();

    public string SlotId { get; init; } = string.Empty;
    public string ReservationId { get; init; } = string.Empty;
    public string TransactionReference { get; init; } = string.Empty;
    public string ReservationStatus { get; init; } = string.Empty;
}
