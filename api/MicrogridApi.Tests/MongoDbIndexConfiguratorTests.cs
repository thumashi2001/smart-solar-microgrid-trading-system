// ============================================================================
// File: MongoDbIndexConfiguratorTests.cs
// Component: Component 2 - Energy Reservation & Slot Management
// Description: Unit tests for MongoDbIndexConfigurator verifying unique index
//              configuration, await semantics, and error surfacing.
// ============================================================================

using System;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using MicrogridApi.Data;
using MicrogridApi.Models;
using MongoDB.Driver;
using Moq;
using Xunit;

namespace MicrogridApi.Tests.Unit
{
    [Trait("Category", "Unit")]
    public class MongoDbIndexConfiguratorTests
    {
        [Fact]
        public async Task ConfigureIndexesAsync_CreatesUniqueIndexesForSlotAndReservation()
        {
            var mockSlotsIndexManager = new Mock<IMongoIndexManager<EnergyBookingSlot>>();
            CreateIndexModel<EnergyBookingSlot>? capturedSlotModel = null;
            mockSlotsIndexManager
                .Setup(m => m.CreateOneAsync(
                    It.IsAny<CreateIndexModel<EnergyBookingSlot>>(),
                    It.IsAny<CreateOneIndexOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<CreateIndexModel<EnergyBookingSlot>, CreateOneIndexOptions, CancellationToken>((model, opt, ct) =>
                {
                    capturedSlotModel = model;
                })
                .ReturnsAsync("ux_slotId");

            var mockReservationsIndexManager = new Mock<IMongoIndexManager<EnergyReservation>>();
            CreateIndexModel<EnergyReservation>? capturedReservationModel = null;
            mockReservationsIndexManager
                .Setup(m => m.CreateOneAsync(
                    It.IsAny<CreateIndexModel<EnergyReservation>>(),
                    It.IsAny<CreateOneIndexOptions>(),
                    It.IsAny<CancellationToken>()))
                .Callback<CreateIndexModel<EnergyReservation>, CreateOneIndexOptions, CancellationToken>((model, opt, ct) =>
                {
                    capturedReservationModel = model;
                })
                .ReturnsAsync("ux_reservationId");

            var mockSlotsCollection = new Mock<IMongoCollection<EnergyBookingSlot>>();
            mockSlotsCollection.Setup(c => c.Indexes).Returns(mockSlotsIndexManager.Object);

            var mockReservationsCollection = new Mock<IMongoCollection<EnergyReservation>>();
            mockReservationsCollection.Setup(c => c.Indexes).Returns(mockReservationsIndexManager.Object);

            await MongoDbIndexConfigurator.ConfigureIndexesAsync(
                mockSlotsCollection.Object,
                mockReservationsCollection.Object
            );

            Assert.NotNull(capturedSlotModel);
            Assert.True(capturedSlotModel.Options.Unique);
            Assert.Equal("ux_slotId", capturedSlotModel.Options.Name);

            Assert.NotNull(capturedReservationModel);
            Assert.True(capturedReservationModel.Options.Unique);
            Assert.Equal("ux_reservationId", capturedReservationModel.Options.Name);
        }

        [Fact]
        public async Task ConfigureIndexesAsync_WhenIndexCreationFails_ThrowsInvalidOperationException()
        {
            var mockSlotsIndexManager = new Mock<IMongoIndexManager<EnergyBookingSlot>>();
            mockSlotsIndexManager
                .Setup(m => m.CreateOneAsync(
                    It.IsAny<CreateIndexModel<EnergyBookingSlot>>(),
                    It.IsAny<CreateOneIndexOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Index creation rejected by database engine."));

            var mockSlotsCollection = new Mock<IMongoCollection<EnergyBookingSlot>>();
            mockSlotsCollection.Setup(c => c.Indexes).Returns(mockSlotsIndexManager.Object);

            var mockReservationsCollection = new Mock<IMongoCollection<EnergyReservation>>();

            var mockDatabase = new Mock<IMongoDatabase>();
            mockDatabase
                .Setup(d => d.GetCollection<EnergyBookingSlot>("energyBookingSlots", It.IsAny<MongoCollectionSettings>()))
                .Returns(mockSlotsCollection.Object);
            mockDatabase
                .Setup(d => d.GetCollection<EnergyReservation>("energyReservation", It.IsAny<MongoCollectionSettings>()))
                .Returns(mockReservationsCollection.Object);

            var dbContext = (MongoDbContext)RuntimeHelpers.GetUninitializedObject(typeof(MongoDbContext));
            var dbField = typeof(MongoDbContext).GetField("_database", BindingFlags.NonPublic | BindingFlags.Instance);
            dbField!.SetValue(dbContext, mockDatabase.Object);

            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                MongoDbIndexConfigurator.ConfigureIndexesAsync(dbContext, isDevelopment: false));

            Assert.Contains("Critical startup failure", ex.Message);
            Assert.Contains("Failed to configure required database unique indexes", ex.Message);
        }

        [Fact]
        public async Task ConfigureIndexesAsync_WhenConnectionFailsInDevelopment_LogsNoticeAndDoesNotThrow()
        {
            var mockSlotsIndexManager = new Mock<IMongoIndexManager<EnergyBookingSlot>>();
            var connectionEx = (MongoConnectionException)RuntimeHelpers.GetUninitializedObject(typeof(MongoConnectionException));

            mockSlotsIndexManager
                .Setup(m => m.CreateOneAsync(
                    It.IsAny<CreateIndexModel<EnergyBookingSlot>>(),
                    It.IsAny<CreateOneIndexOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(connectionEx);

            var mockSlotsCollection = new Mock<IMongoCollection<EnergyBookingSlot>>();
            mockSlotsCollection.Setup(c => c.Indexes).Returns(mockSlotsIndexManager.Object);

            var mockReservationsCollection = new Mock<IMongoCollection<EnergyReservation>>();

            var mockDatabase = new Mock<IMongoDatabase>();
            mockDatabase
                .Setup(d => d.GetCollection<EnergyBookingSlot>("energyBookingSlots", It.IsAny<MongoCollectionSettings>()))
                .Returns(mockSlotsCollection.Object);
            mockDatabase
                .Setup(d => d.GetCollection<EnergyReservation>("energyReservation", It.IsAny<MongoCollectionSettings>()))
                .Returns(mockReservationsCollection.Object);

            var dbContext = (MongoDbContext)RuntimeHelpers.GetUninitializedObject(typeof(MongoDbContext));
            var dbField = typeof(MongoDbContext).GetField("_database", BindingFlags.NonPublic | BindingFlags.Instance);
            dbField!.SetValue(dbContext, mockDatabase.Object);

            // In development, offline MongoConnectionException must be gracefully handled and not crash
            var recordEx = await Record.ExceptionAsync(() =>
                MongoDbIndexConfigurator.ConfigureIndexesAsync(dbContext, isDevelopment: true));

            Assert.Null(recordEx);
        }

        [Fact]
        public async Task ConfigureIndexesAsync_WhenIndexCreationFailsInDevelopment_StillThrowsInvalidOperationException()
        {
            var mockSlotsIndexManager = new Mock<IMongoIndexManager<EnergyBookingSlot>>();
            mockSlotsIndexManager
                .Setup(m => m.CreateOneAsync(
                    It.IsAny<CreateIndexModel<EnergyBookingSlot>>(),
                    It.IsAny<CreateOneIndexOptions>(),
                    It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Duplicate existing key prevents index creation."));

            var mockSlotsCollection = new Mock<IMongoCollection<EnergyBookingSlot>>();
            mockSlotsCollection.Setup(c => c.Indexes).Returns(mockSlotsIndexManager.Object);

            var mockReservationsCollection = new Mock<IMongoCollection<EnergyReservation>>();

            var mockDatabase = new Mock<IMongoDatabase>();
            mockDatabase
                .Setup(d => d.GetCollection<EnergyBookingSlot>("energyBookingSlots", It.IsAny<MongoCollectionSettings>()))
                .Returns(mockSlotsCollection.Object);
            mockDatabase
                .Setup(d => d.GetCollection<EnergyReservation>("energyReservation", It.IsAny<MongoCollectionSettings>()))
                .Returns(mockReservationsCollection.Object);

            var dbContext = (MongoDbContext)RuntimeHelpers.GetUninitializedObject(typeof(MongoDbContext));
            var dbField = typeof(MongoDbContext).GetField("_database", BindingFlags.NonPublic | BindingFlags.Instance);
            dbField!.SetValue(dbContext, mockDatabase.Object);

            // In development, real index creation errors (like duplicates) MUST NOT be swallowed
            var ex = await Assert.ThrowsAsync<InvalidOperationException>(() =>
                MongoDbIndexConfigurator.ConfigureIndexesAsync(dbContext, isDevelopment: true));

            Assert.Contains("Critical startup failure", ex.Message);
        }
    }
}
