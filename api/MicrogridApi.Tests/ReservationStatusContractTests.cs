// =============================================================================
// File: ReservationStatusContractTests.cs
// Description: Guards Suwani transfer/QR against Viman PascalCase reservation statuses.
// Author: Suwani (Component 4 integration)
// =============================================================================

using MicrogridApi.Models;
using MicrogridApi.Services;
using Xunit;

namespace MicrogridApi.Tests;

public class ReservationStatusContractTests
{
    [Fact]
    public void ReservationStatuses_Match_Viman_PascalCase_Contract()
    {
        Assert.Equal("Pending", ReservationStatuses.Pending);
        Assert.Equal("Approved", ReservationStatuses.Approved);
        Assert.Equal("Cancelled", ReservationStatuses.Cancelled);
        Assert.Equal("Completed", ReservationStatuses.Completed);
    }

    [Fact]
    public void EnergyReservation_Default_Status_Is_Pending_Like_Viman_Create()
    {
        var reservation = new EnergyReservation();
        Assert.Equal("Pending", reservation.Status);
        Assert.Equal(string.Empty, reservation.TransactionReference);
    }

    [Fact]
    public void EnergyBookingSlot_Default_Status_Is_Available_Like_Viman_Slots()
    {
        var slot = new EnergyBookingSlot();
        Assert.Equal("Available", slot.Status);
    }

    [Theory]
    [InlineData("approved")]
    [InlineData("APPROVED")]
    [InlineData("pending")]
    public void Lowercase_Legacy_Statuses_Do_Not_Equal_Canonical_Constants(string legacy)
    {
        Assert.NotEqual(ReservationStatuses.Approved, legacy);
        Assert.NotEqual(ReservationStatuses.Pending, legacy);
    }
}
