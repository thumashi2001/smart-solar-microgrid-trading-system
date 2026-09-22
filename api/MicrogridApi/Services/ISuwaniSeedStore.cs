// =============================================================================
// File: ISuwaniSeedStore.cs
// Description: Persistence boundary for Suwani development seeding (testable).
// Author: Suwani (Component 4)
// =============================================================================

using MicrogridApi.Models;

namespace MicrogridApi.Services;

public interface ISuwaniSeedStore
{
    Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task UpsertUserByEmailAsync(User user, CancellationToken cancellationToken = default);

    Task<Prosumer?> FindProsumerByNicAsync(string nic, CancellationToken cancellationToken = default);
    Task UpsertProsumerByNicAsync(Prosumer prosumer, CancellationToken cancellationToken = default);

    Task<MicrogridNode?> FindStationByNodeIdAsync(string nodeId, CancellationToken cancellationToken = default);
    Task UpsertStationByNodeIdAsync(MicrogridNode node, CancellationToken cancellationToken = default);

    Task<EnergyBookingSlot?> FindSlotBySlotIdAsync(string slotId, CancellationToken cancellationToken = default);
    Task UpsertSlotBySlotIdAsync(EnergyBookingSlot slot, CancellationToken cancellationToken = default);

    Task<EnergyReservation?> FindReservationByReservationIdAsync(
        string reservationId,
        CancellationToken cancellationToken = default);
    Task UpsertReservationByReservationIdAsync(
        EnergyReservation reservation,
        CancellationToken cancellationToken = default);

    Task<long> CountUsersByEmailAsync(string email, CancellationToken cancellationToken = default);
    Task<long> CountStationsByNodeIdAsync(string nodeId, CancellationToken cancellationToken = default);
    Task<long> CountReservationsByReservationIdAsync(
        string reservationId,
        CancellationToken cancellationToken = default);
}
