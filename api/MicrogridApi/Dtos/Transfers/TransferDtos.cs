// =============================================================================
// File: TransferDtos.cs
// Description: Request/response contracts for QR issuance and transfer endpoints.
// Author: Suwani (Component 4)
// =============================================================================

namespace MicrogridApi.Dtos.Transfers;

public class VerifyTransferRequest
{
    public string TransactionReference { get; set; } = string.Empty;
}

public class VerifyTransferResponse
{
    public bool Eligible { get; set; }
    public string ReservationId { get; set; } = string.Empty;
    public string TransactionReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public string StationId { get; set; } = string.Empty;
    public string? StationName { get; set; }
    public string? StationLocation { get; set; }
    public string SlotId { get; set; } = string.Empty;
    public string ProsumerNicMasked { get; set; } = string.Empty;
    public string Message { get; set; } = string.Empty;
}

public class CompleteTransferResponse
{
    public bool AlreadyCompleted { get; set; }
    public string ReservationId { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? CompletedAt { get; set; }
    public string? CompletedByOperatorId { get; set; }
    public string? CompletedByOperatorName { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class QrCodeResponse
{
    public string ReservationId { get; set; } = string.Empty;
    public string TransactionReference { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime? IssuedAt { get; set; }

    /// <summary>Exact string encoded into the QR image (opaque reference only).</summary>
    public string Payload { get; set; } = string.Empty;

    public bool ReusedExistingReference { get; set; }
}

public class NearbyStationResponse
{
    public string? Id { get; set; }
    public string NodeId { get; set; } = string.Empty;
    public string NodeName { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public double Latitude { get; set; }
    public double Longitude { get; set; }
    public double CapacityKWh { get; set; }

    /// <summary>
    /// Total configured battery slots on the node (capacity), not live availability.
    /// Live availability requires Viman's slot domain and is not fabricated here.
    /// </summary>
    public int BatterySlots { get; set; }

    public string Status { get; set; } = string.Empty;
    public double DistanceKm { get; set; }
}
