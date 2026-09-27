// =============================================================================
// File: IReservationAccess.cs
// Description: Boundary isolating Suwani transfer/QR logic from Viman booking CRUD.
// Author: Suwani (Component 4)
// =============================================================================

using MicrogridApi.Models;

namespace MicrogridApi.Services;

public interface IReservationAccess
{
    // Loads a reservation by opaque QR transaction reference.
    Task<EnergyReservation?> FindByTransactionReferenceAsync(
        string transactionReference,
        CancellationToken cancellationToken = default);

    // Loads a reservation by business ReservationId.
    Task<EnergyReservation?> FindByReservationIdAsync(
        string reservationId,
        CancellationToken cancellationToken = default);

    // Atomically transitions approved -> completed. Returns null if filter did not match.
    Task<EnergyReservation?> TryCompleteApprovedAsync(
        string reservationId,
        string operatorId,
        string operatorName,
        DateTime completedAtUtc,
        CancellationToken cancellationToken = default);

    // Issues or reuses an opaque transaction reference for an approved reservation.
    Task<EnergyReservation?> EnsureTransactionReferenceAsync(
        string reservationId,
        CancellationToken cancellationToken = default);
}
