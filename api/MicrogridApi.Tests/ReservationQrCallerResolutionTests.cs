// =============================================================================
// File: ReservationQrCallerResolutionTests.cs
// Description: Guards QR ownership key = JWT subject (NIC), not email identifier claim.
// Author: Suwani (Component 4)
// =============================================================================

using System.Security.Claims;
using MicrogridApi.Models;
using MicrogridApi.Services;
using Moq;
using Xunit;

namespace MicrogridApi.Tests;

public class ReservationQrCallerResolutionTests
{
    [Fact]
    public async Task IssueQr_Succeeds_When_Caller_Subject_Is_Owner_Nic_Even_If_Email_Differs()
    {
        // Mirrors AuthController: sub/NameIdentifier = NIC, identifier claim = email.
        var reservation = new EnergyReservation
        {
            ReservationId = SuwaniDevSeedConstants.ApprovedReservationId,
            ProsumerNic = SuwaniDevSeedConstants.ProsumerNic,
            Status = ReservationStatuses.Approved,
            TransactionReference = SuwaniDevSeedConstants.TransactionReference,
            TransactionReferenceIssuedAt = DateTime.UtcNow
        };

        var reservations = new Mock<IReservationAccess>();
        reservations
            .Setup(r => r.FindByReservationIdAsync(reservation.ReservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        reservations
            .Setup(r => r.EnsureTransactionReferenceAsync(reservation.ReservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var sut = new TransferService(reservations.Object, Mock.Of<IStationLookup>());

        var result = await sut.IssueQrAsync(
            reservation.ReservationId,
            callerSubjectId: SuwaniDevSeedConstants.ProsumerNic,
            callerRole: "Prosumer");

        Assert.True(result.Success);
        Assert.Equal(200, result.StatusCode);
    }

    [Fact]
    public async Task IssueQr_Rejects_When_Caller_Subject_Is_Email_Not_Nic()
    {
        var reservation = new EnergyReservation
        {
            ReservationId = SuwaniDevSeedConstants.ApprovedReservationId,
            ProsumerNic = SuwaniDevSeedConstants.ProsumerNic,
            Status = ReservationStatuses.Approved,
            TransactionReference = SuwaniDevSeedConstants.TransactionReference
        };

        var reservations = new Mock<IReservationAccess>();
        reservations
            .Setup(r => r.FindByReservationIdAsync(reservation.ReservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var sut = new TransferService(reservations.Object, Mock.Of<IStationLookup>());

        // Bug regression: using identifier claim (email) as caller key must fail ownership.
        var result = await sut.IssueQrAsync(
            reservation.ReservationId,
            callerSubjectId: SuwaniDevSeedConstants.ProsumerEmail,
            callerRole: "Prosumer");

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
        Assert.Equal("Not permitted to retrieve this QR code.", result.ErrorMessage);
    }

    [Fact]
    public void Prosumer_Jwt_Subject_Is_Nic_While_Identifier_May_Be_Email()
    {
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[]
        {
            new Claim(ClaimTypes.NameIdentifier, SuwaniDevSeedConstants.ProsumerNic),
            new Claim(ClaimTypes.Role, "Prosumer"),
            new Claim("identifier", SuwaniDevSeedConstants.ProsumerEmail),
        }, "test"));

        var subject = user.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var identifier = user.FindFirstValue("identifier") ?? string.Empty;

        Assert.Equal(SuwaniDevSeedConstants.ProsumerNic, subject);
        Assert.Equal(SuwaniDevSeedConstants.ProsumerEmail, identifier);
        Assert.NotEqual(subject, identifier);
    }
}
