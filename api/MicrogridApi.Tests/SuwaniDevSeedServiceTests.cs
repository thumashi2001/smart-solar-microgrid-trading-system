// =============================================================================
// File: SuwaniDevSeedServiceTests.cs
// Description: Idempotency and transfer-flow tests for Suwani development seed.
// Author: Suwani (Component 4)
// =============================================================================

using MicrogridApi.Models;
using MicrogridApi.Services;
using Moq;
using Xunit;

namespace MicrogridApi.Tests;

public class SuwaniDevSeedServiceTests
{
    [Fact]
    public async Task First_Seed_Creates_All_Expected_Records()
    {
        var store = new InMemorySuwaniSeedStore();
        var sut = new SuwaniDevSeedService(store);

        var result = await sut.SeedAsync();

        Assert.True(result.FirstRun);
        Assert.Contains(result.Created, c => c.StartsWith("user:"));
        Assert.Contains(result.Created, c => c == $"prosumer:{SuwaniDevSeedConstants.ProsumerNic}");
        Assert.Equal(3, store.Stations.Count);
        Assert.Single(store.Slots);
        Assert.Single(store.Reservations);
        Assert.Equal(2, store.Users.Count);
        Assert.Single(store.Prosumers);

        var operatorUser = store.Users.Single(u => u.Email == SuwaniDevSeedConstants.OperatorEmail);
        Assert.Equal("GridOperator", operatorUser.Role);
        Assert.Equal("active", operatorUser.Status);
        Assert.True(BCrypt.Net.BCrypt.Verify(SuwaniDevSeedConstants.OperatorPassword, operatorUser.PasswordHash));

        var prosumer = store.Prosumers.Single();
        Assert.Equal("Test Prosumer", prosumer.FullName);
        Assert.Equal("active", prosumer.Status);
        Assert.True(BCrypt.Net.BCrypt.Verify(SuwaniDevSeedConstants.ProsumerPassword, prosumer.PasswordHash));

        Assert.Contains(store.Stations, s => s.NodeId == SuwaniDevSeedConstants.ColomboNodeId
            && Math.Abs(s.Latitude - 6.9271) < 0.0001
            && Math.Abs(s.Longitude - 79.8612) < 0.0001
            && s.Status == "active");

        var reservation = store.Reservations.Single();
        Assert.Equal(ReservationStatuses.Approved, reservation.Status);
        Assert.Equal(SuwaniDevSeedConstants.ProsumerNic, reservation.ProsumerNic);
        Assert.Equal(SuwaniDevSeedConstants.ColomboNodeId, reservation.StationId);
        Assert.Equal(SuwaniDevSeedConstants.ColomboSlotId, reservation.SlotId);
        Assert.StartsWith("TRX-", reservation.TransactionReference);
        Assert.Equal(SuwaniDevSeedConstants.TransactionReference, reservation.TransactionReference);
        Assert.Equal(result.ReservationId, reservation.ReservationId);
        Assert.Equal(result.TransactionReference, reservation.TransactionReference);
    }

    [Fact]
    public async Task Second_Seed_Does_Not_Create_Duplicates()
    {
        var store = new InMemorySuwaniSeedStore();
        var sut = new SuwaniDevSeedService(store);

        var first = await sut.SeedAsync();
        var second = await sut.SeedAsync();

        Assert.True(first.FirstRun);
        Assert.False(second.FirstRun);
        Assert.Empty(second.Created);
        Assert.NotEmpty(second.Updated);

        Assert.Equal(2, store.Users.Count);
        Assert.Single(store.Prosumers);
        Assert.Equal(3, store.Stations.Count);
        Assert.Single(store.Slots);
        Assert.Single(store.Reservations);

        Assert.Equal(1, await store.CountUsersByEmailAsync(SuwaniDevSeedConstants.OperatorEmail));
        Assert.Equal(1, await store.CountStationsByNodeIdAsync(SuwaniDevSeedConstants.ColomboNodeId));
        Assert.Equal(1, await store.CountReservationsByReservationIdAsync(
            SuwaniDevSeedConstants.ApprovedReservationId));
    }

    [Fact]
    public async Task Seeded_Reservation_Supports_Qr_Verify_And_Complete()
    {
        var store = new InMemorySuwaniSeedStore();
        var seed = new SuwaniDevSeedService(store);
        var seedResult = await seed.SeedAsync();

        var reservation = store.Reservations.Single();
        var station = store.Stations.Single(s => s.NodeId == SuwaniDevSeedConstants.ColomboNodeId);

        var reservations = new Mock<IReservationAccess>();
        var stations = new Mock<IStationLookup>();

        reservations
            .Setup(r => r.FindByReservationIdAsync(seedResult.ReservationId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        reservations
            .Setup(r => r.FindByTransactionReferenceAsync(
                seedResult.TransactionReference,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        reservations
            .Setup(r => r.EnsureTransactionReferenceAsync(
                seedResult.ReservationId,
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(reservation);
        reservations
            .Setup(r => r.TryCompleteApprovedAsync(
                seedResult.ReservationId,
                "suwani-seed-operator-001",
                "Suwani Seed Operator",
                It.IsAny<DateTime>(),
                It.IsAny<CancellationToken>()))
            .ReturnsAsync(() =>
            {
                reservation.Status = ReservationStatuses.Completed;
                reservation.CompletedByOperatorId = "suwani-seed-operator-001";
                reservation.CompletedByOperatorName = "Suwani Seed Operator";
                reservation.CompletedAt = DateTime.UtcNow;
                return reservation;
            });

        stations
            .Setup(s => s.FindByNodeIdAsync(SuwaniDevSeedConstants.ColomboNodeId, It.IsAny<CancellationToken>()))
            .ReturnsAsync(station);

        var transfers = new TransferService(reservations.Object, stations.Object);

        var qr = await transfers.IssueQrAsync(
            seedResult.ReservationId,
            SuwaniDevSeedConstants.ProsumerNic,
            "Prosumer");
        Assert.True(qr.Success);
        Assert.Equal(seedResult.TransactionReference, qr.Response!.Payload);

        var verify = await transfers.VerifyAsync(seedResult.TransactionReference);
        Assert.True(verify.Success);
        Assert.True(verify.Response!.Eligible);

        var complete = await transfers.CompleteAsync(
            seedResult.ReservationId,
            "suwani-seed-operator-001",
            "Suwani Seed Operator");
        Assert.True(complete.Success);
        Assert.False(complete.Response!.AlreadyCompleted);
        Assert.Equal(ReservationStatuses.Completed, complete.Response.Status);
    }
}
