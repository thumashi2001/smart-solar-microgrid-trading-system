// =============================================================================
// File: SuwaniDevSeedConstants.cs
// Description: Stable identifiers for Suwani development seed (idempotent upserts).
// Author: Suwani (Component 4)
// =============================================================================

namespace MicrogridApi.Services;

/// <summary>
/// Reserved Suwani seed keys. Upserts only match these identifiers and never
/// delete or rewrite unrelated teammate documents.
/// </summary>
public static class SuwaniDevSeedConstants
{
    public const string BackofficeEmail = "backoffice@test.com";
    public const string BackofficeUserId = "suwani-seed-backoffice-001";
    public const string BackofficeFullName = "Suwani Seed Backoffice";
    public const string BackofficePassword = "Backoffice@12345";

    public const string OperatorEmail = "operator@test.com";
    public const string OperatorUserId = "suwani-seed-operator-001";
    public const string OperatorFullName = "Suwani Seed Operator";
    public const string OperatorPassword = "Operator@12345";

    public const string ProsumerNic = "199912345678";
    public const string ProsumerFullName = "Test Prosumer";
    public const string ProsumerEmail = "prosumer@test.com";
    public const string ProsumerPhone = "0770000000";
    public const string ProsumerPassword = "Prosumer@12345";

    public const string ColomboNodeId = "NODE-SUWANI-COLOMBO";
    public const string KandyNodeId = "NODE-SUWANI-KANDY";
    public const string GalleNodeId = "NODE-SUWANI-GALLE";

    public const string ColomboSlotId = "SLOT-SUWANI-COLOMBO-001";

    public const string ApprovedReservationId = "RES-SUWANI-APPROVED-001";

    /// <summary>
    /// Opaque QR reference. Stable so repeated seeds keep the same credential.
    /// Format matches TransferService validation (TRX- + alphanumeric).
    /// </summary>
    public const string TransactionReference = "TRX-SUWANISEED0001APPROVEDRESERVATION01";

    public const string SeedMarker = "suwani-dev-seed";
}
