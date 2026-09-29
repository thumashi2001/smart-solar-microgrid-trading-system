// =============================================================================
// File: MongoDbContext.cs
// Description: Central MongoDB connection and typed collection accessors used
//              by every controller (Users, Prosumers, MicrogridNodes, Slots,
//              Reservations).
// Author: Thumashi (Component 1)
// =============================================================================

using System.Security.Authentication;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MicrogridApi.Models;
using MicrogridApi.Settings;

namespace MicrogridApi.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

    // Builds the MongoDB client from the connection string and opens the database.
    public MongoDbContext(IOptions<MongoDbSettings> settings)
    {
        var mongoSettings =
            MongoClientSettings.FromConnectionString(
                settings.Value.ConnectionString
            );

        mongoSettings.SslSettings = new SslSettings
        {
            CheckCertificateRevocation = false,
            EnabledSslProtocols = SslProtocols.Tls12
        };

        var client = new MongoClient(mongoSettings);

        _database = client.GetDatabase(
            settings.Value.DatabaseName
        );
    }

    // Backoffice and Grid Operator user accounts.
    public IMongoCollection<User> Users =>
        _database.GetCollection<User>("users");

    // Prosumer accounts, keyed by NIC.
    public IMongoCollection<Prosumer> Prosumers =>
        _database.GetCollection<Prosumer>("prosumers");

    // Microgrid solar station records.
    public IMongoCollection<MicrogridNode> MicrogridNodes =>
        _database.GetCollection<MicrogridNode>("SolarStationInfo");

    // Available energy booking slots.
    public IMongoCollection<EnergyBookingSlot> EnergyBookingSlots =>
        _database.GetCollection<EnergyBookingSlot>("energyBookingSlots");

    // Prosumer reservations against booking slots.
    public IMongoCollection<EnergyReservation> EnergyReservations =>
        _database.GetCollection<EnergyReservation>("energyReservation");
}