// =============================================================================
// File: TransferServiceTests.cs
// Description: Unit tests for Suwani transfer verify/complete and QR issuance.
// Author: Suwani (Component 4)
// =============================================================================

using MicrogridApi.Models;
using MicrogridApi.Services;
using Moq;
using Xunit;

namespace MicrogridApi.Tests;

public class TransferServiceTests
{
    private readonly Mock<IReservationAccess> _reservations = new();
    private readonly Mock<IStationLookup> _stations = new();
    private readonly TransferService _sut;

    public TransferServiceTests()
    {
        _sut = new TransferService(_reservations.Object, _stations.Object);
    }

    [Fact]
    public async Task Verify_Rejects_Malformed_Reference()
    {
        var result = await _sut.VerifyAsync("!!!bad!!!");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Verify_Rejects_Empty_Reference()
    {
        var result = await _sut.VerifyAsync(" ");

        Assert.False(result.Success);
        Assert.Equal(400, result.StatusCode);
    }

    [Fact]
    public async Task Verify_Rejects_Missing_Reservation()
    {
        _reservations
            .Setup(r => r.FindByTransactionReferenceAsync("TRX-ABCDEF12", It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnergyReservation?)null);

        var result = await _sut.VerifyAsync("TRX-ABCDEF12");

        Assert.False(result.Success);
        Assert.Equal(404, result.StatusCode);
    }

    [Theory]
    [InlineData(ReservationStatuses.Pending)]
    [InlineData(ReservationStatuses.Cancelled)]
    [InlineData(ReservationStatuses.Completed)]
    public async Task Verify_Rejects_Ineligible_Statuses(string status)
    {
        var reservation = CreateReservation(status);
        _reservations
            .Setup(r => r.FindByTransactionReferenceAsync(reservation.TransactionReference!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var result = await _sut.VerifyAsync(reservation.TransactionReference);

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task Verify_Succeeds_For_Approved_With_Active_Station()
    {
        var reservation = CreateReservation(ReservationStatuses.Approved);
        _reservations
            .Setup(r => r.FindByTransactionReferenceAsync(reservation.TransactionReference!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _stations
            .Setup(s => s.FindByNodeIdAsync(reservation.StationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateStation("active"));

        var result = await _sut.VerifyAsync(reservation.TransactionReference);

        Assert.True(result.Success);
        Assert.NotNull(result.Response);
        Assert.True(result.Response!.Eligible);
        Assert.Equal(reservation.ReservationId, result.Response.ReservationId);
        Assert.DoesNotContain(reservation.ProsumerNic, result.Response.ProsumerNicMasked);
    }

    [Fact]
    public async Task Verify_Does_Not_Complete_Reservation()
    {
        var reservation = CreateReservation(ReservationStatuses.Approved);
        _reservations
            .Setup(r => r.FindByTransactionReferenceAsync(reservation.TransactionReference!, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _stations
            .Setup(s => s.FindByNodeIdAsync(reservation.StationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateStation("active"));

        await _sut.VerifyAsync(reservation.TransactionReference);

        _reservations.Verify(
            r => r.TryCompleteApprovedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Complete_Returns_Idempotent_Response_When_Already_Completed()
    {
        var reservation = CreateReservation(ReservationStatuses.Completed);
        reservation.CompletedAt = DateTime.UtcNow.AddMinutes(-5);
        reservation.CompletedByOperatorId = "op-1";
        reservation.CompletedByOperatorName = "Op One";

        _reservations
            .Setup(r => r.FindByReservationIdAsync(reservation.ReservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var result = await _sut.CompleteAsync(reservation.ReservationId, "op-2", "Op Two");

        Assert.True(result.Success);
        Assert.True(result.Response!.AlreadyCompleted);
        _reservations.Verify(
            r => r.TryCompleteApprovedAsync(
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<string>(),
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()),
            Times.Never);
    }

    [Fact]
    public async Task Complete_Uses_Server_Operator_Identity_And_Persists_Once()
    {
        var reservation = CreateReservation(ReservationStatuses.Approved);
        var completed = CreateReservation(ReservationStatuses.Completed);
        completed.ReservationId = reservation.ReservationId;
        completed.CompletedByOperatorId = "server-op";
        completed.CompletedByOperatorName = "Server Name";
        completed.CompletedAt = DateTime.UtcNow;

        _reservations
            .Setup(r => r.FindByReservationIdAsync(reservation.ReservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _stations
            .Setup(s => s.FindByNodeIdAsync(reservation.StationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateStation("active"));
        _reservations
            .Setup(r => r.TryCompleteApprovedAsync(
                reservation.ReservationId,
                "server-op",
                "Server Name",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(completed);

        var result = await _sut.CompleteAsync(reservation.ReservationId, "server-op", "Server Name");

        Assert.True(result.Success);
        Assert.False(result.Response!.AlreadyCompleted);
        Assert.Equal("server-op", result.Response.CompletedByOperatorId);
    }

    [Fact]
    public async Task Complete_Handles_Concurrent_Winner_As_AlreadyCompleted()
    {
        var reservation = CreateReservation(ReservationStatuses.Approved);
        var raced = CreateReservation(ReservationStatuses.Completed);
        raced.ReservationId = reservation.ReservationId;
        raced.CompletedByOperatorId = "other-op";
        raced.CompletedAt = DateTime.UtcNow;

        _reservations
            .SetupSequence(r => r.FindByReservationIdAsync(reservation.ReservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation)
            .ReturnsAsync(raced);
        _stations
            .Setup(s => s.FindByNodeIdAsync(reservation.StationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(CreateStation("active"));
        _reservations
            .Setup(r => r.TryCompleteApprovedAsync(
                reservation.ReservationId,
                "op-a",
                "A",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync((EnergyReservation?)null);

        var result = await _sut.CompleteAsync(reservation.ReservationId, "op-a", "A");

        Assert.True(result.Success);
        Assert.True(result.Response!.AlreadyCompleted);
    }

    [Fact]
    public async Task Complete_Rejects_When_Cancelled_Between_Verify_And_Complete()
    {
        var reservation = CreateReservation(ReservationStatuses.Cancelled);
        _reservations
            .Setup(r => r.FindByReservationIdAsync(reservation.ReservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var result = await _sut.CompleteAsync(reservation.ReservationId, "op", "Op");

        Assert.False(result.Success);
        Assert.Equal(409, result.StatusCode);
    }

    [Fact]
    public async Task IssueQr_Reuses_Existing_Reference_For_Approved()
    {
        var reservation = CreateReservation(ReservationStatuses.Approved);
        _reservations
            .Setup(r => r.FindByReservationIdAsync(reservation.ReservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        _reservations
            .Setup(r => r.EnsureTransactionReferenceAsync(reservation.ReservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var result = await _sut.IssueQrAsync(reservation.ReservationId, reservation.ProsumerNic, "Prosumer");

        Assert.True(result.Success);
        Assert.Equal(reservation.TransactionReference, result.Response!.Payload);
        Assert.True(result.Response.ReusedExistingReference);
    }

    [Fact]
    public async Task IssueQr_Rejects_Non_Owner_Prosumer()
    {
        var reservation = CreateReservation(ReservationStatuses.Approved);
        _reservations
            .Setup(r => r.FindByReservationIdAsync(reservation.ReservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);

        var result = await _sut.IssueQrAsync(reservation.ReservationId, "OTHER-NIC", "Prosumer");

        Assert.False(result.Success);
        Assert.Equal(403, result.StatusCode);
    }

    private static EnergyReservation CreateReservation(string status) => new()
    {
        ReservationId = "res-001",
        ProsumerNic = "199912345678",
        StationId = "NODE-AABBCCDD",
        SlotId = "slot-1",
        Status = status,
        TransactionReference = "TRX-0123456789ABCDEF0123456789ABCDEF",
        TransactionReferenceIssuedAt = DateTime.UtcNow
    };

    private static MicrogridNode CreateStation(string status) => new()
    {
        NodeId = "NODE-AABBCCDD",
        NodeName = "Colombo Node",
        Location = "Colombo",
        Status = status,
        Latitude = 6.9271,
        Longitude = 79.8612
    };
}
