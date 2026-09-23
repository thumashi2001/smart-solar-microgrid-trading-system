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
using MongoDB.Driver;
using Moq;
using Xunit;

namespace MicrogridApi.Tests.Unit
{
    [Trait("Category", "Unit")]
    public class SlotsControllerTests
    {
        private readonly Mock<IMongoCollection<EnergyBookingSlot>> _mockSlotsCollection;
        private readonly Mock<IMongoCollection<EnergyReservation>> _mockReservationsCollection;
        private readonly Mock<IMongoCollection<MicrogridNode>> _mockNodesCollection;
        private readonly SlotsController _controller;

        private EnergyBookingSlot? _currentSlot;
        private readonly List<EnergyReservation> _reservations = new();

        public SlotsControllerTests()
        {
            _mockSlotsCollection = new Mock<IMongoCollection<EnergyBookingSlot>>();
            _mockReservationsCollection = new Mock<IMongoCollection<EnergyReservation>>();
            _mockNodesCollection = new Mock<IMongoCollection<MicrogridNode>>();

            var mockDatabase = new Mock<IMongoDatabase>();
            mockDatabase
                .Setup(d => d.GetCollection<EnergyBookingSlot>("energyBookingSlots", It.IsAny<MongoCollectionSettings>()))
                .Returns(_mockSlotsCollection.Object);
            mockDatabase
                .Setup(d => d.GetCollection<EnergyReservation>("energyReservation", It.IsAny<MongoCollectionSettings>()))
                .Returns(_mockReservationsCollection.Object);
            mockDatabase
                .Setup(d => d.GetCollection<MicrogridNode>("SolarStationInfo", It.IsAny<MongoCollectionSettings>()))
                .Returns(_mockNodesCollection.Object);

            // Instantiate MongoDbContext without modifying the shared class or adding 'virtual'
            var dbContext = (MongoDbContext)RuntimeHelpers.GetUninitializedObject(typeof(MongoDbContext));
            var dbField = typeof(MongoDbContext).GetField("_database", BindingFlags.NonPublic | BindingFlags.Instance);
            if (dbField == null)
            {
                throw new InvalidOperationException("Private field '_database' not found in MongoDbContext.");
            }
            dbField.SetValue(dbContext, mockDatabase.Object);

            _controller = new SlotsController(dbContext);

            SetupReservationsQuery();
        }

        private void SetupSlot(EnergyBookingSlot? slot)
        {
            _currentSlot = slot;

            _mockSlotsCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<EnergyBookingSlot>>(),
                    It.IsAny<FindOptions<EnergyBookingSlot, EnergyBookingSlot>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((FilterDefinition<EnergyBookingSlot> filter, FindOptions<EnergyBookingSlot, EnergyBookingSlot> options, CancellationToken ct) =>
                {
                    var list = new List<EnergyBookingSlot>();
                    if (_currentSlot != null)
                    {
                        if (filter is ExpressionFilterDefinition<EnergyBookingSlot> exprFilter)
                        {
                            var predicate = exprFilter.Expression.Compile();
                            if (predicate(_currentSlot))
                            {
                                list.Add(_currentSlot);
                            }
                        }
                        else
                        {
                            list.Add(_currentSlot);
                        }
                    }

                    var mockCursor = new Mock<IAsyncCursor<EnergyBookingSlot>>();
                    mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                              .ReturnsAsync(list.Count > 0)
                              .ReturnsAsync(false);
                    mockCursor.Setup(c => c.Current).Returns(list);

                    return Task.FromResult(mockCursor.Object);
                });

            _mockSlotsCollection
                .Setup(c => c.UpdateOneAsync(
                    It.IsAny<FilterDefinition<EnergyBookingSlot>>(),
                    It.IsAny<UpdateDefinition<EnergyBookingSlot>>(),
                    It.IsAny<UpdateOptions>(),
                    It.IsAny<CancellationToken>()))
                .ReturnsAsync(new UpdateResult.Acknowledged(1, 1, null));
        }

        private void SetupReservationsQuery()
        {
            _mockReservationsCollection
                .Setup(c => c.CountDocumentsAsync(
                    It.IsAny<FilterDefinition<EnergyReservation>>(),
                    It.IsAny<CountOptions>(),
                    It.IsAny<CancellationToken>()))
                .Returns((FilterDefinition<EnergyReservation> filter, CountOptions options, CancellationToken ct) =>
                {
                    if (filter is ExpressionFilterDefinition<EnergyReservation> exprFilter)
                    {
                        var predicate = exprFilter.Expression.Compile();
                        long count = _reservations.Count(predicate);
                        return Task.FromResult(count);
                    }
                    return Task.FromResult((long)_reservations.Count);
                });
        }

        // UT-01: Update date successfully
        [Fact]
        public async Task UT01_UpdateDate_Successfully()
        {
            var slot = new EnergyBookingSlot { Id = "1", SlotId = "SLOT-1", Date = new DateTime(2026, 1, 1), StartTime = "09:00", EndTime = "10:00", Status = "Available" };
            SetupSlot(slot);

            var newDate = new DateTime(2026, 2, 1);
            var req = new UpdateSlotRequest { Date = newDate };
            var result = await _controller.Update("1", req);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            var updatedSlot = Assert.IsType<EnergyBookingSlot>(okResult.Value);
            Assert.Equal(newDate, updatedSlot.Date);
        }

        // UT-02: Update start time successfully
        [Fact]
        public async Task UT02_UpdateStartTime_Successfully()
        {
            var slot = new EnergyBookingSlot { Id = "1", SlotId = "SLOT-1", Date = new DateTime(2026, 1, 1), StartTime = "09:00", EndTime = "10:00", Status = "Available" };
            SetupSlot(slot);

            var req = new UpdateSlotRequest { StartTime = "09:30" };
            var result = await _controller.Update("1", req);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            var updatedSlot = Assert.IsType<EnergyBookingSlot>(okResult.Value);
            Assert.Equal("09:30", updatedSlot.StartTime);
        }

        // UT-03: Update end time successfully
        [Fact]
        public async Task UT03_UpdateEndTime_Successfully()
        {
            var slot = new EnergyBookingSlot { Id = "1", SlotId = "SLOT-1", Date = new DateTime(2026, 1, 1), StartTime = "09:00", EndTime = "10:00", Status = "Available" };
            SetupSlot(slot);

            var req = new UpdateSlotRequest { EndTime = "11:00" };
            var result = await _controller.Update("1", req);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            var updatedSlot = Assert.IsType<EnergyBookingSlot>(okResult.Value);
            Assert.Equal("11:00", updatedSlot.EndTime);
        }

        // UT-04: Update status successfully
        [Fact]
        public async Task UT04_UpdateStatus_Successfully()
        {
            var slot = new EnergyBookingSlot { Id = "1", SlotId = "SLOT-1", Date = new DateTime(2026, 1, 1), StartTime = "09:00", EndTime = "10:00", Status = "Available" };
            SetupSlot(slot);

            var req = new UpdateSlotRequest { Status = "Unavailable" };
            var result = await _controller.Update("1", req);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            var updatedSlot = Assert.IsType<EnergyBookingSlot>(okResult.Value);
            Assert.Equal("Unavailable", updatedSlot.Status);
        }

        // UT-05: Pending reservation blocks schedule modification
        [Fact]
        public async Task UT05_PendingReservation_BlocksScheduleModification()
        {
            var slot = new EnergyBookingSlot { Id = "1", SlotId = "SLOT-1", Date = new DateTime(2026, 1, 1), StartTime = "09:00", EndTime = "10:00", Status = "Available" };
            SetupSlot(slot);
            _reservations.Add(new EnergyReservation { SlotId = "SLOT-1", Status = "Pending" });

            var req = new UpdateSlotRequest { Date = new DateTime(2026, 2, 1) };
            var result = await _controller.Update("1", req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        // UT-06: Approved reservation blocks schedule modification
        [Fact]
        public async Task UT06_ApprovedReservation_BlocksScheduleModification()
        {
            var slot = new EnergyBookingSlot { Id = "1", SlotId = "SLOT-1", Date = new DateTime(2026, 1, 1), StartTime = "09:00", EndTime = "10:00", Status = "Available" };
            SetupSlot(slot);
            _reservations.Add(new EnergyReservation { SlotId = "SLOT-1", Status = "Approved" });

            var req = new UpdateSlotRequest { StartTime = "08:30" };
            var result = await _controller.Update("1", req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        // UT-07: Cancelled reservation does not block schedule modification
        [Fact]
        public async Task UT07_CancelledReservation_DoesNotBlockScheduleModification()
        {
            var slot = new EnergyBookingSlot { Id = "1", SlotId = "SLOT-1", Date = new DateTime(2026, 1, 1), StartTime = "09:00", EndTime = "10:00", Status = "Available" };
            SetupSlot(slot);
            _reservations.Add(new EnergyReservation { SlotId = "SLOT-1", Status = "Cancelled" });

            var newDate = new DateTime(2026, 2, 1);
            var req = new UpdateSlotRequest { Date = newDate };
            var result = await _controller.Update("1", req);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            var updatedSlot = Assert.IsType<EnergyBookingSlot>(okResult.Value);
            Assert.Equal(newDate, updatedSlot.Date);
        }

        // UT-08: Capacity cannot be modified
        [Fact]
        public async Task UT08_Capacity_CannotBeModified()
        {
            var slot = new EnergyBookingSlot { Id = "1", SlotId = "SLOT-1", Date = new DateTime(2026, 1, 1), StartTime = "09:00", EndTime = "10:00", Capacity = 10, Availability = 10, Status = "Available" };
            SetupSlot(slot);

            var req = new UpdateSlotRequest { Status = "Unavailable" };
            var result = await _controller.Update("1", req);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            var updatedSlot = Assert.IsType<EnergyBookingSlot>(okResult.Value);
            Assert.Equal(10, updatedSlot.Capacity);
        }

        // UT-09: Availability cannot be modified
        [Fact]
        public async Task UT09_Availability_CannotBeModified()
        {
            var slot = new EnergyBookingSlot { Id = "1", SlotId = "SLOT-1", Date = new DateTime(2026, 1, 1), StartTime = "09:00", EndTime = "10:00", Capacity = 10, Availability = 7, Status = "Available" };
            SetupSlot(slot);

            var req = new UpdateSlotRequest { Status = "Unavailable" };
            var result = await _controller.Update("1", req);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            var updatedSlot = Assert.IsType<EnergyBookingSlot>(okResult.Value);
            Assert.Equal(7, updatedSlot.Availability);
        }

        // UT-10: Invalid time format rejected
        [Fact]
        public async Task UT10_InvalidTimeFormat_Rejected()
        {
            var slot = new EnergyBookingSlot { Id = "1", SlotId = "SLOT-1", Date = new DateTime(2026, 1, 1), StartTime = "09:00", EndTime = "10:00", Status = "Available" };
            SetupSlot(slot);

            var req = new UpdateSlotRequest { StartTime = "invalid" };
            var result = await _controller.Update("1", req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        // UT-11: EndTime before StartTime rejected
        [Fact]
        public async Task UT11_EndTimeBeforeStartTime_Rejected()
        {
            var slot = new EnergyBookingSlot { Id = "1", SlotId = "SLOT-1", Date = new DateTime(2026, 1, 1), StartTime = "09:00", EndTime = "10:00", Status = "Available" };
            SetupSlot(slot);

            var req = new UpdateSlotRequest { StartTime = "15:00", EndTime = "14:00" };
            var result = await _controller.Update("1", req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        // UT-12: EndTime equal to StartTime rejected
        [Fact]
        public async Task UT12_EndTimeEqualToStartTime_Rejected()
        {
            var slot = new EnergyBookingSlot { Id = "1", SlotId = "SLOT-1", Date = new DateTime(2026, 1, 1), StartTime = "09:00", EndTime = "10:00", Status = "Available" };
            SetupSlot(slot);

            var req = new UpdateSlotRequest { StartTime = "15:00", EndTime = "15:00" };
            var result = await _controller.Update("1", req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        // UT-13: Valid schedule update succeeds
        [Fact]
        public async Task UT13_ValidScheduleUpdate_Succeeds()
        {
            var slot = new EnergyBookingSlot { Id = "1", SlotId = "SLOT-1", Date = new DateTime(2026, 1, 1), StartTime = "09:00", EndTime = "10:00", Status = "Available" };
            SetupSlot(slot);

            var newDate = new DateTime(2026, 3, 15);
            var req = new UpdateSlotRequest
            {
                Date = newDate,
                StartTime = "11:00",
                EndTime = "13:00",
                Status = "Unavailable"
            };
            var result = await _controller.Update("1", req);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            var updatedSlot = Assert.IsType<EnergyBookingSlot>(okResult.Value);
            Assert.Equal(newDate, updatedSlot.Date);
            Assert.Equal("11:00", updatedSlot.StartTime);
            Assert.Equal("13:00", updatedSlot.EndTime);
            Assert.Equal("Unavailable", updatedSlot.Status);
        }

        // Additional: Status only update when active reservation exists succeeds
        [Fact]
        public async Task Update_StatusOnlyActiveReservation_ReturnsOk()
        {
            var slot = new EnergyBookingSlot { Id = "1", SlotId = "SLOT-1", Date = new DateTime(2026, 1, 1), StartTime = "09:00", EndTime = "10:00", Status = "Available" };
            SetupSlot(slot);
            _reservations.Add(new EnergyReservation { SlotId = "SLOT-1", Status = "Pending" });

            var req = new UpdateSlotRequest { Status = "Unavailable" };
            var result = await _controller.Update("1", req);

            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okResult.StatusCode);
            var updatedSlot = Assert.IsType<EnergyBookingSlot>(okResult.Value);
            Assert.Equal("Unavailable", updatedSlot.Status);
        }

        // Additional: Non-existent slot returns 404
        [Fact]
        public async Task Update_NonExistentSlot_ReturnsNotFound()
        {
            SetupSlot(null);
            var req = new UpdateSlotRequest { Status = "Unavailable" };
            var result = await _controller.Update("999", req);

            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFoundResult.StatusCode);
        }

        // Additional: Invalid status rejected
        [Fact]
        public async Task Update_InvalidStatus_ReturnsBadRequest()
        {
            var slot = new EnergyBookingSlot { Id = "1", SlotId = "SLOT-1", Date = new DateTime(2026, 1, 1), StartTime = "09:00", EndTime = "10:00", Status = "Available" };
            SetupSlot(slot);

            var req = new UpdateSlotRequest { Status = "InvalidStatus" };
            var result = await _controller.Update("1", req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        private void SetupStation(MicrogridNode? station)
        {
            _mockNodesCollection
                .Setup(c => c.FindAsync(
                    It.IsAny<FilterDefinition<MicrogridNode>>(),
                    It.IsAny<FindOptions<MicrogridNode, MicrogridNode>>(),
                    It.IsAny<CancellationToken>()))
                .Returns((FilterDefinition<MicrogridNode> filter, FindOptions<MicrogridNode, MicrogridNode> options, CancellationToken ct) =>
                {
                    var list = new List<MicrogridNode>();
                    if (station != null)
                    {
                        if (filter is ExpressionFilterDefinition<MicrogridNode> exprFilter)
                        {
                            var predicate = exprFilter.Expression.Compile();
                            if (predicate(station)) list.Add(station);
                        }
                        else
                        {
                            list.Add(station);
                        }
                    }
                    var mockCursor = new Mock<IAsyncCursor<MicrogridNode>>();
                    mockCursor.SetupSequence(c => c.MoveNextAsync(It.IsAny<CancellationToken>()))
                              .ReturnsAsync(list.Count > 0)
                              .ReturnsAsync(false);
                    mockCursor.Setup(c => c.Current).Returns(list);
                    return Task.FromResult(mockCursor.Object);
                });
        }

        // CREATE: Valid slot creation
        [Fact]
        public async Task Create_ValidSlot_ReturnsCreatedAtAction()
        {
            SetupStation(new MicrogridNode { NodeId = "ST-1", Status = "active" });
            _mockSlotsCollection
                .Setup(c => c.InsertOneAsync(It.IsAny<EnergyBookingSlot>(), It.IsAny<InsertOneOptions>(), It.IsAny<CancellationToken>()))
                .Returns(Task.CompletedTask);

            var req = new CreateSlotRequest
            {
                StationId = "ST-1",
                Date = new DateTime(2026, 4, 1),
                StartTime = "09:00",
                EndTime = "11:00",
                Capacity = 5
            };

            var result = await _controller.Create(req);

            var createdResult = Assert.IsType<CreatedAtActionResult>(result);
            Assert.Equal(201, createdResult.StatusCode);
            var createdSlot = Assert.IsType<EnergyBookingSlot>(createdResult.Value);
            Assert.Equal("ST-1", createdSlot.StationId);
            Assert.Equal(5, createdSlot.Capacity);
            Assert.Equal(5, createdSlot.Availability);
            Assert.Equal("Available", createdSlot.Status);
        }

        // CREATE: Invalid station rejected
        [Fact]
        public async Task Create_InvalidStation_ReturnsNotFound()
        {
            SetupStation(null);

            var req = new CreateSlotRequest
            {
                StationId = "NONEXISTENT",
                Date = new DateTime(2026, 4, 1),
                StartTime = "09:00",
                EndTime = "11:00",
                Capacity = 5
            };

            var result = await _controller.Create(req);

            var notFound = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Equal(404, notFound.StatusCode);
        }

        // CREATE: Inactive station rejected
        [Fact]
        public async Task Create_InactiveStation_ReturnsBadRequest()
        {
            SetupStation(new MicrogridNode { NodeId = "ST-INACTIVE", Status = "inactive" });

            var req = new CreateSlotRequest
            {
                StationId = "ST-INACTIVE",
                Date = new DateTime(2026, 4, 1),
                StartTime = "09:00",
                EndTime = "11:00",
                Capacity = 5
            };

            var result = await _controller.Create(req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        // CREATE: Invalid capacity rejected
        [Fact]
        public async Task Create_InvalidCapacity_ReturnsBadRequest()
        {
            var req = new CreateSlotRequest
            {
                StationId = "ST-1",
                Date = new DateTime(2026, 4, 1),
                StartTime = "09:00",
                EndTime = "11:00",
                Capacity = 0
            };

            var result = await _controller.Create(req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        // CREATE: Invalid start time rejected
        [Fact]
        public async Task Create_InvalidStartTime_ReturnsBadRequest()
        {
            var req = new CreateSlotRequest
            {
                StationId = "ST-1",
                Date = new DateTime(2026, 4, 1),
                StartTime = "invalid",
                EndTime = "11:00",
                Capacity = 5
            };

            var result = await _controller.Create(req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        // CREATE: Invalid end time rejected
        [Fact]
        public async Task Create_InvalidEndTime_ReturnsBadRequest()
        {
            var req = new CreateSlotRequest
            {
                StationId = "ST-1",
                Date = new DateTime(2026, 4, 1),
                StartTime = "09:00",
                EndTime = "invalid",
                Capacity = 5
            };

            var result = await _controller.Create(req);

            var badRequest = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal(400, badRequest.StatusCode);
        }

        // CREATE: End time before or equal to start time rejected
        [Fact]
        public async Task Create_EndTimeBeforeOrEqualToStartTime_ReturnsBadRequest()
        {
            var reqEqual = new CreateSlotRequest
            {
                StationId = "ST-1",
                Date = new DateTime(2026, 4, 1),
                StartTime = "10:00",
                EndTime = "10:00",
                Capacity = 5
            };

            var resultEqual = await _controller.Create(reqEqual);
            var badRequestEqual = Assert.IsType<BadRequestObjectResult>(resultEqual);
            Assert.Equal(400, badRequestEqual.StatusCode);

            var reqBefore = new CreateSlotRequest
            {
                StationId = "ST-1",
                Date = new DateTime(2026, 4, 1),
                StartTime = "11:00",
                EndTime = "10:00",
                Capacity = 5
            };

            var resultBefore = await _controller.Create(reqBefore);
            var badRequestBefore = Assert.IsType<BadRequestObjectResult>(resultBefore);
            Assert.Equal(400, badRequestBefore.StatusCode);
        }
    }
}
