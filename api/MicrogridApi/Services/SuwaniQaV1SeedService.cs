// =============================================================================
// File: SuwaniQaV1SeedService.cs
// Description: Additive create-if-missing SUWANI-QA-V1 fixtures for Component 4
//              manual tests. Never resets Completed/Cancelled or passwords.
// Author: Suwani (Component 4)
// =============================================================================

using MicrogridApi.Models;

namespace MicrogridApi.Services;

public sealed class SuwaniQaV1SeedService
{
    private readonly ISuwaniSeedStore _store;

    public SuwaniQaV1SeedService(ISuwaniSeedStore store)
    {
        _store = store;
    }

    public async Task<SuwaniQaV1SeedResult> SeedAsync(CancellationToken cancellationToken = default)
    {
        var created = new List<string>();
        var skipped = new List<string>();
        var notes = new List<string>();
        var accounts = new Dictionary<string, string>();
        var reservations = new Dictionary<string, string>();
        var now = DateTime.UtcNow;

        // Slot date far enough ahead for cancel 12h rule and current QR eligibility.
        var slotDate = DateTime.UtcNow.Date.AddDays(3);

        await EnsureStationsExistAsync(now, created, skipped, notes, cancellationToken);

        var ownerNic = await EnsureProsumerAsync(
            preferredNic: SuwaniQaV1Constants.OwnerNic,
            email: SuwaniQaV1Constants.OwnerEmail,
            fullName: SuwaniQaV1Constants.OwnerFullName,
            phone: SuwaniQaV1Constants.OwnerPhone,
            password: SuwaniQaV1Constants.OwnerPassword,
            label: "Prosumer A (owner)",
            now,
            created,
            skipped,
            notes,
            accounts,
            cancellationToken);

        var otherNic = await EnsureProsumerAsync(
            preferredNic: SuwaniQaV1Constants.OtherNic,
            email: SuwaniQaV1Constants.OtherEmail,
            fullName: SuwaniQaV1Constants.OtherFullName,
            phone: SuwaniQaV1Constants.OtherPhone,
            password: SuwaniQaV1Constants.OtherPassword,
            label: "Prosumer B (other)",
            now,
            created,
            skipped,
            notes,
            accounts,
            cancellationToken);

        await EnsureOperatorAsync(now, created, skipped, notes, accounts, cancellationToken);

        await EnsureSlotAsync(
            SuwaniQaV1Constants.SlotOwnership,
            SuwaniQaV1Constants.StationColombo,
            slotDate,
            "09:00",
            "10:00",
            now,
            created,
            skipped,
            cancellationToken);

        await EnsureSlotAsync(
            SuwaniQaV1Constants.SlotPending,
            SuwaniQaV1Constants.StationColombo,
            slotDate,
            "10:00",
            "11:00",
            now,
            created,
            skipped,
            cancellationToken);

        await EnsureSlotAsync(
            SuwaniQaV1Constants.SlotCancelled,
            SuwaniQaV1Constants.StationColombo,
            slotDate,
            "11:00",
            "12:00",
            now,
            created,
            skipped,
            cancellationToken);

        await EnsureSlotAsync(
            SuwaniQaV1Constants.SlotCancelledWithQr,
            SuwaniQaV1Constants.StationColombo,
            slotDate,
            "12:00",
            "13:00",
            now,
            created,
            skipped,
            cancellationToken);

        await EnsureSlotAsync(
            SuwaniQaV1Constants.SlotOffline,
            SuwaniQaV1Constants.StationColombo,
            slotDate,
            "13:00",
            "14:00",
            now,
            created,
            skipped,
            cancellationToken);

        await EnsureSlotAsync(
            SuwaniQaV1Constants.SlotHappy,
            SuwaniQaV1Constants.StationColombo,
            slotDate,
            "14:00",
            "15:00",
            now,
            created,
            skipped,
            cancellationToken);

        await EnsureReservationAsync(
            reservationId: SuwaniQaV1Constants.ResOwnershipApproved,
            ownerNic: ownerNic,
            stationId: SuwaniQaV1Constants.StationColombo,
            slotId: SuwaniQaV1Constants.SlotOwnership,
            status: ReservationStatuses.Approved,
            transactionReference: SuwaniQaV1Constants.TrxOwnership,
            issueQr: true,
            purpose: "OWNERSHIP_APPROVED",
            now,
            created,
            skipped,
            notes,
            reservations,
            cancellationToken);

        await EnsureReservationAsync(
            reservationId: SuwaniQaV1Constants.ResOwnerPending,
            ownerNic: ownerNic,
            stationId: SuwaniQaV1Constants.StationColombo,
            slotId: SuwaniQaV1Constants.SlotPending,
            status: ReservationStatuses.Pending,
            transactionReference: string.Empty,
            issueQr: false,
            purpose: "OWNER_PENDING",
            now,
            created,
            skipped,
            notes,
            reservations,
            cancellationToken);

        await EnsureReservationAsync(
            reservationId: SuwaniQaV1Constants.ResOwnerCancelled,
            ownerNic: ownerNic,
            stationId: SuwaniQaV1Constants.StationColombo,
            slotId: SuwaniQaV1Constants.SlotCancelled,
            status: ReservationStatuses.Cancelled,
            transactionReference: string.Empty,
            issueQr: false,
            purpose: "OWNER_CANCELLED",
            now,
            created,
            skipped,
            notes,
            reservations,
            cancellationToken);

        await EnsureCancelledWithOldQrAsync(
            ownerNic,
            now,
            created,
            skipped,
            notes,
            reservations,
            cancellationToken);

        await EnsureReservationAsync(
            reservationId: SuwaniQaV1Constants.ResOfflineComplete,
            ownerNic: ownerNic,
            stationId: SuwaniQaV1Constants.StationColombo,
            slotId: SuwaniQaV1Constants.SlotOffline,
            status: ReservationStatuses.Approved,
            transactionReference: SuwaniQaV1Constants.TrxOffline,
            issueQr: true,
            purpose: "OFFLINE_COMPLETE_APPROVED",
            now,
            created,
            skipped,
            notes,
            reservations,
            cancellationToken);

        await EnsureReservationAsync(
            reservationId: SuwaniQaV1Constants.ResHappyPath,
            ownerNic: ownerNic,
            stationId: SuwaniQaV1Constants.StationColombo,
            slotId: SuwaniQaV1Constants.SlotHappy,
            status: ReservationStatuses.Approved,
            transactionReference: SuwaniQaV1Constants.TrxHappy,
            issueQr: true,
            purpose: "HAPPY_PATH_APPROVED",
            now,
            created,
            skipped,
            notes,
            reservations,
            cancellationToken);

        notes.Add($"Other prosumer NIC for cross-user denial: {otherNic}");
        notes.Add("RES-SUWANI-APPROVED-001 was not read or modified.");
        notes.Add("Nearby radius default: 10 km (MaxNearbyRadiusKm=100).");
        notes.Add("Stations reused: NODE-SUWANI-COLOMBO / KANDY / GALLE (create-if-missing only).");

        return new SuwaniQaV1SeedResult
        {
            Created = created,
            Skipped = skipped,
            Notes = notes,
            Accounts = accounts,
            Reservations = reservations
        };
    }

    private async Task EnsureStationsExistAsync(
        DateTime now,
        List<string> created,
        List<string> skipped,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        await EnsureStationIfMissingAsync(
            SuwaniQaV1Constants.StationColombo,
            "Colombo Fort Microgrid",
            "Colombo Fort, Colombo",
            6.9271,
            79.8612,
            batterySlots: 8,
            now,
            created,
            skipped,
            notes,
            cancellationToken);

        await EnsureStationIfMissingAsync(
            SuwaniQaV1Constants.StationKandy,
            "Kandy Lake Microgrid",
            "Kandy Lake, Kandy",
            7.2906,
            80.6337,
            batterySlots: 6,
            now,
            created,
            skipped,
            notes,
            cancellationToken);

        await EnsureStationIfMissingAsync(
            SuwaniQaV1Constants.StationGalle,
            "Galle Fort Microgrid",
            "Galle Fort, Galle",
            6.0329,
            80.2168,
            batterySlots: 5,
            now,
            created,
            skipped,
            notes,
            cancellationToken);
    }

    private async Task EnsureStationIfMissingAsync(
        string nodeId,
        string name,
        string location,
        double lat,
        double lng,
        int batterySlots,
        DateTime now,
        List<string> created,
        List<string> skipped,
        List<string> notes,
        CancellationToken cancellationToken)
    {
        var key = $"station:{nodeId}";
        var existing = await _store.FindStationByNodeIdAsync(nodeId, cancellationToken);
        if (existing != null)
        {
            skipped.Add(key);
            notes.Add($"Reused existing station {nodeId} without modification (lat={existing.Latitude}, lng={existing.Longitude}, batterySlots={existing.BatterySlots}).");
            return;
        }

        await _store.UpsertStationByNodeIdAsync(new MicrogridNode
        {
            NodeId = nodeId,
            NodeName = $"{name} ({SuwaniQaV1Constants.Marker})",
            Description = $"{SuwaniQaV1Constants.Marker} map fixture station.",
            Location = location,
            Latitude = lat,
            Longitude = lng,
            CapacityKWh = 100,
            BatterySlots = batterySlots,
            Schedule = "08:00-18:00",
            Status = "active",
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);
        created.Add(key);
    }

    private async Task<string> EnsureProsumerAsync(
        string preferredNic,
        string email,
        string fullName,
        string phone,
        string password,
        string label,
        DateTime now,
        List<string> created,
        List<string> skipped,
        List<string> notes,
        Dictionary<string, string> accounts,
        CancellationToken cancellationToken)
    {
        // Prefer email ownership: if prosumer with this email already exists under another NIC, reuse that NIC.
        // Store only indexes by NIC, so scan preferred NIC first, then alternate if collision with unrelated email.
        var nic = preferredNic;
        var byNic = await _store.FindProsumerByNicAsync(nic, cancellationToken);

        if (byNic != null)
        {
            if (!string.Equals(byNic.Email, email, StringComparison.OrdinalIgnoreCase))
            {
                // Unrelated account occupies preferred NIC — pick alternate synthetic NIC.
                nic = preferredNic == SuwaniQaV1Constants.OwnerNic
                    ? "200012345679"
                    : "200098765433";
                notes.Add($"{label}: preferred NIC {preferredNic} occupied by unrelated email; trying {nic}.");
                byNic = await _store.FindProsumerByNicAsync(nic, cancellationToken);
            }
        }

        if (byNic != null)
        {
            skipped.Add($"prosumer:{byNic.Nic}");
            notes.Add($"{label}: existing account preserved (password not reset). NIC={byNic.Nic}, Email={byNic.Email}, Status={byNic.Status}.");
            accounts[label] = $"existing NIC={byNic.Nic} email={byNic.Email} (password preserved; use original TEST password if known)";
            return byNic.Nic;
        }

        await _store.UpsertProsumerByNicAsync(new Prosumer
        {
            Nic = nic,
            FullName = fullName,
            Email = email,
            Phone = phone,
            Status = "active",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(password),
            CreatedAt = now
        }, cancellationToken);

        created.Add($"prosumer:{nic}");
        accounts[label] = $"created NIC={nic} email={email}";
        return nic;
    }

    private async Task EnsureOperatorAsync(
        DateTime now,
        List<string> created,
        List<string> skipped,
        List<string> notes,
        Dictionary<string, string> accounts,
        CancellationToken cancellationToken)
    {
        const string label = "GridOperator QA";
        var email = SuwaniQaV1Constants.OperatorEmail;
        var existing = await _store.FindUserByEmailAsync(email, cancellationToken);
        if (existing != null)
        {
            skipped.Add($"user:{email}");
            notes.Add($"{label}: existing user preserved (password not reset). Role={existing.Role}, Status={existing.Status}.");
            accounts[label] = $"existing email={email} role={existing.Role} (password preserved)";
            if (!string.Equals(existing.Role, "GridOperator", StringComparison.Ordinal))
            {
                notes.Add($"{label}: WARNING role is '{existing.Role}', expected GridOperator. Not overwritten.");
            }

            return;
        }

        await _store.UpsertUserByEmailAsync(new User
        {
            UserId = SuwaniQaV1Constants.OperatorUserId,
            Email = email,
            FullName = SuwaniQaV1Constants.OperatorFullName,
            Role = "GridOperator",
            Status = "active",
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(SuwaniQaV1Constants.OperatorPassword),
            CreatedAt = now
        }, cancellationToken);

        created.Add($"user:{email}");
        accounts[label] = $"created email={email} role=GridOperator";
    }

    private async Task EnsureSlotAsync(
        string slotId,
        string stationId,
        DateTime slotDate,
        string start,
        string end,
        DateTime now,
        List<string> created,
        List<string> skipped,
        CancellationToken cancellationToken)
    {
        var key = $"slot:{slotId}";
        var existing = await _store.FindSlotBySlotIdAsync(slotId, cancellationToken);
        if (existing != null)
        {
            skipped.Add(key);
            return;
        }

        await _store.UpsertSlotBySlotIdAsync(new EnergyBookingSlot
        {
            SlotId = slotId,
            StationId = stationId,
            Date = slotDate,
            StartTime = start,
            EndTime = end,
            Capacity = 4,
            Availability = 3,
            Status = "Available",
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);
        created.Add(key);
    }

    private async Task EnsureReservationAsync(
        string reservationId,
        string ownerNic,
        string stationId,
        string slotId,
        string status,
        string transactionReference,
        bool issueQr,
        string purpose,
        DateTime now,
        List<string> created,
        List<string> skipped,
        List<string> notes,
        Dictionary<string, string> reservations,
        CancellationToken cancellationToken)
    {
        var key = $"reservation:{reservationId}";
        var existing = await _store.FindReservationByReservationIdAsync(reservationId, cancellationToken);
        if (existing != null)
        {
            skipped.Add(key);
            notes.Add($"{purpose}: existing status={existing.Status} (not overwritten). HasQr={!string.IsNullOrWhiteSpace(existing.TransactionReference)}.");
            reservations[purpose] = $"{reservationId}|{existing.Status}|trx={(string.IsNullOrWhiteSpace(existing.TransactionReference) ? "(none)" : existing.TransactionReference)}";
            return;
        }

        await _store.UpsertReservationByReservationIdAsync(new EnergyReservation
        {
            ReservationId = reservationId,
            ProsumerNic = ownerNic,
            StationId = stationId,
            SlotId = slotId,
            Status = status,
            TransactionReference = issueQr ? transactionReference : string.Empty,
            TransactionReferenceIssuedAt = issueQr ? now : null,
            CompletedAt = null,
            CompletedByOperatorId = null,
            CompletedByOperatorName = null,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);

        created.Add(key);
        reservations[purpose] = $"{reservationId}|{status}|trx={(issueQr ? transactionReference : "(none)")}";
    }

    /// <summary>
    /// Creates Approved+QR then applies the same Cancelled status transition as DELETE
    /// /api/reservations/{id}, preserving TransactionReference so the old QR remains for denial tests.
    /// </summary>
    private async Task EnsureCancelledWithOldQrAsync(
        string ownerNic,
        DateTime now,
        List<string> created,
        List<string> skipped,
        List<string> notes,
        Dictionary<string, string> reservations,
        CancellationToken cancellationToken)
    {
        const string purpose = "CANCELLED_WITH_OLD_QR";
        var reservationId = SuwaniQaV1Constants.ResCancelledWithOldQr;
        var key = $"reservation:{reservationId}";
        var existing = await _store.FindReservationByReservationIdAsync(reservationId, cancellationToken);

        if (existing != null)
        {
            skipped.Add(key);
            var hasQr = !string.IsNullOrWhiteSpace(existing.TransactionReference);
            notes.Add(
                $"{purpose}: existing status={existing.Status}, HasQr={hasQr} (not overwritten).");
            if (string.Equals(existing.Status, ReservationStatuses.Cancelled, StringComparison.OrdinalIgnoreCase)
                && hasQr)
            {
                notes.Add($"{purpose}: fixture ready for old-QR denial.");
            }
            else if (!hasQr)
            {
                notes.Add(
                    $"{purpose}: WARNING existing doc has no TransactionReference; create a new batch id if needed.");
            }

            reservations[purpose] =
                $"{reservationId}|{existing.Status}|trx={(hasQr ? existing.TransactionReference : "(none)")}";
            return;
        }

        // Step 1: Approved with legitimate QR reference (as if issued by QR endpoint).
        await _store.UpsertReservationByReservationIdAsync(new EnergyReservation
        {
            ReservationId = reservationId,
            ProsumerNic = ownerNic,
            StationId = SuwaniQaV1Constants.StationColombo,
            SlotId = SuwaniQaV1Constants.SlotCancelledWithQr,
            Status = ReservationStatuses.Approved,
            TransactionReference = SuwaniQaV1Constants.TrxCancelledWithOldQr,
            TransactionReferenceIssuedAt = now,
            CreatedAt = now,
            UpdatedAt = now
        }, cancellationToken);

        // Step 2: Supported cancel transition — status Cancelled, QR reference retained.
        var afterApprove = await _store.FindReservationByReservationIdAsync(reservationId, cancellationToken)
            ?? throw new InvalidOperationException("CANCELLED_WITH_OLD_QR missing after approve insert.");

        afterApprove.Status = ReservationStatuses.Cancelled;
        afterApprove.UpdatedAt = DateTime.UtcNow;
        await _store.UpsertReservationByReservationIdAsync(afterApprove, cancellationToken);

        // Restore one availability unit like ReservationsController.Delete (best-effort).
        var slot = await _store.FindSlotBySlotIdAsync(SuwaniQaV1Constants.SlotCancelledWithQr, cancellationToken);
        if (slot != null && slot.Availability < slot.Capacity)
        {
            slot.Availability += 1;
            slot.UpdatedAt = DateTime.UtcNow;
            await _store.UpsertSlotBySlotIdAsync(slot, cancellationToken);
        }

        created.Add(key);
        notes.Add(
            $"{purpose}: created Approved+QR then transitioned to Cancelled; TRX retained for operator denial.");
        reservations[purpose] =
            $"{reservationId}|{ReservationStatuses.Cancelled}|trx={SuwaniQaV1Constants.TrxCancelledWithOldQr}";
    }
}
