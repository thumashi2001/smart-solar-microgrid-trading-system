using System;
using System.Collections.Generic;
using System.Net;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MicrogridApi.Data;
using MicrogridApi.Dtos;
using MicrogridApi.Models;
using MicrogridApi.Settings;
using MongoDB.Driver;
using Xunit;

namespace MicrogridApi.Tests.Integration
{
    // API/Integration tests for ReservationsController using WebApplicationFactory and isolated MongoDB
    [Trait("Category", "Integration")]
    public class ReservationsApiTests : IClassFixture<WebApplicationFactory<Program>>, IAsyncLifetime
    {
        private readonly HttpClient _client;
        private readonly WebApplicationFactory<Program> _factory;
        private IMongoDatabase? _database;
        private readonly string _testDbName = $"microgrid_test_{Guid.NewGuid():N}";

        public ReservationsApiTests(WebApplicationFactory<Program> factory)
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

        private async Task SeedProsumerAsync(string nic = "123456789V", string status = "active")
        {
            var collection = _database!.GetCollection<Prosumer>("prosumers");
            await collection.InsertOneAsync(new Prosumer { Nic = nic, Status = status });
        }

        private async Task SeedStationAsync(string stationId = "ST-TEST", string status = "active")
        {
            var collection = _database!.GetCollection<MicrogridNode>("SolarStationInfo");
            await collection.InsertOneAsync(new MicrogridNode { NodeId = stationId, Status = status });
        }

        private async Task<EnergyBookingSlot> SeedSlotAsync(
            string slotId,
            string stationId = "ST-TEST",
            int capacity = 5,
            int availability = 5,
            string status = "Available",
            DateTime? date = null,
            string startTime = "14:00",
            string endTime = "15:00")
        {
            var slot = new EnergyBookingSlot
            {
                SlotId = slotId,
                StationId = stationId,
                Date = date ?? DateTime.UtcNow.AddDays(1).Date,
                StartTime = startTime,
                EndTime = endTime,
                Capacity = capacity,
                Availability = availability,
                Status = status,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            var collection = _database!.GetCollection<EnergyBookingSlot>("energyBookingSlots");
            await collection.InsertOneAsync(slot);
            return slot;
        }

        private async Task<EnergyReservation> SeedReservationAsync(
            string slotId,
            string stationId = "ST-TEST",
            string prosumerNic = "123456789V",
            string status = "Pending",
            DateTime? updatedAt = null)
        {
            var res = new EnergyReservation
            {
                ReservationId = $"RES-{Guid.NewGuid().ToString("N")[..8].ToUpper()}",
                SlotId = slotId,
                StationId = stationId,
                ProsumerNic = prosumerNic,
                Status = status,
                CreatedAt = DateTime.UtcNow.AddHours(-1),
                UpdatedAt = updatedAt ?? DateTime.UtcNow.AddHours(-1)
            };

            var collection = _database!.GetCollection<EnergyReservation>("energyReservation");
            await collection.InsertOneAsync(res);
            return res;
        }

        // IT-RES-01: Create reservation successfully decrements availability and returns 201 Created
        [Fact]
        public async Task Post_ValidReservation_CreatesReservationAndDecrementsSlotAvailability()
        {
            await SeedProsumerAsync();
            await SeedStationAsync();
            var slot = await SeedSlotAsync("SLOT-CREATE-01", capacity: 5, availability: 5);

            var req = new CreateReservationRequest
            {
                ProsumerNic = "123456789V",
                StationId = slot.StationId,
                SlotId = slot.SlotId
            };

            var response = await _client.PostAsJsonAsync("/api/reservations", req);

            Assert.Equal(HttpStatusCode.Created, response.StatusCode);

            // Verify Reservation in Database
            var resCollection = _database!.GetCollection<EnergyReservation>("energyReservation");
            var reservation = await resCollection.Find(r => r.SlotId == slot.SlotId).FirstOrDefaultAsync();

            Assert.NotNull(reservation);
            Assert.Equal("Pending", reservation.Status);
            Assert.StartsWith("RES-", reservation.ReservationId);
            Assert.Equal("123456789V", reservation.ProsumerNic);

            // Verify Slot availability decremented from 5 to 4
            var slotCollection = _database!.GetCollection<EnergyBookingSlot>("energyBookingSlots");
            var updatedSlot = await slotCollection.Find(s => s.SlotId == slot.SlotId).FirstOrDefaultAsync();

            Assert.NotNull(updatedSlot);
            Assert.Equal(4, updatedSlot.Availability);
        }

        // IT-RES-02: Create reservation on full slot returns 400 BadRequest
        [Fact]
        public async Task Post_ZeroAvailabilitySlot_Returns400_DoesNotCreateReservation()
        {
            await SeedProsumerAsync();
            await SeedStationAsync();
            var slot = await SeedSlotAsync("SLOT-ZERO-AVAIL", capacity: 5, availability: 0);

            var req = new CreateReservationRequest
            {
                ProsumerNic = "123456789V",
                StationId = slot.StationId,
                SlotId = slot.SlotId
            };

            var response = await _client.PostAsJsonAsync("/api/reservations", req);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            var resCollection = _database!.GetCollection<EnergyReservation>("energyReservation");
            var count = await resCollection.CountDocumentsAsync(r => r.SlotId == slot.SlotId);
            Assert.Equal(0, count);
        }

        // IT-RES-03: Create reservation with invalid NIC format returns 400 BadRequest
        [Fact]
        public async Task Post_InvalidNicFormat_Returns400()
        {
            await SeedProsumerAsync("INVALID-NIC-FORMAT");
            await SeedStationAsync();
            var slot = await SeedSlotAsync("SLOT-INVALID-NIC", capacity: 5, availability: 5);

            var req = new CreateReservationRequest
            {
                ProsumerNic = "INVALID-NIC-FORMAT",
                StationId = slot.StationId,
                SlotId = slot.SlotId
            };

            var response = await _client.PostAsJsonAsync("/api/reservations", req);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // IT-RES-04: Update reservation to a new slot safely exchanges availability
        [Fact]
        public async Task Put_UpdateReservationToNewSlot_ExchangesAvailabilityCorrectly()
        {
            var oldSlot = await SeedSlotAsync("SLOT-OLD-01", capacity: 5, availability: 4);
            var newSlot = await SeedSlotAsync("SLOT-NEW-01", capacity: 5, availability: 5, startTime: "16:00", endTime: "17:00");
            var reservation = await SeedReservationAsync(oldSlot.SlotId, stationId: oldSlot.StationId, status: "Pending");

            var req = new UpdateReservationRequest
            {
                StationId = newSlot.StationId,
                SlotId = newSlot.SlotId
            };

            var response = await _client.PutAsJsonAsync($"/api/reservations/{reservation.Id}", req);

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify Reservation Document
            var resCollection = _database!.GetCollection<EnergyReservation>("energyReservation");
            var updatedRes = await resCollection.Find(r => r.Id == reservation.Id).FirstOrDefaultAsync();

            Assert.NotNull(updatedRes);
            Assert.Equal(newSlot.SlotId, updatedRes.SlotId);

            // Verify Slot Availability Exchange
            var slotCollection = _database!.GetCollection<EnergyBookingSlot>("energyBookingSlots");
            var oldSlotInDb = await slotCollection.Find(s => s.SlotId == oldSlot.SlotId).FirstOrDefaultAsync();
            var newSlotInDb = await slotCollection.Find(s => s.SlotId == newSlot.SlotId).FirstOrDefaultAsync();

            Assert.NotNull(oldSlotInDb);
            Assert.NotNull(newSlotInDb);
            Assert.Equal(5, oldSlotInDb.Availability); // Old slot restored: 4 -> 5
            Assert.Equal(4, newSlotInDb.Availability); // New slot decremented: 5 -> 4
        }

        // IT-RES-05: Update completed or cancelled reservation returns 400 BadRequest
        [Fact]
        public async Task Put_CompletedOrCancelledReservation_Returns400()
        {
            var slot = await SeedSlotAsync("SLOT-COMPLETED-RES", capacity: 5, availability: 4);
            var completedRes = await SeedReservationAsync(slot.SlotId, status: "Completed");

            var req = new UpdateReservationRequest
            {
                StationId = slot.StationId,
                SlotId = slot.SlotId
            };

            var response = await _client.PutAsJsonAsync($"/api/reservations/{completedRes.Id}", req);

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        }

        // IT-RES-06: Delete cancels reservation and restores slot availability when notice >= 12h
        [Fact]
        public async Task Delete_ValidReservationWithSufficientNotice_CancelsAndRestoresAvailability()
        {
            // Slot starts 48 hours from now (far exceeding 12 hours)
            var slot = await SeedSlotAsync(
                "SLOT-CANCEL-01",
                capacity: 5,
                availability: 4,
                date: DateTime.UtcNow.AddDays(2).Date,
                startTime: "10:00",
                endTime: "11:00"
            );
            var reservation = await SeedReservationAsync(slot.SlotId, status: "Pending");

            var response = await _client.DeleteAsync($"/api/reservations/{reservation.Id}");

            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // Verify Reservation Document is Cancelled
            var resCollection = _database!.GetCollection<EnergyReservation>("energyReservation");
            var cancelledRes = await resCollection.Find(r => r.Id == reservation.Id).FirstOrDefaultAsync();

            Assert.NotNull(cancelledRes);
            Assert.Equal("Cancelled", cancelledRes.Status);

            // Verify Slot Availability restored: 4 -> 5
            var slotCollection = _database!.GetCollection<EnergyBookingSlot>("energyBookingSlots");
            var restoredSlot = await slotCollection.Find(s => s.SlotId == slot.SlotId).FirstOrDefaultAsync();

            Assert.NotNull(restoredSlot);
            Assert.Equal(5, restoredSlot.Availability);
        }

        // IT-RES-07: Delete with less than 12 hours' notice returns 400 BadRequest
        [Fact]
        public async Task Delete_LessThan12HoursNotice_Returns400()
        {
            // Slot starts in 2 hours
            var targetTime = DateTime.UtcNow.AddHours(2);
            var slot = await SeedSlotAsync(
                "SLOT-CANCEL-SHORT-NOTICE",
                capacity: 5,
                availability: 4,
                date: targetTime.Date,
                startTime: targetTime.ToString("HH:mm"),
                endTime: targetTime.AddHours(1).ToString("HH:mm")
            );
            var reservation = await SeedReservationAsync(slot.SlotId, status: "Pending");

            var response = await _client.DeleteAsync($"/api/reservations/{reservation.Id}");

            Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);

            // Verify Reservation Document is NOT cancelled
            var resCollection = _database!.GetCollection<EnergyReservation>("energyReservation");
            var unchangedRes = await resCollection.Find(r => r.Id == reservation.Id).FirstOrDefaultAsync();

            Assert.NotNull(unchangedRes);
            Assert.Equal("Pending", unchangedRes.Status);
        }

        // IT-RES-08: Unique index prevents duplicate ReservationId values
        [Fact]
        public async Task UniqueIndex_DuplicateReservationId_ThrowsMongoWriteException()
        {
            var duplicateResId = "RES-UNIQUE-COLLISION-TEST";
            var res1 = new EnergyReservation
            {
                ReservationId = duplicateResId,
                SlotId = "SLOT-01",
                StationId = "ST-TEST",
                ProsumerNic = "123456789V",
                Status = "Pending"
            };
            var res2 = new EnergyReservation
            {
                ReservationId = duplicateResId,
                SlotId = "SLOT-02",
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
