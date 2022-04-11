namespace MJP.MaerskOfflineTest.Tests.Services
{
    using AutoFixture;
    using AutoFixture.AutoMoq;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MJP.MaerskOfflineTest.Models;
    using MJP.MaerskOfflineTest.Services;
    using NodaMoney;
    using System;
    using System.Linq;
    using System.Threading.Tasks;

    [TestClass]
    public class DatabaseServiceTests
    {
        private IFixture _fixture;

        [TestInitialize]
        public void Setup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(10)]
        [DataRow(50)]
        public async Task Given_ValidContainerBookingAdded_When_DatabaseServiceReturnsBookings_Then_AllGivenValidContainerBookingsAreReturned(int bookingCount)
        {
            // Arrange
            var sut = _fixture.Create<DatabaseService>();
            var voyageCode = _fixture.Create<string>();
            var inputData = _fixture.CreateMany<ContainerBooking>(bookingCount);

            foreach (var item in inputData)
            {
                await sut.AddContainerBookingForVoyage(voyageCode, item);
            }

            // Act
            var actual = await sut.GetContainerBookings(voyageCode);

            // Assert
            // This takes a surprisingly long time with many elements. Almost 15 seconds for 1000 elements? Oh well.
            actual.Should().BeEquivalentTo(inputData);
        }

        [DataTestMethod]
        [DataRow(1, 1)]
        [DataRow(10, 5)]
        public async Task Given_ValidContainerBookingAddedAndMultipleVoyages_When_DatabaseServiceReturnsBookings_Then_AllGivenValidContainerBookingsAreReturned(int bookingCount1, int bookingCount2)
        {
            // Arrange
            var sut = _fixture.Create<DatabaseService>();
            var voyageCode1 = _fixture.Create<string>();
            var voyageCode2 = _fixture.Create<string>();
            var inputData1 = _fixture.CreateMany<ContainerBooking>(bookingCount1);
            var inputData2 = _fixture.CreateMany<ContainerBooking>(bookingCount1);

            foreach (var item in inputData1)
            {
                await sut.AddContainerBookingForVoyage(voyageCode1, item);
            }

            foreach (var item in inputData2)
            {
                await sut.AddContainerBookingForVoyage(voyageCode2, item);
            }

            // Act
            var actual1 = await sut.GetContainerBookings(voyageCode1);
            var actual2 = await sut.GetContainerBookings(voyageCode2);

            // Assert
            actual1.Should().BeEquivalentTo(inputData1);
            actual2.Should().BeEquivalentTo(inputData2);
        }

        [TestMethod]
        public async Task Given_ValidContainerBookings_When_ReturningContainerBookings_Then_ListMustbeReturnedInDescendingOrder()
        {
            // Arrange
            var sut = _fixture.Create<DatabaseService>();
            var voyageCode = _fixture.Create<string>();
            var inputData = new ContainerBooking[]
            {
                new ContainerBooking(voyageCode, DateTime.Now.AddSeconds(10), 100m),
                new ContainerBooking(voyageCode, DateTime.Now, 100m),
                new ContainerBooking(voyageCode, DateTime.Now.AddSeconds(-10), 100m),
                new ContainerBooking(voyageCode, DateTime.Now.AddSeconds(-20), 100m),
                new ContainerBooking(voyageCode, DateTime.Now.AddSeconds(20), 100m)
            };

            foreach (var item in inputData)
            {
                await sut.AddContainerBookingForVoyage(voyageCode, item);
            }

            var expected = inputData.OrderByDescending(booking => booking.Timestamp);

            // Act
            var actual = await sut.GetContainerBookings(voyageCode);

            // Assert
            actual.Should().BeEquivalentTo(expected, options => options.WithStrictOrdering());
        }

        [DataTestMethod]
        [DataRow(1)]
        [DataRow(10)]
        [DataRow(50)]
        public async Task Given_ValidContainerBookingAdded_When_GettingCumulativeAverage_Then_CorrectlyCalculatedCumulativeAverageIsReturned(int bookingCount)
        {
            // Arrange
            var sut = _fixture.Create<DatabaseService>();
            var voyageCode = _fixture.Create<string>();
            // Autofixture will generate random values for price for us here, which is perfect for the test cases
            var bookings = _fixture.CreateMany<ContainerBooking>(bookingCount);
            var expectedAverage = bookings.Average(booking => booking.Price.Amount);
            foreach (var booking in bookings)
            {
                await sut.AddContainerBookingForVoyage(voyageCode, booking);
            }

            // Act
            var actual = await sut.GetCumulativeAverage(voyageCode);

            // Assert
            actual.Should().BeApproximately(expectedAverage, 0.001m);
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public async Task Given_InvalidVoyageCode_When_GettingCumulativeAverage_Then_ThrowException(string voyageCode)
        {
            // Arrange
            var sut = _fixture.Create<DatabaseService>();

            // Act
            var action = () => sut.GetCumulativeAverage(voyageCode);

            // Assert
            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        [TestMethod]
        public async Task Given_ValidVoyageCodeButNoContainerBookingsAreAdded_When_GettingCumulativeAverage_Then_ThrowException()
        {
            // Arrange
            var sut = _fixture.Create<DatabaseService>();
            var voyageCode = _fixture.Create<string>();

            // Act
            var action = () => sut.GetCumulativeAverage(voyageCode);

            // Assert
            await action.Should().ThrowAsync<ArgumentException>();
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public async Task Given_ValidContainerBookingAndInvalidVoyageCode_When_DatabaseServiceAddIsCalled_Then_ThrowException(string voyageCode)
        {
            // Arrange
            var sut = _fixture.Create<DatabaseService>();
            var booking = _fixture.Create<ContainerBooking>();

            // Act
            var action = () => sut.AddContainerBookingForVoyage(voyageCode, booking);

            // Assert
            await action.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task Given_InvalidContainerBookingAndValidVoyageCode_When_DatabaseServiceAddIsCalled_Then_ThrowException()
        {
            // Arrange
            var sut = _fixture.Create<DatabaseService>();
            var voyageCode = _fixture.Create<string>();

            // Act
            var action = () => sut.AddContainerBookingForVoyage(voyageCode, null);

            // Assert
            await action.Should().ThrowAsync<ArgumentException>();
        }

        [TestMethod]
        public async Task Given_ValidVoyageCode_When_DatabaseServiceGetIsCalledWithoutAdding_Then_ThrowException()
        {
            // Arrange
            var sut = _fixture.Create<DatabaseService>();
            var voyageCode = _fixture.Create<string>();

            // Act
            var action = () => sut.GetContainerBookings(voyageCode);

            // Assert
            await action.Should().ThrowAsync<ArgumentException>();
        }

        [DataTestMethod]
        [DataRow(null)]
        [DataRow("")]
        public async Task Given_InvalidVoyageCode_When_DatabaseServiceGetIsCalled_Then_ThrowException(string voyageCode)
        {
            // Arrange
            var sut = _fixture.Create<DatabaseService>();

            // Act
            var action = () => sut.GetContainerBookings(voyageCode);

            // Assert
            await action.Should().ThrowAsync<ArgumentNullException>();
        }

        // And so many more positive / negative path unit tests... You get the idea!
    }
}
