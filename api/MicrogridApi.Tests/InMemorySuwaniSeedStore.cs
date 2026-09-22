// =============================================================================
// File: InMemorySuwaniSeedStore.cs
// Description: In-memory seed store for idempotency unit tests (no Mongo).
// Author: Suwani (Component 4)
// =============================================================================

using MicrogridApi.Models;
using MicrogridApi.Services;

namespace MicrogridApi.Tests;

public class InMemorySuwaniSeedStore : ISuwaniSeedStore
{
    public List<User> Users { get; } = new();
    public List<Prosumer> Prosumers { get; } = new();
    public List<MicrogridNode> Stations { get; } = new();
    public List<EnergyBookingSlot> Slots { get; } = new();
    public List<EnergyReservation> Reservations { get; } = new();

    public Task<User?> FindUserByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult(Users.FirstOrDefault(u => u.Email == email));

    public Task UpsertUserByEmailAsync(User user, CancellationToken cancellationToken = default)
    {
        Users.RemoveAll(u => u.Email == user.Email);
        Users.Add(CloneUser(user));
        return Task.CompletedTask;
    }

    public Task<Prosumer?> FindProsumerByNicAsync(string nic, CancellationToken cancellationToken = default) =>
        Task.FromResult(Prosumers.FirstOrDefault(p => p.Nic == nic));

    public Task UpsertProsumerByNicAsync(Prosumer prosumer, CancellationToken cancellationToken = default)
    {
        Prosumers.RemoveAll(p => p.Nic == prosumer.Nic);
        Prosumers.Add(CloneProsumer(prosumer));
        return Task.CompletedTask;
    }

    public Task<MicrogridNode?> FindStationByNodeIdAsync(
        string nodeId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Stations.FirstOrDefault(n => n.NodeId == nodeId));

    public Task UpsertStationByNodeIdAsync(MicrogridNode node, CancellationToken cancellationToken = default)
    {
        Stations.RemoveAll(n => n.NodeId == node.NodeId);
        Stations.Add(CloneStation(node));
        return Task.CompletedTask;
    }

    public Task<EnergyBookingSlot?> FindSlotBySlotIdAsync(
        string slotId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Slots.FirstOrDefault(s => s.SlotId == slotId));

    public Task UpsertSlotBySlotIdAsync(EnergyBookingSlot slot, CancellationToken cancellationToken = default)
    {
        Slots.RemoveAll(s => s.SlotId == slot.SlotId);
        Slots.Add(CloneSlot(slot));
        return Task.CompletedTask;
    }

    public Task<EnergyReservation?> FindReservationByReservationIdAsync(
        string reservationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult(Reservations.FirstOrDefault(r => r.ReservationId == reservationId));

    public Task UpsertReservationByReservationIdAsync(
        EnergyReservation reservation,
        CancellationToken cancellationToken = default)
    {
        Reservations.RemoveAll(r => r.ReservationId == reservation.ReservationId);
        Reservations.Add(CloneReservation(reservation));
        return Task.CompletedTask;
    }

    public Task<long> CountUsersByEmailAsync(string email, CancellationToken cancellationToken = default) =>
        Task.FromResult((long)Users.Count(u => u.Email == email));

    public Task<long> CountStationsByNodeIdAsync(string nodeId, CancellationToken cancellationToken = default) =>
        Task.FromResult((long)Stations.Count(n => n.NodeId == nodeId));

    public Task<long> CountReservationsByReservationIdAsync(
        string reservationId,
        CancellationToken cancellationToken = default) =>
        Task.FromResult((long)Reservations.Count(r => r.ReservationId == reservationId));

    private static User CloneUser(User u) => new()
    {
        Id = u.Id,
        UserId = u.UserId,
        Email = u.Email,
        PasswordHash = u.PasswordHash,
        FullName = u.FullName,
        Role = u.Role,
        Status = u.Status,
        CreatedAt = u.CreatedAt
    };

    private static Prosumer CloneProsumer(Prosumer p) => new()
    {
        Id = p.Id,
        Nic = p.Nic,
        FullName = p.FullName,
        Email = p.Email,
        Phone = p.Phone,
        PasswordHash = p.PasswordHash,
        Status = p.Status,
        CreatedAt = p.CreatedAt
    };

    private static MicrogridNode CloneStation(MicrogridNode n) => new()
    {
        Id = n.Id,
        NodeId = n.NodeId,
        NodeName = n.NodeName,
        Description = n.Description,
        Location = n.Location,
        Latitude = n.Latitude,
        Longitude = n.Longitude,
        CapacityKWh = n.CapacityKWh,
        BatterySlots = n.BatterySlots,
        Schedule = n.Schedule,
        Status = n.Status,
        CreatedAt = n.CreatedAt,
        UpdatedAt = n.UpdatedAt
    };

    private static EnergyBookingSlot CloneSlot(EnergyBookingSlot s) => new()
    {
        Id = s.Id,
        SlotId = s.SlotId,
        StationId = s.StationId,
        Date = s.Date,
        StartTime = s.StartTime,
        EndTime = s.EndTime,
        Capacity = s.Capacity,
        Availability = s.Availability,
        Status = s.Status,
        CreatedAt = s.CreatedAt,
        UpdatedAt = s.UpdatedAt
    };

    private static EnergyReservation CloneReservation(EnergyReservation r) => new()
    {
        Id = r.Id,
        ReservationId = r.ReservationId,
        ProsumerNic = r.ProsumerNic,
        StationId = r.StationId,
        SlotId = r.SlotId,
        Status = r.Status,
        TransactionReference = r.TransactionReference,
        TransactionReferenceIssuedAt = r.TransactionReferenceIssuedAt,
        CompletedAt = r.CompletedAt,
        CompletedByOperatorId = r.CompletedByOperatorId,
        CompletedByOperatorName = r.CompletedByOperatorName,
        CreatedAt = r.CreatedAt,
        UpdatedAt = r.UpdatedAt
    };
}
