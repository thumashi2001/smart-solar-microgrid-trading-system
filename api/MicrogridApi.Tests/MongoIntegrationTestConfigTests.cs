// =============================================================================
// File: MongoIntegrationTestConfigTests.cs
// Description: Guards for isolated integration-test database naming.
// Author: Suwani (Component 4 integration)
// =============================================================================

using MicrogridApi.Tests.Integration;
using Xunit;

namespace MicrogridApi.Tests;

public class MongoIntegrationTestConfigTests
{
    [Theory]
    [InlineData("microgrid_db")]
    [InlineData("admin")]
    [InlineData("local")]
    [InlineData("config")]
    public void GuardTestDatabaseName_Rejects_Shared_Or_System_Databases(string name)
    {
        Assert.Throws<InvalidOperationException>(() =>
            MongoIntegrationTestConfig.GuardTestDatabaseName(name));
    }

    [Theory]
    [InlineData("production")]
    [InlineData("microgrid_db_backup")]
    public void GuardTestDatabaseName_Rejects_Names_Without_Test_Marker(string name)
    {
        Assert.Throws<InvalidOperationException>(() =>
            MongoIntegrationTestConfig.GuardTestDatabaseName(name));
    }

    [Fact]
    public void GuardTestDatabaseName_Allows_Explicit_Test_Database()
    {
        var allowed = MongoIntegrationTestConfig.GuardTestDatabaseName("suwani_test_abc");
        Assert.Equal("suwani_test_abc", allowed);
    }

    [Fact]
    public void CreateUniqueTestDatabaseName_Stays_Within_Mongo_Limit()
    {
        var name = MongoIntegrationTestConfig.CreateUniqueTestDatabaseName();
        Assert.True(name.Length <= 38, $"Database name length {name.Length} exceeds 38.");
        Assert.Contains("test", name, StringComparison.OrdinalIgnoreCase);
    }
}
