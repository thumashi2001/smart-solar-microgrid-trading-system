// =============================================================================
// File: SuwaniDevSeedService.cs
// Description: Idempotent Development seed for Suwani QR/maps/transfer demos.
// Author: Suwani (Component 4)
// =============================================================================

using MicrogridApi.Models;

namespace MicrogridApi.Services;

public class SuwaniDevSeedService
{
    private readonly ISuwaniSeedStore _store;

    public SuwaniDevSeedService(ISuwaniSeedStore store)
    {
        _store = store;
    }

    // Upserts Suwani seed records by stable keys; never deletes unrelated data.
    public async Task<SuwaniSeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        var created = new List<string>();
        var updated = new List<string>();
        var now = DateTime.UtcNow;

        await UpsertUserAsync(
            email: SuwaniDevSeedConstants.BackofficeEmail,
            userId: SuwaniDevSeedConstants.BackofficeUserId,
            fullName: SuwaniDevSeedConstants.BackofficeFullName,
            role: "Backoffice",
            password: SuwaniDevSeedConstants.BackofficePassword,
            now,
            created,
            updated,
            cancellationToken);

        await UpsertUserAsync(
            email: SuwaniDevSeedConstants.OperatorEmail,
            userId: SuwaniDevSeedConstants.OperatorUserId,
            fullName: SuwaniDevSeedConstants.OperatorFullName,
            role: "GridOperator",
            password: SuwaniDevSeedConstants.OperatorPassword,
            now,
            created,
            updated,
            cancellationToken);

        await UpsertProsumerAsync(now, created, updated, cancellationToken);

        await UpsertStationAsync(
            SuwaniDevSeedConstants.ColomboNodeId,
            "Colombo Solar Hub",
            "Colombo",
            6.9271,
            79.8612,
            now,
            created,
            updated,
            cancellationToken);

        await UpsertStationAsync(
            SuwaniDevSeedConstants.KandyNodeId,
            "Kandy Solar Hub",
            "Kandy",
            7.2906,
            80.6337,
            now,
            created,
            updated,
            cancellationToken);

        await UpsertStationAsync(
            SuwaniDevSeedConstants.GalleNodeId,
            "Galle Solar Hub",
            "Galle",
            6.0329,
            80.2168,
            now,
            created,
            updated,
            cancellationToken);

        await UpsertSlotAsync(now, created, updated, cancellationToken);
        await UpsertReservationAsync(now, created, updated, cancellationToken);

        return new SuwaniSeedResult
        {
            FirstRun = created.Count > 0 && updated.Count == 0,
            Created = created,
            Updated = updated,
            BackofficeEmail = SuwaniDevSeedConstants.BackofficeEmail,
            BackofficeUserId = SuwaniDevSeedConstants.BackofficeUserId,
            OperatorEmail = SuwaniDevSeedConstants.OperatorEmail,
            OperatorUserId = SuwaniDevSeedConstants.OperatorUserId,
            ProsumerNic = SuwaniDevSeedConstants.ProsumerNic,
            StationNodeIds = new[]
            {
                SuwaniDevSeedConstants.ColomboNodeId,
                SuwaniDevSeedConstants.KandyNodeId,
                SuwaniDevSeedConstants.GalleNodeId
            },
            SlotId = SuwaniDevSeedConstants.ColomboSlotId,
            ReservationId = SuwaniDevSeedConstants.ApprovedReservationId,
            TransactionReference = SuwaniDevSeedConstants.TransactionReference,
            ReservationStatus = ReservationStatuses.Approved
        };
    }

    private async Task UpsertUserAsync(
        string email,
        string userId,
        string fullName,
        string role,
        string password,
        DateTime now,
        List<string> created,
        List<string> updated,
        CancellationToken cancellationToken)
    {
        var existing = await _store.FindUserByEmailAsync(email, cancellationToken);
        var key = $"user:{email}";
        if (existing == null) created.Add(key); else updated.Add(key);

        await _store.UpsertUserByEmailAsync(new User
        {
            UserId = userId,
            Email = email,
            FullName = fullName,
            Role = role,
            Status = "active",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAt = existing?.CreatedAt ?? now
        }, cancellationToken);
    }

    private async Task UpsertProsumerAsync(
        DateTime now,
        List<string> created,
        List<string> updated,
        CancellationToken cancellationToken)
    {
        var nic = SuwaniDevSeedConstants.ProsumerNic;
        var existing = await _store.FindProsumerByNicAsync(nic, cancellationToken);
        var key = $"prosumer:{nic}";
        if (existing == null) created.Add(key); else updated.Add(key);

        await _store.UpsertProsumerByNicAsync(new Prosumer
        {
            Nic = nic,
            FullName = SuwaniDevSeedConstants.ProsumerFullName,
            Email = SuwaniDevSeedConstants.ProsumerEmail,
            Phone = SuwaniDevSeedConstants.ProsumerPhone,
            Status = "active",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(SuwaniDevSeedConstants.ProsumerPassword),
            CreatedAt = existing?.CreatedAt ?? now
        }, cancellationToken);
    }

    private async Task UpsertStationAsync(
        string nodeId,
        string name,
        string location,
        double lat,
        double lng,
        DateTime now,
        List<string> created,
        List<string> updated,
        CancellationToken cancellationToken)
    {
        var existing = await _store.FindStationByNodeIdAsync(nodeId, cancellationToken);
        var key = $"station:{nodeId}";
        if (existing == null) created.Add(key); else updated.Add(key);

        await _store.UpsertStationByNodeIdAsync(new MicrogridNode
        {
            NodeId = nodeId,
            NodeName = name,
            Description = $"Suwani development seed station ({SuwaniDevSeedConstants.SeedMarker}).",
            Location = location,
            Latitude = lat,
            Longitude = lng,
            CapacityKWh = 100,
            BatterySlots = 8,
            Schedule = "08:00-18:00",
            Status = "active",
            CreatedAt = existing?.CreatedAt ?? now,
            UpdatedAt = now
        }, cancellationToken);
    }

    private async Task UpsertSlotAsync(
        DateTime now,
        List<string> created,
        List<string> updated,
        CancellationToken cancellationToken)
    {
        var slotId = SuwaniDevSeedConstants.ColomboSlotId;
        var existing = await _store.FindSlotBySlotIdAsync(slotId, cancellationToken);
        var key = $"slot:{slotId}";
        if (existing == null) created.Add(key); else updated.Add(key);

        var slotDate = DateTime.UtcNow.Date.AddDays(1);

        await _store.UpsertSlotBySlotIdAsync(new EnergyBookingSlot
        {
            SlotId = slotId,
            StationId = SuwaniDevSeedConstants.ColomboNodeId,
            Date = slotDate,
            StartTime = "10:00",
            EndTime = "12:00",
            Capacity = 4,
            Availability = 3,
            Status = "active",
            CreatedAt = existing?.CreatedAt ?? now,
            UpdatedAt = now
        }, cancellationToken);
    }

    private async Task UpsertReservationAsync(
        DateTime now,
        List<string> created,
        List<string> updated,
        CancellationToken cancellationToken)
    {
        var reservationId = SuwaniDevSeedConstants.ApprovedReservationId;
        var existing = await _store.FindReservationByReservationIdAsync(reservationId, cancellationToken);
        var key = $"reservation:{reservationId}";
        if (existing == null) created.Add(key); else updated.Add(key);

        await _store.UpsertReservationByReservationIdAsync(new EnergyReservation
        {
            ReservationId = reservationId,
            ProsumerNic = SuwaniDevSeedConstants.ProsumerNic,
            StationId = SuwaniDevSeedConstants.ColomboNodeId,
            SlotId = SuwaniDevSeedConstants.ColomboSlotId,
            Status = ReservationStatuses.Approved,
            TransactionReference = SuwaniDevSeedConstants.TransactionReference,
            TransactionReferenceIssuedAt = existing?.TransactionReferenceIssuedAt ?? now,
            CompletedAt = null,
            CompletedByOperatorId = null,
            CompletedByOperatorName = null,
            CreatedAt = existing?.CreatedAt ?? now,
            UpdatedAt = now
        }, cancellationToken);
    }
}
