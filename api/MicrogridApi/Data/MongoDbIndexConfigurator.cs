// ============================================================================
// File: MongoDbIndexConfigurator.cs
// Component: Component 2 - Energy Reservation & Slot Management
// Description: Configures database-level unique indexes for EnergyBookingSlot
//              and EnergyReservation collections to guarantee ID uniqueness.
// ============================================================================

using MongoDB.Driver;
using MicrogridApi.Models;

namespace MicrogridApi.Data;

public static class MongoDbIndexConfigurator
{
    /// <summary>
    /// Indicates whether database unique indexes have been successfully created or verified.
    /// Acts as a startup and write readiness gate to ensure unverified states fail closed.
    /// </summary>
    public static bool IndexesVerified { get; private set; } = false;

    /// <summary>
    /// Resets or overrides verification state for isolated unit testing.
    /// </summary>
    public static void ResetVerificationStateForTesting(bool verified = false)
    {
        IndexesVerified = verified;
    }

    /// <summary>
    /// Ensures unique indexes are verified before write operations proceed.
    /// Returns true if verified, or false if database is unreachable.
    /// </summary>
    public static async Task<bool> EnsureIndexesVerifiedAsync(MongoDbContext db)
    {
        if (IndexesVerified) return true;

        try
        {
            await ConfigureIndexesAsync(db.EnergyBookingSlots, db.EnergyReservations);
            IndexesVerified = true;
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Configures unique indexes on SlotId and ReservationId via MongoDbContext.
    /// Must be awaited during application startup before accepting traffic.
    /// If MongoDB is offline during local development, a diagnostic notice is logged.
    /// If MongoDB is connected and index creation fails, an exception is thrown to halt startup.
    /// </summary>
    public static async Task ConfigureIndexesAsync(MongoDbContext db, bool isDevelopment = false)
    {
        try
        {
            await ConfigureIndexesAsync(db.EnergyBookingSlots, db.EnergyReservations);
        }
        catch (MongoConnectionException ex) when (isDevelopment)
        {
            // In local offline development/testing, MongoDB instance may not be running yet.
            // A clear diagnostic notice is logged so offline test tools and builds do not crash.
            Console.WriteLine($"[MongoDbIndexConfigurator] Notice: MongoDB is offline ({ex.Message}). Startup unique index creation skipped.");
        }
        catch (TimeoutException ex) when (isDevelopment)
        {
            Console.WriteLine($"[MongoDbIndexConfigurator] Notice: MongoDB connection timed out ({ex.Message}). Startup unique index creation skipped.");
        }
        catch (Exception ex)
        {
            // If MongoDB is connected and index creation fails (e.g. duplicate existing data, invalid options),
            // or if in non-development environment, do NOT swallow the failure!
            Console.Error.WriteLine($"[MongoDbIndexConfigurator] ERROR: Failed to configure database unique indexes: {ex.Message}");
            throw new InvalidOperationException($"Critical startup failure: Failed to configure required database unique indexes for Component 2: {ex.Message}", ex);
        }
    }

    /// <summary>
    /// Configures unique indexes on SlotId and ReservationId via an IMongoDatabase instance.
    /// Used by integration test suites with isolated test databases.
    /// </summary>
    public static async Task ConfigureIndexesAsync(IMongoDatabase database)
    {
        var slotsCollection = database.GetCollection<EnergyBookingSlot>("energyBookingSlots");
        var reservationsCollection = database.GetCollection<EnergyReservation>("energyReservation");
        await ConfigureIndexesAsync(slotsCollection, reservationsCollection);
    }

    /// <summary>
    /// Core configuration method that creates unique indexes on target collections.
    /// </summary>
    public static async Task ConfigureIndexesAsync(
        IMongoCollection<EnergyBookingSlot> slotsCollection,
        IMongoCollection<EnergyReservation> reservationsCollection)
    {
        // Unique index on SlotId
        var slotIndexModel = new CreateIndexModel<EnergyBookingSlot>(
            Builders<EnergyBookingSlot>.IndexKeys.Ascending(s => s.SlotId),
            new CreateIndexOptions { Unique = true, Name = "ux_slotId" }
        );
        await slotsCollection.Indexes.CreateOneAsync(slotIndexModel);

        // Unique index on ReservationId
        var resIndexModel = new CreateIndexModel<EnergyReservation>(
            Builders<EnergyReservation>.IndexKeys.Ascending(r => r.ReservationId),
            new CreateIndexOptions { Unique = true, Name = "ux_reservationId" }
        );
        await reservationsCollection.Indexes.CreateOneAsync(resIndexModel);
        IndexesVerified = true;
    }
}
