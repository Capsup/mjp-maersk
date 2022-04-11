namespace MJP.MaerskOfflineTest.Tests.Controllers
{
    using AutoFixture;
    using AutoFixture.AutoMoq;
    using FluentAssertions;
    using Microsoft.AspNetCore.Mvc;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MJP.MaerskOfflineTest.Controllers;
    using MJP.MaerskOfflineTest.Models;
    using MJP.MaerskOfflineTest.Services.Interfaces;
    using Moq;
    using NodaMoney;
    using System;
    using System.Collections.Generic;
    using System.Linq;
    using System.Text;
    using System.Threading.Tasks;

    [TestClass]
    public class VoyageControllerTests
    {
        private IFixture _fixture;

        [TestInitialize]
        public void Setup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
        }

        [TestMethod]
        public async Task Given_ValidParameters_When_CallingUpdatePrice_Then_AddNewContainerBookingAndReturnOk()
        {
            // Arrange
            var expectedVoyageCode = _fixture.Create<string>();
            var expectedBooking = _fixture.Create<ContainerBooking>();
            expectedBooking.VoyageCode = expectedVoyageCode;

            string actualVoyageCode = null;
            ContainerBooking actualBooking = null;

            var databaseServiceMock = _fixture.Freeze<Mock<IDatabaseService>>();
            databaseServiceMock.Setup(m => m.AddContainerBookingForVoyage(It.IsAny<string>(), It.IsAny<ContainerBooking>()))
                .Callback<string, ContainerBooking>((code, booking) => { actualVoyageCode = code; actualBooking = booking; });

            var sut = _fixture.Build<VoyageController>().OmitAutoProperties().Create();

            // Act
            var result = await sut.UpdatePrice(expectedVoyageCode, expectedBooking.Price.Amount, CurrencyEnum.DKK, expectedBooking.Timestamp);

            // Assert
            actualBooking.Should().BeEquivalentTo(expectedBooking);
            actualVoyageCode.Should().BeEquivalentTo(expectedVoyageCode);
        }

        [TestMethod]
        public async Task Given_ValidParameters_When_CallingGetAverage_Then_ReturnCorrectAverage()
        {
            // Arrange
            var voyageCode = _fixture.Create<string>();
            var expectedValue = 50m;

            var databaseServiceMock = _fixture.Freeze<Mock<IDatabaseService>>();
            databaseServiceMock.Setup(m => m.GetCumulativeAverage(It.IsAny<string>())).Returns(() => Task.FromResult(100m));

            var exchangeRateServiceMock = _fixture.Freeze<Mock<IExchangeRateService>>();
            exchangeRateServiceMock.Setup(m => m.ExchangeMoney(It.IsAny<Money>(), It.IsAny<Currency>()))
                .Returns(() => new Money(expectedValue, Currency.FromCode("DKK")));

            var sut = _fixture.Build<VoyageController>().OmitAutoProperties().Create();

            // Act
            var result = (await sut.GetAverage(voyageCode, CurrencyEnum.USD)).Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result.Value.Should().Be(expectedValue);
        }

        [TestMethod]
        public async Task Given_ValidParameters_When_CallingGetAverage_Then_MakeSureMoneyIsExchangedFromUSD()
        {
            // Arrange
            var voyageCode = _fixture.Create<string>();
            var expectedValue = 500m;

            var databaseServiceMock = _fixture.Freeze<Mock<IDatabaseService>>();
            databaseServiceMock.Setup(m => m.GetCumulativeAverage(It.IsAny<string>())).Returns(() => Task.FromResult(1000m));

            var exchangeRateServiceMock = _fixture.Freeze<Mock<IExchangeRateService>>();
            exchangeRateServiceMock.Setup(m => m.ExchangeMoney(It.Is<Money>(x => x.Currency.Code == "USD"), It.IsAny<Currency>()))
                .Returns<Money, Currency>((money, currency) => new Money(expectedValue, currency));

            var sut = _fixture.Build<VoyageController>().OmitAutoProperties().Create();

            // Act
            var result = (await sut.GetAverage(voyageCode, CurrencyEnum.DKK)).Result as OkObjectResult;

            // Assert
            result.Should().NotBeNull();
            result.Value.Should().Be(expectedValue);
        }

        [TestMethod]
        public async Task Given_ValidParameters_When_CallingGetLastPrices_Then_ReturnCorrectPrices()
        {
            // Arrange
            var voyageCode = _fixture.Create<string>();
            var inputData = _fixture.CreateMany<ContainerBooking>(5).OrderByDescending(x => x.Timestamp).ToArray();

            var databaseServiceMock = _fixture.Freeze<Mock<IDatabaseService>>();
            databaseServiceMock.Setup(m => m.GetContainerBookings(It.IsAny<string>())).Returns(() => Task.FromResult(inputData.AsEnumerable()));

            var exchangeRateServiceMock = _fixture.Freeze<Mock<IExchangeRateService>>();
            exchangeRateServiceMock.Setup(m => m.ExchangeMoney(It.IsAny<Money>(), It.IsAny<Currency>()))
                .Returns<Money, Currency>((money, currency) => new Money(money.Amount * 2, currency));

            var expectedData = inputData.Select(x => (x.Price * 2).Amount);

            var sut = _fixture.Build<VoyageController>().OmitAutoProperties().Create();

            // Act
            var actualData = (await sut.GetLastPrices(voyageCode, CurrencyEnum.DKK)).Result as OkObjectResult;

            // Assert
            actualData.Should().NotBeNull();
            actualData.Value.Should().BeEquivalentTo(expectedData);
        }

        // And so many more positive / negative path unit tests... You get the idea!
    }
}
