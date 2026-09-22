// =============================================================================
// File: TransferService.cs
// Description: Authoritative verify/complete rules for energy transfer finalization.
// Author: Suwani (Component 4)
// =============================================================================

using MicrogridApi.Dtos.Transfers;
using MicrogridApi.Models;

namespace MicrogridApi.Services;

public class TransferService
{
    public const int MaxTransactionReferenceLength = 128;

    private readonly IReservationAccess _reservations;
    private readonly IStationLookup _stations;

    // Wires reservation access and station lookups for eligibility checks.
    public TransferService(IReservationAccess reservations, IStationLookup stations)
    {
        _reservations = reservations;
        _stations = stations;
    }

    // Validates QR payload format and returns authoritative eligibility without completing.
    public async Task<TransferVerifyResult> VerifyAsync(
        string? transactionReference,
        CancellationToken cancellationToken = default)
    {
        var formatError = ValidateReferenceFormat(transactionReference);
        if (formatError != null)
        {
            return TransferVerifyResult.Fail(400, formatError);
        }

        var reservation = await _reservations.FindByTransactionReferenceAsync(
            transactionReference!,
            cancellationToken);

        if (reservation == null)
        {
            return TransferVerifyResult.Fail(404, "No reservation found for this transaction reference.");
        }

        var eligibility = await EvaluateEligibilityAsync(reservation, cancellationToken);
        if (!eligibility.Eligible)
        {
            return TransferVerifyResult.Fail(409, eligibility.Reason!, reservation);
        }

        var station = await FindStationAsync(reservation.StationId, cancellationToken);

        return TransferVerifyResult.Ok(new VerifyTransferResponse
        {
            Eligible = true,
            ReservationId = reservation.ReservationId,
            TransactionReference = reservation.TransactionReference!,
            Status = reservation.Status,
            StationId = reservation.StationId,
            StationName = station?.NodeName,
            StationLocation = station?.Location,
            SlotId = reservation.SlotId,
            ProsumerNicMasked = MaskNic(reservation.ProsumerNic),
            Message = "Reservation is approved and eligible for energy transfer."
        });
    }

    // Atomically completes an eligible approved reservation as the authenticated operator.
    public async Task<TransferCompleteResult> CompleteAsync(
        string reservationId,
        string operatorId,
        string operatorName,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(reservationId) || reservationId.Length > 64)
        {
            return TransferCompleteResult.Fail(400, "Invalid reservation id.");
        }

        var existing = await _reservations.FindByReservationIdAsync(reservationId, cancellationToken);
        if (existing == null)
        {
            return TransferCompleteResult.Fail(404, "Reservation not found.");
        }

        if (existing.Status == ReservationStatuses.Completed)
        {
            return TransferCompleteResult.Ok(new CompleteTransferResponse
            {
                AlreadyCompleted = true,
                ReservationId = existing.ReservationId,
                Status = existing.Status,
                CompletedAt = existing.CompletedAt,
                CompletedByOperatorId = existing.CompletedByOperatorId,
                CompletedByOperatorName = existing.CompletedByOperatorName,
                Message = "Transfer was already completed. No duplicate side effects applied."
            });
        }

        var eligibility = await EvaluateEligibilityAsync(existing, cancellationToken);
        if (!eligibility.Eligible)
        {
            return TransferCompleteResult.Fail(409, eligibility.Reason!, existing.Status);
        }

        var completedAt = DateTime.UtcNow;
        var updated = await _reservations.TryCompleteApprovedAsync(
            reservationId,
            operatorId,
            operatorName,
            completedAt,
            cancellationToken);

        if (updated == null)
        {
            // Race: another operator completed between eligibility check and update.
            var raced = await _reservations.FindByReservationIdAsync(reservationId, cancellationToken);
            if (raced?.Status == ReservationStatuses.Completed)
            {
                return TransferCompleteResult.Ok(new CompleteTransferResponse
                {
                    AlreadyCompleted = true,
                    ReservationId = raced.ReservationId,
                    Status = raced.Status,
                    CompletedAt = raced.CompletedAt,
                    CompletedByOperatorId = raced.CompletedByOperatorId,
                    CompletedByOperatorName = raced.CompletedByOperatorName,
                    Message = "Transfer was already completed. No duplicate side effects applied."
                });
            }

            return TransferCompleteResult.Fail(
                409,
                "Reservation is no longer eligible for completion.",
                raced?.Status);
        }

        return TransferCompleteResult.Ok(new CompleteTransferResponse
        {
            AlreadyCompleted = false,
            ReservationId = updated.ReservationId,
            Status = updated.Status,
            CompletedAt = updated.CompletedAt,
            CompletedByOperatorId = updated.CompletedByOperatorId,
            CompletedByOperatorName = updated.CompletedByOperatorName,
            Message = "Energy transfer completed successfully."
        });
    }

    // Issues or reuses QR reference for an approved reservation (authorized caller).
    public async Task<QrIssueResult> IssueQrAsync(
        string reservationId,
        string callerSubjectId,
        string callerRole,
        CancellationToken cancellationToken = default)
    {
        var reservation = await _reservations.FindByReservationIdAsync(reservationId, cancellationToken);
        if (reservation == null)
        {
            return QrIssueResult.Fail(404, "Reservation not found.");
        }

        var isOwner = string.Equals(
            reservation.ProsumerNic,
            callerSubjectId,
            StringComparison.OrdinalIgnoreCase)
            || string.Equals(
                reservation.ProsumerNic,
                callerSubjectId,
                StringComparison.Ordinal);

        // identifier claim may hold NIC for prosumers; subject is UserId for staff.
        var allowedRole = callerRole is "Backoffice" or "GridOperator" or "Prosumer";
        if (!allowedRole)
        {
            return QrIssueResult.Fail(403, "Not permitted to retrieve this QR code.");
        }

        if (callerRole == "Prosumer" && !IsProsumerOwner(reservation, callerSubjectId))
        {
            return QrIssueResult.Fail(403, "Not permitted to retrieve this QR code.");
        }

        if (reservation.Status != ReservationStatuses.Approved)
        {
            return QrIssueResult.Fail(
                409,
                $"QR is only available for approved reservations. Current status: {reservation.Status}.");
        }

        var withRef = await _reservations.EnsureTransactionReferenceAsync(reservationId, cancellationToken);
        if (withRef == null || string.IsNullOrWhiteSpace(withRef.TransactionReference))
        {
            return QrIssueResult.Fail(409, "Unable to issue QR for this reservation.");
        }

        return QrIssueResult.Ok(new QrCodeResponse
        {
            ReservationId = withRef.ReservationId,
            TransactionReference = withRef.TransactionReference!,
            Status = withRef.Status,
            IssuedAt = withRef.TransactionReferenceIssuedAt,
            Payload = withRef.TransactionReference!,
            ReusedExistingReference = string.Equals(
                reservation.TransactionReference,
                withRef.TransactionReference,
                StringComparison.Ordinal)
        });
    }

    // Checks reservation status plus related station presence/active state.
    private async Task<(bool Eligible, string? Reason)> EvaluateEligibilityAsync(
        EnergyReservation reservation,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(reservation.TransactionReference))
        {
            return (false, "Reservation has no issued transaction reference.");
        }

        switch (reservation.Status)
        {
            case ReservationStatuses.Approved:
                break;
            case ReservationStatuses.Pending:
                return (false, "Reservation is still pending approval.");
            case ReservationStatuses.Cancelled:
                return (false, "Reservation has been cancelled.");
            case ReservationStatuses.Completed:
                return (false, "Reservation transfer is already completed.");
            default:
                return (false, $"Reservation status '{reservation.Status}' is not eligible.");
        }

        var station = await FindStationAsync(reservation.StationId, cancellationToken);
        if (station == null)
        {
            return (false, "Linked microgrid station was not found.");
        }

        if (!string.Equals(station.Status, "active", StringComparison.OrdinalIgnoreCase))
        {
            return (false, "Linked microgrid station is not active.");
        }

        return (true, null);
    }

    // Resolves station by application NodeId (not Mongo ObjectId).
    private Task<MicrogridNode?> FindStationAsync(
        string stationNodeId,
        CancellationToken cancellationToken)
    {
        return _stations.FindByNodeIdAsync(stationNodeId, cancellationToken);
    }

    // Validates QR reference length and charset before DB lookup.
    private static string? ValidateReferenceFormat(string? transactionReference)
    {
        if (string.IsNullOrWhiteSpace(transactionReference))
        {
            return "Transaction reference is required.";
        }

        var value = transactionReference.Trim();
        if (value.Length < 8 || value.Length > MaxTransactionReferenceLength)
        {
            return "Transaction reference length is invalid.";
        }

        foreach (var ch in value)
        {
            if (!(char.IsLetterOrDigit(ch) || ch is '-' or '_' or '.'))
            {
                return "Transaction reference contains invalid characters.";
            }
        }

        return null;
    }

    // Masks NIC for operator display (keeps last 3 characters).
    private static string MaskNic(string nic)
    {
        if (string.IsNullOrWhiteSpace(nic) || nic.Length <= 3)
        {
            return "***";
        }

        return new string('*', nic.Length - 3) + nic[^3..];
    }

    // Determines whether the authenticated prosumer owns the reservation.
    private static bool IsProsumerOwner(EnergyReservation reservation, string callerSubjectId)
    {
        return string.Equals(reservation.ProsumerNic, callerSubjectId, StringComparison.OrdinalIgnoreCase);
    }
}

public sealed class TransferVerifyResult
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string? ErrorMessage { get; init; }
    public VerifyTransferResponse? Response { get; init; }
    public EnergyReservation? Reservation { get; init; }

    public static TransferVerifyResult Ok(VerifyTransferResponse response) =>
        new() { Success = true, StatusCode = 200, Response = response };

    public static TransferVerifyResult Fail(int code, string message, EnergyReservation? reservation = null) =>
        new() { Success = false, StatusCode = code, ErrorMessage = message, Reservation = reservation };
}

public sealed class TransferCompleteResult
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string? ErrorMessage { get; init; }
    public string? CurrentStatus { get; init; }
    public CompleteTransferResponse? Response { get; init; }

    public static TransferCompleteResult Ok(CompleteTransferResponse response) =>
        new() { Success = true, StatusCode = 200, Response = response };

    public static TransferCompleteResult Fail(int code, string message, string? status = null) =>
        new() { Success = false, StatusCode = code, ErrorMessage = message, CurrentStatus = status };
}

public sealed class QrIssueResult
{
    public bool Success { get; init; }
    public int StatusCode { get; init; }
    public string? ErrorMessage { get; init; }
    public QrCodeResponse? Response { get; init; }

    public static QrIssueResult Ok(QrCodeResponse response) =>
        new() { Success = true, StatusCode = 200, Response = response };

    public static QrIssueResult Fail(int code, string message) =>
        new() { Success = false, StatusCode = code, ErrorMessage = message };
}
