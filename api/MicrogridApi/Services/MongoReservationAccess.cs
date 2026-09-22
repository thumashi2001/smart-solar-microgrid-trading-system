// =============================================================================
// File: MongoReservationAccess.cs
// Description: MongoDB adapter for energyReservation used by transfer/QR flows.
// Author: Suwani (Component 4)
// =============================================================================

using System.Security.Cryptography;
using MongoDB.Driver;
using MicrogridApi.Data;
using MicrogridApi.Models;

namespace MicrogridApi.Services;

public class MongoReservationAccess : IReservationAccess
{
    private readonly IMongoCollection<EnergyReservation> _reservations;

    // Binds to the shared energyReservation collection.
    public MongoReservationAccess(MongoDbContext db)
    {
        _reservations = db.EnergyReservations;
    }

    // Finds a reservation by opaque QR transaction reference.
    public async Task<EnergyReservation?> FindByTransactionReferenceAsync(
        string transactionReference,
        CancellationToken cancellationToken = default)
    {
        return await _reservations
            .Find(r => r.TransactionReference == transactionReference)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Finds a reservation by business ReservationId.
    public async Task<EnergyReservation?> FindByReservationIdAsync(
        string reservationId,
        CancellationToken cancellationToken = default)
    {
        return await _reservations
            .Find(r => r.ReservationId == reservationId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    // Atomically completes an approved reservation; losers of races get null.
    public async Task<EnergyReservation?> TryCompleteApprovedAsync(
        string reservationId,
        string operatorId,
        string operatorName,
        DateTime completedAtUtc,
        CancellationToken cancellationToken = default)
    {
        var filter = Builders<EnergyReservation>.Filter.And(
            Builders<EnergyReservation>.Filter.Eq(r => r.ReservationId, reservationId),
            Builders<EnergyReservation>.Filter.Eq(r => r.Status, ReservationStatuses.Approved));

        var update = Builders<EnergyReservation>.Update
            .Set(r => r.Status, ReservationStatuses.Completed)
            .Set(r => r.CompletedByOperatorId, operatorId)
            .Set(r => r.CompletedByOperatorName, operatorName)
            .Set(r => r.CompletedAt, completedAtUtc)
            .Set(r => r.UpdatedAt, completedAtUtc);

        return await _reservations.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<EnergyReservation>
            {
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken);
    }

    // Reuses an existing QR reference when present; otherwise issues a new opaque one.
    public async Task<EnergyReservation?> EnsureTransactionReferenceAsync(
        string reservationId,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindByReservationIdAsync(reservationId, cancellationToken);
        if (existing == null)
        {
            return null;
        }

        if (!string.IsNullOrWhiteSpace(existing.TransactionReference)
            && existing.Status == ReservationStatuses.Approved)
        {
            return existing;
        }

        if (existing.Status != ReservationStatuses.Approved)
        {
            return existing;
        }

        var reference = CreateOpaqueReference();
        var now = DateTime.UtcNow;

        var filter = Builders<EnergyReservation>.Filter.And(
            Builders<EnergyReservation>.Filter.Eq(r => r.ReservationId, reservationId),
            Builders<EnergyReservation>.Filter.Eq(r => r.Status, ReservationStatuses.Approved));

        var update = Builders<EnergyReservation>.Update
            .Set(r => r.TransactionReference, reference)
            .Set(r => r.TransactionReferenceIssuedAt, now)
            .Set(r => r.UpdatedAt, now);

        return await _reservations.FindOneAndUpdateAsync(
            filter,
            update,
            new FindOneAndUpdateOptions<EnergyReservation>
            {
                ReturnDocument = ReturnDocument.After
            },
            cancellationToken);
    }

    // Generates a high-entropy opaque QR credential (not a sequential id).
    private static string CreateOpaqueReference()
    {
        var bytes = RandomNumberGenerator.GetBytes(24);
        return "TRX-" + Convert.ToHexString(bytes);
    }
}

public static class ReservationStatuses
{
    public const string Pending = "pending";
    public const string Approved = "approved";
    public const string Cancelled = "cancelled";
    public const string Completed = "completed";
}
