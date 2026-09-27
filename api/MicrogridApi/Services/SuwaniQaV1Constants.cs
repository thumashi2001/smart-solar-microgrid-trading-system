// =============================================================================
// File: SuwaniQaV1Constants.cs
// Description: Stable SUWANI-QA-V1 fixture keys (additive manual-test seed).
// Author: Suwani (Component 4)
// =============================================================================

namespace MicrogridApi.Services;

public static class SuwaniQaV1Constants
{
    public const string Marker = "SUWANI-QA-V1";

    public const string OwnerEmail = "suwani.qa.owner@example.com";
    public const string OwnerNic = "200012345678";
    public const string OwnerFullName = "Suwani QA V1 Owner";
    public const string OwnerPhone = "0771000001";
    public const string OwnerPassword = "SuwaniQaOwner@2026!";

    public const string OtherEmail = "suwani.qa.other@example.com";
    public const string OtherNic = "200098765432";
    public const string OtherFullName = "Suwani QA V1 Other";
    public const string OtherPhone = "0771000002";
    public const string OtherPassword = "SuwaniQaOther@2026!";

    public const string OperatorEmail = "suwani.qa.operator@example.com";
    public const string OperatorUserId = "suwani-qa-v1-operator-001";
    public const string OperatorFullName = "Suwani QA V1 Operator";
    public const string OperatorPassword = "SuwaniQaOperator@2026!";

    // Reuse existing seeded stations (do not modify them).
    public const string StationColombo = SuwaniDevSeedConstants.ColomboNodeId;
    public const string StationKandy = SuwaniDevSeedConstants.KandyNodeId;
    public const string StationGalle = SuwaniDevSeedConstants.GalleNodeId;

    public const string SlotOwnership = "SLOT-SUWANI-QA-V1-OWN-01";
    public const string SlotPending = "SLOT-SUWANI-QA-V1-PEND-01";
    public const string SlotCancelled = "SLOT-SUWANI-QA-V1-CANC-01";
    public const string SlotCancelledWithQr = "SLOT-SUWANI-QA-V1-CANCQR-01";
    public const string SlotOffline = "SLOT-SUWANI-QA-V1-OFF-01";
    public const string SlotHappy = "SLOT-SUWANI-QA-V1-HAPPY-01";

    public const string ResOwnershipApproved = "RES-SUWANI-QA-V1-OWNERSHIP";
    public const string ResOwnerPending = "RES-SUWANI-QA-V1-PENDING";
    public const string ResOwnerCancelled = "RES-SUWANI-QA-V1-CANCELLED";
    public const string ResCancelledWithOldQr = "RES-SUWANI-QA-V1-CANCELLED-QR";
    public const string ResOfflineComplete = "RES-SUWANI-QA-V1-OFFLINE";
    public const string ResHappyPath = "RES-SUWANI-QA-V1-HAPPY";

    public const string TrxCancelledWithOldQr = "TRX-SUWANIQAV1CANCELLEDOLDQR000001";
    public const string TrxOwnership = "TRX-SUWANIQAV1OWNERSHIPAPPROVED0001";
    public const string TrxOffline = "TRX-SUWANIQAV1OFFLINECOMPLETE000001";
    public const string TrxHappy = "TRX-SUWANIQAV1HAPPYPATHAPPROVED001";
}
