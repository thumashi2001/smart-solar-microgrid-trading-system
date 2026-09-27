// =============================================================================
// File: MongoIntegrationTestConfig.cs
// Description: Resolves Mongo settings for integration tests.
//              Default remains teammate localhost; Atlas requires explicit env URI.
// Author: Suwani (Component 4 integration)
// =============================================================================

using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Microsoft.Extensions.Options;
using MicrogridApi.Data;
using MicrogridApi.Settings;
using MongoDB.Driver;

namespace MicrogridApi.Tests.Integration;

internal static class MongoIntegrationTestConfig
{
    public const string UriEnvironmentVariable = "MICROGRID_TEST_MONGODB_URI";
    public const string DatabaseEnvironmentVariable = "MICROGRID_TEST_MONGODB_DATABASE";

    // Teammate default from origin/dev SlotsApiTests.
    public const string DefaultLocalUri = "mongodb://localhost:27017";

    private static readonly HashSet<string> ForbiddenDatabaseNames = new(StringComparer.OrdinalIgnoreCase)
    {
        "microgrid_db",
        "admin",
        "local",
        "config"
    };

    public static string ResolveConnectionString()
    {
        var configured = Environment.GetEnvironmentVariable(UriEnvironmentVariable);
        if (!string.IsNullOrWhiteSpace(configured))
        {
            return configured.Trim();
        }

        return DefaultLocalUri;
    }

    public static string CreateUniqueTestDatabaseName()
    {
        var prefix = Environment.GetEnvironmentVariable(DatabaseEnvironmentVariable);
        if (string.IsNullOrWhiteSpace(prefix))
        {
            // Keep well under MongoDB's 38-byte database name limit; must contain "test".
            prefix = "suwani_test";
        }

        prefix = prefix.Trim();
        // 11 + 1 + 12 = 24 bytes with default prefix.
        var suffix = Guid.NewGuid().ToString("N")[..12];
        var candidate = $"{prefix}_{suffix}";
        if (candidate.Length > 38)
        {
            var maxPrefix = Math.Max(1, 38 - 1 - suffix.Length);
            candidate = $"{prefix[..Math.Min(prefix.Length, maxPrefix)]}_{suffix}";
        }

        return GuardTestDatabaseName(candidate);
    }

    public static string GuardTestDatabaseName(string databaseName)
    {
        if (string.IsNullOrWhiteSpace(databaseName))
        {
            throw new InvalidOperationException("Test database name is required.");
        }

        if (ForbiddenDatabaseNames.Contains(databaseName))
        {
            throw new InvalidOperationException(
                $"Refusing to use forbidden database '{databaseName}' for integration tests.");
        }

        // Require an explicit test marker so shared app DBs cannot be selected by accident.
        if (databaseName.IndexOf("test", StringComparison.OrdinalIgnoreCase) < 0)
        {
            throw new InvalidOperationException(
                $"Refusing database '{databaseName}': name must contain 'test'.");
        }

        return databaseName;
    }

    public static void ConfigureIsolatedMongo(
        IServiceCollection services,
        string connectionString,
        string databaseName)
    {
        GuardTestDatabaseName(databaseName);

        // Ensure the app under test cannot keep the singleton built against microgrid_db.
        services.RemoveAll<MongoDbContext>();
        services.RemoveAll<IOptions<MongoDbSettings>>();
        services.RemoveAll<IOptionsSnapshot<MongoDbSettings>>();
        services.RemoveAll<IOptionsMonitor<MongoDbSettings>>();

        services.Configure<MongoDbSettings>(options =>
        {
            options.ConnectionString = connectionString;
            options.DatabaseName = databaseName;
        });

        services.AddSingleton<MongoDbContext>();
    }

    public static IMongoDatabase Connect(string connectionString, string databaseName)
    {
        GuardTestDatabaseName(databaseName);
        var client = new MongoClient(connectionString);
        return client.GetDatabase(databaseName);
    }

    public static async System.Threading.Tasks.Task DropTestDatabaseAsync(
        string connectionString,
        string databaseName)
    {
        GuardTestDatabaseName(databaseName);
        var client = new MongoClient(connectionString);
        var names = await client.ListDatabaseNamesAsync();
        var existing = await names.ToListAsync();
        if (existing.Any(n => string.Equals(n, databaseName, StringComparison.Ordinal)))
        {
            await client.DropDatabaseAsync(databaseName);
        }
    }
}
