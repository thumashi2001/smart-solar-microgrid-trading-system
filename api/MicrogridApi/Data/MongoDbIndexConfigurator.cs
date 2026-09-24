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
    /// Configures unique indexes on SlotId and ReservationId via MongoDbContext.
    /// Safe to call during application startup; catches connection exceptions if MongoDB is offline.
    /// </summary>
    public static async Task ConfigureIndexesAsync(MongoDbContext db)
    {
        try
        {
            await ConfigureIndexesAsync(db.EnergyBookingSlots, db.EnergyReservations);
        }
        catch
        {
            // Catch and suppress errors if MongoDB is offline during startup
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
    }
}
