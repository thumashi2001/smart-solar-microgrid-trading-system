// =============================================================================
// File: SuwaniDevFixturesControllerTests.cs
// Description: Ensures Suwani seed endpoints are unavailable outside Development.
// Author: Suwani (Component 4)
// =============================================================================

using MicrogridApi.Controllers;
using MicrogridApi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Hosting;
using Moq;
using Xunit;

namespace MicrogridApi.Tests;

public class SuwaniDevFixturesControllerTests
{
    [Fact]
    public async Task Seed_Returns_NotFound_In_Production()
    {
        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);

        var store = new InMemorySuwaniSeedStore();
        var seedService = new SuwaniDevSeedService(store);
        var controller = new SuwaniDevFixturesController(seedService, env.Object);

        var result = await controller.Seed(CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
        Assert.Empty(store.Users);
        Assert.Empty(store.Reservations);
    }

    [Fact]
    public async Task ApprovedReservation_Alias_Returns_NotFound_In_Production()
    {
        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(Environments.Production);

        var controller = new SuwaniDevFixturesController(
            new SuwaniDevSeedService(new InMemorySuwaniSeedStore()),
            env.Object);

        var result = await controller.CreateApprovedReservationFixture(CancellationToken.None);

        Assert.IsType<NotFoundResult>(result);
    }

    [Fact]
    public async Task Seed_Returns_Ok_In_Development()
    {
        var env = new Mock<IHostEnvironment>();
        env.SetupGet(e => e.EnvironmentName).Returns(Environments.Development);

        var store = new InMemorySuwaniSeedStore();
        var controller = new SuwaniDevFixturesController(new SuwaniDevSeedService(store), env.Object);

        var result = await controller.Seed(CancellationToken.None);

        var ok = Assert.IsType<OkObjectResult>(result);
        Assert.NotNull(ok.Value);
        Assert.Single(store.Reservations);
        Assert.Equal(SuwaniDevSeedConstants.OperatorEmail,
            store.Users.Single(u => u.Role == "GridOperator").Email);
    }
}
