using System.Security.Authentication;
using Microsoft.Extensions.Options;
using MongoDB.Driver;
using MicrogridApi.Models;
using MicrogridApi.Settings;

namespace MicrogridApi.Data;

public class MongoDbContext
{
    private readonly IMongoDatabase _database;

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

    public IMongoCollection<User> Users =>
        _database.GetCollection<User>("users");

    public IMongoCollection<Prosumer> Prosumers =>
        _database.GetCollection<Prosumer>("prosumers");

    public IMongoCollection<MicrogridNode> MicrogridNodes =>
        _database.GetCollection<MicrogridNode>("SolarStationInfo");

    // Shared booking collection name from team plan. Suwani seed writes compatible docs only.
    public IMongoCollection<EnergyBookingSlot> EnergyBookingSlots =>
        _database.GetCollection<EnergyBookingSlot>("energyBookingSlots");

    // Shared booking collection name from team plan. Suwani reads/updates for QR/transfer only.
    public IMongoCollection<EnergyReservation> EnergyReservations =>
        _database.GetCollection<EnergyReservation>("energyReservation");
}