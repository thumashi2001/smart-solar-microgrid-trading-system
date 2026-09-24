using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using MicrogridApi.Controllers;
using MicrogridApi.Data;
using MicrogridApi.Dtos;
using MicrogridApi.Models;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace MicrogridApi.Tests.Unit
{
    [Trait("Category", "Unit")]
    public class ReservationsControllerTests
    {
        private readonly Mock<IMongoCollection<Prosumer>> _mockProsumersCollection;
        private readonly Mock<IMongoCollection<MicrogridNode>> _mockNodesCollection;
        private readonly Mock<IMongoCollection<EnergyBookingSlot>> _mockSlotsCollection;
        private readonly Mock<IMongoCollection<EnergyReservation>> _mockReservationsCollection;
        private readonly ReservationsController _controller;

        private readonly List<Prosumer> _prosumers = new();
        private readonly List<MicrogridNode> _stations = new();
        private readonly List<EnergyBookingSlot> _slots = new();
        private readonly List<EnergyReservation> _reservations = new();

        public ReservationsControllerTests()
        {
            _mockProsumersCollection = new Mock<IMongoCollection<Prosumer>>();
            _mockNodesCollection = new Mock<IMongoCollection<MicrogridNode>>();
            _mockSlotsCollection = new Mock<IMongoCollection<EnergyBookingSlot>>();
            _mockReservationsCollection = new Mock<IMongoCollection<EnergyReservation>>();

            var mockDatabase = new Mock<IMongoDatabase>();
            mockDatabase
                .Setup(d => d.GetCollection<Prosumer>("prosumers", It.IsAny<MongoCollectionSettings>()))
                .Returns(_mockProsumersCollection.Object);
            mockDatabase
                .Setup(d => d.GetCollection<MicrogridNode>("SolarStationInfo", It.IsAny<MongoCollectionSettings>()))
                .Returns(_mockNodesCollection.Object);
            mockDatabase
                .Setup(d => d.GetCollection<EnergyBookingSlot>("energyBookingSlots", It.IsAny<MongoCollectionSettings>()))
                .Returns(_mockSlotsCollection.Object);
            mockDatabase
                .Setup(d => d.GetCollection<EnergyReservation>("energyReservation", It.IsAny<MongoCollectionSettings>()))
                .Returns(_mockReservationsCollection.Object);

            var dbContext = (MongoDbContext)RuntimeHelpers.GetUninitializedObject(typeof(MongoDbContext));
            var dbField = typeof(MongoDbContext).GetField("_database", BindingFlags.NonPublic | BindingFlags.Instance);
            if (dbField == null)
            {
                throw new InvalidOperationException("Private field '_database' not found in MongoDbContext.");
            }
            dbField.SetValue(dbContext, mockDatabase.Object);

            _controller = new ReservationsController(dbContext);

            SetupCollections();
        }

        private void SetupCollections()
        {
            // Prosumers FindAsync
            _mockProsumersCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<Prosumer>>(),
                    It.IsAny<FindOptions<Prosumer, Prosumer>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((FilterDefinition<Prosumer> filter, FindOptions<Prosumer, Prosumer> options, CancellationToken ct) =>
                {
                    var list = new List<Prosumer>();
                    if (filter is ExpressionFilterDefinition<Prosumer> exprFilter)
                    {
                        var pred = exprFilter.Expression.Compile();
                        list = _prosumers.Where(pred).ToList();
                    }
                    else
                    {
                        list = _prosumers.ToList();
                    }
                    var mockCursor = new Mock<IAsyncCursor<Prosumer>>();
                    mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                              .ReturnsAsync(list.Count > 0)
                              .ReturnsAsync(false);
                    mockCursor.Setup(c => c.Current).Returns(list);
                    return Task.FromResult(mockCursor.Object);
                });

            // MicrogridNodes FindAsync
            _mockNodesCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<MicrogridNode>>(),
                    It.IsAny<FindOptions<MicrogridNode, MicrogridNode>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((FilterDefinition<MicrogridNode> filter, FindOptions<MicrogridNode, MicrogridNode> options, CancellationToken ct) =>
                {
                    var list = new List<MicrogridNode>();
                    if (filter is ExpressionFilterDefinition<MicrogridNode> exprFilter)
                    {
                        var pred = exprFilter.Expression.Compile();
                        list = _stations.Where(pred).ToList();
                    }
                    else
                    {
                        list = _stations.ToList();
                    }
                    var mockCursor = new Mock<IAsyncCursor<MicrogridNode>>();
                    mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                              .ReturnsAsync(list.Count > 0)
                              .ReturnsAsync(false);
                    mockCursor.Setup(c => c.Current).Returns(list);
                    return Task.FromResult(mockCursor.Object);
                });

            // EnergyBookingSlots FindAsync
            _mockSlotsCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<EnergyBookingSlot>>(),
                    It.IsAny<FindOptions<EnergyBookingSlot, EnergyBookingSlot>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((FilterDefinition<EnergyBookingSlot> filter, FindOptions<EnergyBookingSlot, EnergyBookingSlot> options, CancellationToken ct) =>
                {
                    var list = new List<EnergyBookingSlot>();
                    if (filter is ExpressionFilterDefinition<EnergyBookingSlot> exprFilter)
                    {
                        var pred = exprFilter.Expression.Compile();
                        list = _slots.Where(pred).ToList();
                    }
                    else
                    {
                        list = _slots.ToList();
                    }
                    var mockCursor = new Mock<IAsyncCursor<EnergyBookingSlot>>();
                    mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                              .ReturnsAsync(list.Count > 0)
                              .ReturnsAsync(false);
                    mockCursor.Setup(c => c.Current).Returns(list);
                    return Task.FromResult(mockCursor.Object);
                });

            // EnergyBookingSlots FindOneAndUpdateAsync (used in Create and Modify to safely decrement availability)
            _mockSlotsCollection
                .Setup(c => c.FindOneAndUpdateAsync(
                    It.IsAny<FilterDefinition<EnergyBookingSlot>>(),
                    It.IsAny<UpdateDefinition<EnergyBookingSlot>>(),
                    It.IsAny<FindOneAndUpdateOptions<EnergyBookingSlot, EnergyBookingSlot>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((FilterDefinition<EnergyBookingSlot> filter, UpdateDefinition<EnergyBookingSlot> update, FindOneAndUpdateOptions<EnergyBookingSlot, EnergyBookingSlot> options, CancellationToken ct) =>
                {
                    var serializerRegistry = BsonSerializer.SerializerRegistry;
                    var documentSerializer = serializerRegistry.GetSerializer<EnergyBookingSlot>();
                    var rendered = filter.Render(new RenderArgs<EnergyBookingSlot>(documentSerializer, serializerRegistry));
                    
                    EnergyBookingSlot? target = null;
                    if (rendered.Contains("SlotId"))
                    {
                        var slotId = rendered["SlotId"].AsString;
                        target = _slots.FirstOrDefault(s => s.SlotId == slotId);
                    }
                    else if (rendered.Contains("$and"))
                    {
                        var andArray = rendered["$and"].AsBsonArray;
                        foreach (var item in andArray)
                        {
                            var doc = item.AsBsonDocument;
                            if (doc.Contains("SlotId"))
                            {
                                var slotId = doc["SlotId"].AsString;
                                target = _slots.FirstOrDefault(s => s.SlotId == slotId);
                                break;
                            }
                        }
                    }

                    if (target != null && target.Availability > 0 && target.Status == "Available")
                    {
                        target.Availability -= 1;
                        target.UpdatedAt = DateTime.UtcNow;
                        return Task.FromResult<EnergyBookingSlot>(target);
                    }

                    return Task.FromResult<EnergyBookingSlot>(null!);
                });

            // EnergyBookingSlots UpdateOneAsync (used in Restore Availability)
            _mockSlotsCollection
                .Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<EnergyBookingSlot>>(),
                    It.IsAny<UpdateDefinition<EnergyBookingSlot>>(),
                    It.IsAny<UpdateOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((FilterDefinition<EnergyBookingSlot> filter, UpdateDefinition<EnergyBookingSlot> update, UpdateOptions options, CancellationToken ct) =>
                {
                    var serializerRegistry = BsonSerializer.SerializerRegistry;
                    var documentSerializer = serializerRegistry.GetSerializer<EnergyBookingSlot>();
                    var rendered = filter.Render(new RenderArgs<EnergyBookingSlot>(documentSerializer, serializerRegistry));

                    EnergyBookingSlot? target = null;
                    bool checkLessThanCapacity = false;
                    if (rendered.Contains("SlotId"))
                    {
                        var slotId = rendered["SlotId"].AsString;
                        target = _slots.FirstOrDefault(s => s.SlotId == slotId);
                    }
                    else if (rendered.Contains("$and"))
                    {
                        var andArray = rendered["$and"].AsBsonArray;
                        foreach (var item in andArray)
                        {
                            var doc = item.AsBsonDocument;
                            if (doc.Contains("SlotId"))
                            {
                                var slotId = doc["SlotId"].AsString;
                                target = _slots.FirstOrDefault(s => s.SlotId == slotId);
                            }
                            if (doc.Contains("Availability") && doc["Availability"].AsBsonDocument.Contains("$lt"))
                            {
                                checkLessThanCapacity = true;
                            }
                        }
                    }

                    if (target != null)
                    {
                        if (checkLessThanCapacity && target.Availability >= target.Capacity)
                        {
                            // Did not modify because availability is already at capacity
                            return Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(0, 0, null));
                        }

                        target.Availability = Math.Min(target.Capacity, target.Availability + 1);
                        target.UpdatedAt = DateTime.UtcNow;
                        return Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(1, 1, null));
                    }

                    return Task.FromResult<UpdateResult>(new UpdateResult.Acknowledged(0, 0, null));
                });

            // EnergyReservations InsertOneAsync
            _mockReservationsCollection
                .Setup(c => c.InsertOneAsync(
                    It.IsAny<EnergyReservation>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((EnergyReservation r, InsertOneOptions opt, CancellationToken ct) =>
                {
                    if (string.IsNullOrEmpty(r.Id))
                    {
                        r.Id = Guid.NewGuid().ToString("N");
                    }
                    _reservations.Add(r);
                    return Task.CompletedTask;
                });

            // EnergyReservations FindAsync
            _mockReservationsCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<EnergyReservation>>(),
                    It.IsAny<FindOptions<EnergyReservation, EnergyReservation>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((FilterDefinition<EnergyReservation> filter, FindOptions<EnergyReservation, EnergyReservation> options, CancellationToken ct) =>
                {
                    var list = new List<EnergyReservation>();
                    if (filter is ExpressionFilterDefinition<EnergyReservation> exprFilter)
                    {
                        var pred = exprFilter.Expression.Compile();
                        list = _reservations.Where(pred).ToList();
                    }
                    else
                    {
                        list = _reservations.ToList();
                    }
                    var mockCursor = new Mock<IAsyncCursor<EnergyReservation>>();
                    mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                              .ReturnsAsync(list.Count > 0)
                              .ReturnsAsync(false);
                    mockCursor.Setup(c => c.Current).Returns(list);
                    return Task.FromResult(mockCursor.Object);
                });

            // EnergyReservations ReplaceOneAsync
            _mockReservationsCollection
                .Setup(c => c.ReplaceOneAsync(
                    It.IsAny<FilterDefinition<EnergyReservation>>(),
                    It.IsAny<EnergyReservation>(),
                    It.IsAny<ReplaceOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((FilterDefinition<EnergyReservation> filter, EnergyReservation replacement, ReplaceOptions opt, CancellationToken ct) =>
                {
                    var existing = _reservations.FirstOrDefault(r => r.Id == replacement.Id);
                    if (existing != null)
                    {
                        _reservations.Remove(existing);
                        _reservations.Add(replacement);
                        return Task.FromResult<ReplaceOneResult>(new ReplaceOneResult.Acknowledged(1, 1, null));
                    }
                    return Task.FromResult<ReplaceOneResult>(new ReplaceOneResult.Acknowledged(0, 0, null));
                });

            // EnergyReservations FindOneAndUpdateAsync (used in Delete/Cancel)
            _mockReservationsCollection
                .Setup(c => c.FindOneAndUpdateAsync(
                    It.IsAny<FilterDefinition<EnergyReservation>>(),
                    It.IsAny<UpdateDefinition<EnergyReservation>>(),
                    It.IsAny<FindOneAndUpdateOptions<EnergyReservation, EnergyReservation>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((FilterDefinition<EnergyReservation> filter, UpdateDefinition<EnergyReservation> update, FindOneAndUpdateOptions<EnergyReservation, EnergyReservation> options, CancellationToken ct) =>
                {
                    var serializerRegistry = BsonSerializer.SerializerRegistry;
                    var documentSerializer = serializerRegistry.GetSerializer<EnergyReservation>();
                    var rendered = filter.Render(new RenderArgs<EnergyReservation>(documentSerializer, serializerRegistry));

                    string? id = null;
                    if (rendered.Contains("_id"))
                    {
                        id = rendered["_id"].IsObjectId ? rendered["_id"].AsObjectId.ToString() : rendered["_id"].AsString;
                    }
                    else if (rendered.Contains("$and"))
                    {
                        var andArray = rendered["$and"].AsBsonArray;
                        foreach (var item in andArray)
                        {
                            var doc = item.AsBsonDocument;
                            if (doc.Contains("_id"))
                            {
                                id = doc["_id"].IsObjectId ? doc["_id"].AsObjectId.ToString() : doc["_id"].AsString;
                                break;
                            }
                        }
                    }

                    var target = _reservations.FirstOrDefault(r => r.Id == id);
                    if (target != null && target.Status != "Cancelled" && target.Status != "Completed")
                    {
                        var updateDoc = update.Render(new RenderArgs<EnergyReservation>(documentSerializer, serializerRegistry)) as BsonDocument;
                        if (updateDoc != null && updateDoc.Contains("$set"))
                        {
                            var setDoc = updateDoc["$set"].AsBsonDocument;
                            if (setDoc.Contains("Status"))
                            {
                                target.Status = setDoc["Status"].AsString;
                            }
                            if (setDoc.Contains("SlotId"))
                            {
                                target.SlotId = setDoc["SlotId"].AsString;
                            }
                            if (setDoc.Contains("StationId"))
                            {
                                target.StationId = setDoc["StationId"].AsString;
                            }
                        }
                        target.UpdatedAt = DateTime.UtcNow;
                        return Task.FromResult<EnergyReservation>(target);
                    }

                    return Task.FromResult<EnergyReservation>(null!);
                });
        }

        // =========================================================================
        // SECTION 5: RESERVATION CREATE TESTS
        // =========================================================================

        [Fact]
        public async Task Create_ValidRequest_ReturnsCreatedAtAction()
        {
            _prosumers.Add(new Prosumer { Nic = "123456789V", Status = "active" });
            _stations.Add(new MicrogridNode { NodeId = "ST-1", Status = "active" });
            var slotDate = DateTime.UtcNow.Date.AddDays(2);
            _slots.Add(new EnergyBookingSlot
            {
                Id = "slot-1",
                SlotId = "SLOT-1",
                StationId = "ST-1",
                Date = slotDate,
                StartTime = "10:00",
                EndTime = "12:00",
                Capacity = 5,
                Availability = 5,
                Status = "Available"
            });

            var req = new CreateReservationRequest
            {
                ProsumerNic = "123456789V",
                StationId = "ST-1",
                SlotId = "SLOT-1"
            };

            var result = await _controller.Create(req);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, createdResult.StatusCode);
            var res = Assert.IsType<EnergyReservation>(createdResult.Value);
            Assert.Equal("123456789V", res.ProsumerNic);
            Assert.Equal("ST-1", res.StationId);
            Assert.Equal("SLOT-1", res.SlotId);
            Assert.Equal("Pending", res.Status);
        }

        [Fact]
        public async Task Create_ValidRequest_DecrementsAvailabilityByOne_AndPreservesCapacity()
        {
            _prosumers.Add(new Prosumer { Nic = "123456789V", Status = "active" });
            _stations.Add(new MicrogridNode { NodeId = "ST-1", Status = "active" });
            var slotDate = DateTime.UtcNow.Date.AddDays(2);
            var slot = new EnergyBookingSlot
            {
                Id = "slot-1",
                SlotId = "SLOT-1",
                StationId = "ST-1",
                Date = slotDate,
                StartTime = "10:00",
                EndTime = "12:00",
                Capacity = 5,
                Availability = 5,
                Status = "Available"
            };
            _slots.Add(slot);

            var req = new CreateReservationRequest
            {
                ProsumerNic = "123456789V",
                StationId = "ST-1",
                SlotId = "SLOT-1"
            };

            await _controller.Create(req);

            Assert.Equal(4, slot.Availability);
            Assert.Equal(5, slot.Capacity);
        }

        [Fact]
        public async Task Create_ValidRequest_ReservationStartsAsPending()
        {
            _prosumers.Add(new Prosumer { Nic = "123456789V", Status = "active" });
            _stations.Add(new MicrogridNode { NodeId = "ST-1", Status = "active" });
            var slotDate = DateTime.UtcNow.Date.AddDays(1);
            _slots.Add(new EnergyBookingSlot
            {
                SlotId = "SLOT-1",
                StationId = "ST-1",
                Date = slotDate,
                StartTime = "10:00",
                EndTime = "12:00",
                Capacity = 3,
                Availability = 3,
                Status = "Available"
            });

            var req = new CreateReservationRequest { ProsumerNic = "123456789V", StationId = "ST-1", SlotId = "SLOT-1" };
            var result = await _controller.Create(req);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            var reservation = Assert.IsType<EnergyReservation>(created.Value);
            Assert.Equal("Pending", reservation.Status);
        }

        [Fact]
        public async Task Create_NonExistentProsumer_ReturnsNotFound()
        {
            _stations.Add(new MicrogridNode { NodeId = "ST-1", Status = "active" });
            _slots.Add(new EnergyBookingSlot { SlotId = "SLOT-1", StationId = "ST-1", Date = DateTime.UtcNow.Date.AddDays(1), StartTime = "10:00", EndTime = "11:00", Capacity = 5, Availability = 5, Status = "Available" });

            var req = new CreateReservationRequest { ProsumerNic = "NONEXISTENT", StationId = "ST-1", SlotId = "SLOT-1" };
            var result = await _controller.Create(req);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }

        [Fact]
        public async Task Create_InactiveProsumer_ReturnsBadRequest()
        {
            _prosumers.Add(new Prosumer { Nic = "INACTIVE_NIC", Status = "inactive" });
            _stations.Add(new MicrogridNode { NodeId = "ST-1", Status = "active" });
            _slots.Add(new EnergyBookingSlot { SlotId = "SLOT-1", StationId = "ST-1", Date = DateTime.UtcNow.Date.AddDays(1), StartTime = "10:00", EndTime = "11:00", Capacity = 5, Availability = 5, Status = "Available" });

            var req = new CreateReservationRequest { ProsumerNic = "INACTIVE_NIC", StationId = "ST-1", SlotId = "SLOT-1" };
            var result = await _controller.Create(req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Create_NonExistentStation_ReturnsNotFound()
        {
            _prosumers.Add(new Prosumer { Nic = "123456789V", Status = "active" });

            var req = new CreateReservationRequest { ProsumerNic = "123456789V", StationId = "ST-MISSING", SlotId = "SLOT-1" };
            var result = await _controller.Create(req);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }

        [Fact]
        public async Task Create_InactiveStation_ReturnsBadRequest()
        {
            _prosumers.Add(new Prosumer { Nic = "123456789V", Status = "active" });
            _stations.Add(new MicrogridNode { NodeId = "ST-INACTIVE", Status = "inactive" });

            var req = new CreateReservationRequest { ProsumerNic = "123456789V", StationId = "ST-INACTIVE", SlotId = "SLOT-1" };
            var result = await _controller.Create(req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Create_NonExistentSlot_ReturnsNotFound()
        {
            _prosumers.Add(new Prosumer { Nic = "123456789V", Status = "active" });
            _stations.Add(new MicrogridNode { NodeId = "ST-1", Status = "active" });

            var req = new CreateReservationRequest { ProsumerNic = "123456789V", StationId = "ST-1", SlotId = "SLOT-MISSING" };
            var result = await _controller.Create(req);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }

        [Fact]
        public async Task Create_SlotBelongingToAnotherStation_ReturnsBadRequest()
        {
            _prosumers.Add(new Prosumer { Nic = "123456789V", Status = "active" });
            _stations.Add(new MicrogridNode { NodeId = "ST-1", Status = "active" });
            _slots.Add(new EnergyBookingSlot
            {
                SlotId = "SLOT-OTHER",
                StationId = "ST-OTHER",
                Date = DateTime.UtcNow.Date.AddDays(1),
                StartTime = "10:00",
                EndTime = "11:00",
                Capacity = 5,
                Availability = 5,
                Status = "Available"
            });

            var req = new CreateReservationRequest { ProsumerNic = "123456789V", StationId = "ST-1", SlotId = "SLOT-OTHER" };
            var result = await _controller.Create(req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Create_UnavailableSlot_ReturnsBadRequest()
        {
            _prosumers.Add(new Prosumer { Nic = "123456789V", Status = "active" });
            _stations.Add(new MicrogridNode { NodeId = "ST-1", Status = "active" });
            _slots.Add(new EnergyBookingSlot
            {
                SlotId = "SLOT-1",
                StationId = "ST-1",
                Date = DateTime.UtcNow.Date.AddDays(1),
                StartTime = "10:00",
                EndTime = "11:00",
                Capacity = 5,
                Availability = 5,
                Status = "Unavailable"
            });

            var req = new CreateReservationRequest { ProsumerNic = "123456789V", StationId = "ST-1", SlotId = "SLOT-1" };
            var result = await _controller.Create(req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Create_ZeroAvailability_ReturnsBadRequest()
        {
            _prosumers.Add(new Prosumer { Nic = "123456789V", Status = "active" });
            _stations.Add(new MicrogridNode { NodeId = "ST-1", Status = "active" });
            _slots.Add(new EnergyBookingSlot
            {
                SlotId = "SLOT-1",
                StationId = "ST-1",
                Date = DateTime.UtcNow.Date.AddDays(1),
                StartTime = "10:00",
                EndTime = "11:00",
                Capacity = 5,
                Availability = 0,
                Status = "Available"
            });

            var req = new CreateReservationRequest { ProsumerNic = "123456789V", StationId = "ST-1", SlotId = "SLOT-1" };
            var result = await _controller.Create(req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Create_PastSlot_ReturnsBadRequest()
        {
            _prosumers.Add(new Prosumer { Nic = "123456789V", Status = "active" });
            _stations.Add(new MicrogridNode { NodeId = "ST-1", Status = "active" });
            var yesterday = DateTime.UtcNow.Date.AddDays(-1);
            _slots.Add(new EnergyBookingSlot
            {
                SlotId = "SLOT-PAST",
                StationId = "ST-1",
                Date = yesterday,
                StartTime = "09:00",
                EndTime = "10:00",
                Capacity = 5,
                Availability = 5,
                Status = "Available"
            });

            var req = new CreateReservationRequest { ProsumerNic = "123456789V", StationId = "ST-1", SlotId = "SLOT-PAST" };
            var result = await _controller.Create(req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Create_BookingBeyond7Days_ReturnsBadRequest()
        {
            _prosumers.Add(new Prosumer { Nic = "123456789V", Status = "active" });
            _stations.Add(new MicrogridNode { NodeId = "ST-1", Status = "active" });
            var day8 = DateTime.UtcNow.Date.AddDays(8);
            _slots.Add(new EnergyBookingSlot
            {
                SlotId = "SLOT-FAR",
                StationId = "ST-1",
                Date = day8,
                StartTime = "09:00",
                EndTime = "10:00",
                Capacity = 5,
                Availability = 5,
                Status = "Available"
            });

            var req = new CreateReservationRequest { ProsumerNic = "123456789V", StationId = "ST-1", SlotId = "SLOT-FAR" };
            var result = await _controller.Create(req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Create_ValidBookingInside7Days_Accepted()
        {
            _prosumers.Add(new Prosumer { Nic = "123456789V", Status = "active" });
            _stations.Add(new MicrogridNode { NodeId = "ST-1", Status = "active" });
            var day3 = DateTime.UtcNow.Date.AddDays(3);
            _slots.Add(new EnergyBookingSlot
            {
                SlotId = "SLOT-NEAR",
                StationId = "ST-1",
                Date = day3,
                StartTime = "12:00",
                EndTime = "13:00",
                Capacity = 5,
                Availability = 5,
                Status = "Available"
            });

            var req = new CreateReservationRequest { ProsumerNic = "123456789V", StationId = "ST-1", SlotId = "SLOT-NEAR" };
            var result = await _controller.Create(req);

            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, created.StatusCode);
        }

        [Fact]
        public async Task Create_Exactly7DaysBoundary_VerifiedCorrectly()
        {
            _prosumers.Add(new Prosumer { Nic = "123456789V", Status = "active" });
            _stations.Add(new MicrogridNode { NodeId = "ST-1", Status = "active" });

            // Exactly 7 days boundary: slot just inside 7 days from now
            var boundarySlotDate = DateTime.UtcNow.Date.AddDays(6);
            _slots.Add(new EnergyBookingSlot
            {
                SlotId = "SLOT-BOUNDARY-INSIDE",
                StationId = "ST-1",
                Date = boundarySlotDate,
                StartTime = "23:00",
                EndTime = "23:59",
                Capacity = 5,
                Availability = 5,
                Status = "Available"
            });

            var reqInside = new CreateReservationRequest { ProsumerNic = "123456789V", StationId = "ST-1", SlotId = "SLOT-BOUNDARY-INSIDE" };
            var resultInside = await _controller.Create(reqInside);
            Assert.IsType<CreatedAtActionResult>(resultInside);

            // Boundary slot beyond 7 days from now
            var beyondBoundarySlotDate = DateTime.UtcNow.Date.AddDays(8);
            _slots.Add(new EnergyBookingSlot
            {
                SlotId = "SLOT-BOUNDARY-OUTSIDE",
                StationId = "ST-1",
                Date = beyondBoundarySlotDate,
                StartTime = "12:00",
                EndTime = "13:00",
                Capacity = 5,
                Availability = 5,
                Status = "Available"
            });

            var reqOutside = new CreateReservationRequest { ProsumerNic = "123456789V", StationId = "ST-1", SlotId = "SLOT-BOUNDARY-OUTSIDE" };
            var resultOutside = await _controller.Create(reqOutside);
            var badRequest = Assert.IsType<BadRequestObjectResult>(resultOutside);
            Assert.Equal(400, badRequest.StatusCode);
        }

        // =========================================================================
        // SECTION 6: RESERVATION UPDATE TESTS
        // =========================================================================

        [Fact]
        public async Task Update_ValidModification_ReturnsOk_AndExchangesAvailability()
        {
            var oldSlotDate = DateTime.UtcNow.Date.AddDays(2);
            var newSlotDate = DateTime.UtcNow.Date.AddDays(3);

            var oldSlot = new EnergyBookingSlot
            {
                Id = "slot-1",
                SlotId = "SLOT-1",
                StationId = "ST-1",
                Date = oldSlotDate,
                StartTime = "10:00",
                EndTime = "11:00",
                Capacity = 5,
                Availability = 4,
                Status = "Available"
            };
            var newSlot = new EnergyBookingSlot
            {
                Id = "slot-2",
                SlotId = "SLOT-2",
                StationId = "ST-1",
                Date = newSlotDate,
                StartTime = "14:00",
                EndTime = "15:00",
                Capacity = 5,
                Availability = 5,
                Status = "Available"
            };
            _slots.Add(oldSlot);
            _slots.Add(newSlot);

            var validResId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            var reservation = new EnergyReservation
            {
                Id = validResId,
                ReservationId = "RES-001",
                ProsumerNic = "123456789V",
                StationId = "ST-1",
                SlotId = "SLOT-1",
                Status = "Pending"
            };
            _reservations.Add(reservation);

            var req = new UpdateReservationRequest { StationId = "ST-1", SlotId = "SLOT-2" };
            var result = await _controller.Update(validResId, req);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            var updatedRes = Assert.IsType<EnergyReservation>(okResult.Value);
            Assert.Equal("SLOT-2", updatedRes.SlotId);

            // Verify old availability incremented, new availability decremented, capacities unchanged
            Assert.Equal(5, oldSlot.Availability);
            Assert.Equal(5, oldSlot.Capacity);
            Assert.Equal(4, newSlot.Availability);
            Assert.Equal(5, newSlot.Capacity);
        }

        [Fact]
        public async Task Update_NoticeLessThan12Hours_ReturnsBadRequest()
        {
            // Current slot is 3 hours away
            var soonSlotTime = DateTime.UtcNow.AddHours(3);
            var oldSlot = new EnergyBookingSlot
            {
                Id = "slot-soon",
                SlotId = "SLOT-SOON",
                StationId = "ST-1",
                Date = soonSlotTime.Date,
                StartTime = soonSlotTime.ToString("HH:mm"),
                EndTime = soonSlotTime.AddHours(1).ToString("HH:mm"),
                Capacity = 5,
                Availability = 4,
                Status = "Available"
            };
            _slots.Add(oldSlot);

            var reservation = new EnergyReservation
            {
                Id = "res-soon",
                ReservationId = "RES-SOON",
                ProsumerNic = "123456789V",
                StationId = "ST-1",
                SlotId = "SLOT-SOON",
                Status = "Pending"
            };
            _reservations.Add(reservation);

            var req = new UpdateReservationRequest { StationId = "ST-1", SlotId = "SLOT-NEW" };
            var result = await _controller.Update("res-soon", req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Update_NoticeMoreThan12Hours_Succeeds()
        {
            var oldSlotTime = DateTime.UtcNow.AddHours(25);
            var newSlotTime = DateTime.UtcNow.AddHours(30);

            var oldSlot = new EnergyBookingSlot
            {
                Id = "slot-old",
                SlotId = "SLOT-OLD",
                StationId = "ST-1",
                Date = oldSlotTime.Date,
                StartTime = oldSlotTime.ToString("HH:mm"),
                EndTime = oldSlotTime.AddHours(1).ToString("HH:mm"),
                Capacity = 5,
                Availability = 4,
                Status = "Available"
            };
            var newSlot = new EnergyBookingSlot
            {
                Id = "slot-new",
                SlotId = "SLOT-NEW",
                StationId = "ST-1",
                Date = newSlotTime.Date,
                StartTime = newSlotTime.ToString("HH:mm"),
                EndTime = newSlotTime.AddHours(1).ToString("HH:mm"),
                Capacity = 5,
                Availability = 5,
                Status = "Available"
            };
            _slots.Add(oldSlot);
            _slots.Add(newSlot);

            var validResId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            var reservation = new EnergyReservation
            {
                Id = validResId,
                ReservationId = "RES-12H",
                ProsumerNic = "123456789V",
                StationId = "ST-1",
                SlotId = "SLOT-OLD",
                Status = "Pending"
            };
            _reservations.Add(reservation);

            var req = new UpdateReservationRequest { StationId = "ST-1", SlotId = "SLOT-NEW" };
            var result = await _controller.Update(validResId, req);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task Update_SameSlot_ReturnsOkIdempotent()
        {
            var reservation = new EnergyReservation
            {
                Id = "res-same",
                ReservationId = "RES-SAME",
                ProsumerNic = "123456789V",
                StationId = "ST-1",
                SlotId = "SLOT-1",
                Status = "Pending"
            };
            _reservations.Add(reservation);

            var req = new UpdateReservationRequest { StationId = "ST-1", SlotId = "SLOT-1" };
            var result = await _controller.Update("res-same", req);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
        }

        [Fact]
        public async Task Update_NewSlotBelongsToAnotherStation_ReturnsBadRequest()
        {
            var oldSlotTime = DateTime.UtcNow.AddHours(25);
            var oldSlot = new EnergyBookingSlot
            {
                SlotId = "SLOT-1",
                StationId = "ST-1",
                Date = oldSlotTime.Date,
                StartTime = oldSlotTime.ToString("HH:mm"),
                EndTime = oldSlotTime.AddHours(1).ToString("HH:mm"),
                Capacity = 5,
                Availability = 4,
                Status = "Available"
            };
            var newSlot = new EnergyBookingSlot
            {
                SlotId = "SLOT-OTHER",
                StationId = "ST-OTHER",
                Date = oldSlotTime.Date.AddDays(1),
                StartTime = "10:00",
                EndTime = "11:00",
                Capacity = 5,
                Availability = 5,
                Status = "Available"
            };
            _slots.Add(oldSlot);
            _slots.Add(newSlot);

            var reservation = new EnergyReservation { Id = "res-1", StationId = "ST-1", SlotId = "SLOT-1", Status = "Pending" };
            _reservations.Add(reservation);

            var req = new UpdateReservationRequest { StationId = "ST-1", SlotId = "SLOT-OTHER" };
            var result = await _controller.Update("res-1", req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Update_NewSlotZeroAvailability_ReturnsBadRequest()
        {
            var oldSlotTime = DateTime.UtcNow.AddHours(25);
            var oldSlot = new EnergyBookingSlot
            {
                SlotId = "SLOT-1",
                StationId = "ST-1",
                Date = oldSlotTime.Date,
                StartTime = oldSlotTime.ToString("HH:mm"),
                EndTime = oldSlotTime.AddHours(1).ToString("HH:mm"),
                Capacity = 5,
                Availability = 4,
                Status = "Available"
            };
            var newSlot = new EnergyBookingSlot
            {
                SlotId = "SLOT-FULL",
                StationId = "ST-1",
                Date = oldSlotTime.Date.AddDays(1),
                StartTime = "10:00",
                EndTime = "11:00",
                Capacity = 5,
                Availability = 0,
                Status = "Available"
            };
            _slots.Add(oldSlot);
            _slots.Add(newSlot);

            var reservation = new EnergyReservation { Id = "res-1", StationId = "ST-1", SlotId = "SLOT-1", Status = "Pending" };
            _reservations.Add(reservation);

            var req = new UpdateReservationRequest { StationId = "ST-1", SlotId = "SLOT-FULL" };
            var result = await _controller.Update("res-1", req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        // =========================================================================
        // SECTION 7: RESERVATION CANCELLATION TESTS
        // =========================================================================

        [Fact]
        public async Task Delete_ValidCancellation_ReturnsOk_AndRestoresAvailability()
        {
            var validId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            var slotDate = DateTime.UtcNow.Date.AddDays(3);
            var slot = new EnergyBookingSlot
            {
                SlotId = "SLOT-CANCEL",
                StationId = "ST-1",
                Date = slotDate,
                StartTime = "10:00",
                EndTime = "11:00",
                Capacity = 5,
                Availability = 4,
                Status = "Available"
            };
            _slots.Add(slot);

            var reservation = new EnergyReservation
            {
                Id = validId,
                ReservationId = "RES-CANCEL",
                ProsumerNic = "123456789V",
                StationId = "ST-1",
                SlotId = "SLOT-CANCEL",
                Status = "Pending"
            };
            _reservations.Add(reservation);

            var result = await _controller.Delete(validId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal("Cancelled", reservation.Status);
            Assert.Equal(5, slot.Availability);
            Assert.Equal(5, slot.Capacity);
        }

        [Fact]
        public async Task Delete_NoticeLessThan12Hours_ReturnsBadRequest()
        {
            var validId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            var slotTime = DateTime.UtcNow.AddHours(5);
            var slot = new EnergyBookingSlot
            {
                SlotId = "SLOT-SOON",
                StationId = "ST-1",
                Date = slotTime.Date,
                StartTime = slotTime.ToString("HH:mm"),
                EndTime = slotTime.AddHours(1).ToString("HH:mm"),
                Capacity = 5,
                Availability = 4,
                Status = "Available"
            };
            _slots.Add(slot);

            var reservation = new EnergyReservation
            {
                Id = validId,
                ReservationId = "RES-SOON",
                ProsumerNic = "123456789V",
                StationId = "ST-1",
                SlotId = "SLOT-SOON",
                Status = "Pending"
            };
            _reservations.Add(reservation);

            var result = await _controller.Delete(validId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Delete_AlreadyCancelled_ReturnsBadRequest()
        {
            var validId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            var reservation = new EnergyReservation
            {
                Id = validId,
                ReservationId = "RES-ALREADY",
                Status = "Cancelled"
            };
            _reservations.Add(reservation);

            var result = await _controller.Delete(validId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Delete_CompletedReservation_ReturnsBadRequest()
        {
            var validId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            var reservation = new EnergyReservation
            {
                Id = validId,
                ReservationId = "RES-COMPLETED",
                Status = "Completed"
            };
            _reservations.Add(reservation);

            var result = await _controller.Delete(validId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Delete_AvailabilityNeverExceedsCapacity()
        {
            var validId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            var slotDate = DateTime.UtcNow.Date.AddDays(3);
            var slot = new EnergyBookingSlot
            {
                SlotId = "SLOT-CAP",
                StationId = "ST-1",
                Date = slotDate,
                StartTime = "10:00",
                EndTime = "11:00",
                Capacity = 5,
                Availability = 5, // Already at capacity
                Status = "Available"
            };
            _slots.Add(slot);

            var reservation = new EnergyReservation
            {
                Id = validId,
                ReservationId = "RES-CAP",
                StationId = "ST-1",
                SlotId = "SLOT-CAP",
                Status = "Approved"
            };
            _reservations.Add(reservation);

            var result = await _controller.Delete(validId);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            Assert.Equal(5, slot.Availability); // Must not exceed capacity
            Assert.Equal(5, slot.Capacity);
        }

        // =========================================================================
        // SECTION 8: RETRIEVAL & HISTORY TESTS
        // =========================================================================

        [Fact]
        public async Task GetAll_ReturnsAllReservations()
        {
            _reservations.Add(new EnergyReservation { Id = "1", ReservationId = "RES-1" });
            _reservations.Add(new EnergyReservation { Id = "2", ReservationId = "RES-2" });

            var result = await _controller.GetAll();

            var okResult = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<EnergyReservation>>(okResult.Value);
            Assert.Equal(2, list.Count);
        }

        [Fact]
        public async Task GetById_ExistingId_ReturnsOk()
        {
            _reservations.Add(new EnergyReservation { Id = "res-100", ReservationId = "RES-100" });

            var result = await _controller.GetById("res-100");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var res = Assert.IsType<EnergyReservation>(okResult.Value);
            Assert.Equal("RES-100", res.ReservationId);
        }

        [Fact]
        public async Task GetById_NonExistentId_ReturnsNotFound()
        {
            var result = await _controller.GetById("NONEXISTENT");

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }

        [Fact]
        public async Task GetHistory_ReturnsProsumerReservations()
        {
            _reservations.Add(new EnergyReservation { Id = "1", ProsumerNic = "NIC-1" });
            _reservations.Add(new EnergyReservation { Id = "2", ProsumerNic = "NIC-2" });
            _reservations.Add(new EnergyReservation { Id = "3", ProsumerNic = "NIC-1" });

            var result = await _controller.GetHistory("NIC-1");

            var okResult = Assert.IsType<OkObjectResult>(result);
            var list = Assert.IsType<List<EnergyReservation>>(okResult.Value);
            Assert.Equal(2, list.Count);
        }

        // =========================================================================
        // SECTION 9: TERMINAL STATUS & HARDENED VALIDATION TESTS
        // =========================================================================

        [Fact]
        public async Task Update_CancelledReservation_ReturnsBadRequest()
        {
            var oldSlotTime = DateTime.UtcNow.AddHours(25);
            var slot = new EnergyBookingSlot
            {
                Id = "slot-cancelled",
                SlotId = "SLOT-CANCELLED",
                StationId = "ST-1",
                Date = oldSlotTime.Date,
                StartTime = oldSlotTime.ToString("HH:mm"),
                EndTime = oldSlotTime.AddHours(1).ToString("HH:mm"),
                Capacity = 5,
                Availability = 4,
                Status = "Available"
            };
            _slots.Add(slot);

            var reservation = new EnergyReservation
            {
                Id = "res-cancelled",
                ReservationId = "RES-CANCELLED",
                ProsumerNic = "123456789V",
                StationId = "ST-1",
                SlotId = "SLOT-CANCELLED",
                Status = "Cancelled"
            };
            _reservations.Add(reservation);

            var req = new UpdateReservationRequest { StationId = "ST-1", SlotId = "SLOT-NEW" };
            var result = await _controller.Update("res-cancelled", req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);

            // Verify availability was NOT exchanged
            Assert.Equal(4, slot.Availability);
            Assert.Equal(5, slot.Capacity);
        }

        [Fact]
        public async Task Update_CompletedReservation_ReturnsBadRequest()
        {
            var oldSlotTime = DateTime.UtcNow.AddHours(25);
            var slot = new EnergyBookingSlot
            {
                Id = "slot-completed",
                SlotId = "SLOT-COMPLETED",
                StationId = "ST-1",
                Date = oldSlotTime.Date,
                StartTime = oldSlotTime.ToString("HH:mm"),
                EndTime = oldSlotTime.AddHours(1).ToString("HH:mm"),
                Capacity = 5,
                Availability = 4,
                Status = "Available"
            };
            _slots.Add(slot);

            var reservation = new EnergyReservation
            {
                Id = "res-completed",
                ReservationId = "RES-COMPLETED",
                ProsumerNic = "123456789V",
                StationId = "ST-1",
                SlotId = "SLOT-COMPLETED",
                Status = "Completed"
            };
            _reservations.Add(reservation);

            var req = new UpdateReservationRequest { StationId = "ST-1", SlotId = "SLOT-NEW" };
            var result = await _controller.Update("res-completed", req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);

            // Verify availability was NOT exchanged
            Assert.Equal(4, slot.Availability);
            Assert.Equal(5, slot.Capacity);
        }

        [Fact]
        public async Task Update_SameSlotDifferentStation_ReturnsBadRequest()
        {
            var reservation = new EnergyReservation
            {
                Id = "res-diff-station",
                ReservationId = "RES-DIFF-ST",
                ProsumerNic = "123456789V",
                StationId = "ST-1",
                SlotId = "SLOT-1",
                Status = "Pending"
            };
            _reservations.Add(reservation);

            // Request specifies same SlotId but different StationId
            var req = new UpdateReservationRequest { StationId = "ST-OTHER", SlotId = "SLOT-1" };
            var result = await _controller.Update("res-diff-station", req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Create_WhenInsertFails_RollbackPreservesCapacityInvariant()
        {
            _prosumers.Add(new Prosumer { Nic = "123456789V", Status = "active" });
            _stations.Add(new MicrogridNode { NodeId = "ST-1", Status = "active" });

            var slotDate = DateTime.UtcNow.Date.AddDays(2);
            var slot = new EnergyBookingSlot
            {
                SlotId = "SLOT-ROLLBACK",
                StationId = "ST-1",
                Date = slotDate,
                StartTime = "10:00",
                EndTime = "11:00",
                Capacity = 5,
                Availability = 5,
                Status = "Available"
            };
            _slots.Add(slot);

            // Force InsertOneAsync on reservations to fail
            _mockReservationsCollection
                .Setup(c => c.InsertOneAsync(It.IsAny<EnergyReservation>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Simulated DB Insert Failure"));

            var req = new CreateReservationRequest { ProsumerNic = "123456789V", StationId = "ST-1", SlotId = "SLOT-ROLLBACK" };

            await Assert.ThrowsAsync<InvalidOperationException>(() => _controller.Create(req));

            // Availability must be safely rolled back to 5, never exceeding capacity 5
            Assert.Equal(5, slot.Availability);
            Assert.Equal(5, slot.Capacity);
        }

        // =========================================================================
        // SECTION: CONCURRENCY, CANCELLATION INTEGRITY & UNIQUE ID TESTS
        // =========================================================================

        [Fact]
        public async Task Update_ConcurrentModification_ReturnsConflict()
        {
            var oldSlotTime = DateTime.UtcNow.AddHours(25);
            var newSlotTime = DateTime.UtcNow.AddHours(30);

            var oldSlot = new EnergyBookingSlot
            {
                Id = "slot-old",
                SlotId = "SLOT-OLD",
                StationId = "ST-1",
                Date = oldSlotTime.Date,
                StartTime = oldSlotTime.ToString("HH:mm"),
                EndTime = oldSlotTime.AddHours(1).ToString("HH:mm"),
                Capacity = 5,
                Availability = 4,
                Status = "Available"
            };
            var newSlot = new EnergyBookingSlot
            {
                Id = "slot-new",
                SlotId = "SLOT-NEW",
                StationId = "ST-1",
                Date = newSlotTime.Date,
                StartTime = newSlotTime.ToString("HH:mm"),
                EndTime = newSlotTime.AddHours(1).ToString("HH:mm"),
                Capacity = 5,
                Availability = 5,
                Status = "Available"
            };
            _slots.Add(oldSlot);
            _slots.Add(newSlot);

            var validResId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            var reservation = new EnergyReservation
            {
                Id = validResId,
                ReservationId = "RES-CONCURRENT",
                ProsumerNic = "123456789V",
                StationId = "ST-1",
                SlotId = "SLOT-OLD",
                Status = "Pending",
                UpdatedAt = DateTime.UtcNow
            };
            _reservations.Add(reservation);

            // Simulate reservation being modified concurrently so FindOneAndUpdateAsync returns null
            _mockReservationsCollection
                .Setup(c => c.FindOneAndUpdateAsync(
                    It.IsAny<FilterDefinition<EnergyReservation>>(),
                    It.IsAny<UpdateDefinition<EnergyReservation>>(),
                    It.IsAny<FindOneAndUpdateOptions<EnergyReservation, EnergyReservation>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((EnergyReservation)null!);

            var req = new UpdateReservationRequest { StationId = "ST-1", SlotId = "SLOT-NEW" };
            var result = await _controller.Update(validResId, req);

            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflictResult.StatusCode);

            // Verify new slot availability was compensated back to 5
            Assert.Equal(5, newSlot.Availability);
        }

        [Fact]
        public async Task Delete_ConcurrentModification_ReturnsConflict()
        {
            var validId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            var slotDate = DateTime.UtcNow.Date.AddDays(3);
            var slot = new EnergyBookingSlot
            {
                SlotId = "SLOT-CONCURRENT-DEL",
                StationId = "ST-1",
                Date = slotDate,
                StartTime = "10:00",
                EndTime = "11:00",
                Capacity = 5,
                Availability = 4,
                Status = "Available"
            };
            _slots.Add(slot);

            var reservation = new EnergyReservation
            {
                Id = validId,
                ReservationId = "RES-CONCURRENT-DEL",
                ProsumerNic = "123456789V",
                StationId = "ST-1",
                SlotId = "SLOT-CONCURRENT-DEL",
                Status = "Pending"
            };
            _reservations.Add(reservation);

            // Simulate concurrent cancellation so FindOneAndUpdateAsync returns null
            _mockReservationsCollection
                .Setup(c => c.FindOneAndUpdateAsync(
                    It.IsAny<FilterDefinition<EnergyReservation>>(),
                    It.IsAny<UpdateDefinition<EnergyReservation>>(),
                    It.IsAny<FindOneAndUpdateOptions<EnergyReservation, EnergyReservation>>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync((EnergyReservation)null!);

            var result = await _controller.Delete(validId);

            var conflictResult = Assert.IsType<ConflictObjectResult>(result);
            Assert.Equal(409, conflictResult.StatusCode);
        }

        [Fact]
        public async Task Delete_InvalidUnexpectedStatus_ReturnsBadRequest()
        {
            var validId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            var reservation = new EnergyReservation
            {
                Id = validId,
                ReservationId = "RES-UNEXPECTED",
                ProsumerNic = "123456789V",
                StationId = "ST-1",
                SlotId = "SLOT-1",
                Status = "Draft" // Unexpected non-cancellable status
            };
            _reservations.Add(reservation);

            var result = await _controller.Delete(validId);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        [Fact]
        public async Task Delete_SlotRestorationFails_ReturnsStatusCode500()
        {
            var validId = MongoDB.Bson.ObjectId.GenerateNewId().ToString();
            var slotDate = DateTime.UtcNow.Date.AddDays(3);
            var slot = new EnergyBookingSlot
            {
                SlotId = "SLOT-FAIL-RESTORE",
                StationId = "ST-1",
                Date = slotDate,
                StartTime = "10:00",
                EndTime = "11:00",
                Capacity = 5,
                Availability = 4,
                Status = "Available"
            };
            _slots.Add(slot);

            var reservation = new EnergyReservation
            {
                Id = validId,
                ReservationId = "RES-FAIL-RESTORE",
                ProsumerNic = "123456789V",
                StationId = "ST-1",
                SlotId = "SLOT-FAIL-RESTORE",
                Status = "Pending"
            };
            _reservations.Add(reservation);

            // Simulate slot restoration failing (ModifiedCount == 0)
            _mockSlotsCollection
                .Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<EnergyBookingSlot>>(),
                    It.IsAny<UpdateDefinition<EnergyBookingSlot>>(),
                    It.IsAny<UpdateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateResult.Acknowledged(0, 0, null));

            var result = await _controller.Delete(validId);

            var statusResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, statusResult.StatusCode);
        }

        [Fact]
        public async Task Create_ReservationIdCollision_RetriesAndSucceeds()
        {
            _prosumers.Add(new Prosumer { Nic = "123456789V", Status = "active" });
            _stations.Add(new MicrogridNode { NodeId = "ST-1", Status = "active" });
            var slotDate = DateTime.UtcNow.Date.AddDays(2);
            _slots.Add(new EnergyBookingSlot
            {
                SlotId = "SLOT-RETRY",
                StationId = "ST-1",
                Date = slotDate,
                StartTime = "10:00",
                EndTime = "12:00",
                Capacity = 5,
                Availability = 5,
                Status = "Available"
            });

            var duplicateKeyException = CreateDuplicateKeyException();

            int callCount = 0;
            _mockReservationsCollection
                .Setup(c => c.InsertOneAsync(
                    It.IsAny<EnergyReservation>(),
                    It.IsAny<InsertOneOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((EnergyReservation r, InsertOneOptions opt, CancellationToken ct) =>
                {
                    callCount++;
                    if (callCount == 1 && duplicateKeyException != null)
                    {
                        throw duplicateKeyException;
                    }
                    r.Id = "res-retry-id";
                    _reservations.Add(r);
                    return Task.CompletedTask;
                });

            var req = new CreateReservationRequest
            {
                ProsumerNic = "123456789V",
                StationId = "ST-1",
                SlotId = "SLOT-RETRY"
            };

            var result = await _controller.Create(req);
            var created = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, created.StatusCode);
            Assert.Equal(2, callCount); // Verified collision retry executed
        }

        private static MongoWriteException CreateDuplicateKeyException()
        {
            var writeError = (WriteError)RuntimeHelpers.GetUninitializedObject(typeof(WriteError));
            foreach (var field in typeof(WriteError).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.FieldType == typeof(ServerErrorCategory))
                    field.SetValue(writeError, ServerErrorCategory.DuplicateKey);
                else if (field.FieldType == typeof(int))
                    field.SetValue(writeError, 11000);
                else if (field.FieldType == typeof(string))
                    field.SetValue(writeError, "E11000 duplicate key error");
            }

            var ex = (MongoWriteException)RuntimeHelpers.GetUninitializedObject(typeof(MongoWriteException));
            foreach (var field in typeof(MongoWriteException).GetFields(BindingFlags.NonPublic | BindingFlags.Instance))
            {
                if (field.FieldType == typeof(WriteError))
                    field.SetValue(ex, writeError);
            }
            return ex;
        }
    }
}
