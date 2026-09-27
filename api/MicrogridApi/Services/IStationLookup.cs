// =============================================================================
// File: IStationLookup.cs
// Description: Read-only station lookup used by transfer eligibility checks.
// Author: Suwani (Component 4)
// =============================================================================

using MicrogridApi.Models;

namespace MicrogridApi.Services;

public interface IStationLookup
{
    Task<MicrogridNode?> FindByNodeIdAsync(string nodeId, CancellationToken cancellationToken = default);
}
