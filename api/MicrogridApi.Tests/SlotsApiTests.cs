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
        private const string TestDbName = "microgrid_test_db";

        public SlotsApiTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory.WithWebHostBuilder(builder =>
            {
                builder.ConfigureTestServices(services =>
                {
                    // Override the MongoDB settings to point to a local test database
                    services.Configure<MongoDbSettings>(options =>
                    {
                        options.ConnectionString = "mongodb://localhost:27017";
                        options.DatabaseName = TestDbName;
                    });
                });
            });

            _client = _factory.CreateClient();
        }

        public async Task InitializeAsync()
        {
            // Set up test database connection before tests run
            var client = new MongoClient("mongodb://localhost:27017");
            _database = client.GetDatabase(TestDbName);

            // Clear collections for clean state
            await _database.DropCollectionAsync("energyBookingSlots");
            await _database.DropCollectionAsync("energyReservation");
        }

        public Task DisposeAsync()
        {
            // Cleanup test data
            return Task.CompletedTask;
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

            var collection = _database.GetCollection<EnergyBookingSlot>("energyBookingSlots");
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
            var collection = _database.GetCollection<EnergyReservation>("energyReservation");
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
            var collection = _database.GetCollection<EnergyBookingSlot>("energyBookingSlots");
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

            var collection = _database.GetCollection<EnergyBookingSlot>("energyBookingSlots");
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
            
            var collection = _database.GetCollection<EnergyBookingSlot>("energyBookingSlots");
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
