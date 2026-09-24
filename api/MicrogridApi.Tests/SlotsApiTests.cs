using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using MicrogridApi.Data;
using MicrogridApi.Dtos;
using MicrogridApi.Models;
using MicrogridApi.Settings;
using MongoDB.Driver;
using Xunit;

namespace MicrogridApi.Tests.Integration
{
    // API/Integration tests using WebApplicationFactory
    [Trait("Category", "Integration")]
    public class SlotsApiTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;
        private IMongoDatabase? _database;
        private readonly string _testDbName = $"microgrid_test_{Guid.NewGuid():N}";

        public SlotsApiTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Override the MongoDB settings to point to an isolated local test database
                    services.Configure<MongoDbSettings>(options =>
                    {
                        options.ConnectionString = "mongodb://localhost:27017";
                        options.DatabaseName = _testDbName;
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            try
            {
                var settings = MongoClientSettings.FromConnectionString("mongodb://localhost:27017");
                settings.ServerSelectionTimeout = TimeSpan.FromSeconds(2);
                var client = new MongoClient(settings);

                // Explicitly verify MongoDB connectivity before running integration tests
                await client.GetDatabase("admin").RunCommandAsync<MongoDB.Bson.BsonDocument>(new MongoDB.Bson.BsonDocument("ping", 1));

                _database = client.GetDatabase(_testDbName);

                // Clear collections for clean isolated test state
                await _database.DropCollectionAsync("energyBookingSlots");
                await _database.DropCollectionAsync("energyReservation");

                // Configure database-level unique indexes
                await MongoDbIndexConfigurator.ConfigureIndexesAsync(_database);
            }
            catch (Exception ex)
            {
                throw new InvalidOperationException(
                    "Integration test prerequisite missing: Local MongoDB instance is not reachable at mongodb://localhost:27017. " +
                    "Integration tests require an active MongoDB server. Ensure MongoDB is running before executing integration tests.", ex);
            }
        }

        public async Task DisposeAsync()
        {
            // Drop isolated test database after test run
            if (_database != null)
            {
                try
                {
                    var client = new MongoClient("mongodb://localhost:27017");
                    await client.DropDatabaseAsync(_testDbName);
                }
                catch
                {
                    // Ignore dispose cleanup errors
                }
            }
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

        // DB-UX-01: Unique index prevents duplicate SlotId values
        [Fact]
        public async Task UniqueIndex_DuplicateSlotId_ThrowsMongoWriteException()
        {
            var duplicateSlotId = "SLOT-UNIQUE-TEST";
            var slot1 = new EnergyBookingSlot
            {
                SlotId = duplicateSlotId,
                StationId = "ST-TEST",
                Date = DateTime.UtcNow.Date,
                StartTime = "09:00",
                EndTime = "10:00",
                Capacity = 5,
                Availability = 5,
                Status = "Available"
            };
            var slot2 = new EnergyBookingSlot
            {
                SlotId = duplicateSlotId,
                StationId = "ST-TEST",
                Date = DateTime.UtcNow.Date,
                StartTime = "10:00",
                EndTime = "11:00",
                Capacity = 5,
                Availability = 5,
                Status = "Available"
            };

            var collection = _database!.GetCollection<EnergyBookingSlot>("energyBookingSlots");
            await collection.InsertOneAsync(slot1);

            var ex = await Assert.ThrowsAsync<MongoWriteException>(() => collection.InsertOneAsync(slot2));
            Assert.Equal(ServerErrorCategory.DuplicateKey, ex.WriteError.Category);
        }

        // DB-UX-02: Unique index prevents duplicate ReservationId values
        [Fact]
        public async Task UniqueIndex_DuplicateReservationId_ThrowsMongoWriteException()
        {
            var duplicateReservationId = "RES-UNIQUE-TEST";
            var res1 = new EnergyReservation
            {
                ReservationId = duplicateReservationId,
                SlotId = "SLOT-1",
                StationId = "ST-TEST",
                ProsumerNic = "123456789V",
                Status = "Pending"
            };
            var res2 = new EnergyReservation
            {
                ReservationId = duplicateReservationId,
                SlotId = "SLOT-2",
                StationId = "ST-TEST",
                ProsumerNic = "987654321V",
                Status = "Pending"
            };

            var collection = _database!.GetCollection<EnergyReservation>("energyReservation");
            await collection.InsertOneAsync(res1);

            var ex = await Assert.ThrowsAsync<MongoWriteException>(() => collection.InsertOneAsync(res2));
            Assert.Equal(ServerErrorCategory.DuplicateKey, ex.WriteError.Category);
        }
    }
}
