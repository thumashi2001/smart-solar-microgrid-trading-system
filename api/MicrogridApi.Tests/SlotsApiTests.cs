using System;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using MicrogridApi.Dtos;
using MicrogridApi.Models;
using MongoDB.Driver;
using Xunit;

namespace MicrogridApi.Tests.Integration
{
    // API/Integration tests using WebApplicationFactory
    // Default Mongo target (no env): mongodb://localhost:27017 + unique *test* DB name.
    // Atlas override: set MICROGRID_TEST_MONGODB_URI (required for Atlas) and optional
    // MICROGRID_TEST_MONGODB_DATABASE. Shared microgrid_db is always rejected.
    [Trait("Category", "Integration")]
    public class SlotsApiTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly HttpClient _client;
        private readonly string _connectionString;
        private readonly string _testDbName;
        private IMongoDatabase? _database;

        public SlotsApiTests(WebApplicationFactory<Program> factory)
        {
            _connectionString = MongoIntegrationTestConfig.ResolveConnectionString();
            _testDbName = MongoIntegrationTestConfig.CreateUniqueTestDatabaseName();

            var configured = factory.WithWebHostBuilder(builder =>
            {
                builder.UseSetting("Environment", "Development");
                builder.ConfigureTestServices(services =>
                {
                    MongoIntegrationTestConfig.ConfigureIsolatedMongo(
                        services,
                        _connectionString,
                        _testDbName);
                });
            });

            _client = configured.CreateClient();
        }

        public async Task InitializeAsync()
        {
            _database = MongoIntegrationTestConfig.Connect(_connectionString, _testDbName);

            // Clear collections for clean state (test DB only).
            await _database.DropCollectionAsync("energyBookingSlots");
            await _database.DropCollectionAsync("energyReservation");
        }

        public async Task DisposeAsync()
        {
            // Drop only this run's unique test database.
            await MongoIntegrationTestConfig.DropTestDatabaseAsync(_connectionString, _testDbName);
        }

        private async Task<EnergyBookingSlot> SeedSlotAsync(string status = "Available", int capacity = 5)
        {
            var slot = new EnergyBookingSlot
            {
                SlotId = $"SLOT-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                StationId = "ST-TEST",
                Date = DateTime.UtcNow.Date,
                StartTime = "09:00",
                EndTime = "10:00",
                Capacity = capacity,
                Availability = capacity,
                Status = status
            };

            var collection = _database!.GetCollection<EnergyBookingSlot>("energyBookingSlots");
            await collection.InsertOneAsync(slot);
            return slot;
        }

        private async Task SeedReservationAsync(string slotId, string status)
        {
            var res = new EnergyReservation
            {
                ReservationId = $"RES-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                SlotId = slotId,
                StationId = "ST-TEST",
                Status = status
            };
            var collection = _database!.GetCollection<EnergyReservation>("energyReservation");
            await collection.InsertOneAsync(res);
        }

        // IT-01 & API-01: Update slot successfully (Valid complete update)
        [Fact]
        public async Task Put_ValidCompleteUpdate_Returns200AndUpdatesDatabase()
        {
            var slot = await SeedSlotAsync();
            var req = new UpdateSlotRequest
            {
                Date = DateTime.UtcNow.AddDays(1).Date,
                StartTime = "11:00",
                EndTime = "12:00",
                Status = "Unavailable"
            };

            var response = await _client.PutAsJsonAsync($"/api/slots/{slot.Id}", req);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify Database
            var collection = _database!.GetCollection<EnergyBookingSlot>("energyBookingSlots");
            var updatedSlot = await collection.Find(s => s.Id == slot.Id).FirstOrDefaultAsync();

            Assert.NotNull(updatedSlot);
            Assert.Equal(req.Date, updatedSlot.Date);
            Assert.Equal("11:00", updatedSlot.StartTime);
            Assert.Equal("12:00", updatedSlot.EndTime);
            Assert.Equal("Unavailable", updatedSlot.Status);
        }

        // IT-03 & API-10: Pending reservation protection
        [Fact]
        public async Task Put_WithPendingReservation_ChangingDate_Returns400()
        {
            var slot = await SeedSlotAsync();
            await SeedReservationAsync(slot.SlotId, "Pending");

            var req = new UpdateSlotRequest { Date = DateTime.UtcNow.AddDays(2).Date };

            var response = await _client.PutAsJsonAsync($"/api/slots/{slot.Id}", req);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var collection = _database!.GetCollection<EnergyBookingSlot>("energyBookingSlots");
            var unchangedSlot = await collection.Find(s => s.Id == slot.Id).FirstOrDefaultAsync();
            Assert.Equal(slot.Date, unchangedSlot.Date); // Original date remains unchanged
        }

        // IT-04 & API-14: Approved reservation protection
        [Fact]
        public async Task Put_WithApprovedReservation_ChangingStartTime_Returns400()
        {
            var slot = await SeedSlotAsync();
            await SeedReservationAsync(slot.SlotId, "Approved");

            var req = new UpdateSlotRequest { StartTime = "08:00" };

            var response = await _client.PutAsJsonAsync($"/api/slots/{slot.Id}", req);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // IT-06 & API-18: Cancelled reservation allows update
        [Fact]
        public async Task Put_WithCancelledReservation_ChangingDate_Returns200()
        {
            var slot = await SeedSlotAsync();
            await SeedReservationAsync(slot.SlotId, "Cancelled"); // Inactive

            var newDate = DateTime.UtcNow.AddDays(2).Date;
            var req = new UpdateSlotRequest { Date = newDate };

            var response = await _client.PutAsJsonAsync($"/api/slots/{slot.Id}", req);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            var collection = _database!.GetCollection<EnergyBookingSlot>("energyBookingSlots");
            var updatedSlot = await collection.Find(s => s.Id == slot.Id).FirstOrDefaultAsync();
            Assert.Equal(newDate, updatedSlot.Date);
        }

        // API-07: EndTime before StartTime
        [Fact]
        public async Task Put_EndTimeBeforeStartTime_Returns400()
        {
            var slot = await SeedSlotAsync();
            var req = new UpdateSlotRequest { StartTime = "15:00", EndTime = "14:00" };

            var response = await _client.PutAsJsonAsync($"/api/slots/{slot.Id}", req);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // API-09: Non-existent SlotId
        [Fact]
        public async Task Put_NonExistentSlotId_Returns404()
        {
            var req = new UpdateSlotRequest { Status = "Unavailable" };
            var response = await _client.PutAsJsonAsync($"/api/slots/64c8d5f3b1abcdef12345678", req);

            Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        }
    }
}
