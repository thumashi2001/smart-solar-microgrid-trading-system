// =============================================================================
// File: MongoSuwaniSeedStore.cs
// Description: Mongo upserts for Suwani seed keys only (no deletes).
// Author: Suwani (Component 4)
// =============================================================================

using MicrogridApi.Data;
using MicrogridApi.Models;
using MongoDB.Driver;

namespace MicrogridApi.Services;

public class MongoSuwaniSeedStore : ISuwaniSeedStore
{
    private readonly MongoDbContext _db;

    public MongoSuwaniSeedStore(MongoDbContext db)
    {
        _db = db;
    }

    public async Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _db.Users.Find(u => u.Email == email).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertUserByEmailAsync(User user, CancellationToken cancellationToken = default)
    {
        var existing = await FindUserByEmailAsync(user.Email, cancellationToken);
        if (existing != null)
        {
            user.Id = existing.Id;
            user.CreatedAt = existing.CreatedAt;
            await _db.Users.ReplaceOneAsync(
                u => u.Email == user.Email,
                user,
                cancellationToken: cancellationToken);
            return;
        }

        // Insert without a null _id so MongoDB can assign ObjectId.
        user.Id = null;
        await _db.Users.InsertOneAsync(user, cancellationToken: cancellationToken);
    }

    public async Task<Prosumer?> FindProsumerByNicAsync(string nic, CancellationToken cancellationToken = default)
    {
        return await _db.Prosumers.Find(p => p.Nic == nic).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertProsumerByNicAsync(Prosumer prosumer, CancellationToken cancellationToken = default)
    {
        var existing = await FindProsumerByNicAsync(prosumer.Nic, cancellationToken);
        if (existing != null)
        {
            prosumer.Id = existing.Id;
            prosumer.CreatedAt = existing.CreatedAt;
            await _db.Prosumers.ReplaceOneAsync(
                p => p.Nic == prosumer.Nic,
                prosumer,
                cancellationToken: cancellationToken);
            return;
        }

        prosumer.Id = null;
        await _db.Prosumers.InsertOneAsync(prosumer, cancellationToken: cancellationToken);
    }

    public async Task<MicrogridNode?> FindStationByNodeIdAsync(
        string nodeId,
        CancellationToken cancellationToken = default)
    {
        return await _db.MicrogridNodes.Find(n => n.NodeId == nodeId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertStationByNodeIdAsync(
        MicrogridNode node,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindStationByNodeIdAsync(node.NodeId, cancellationToken);
        if (existing != null)
        {
            node.Id = existing.Id;
            node.CreatedAt = existing.CreatedAt;
            await _db.MicrogridNodes.ReplaceOneAsync(
                n => n.NodeId == node.NodeId,
                node,
                cancellationToken: cancellationToken);
            return;
        }

        node.Id = null;
        await _db.MicrogridNodes.InsertOneAsync(node, cancellationToken: cancellationToken);
    }

    public async Task<EnergyBookingSlot?> FindSlotBySlotIdAsync(
        string slotId,
        CancellationToken cancellationToken = default)
    {
        return await _db.EnergyBookingSlots.Find(s => s.SlotId == slotId).FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertSlotBySlotIdAsync(
        EnergyBookingSlot slot,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindSlotBySlotIdAsync(slot.SlotId, cancellationToken);
        if (existing != null)
        {
            slot.Id = existing.Id;
            slot.CreatedAt = existing.CreatedAt;
            await _db.EnergyBookingSlots.ReplaceOneAsync(
                s => s.SlotId == slot.SlotId,
                slot,
                cancellationToken: cancellationToken);
            return;
        }

        slot.Id = null;
        await _db.EnergyBookingSlots.InsertOneAsync(slot, cancellationToken: cancellationToken);
    }

    public async Task<EnergyReservation?> FindReservationByReservationIdAsync(
        string reservationId,
        CancellationToken cancellationToken = default)
    {
        return await _db.EnergyReservations
            .Find(r => r.ReservationId == reservationId)
            .FirstOrDefaultAsync(cancellationToken);
    }

    public async Task UpsertReservationByReservationIdAsync(
        EnergyReservation reservation,
        CancellationToken cancellationToken = default)
    {
        var existing = await FindReservationByReservationIdAsync(reservation.ReservationId, cancellationToken);
        if (existing != null)
        {
            reservation.Id = existing.Id;
            reservation.CreatedAt = existing.CreatedAt;
            await _db.EnergyReservations.ReplaceOneAsync(
                r => r.ReservationId == reservation.ReservationId,
                reservation,
                cancellationToken: cancellationToken);
            return;
        }

        reservation.Id = null;
        await _db.EnergyReservations.InsertOneAsync(reservation, cancellationToken: cancellationToken);
    }

    public async Task<long> CountUsersByEmailAsync(string email, CancellationToken cancellationToken = default)
    {
        return await _db.Users.CountDocumentsAsync(u => u.Email == email, cancellationToken: cancellationToken);
    }

    public async Task<long> CountStationsByNodeIdAsync(string nodeId, CancellationToken cancellationToken = default)
    {
        return await _db.MicrogridNodes.CountDocumentsAsync(
            n => n.NodeId == nodeId,
            cancellationToken: cancellationToken);
    }

    public async Task<long> CountReservationsByReservationIdAsync(
        string reservationId,
        CancellationToken cancellationToken = default)
    {
        return await _db.EnergyReservations.CountDocumentsAsync(
            r => r.ReservationId == reservationId,
            cancellationToken: cancellationToken);
    }
}
