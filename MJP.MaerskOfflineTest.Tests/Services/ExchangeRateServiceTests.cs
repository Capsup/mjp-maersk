namespace MJP.MaerskOfflineTest.Tests.Services
{
    using AutoFixture;
    using AutoFixture.AutoMoq;
    using FluentAssertions;
    using Microsoft.VisualStudio.TestTools.UnitTesting;
    using MJP.MaerskOfflineTest.Services;
    using NodaMoney;
    using System;
    using System.Threading.Tasks;

    [TestClass]
    public class ExchangeRateServiceTests
    {
        private IFixture _fixture;

        [TestInitialize]
        public void Setup()
        {
            _fixture = new Fixture().Customize(new AutoMoqCustomization());
        }

        [DataTestMethod]
        [DataRow("EUR", "USD")]
        [DataRow("EUR", "DKK")]
        [DataRow("USD", "EUR")]
        [DataRow("USD", "DKK")]
        [DataRow("DKK", "EUR")]
        [DataRow("DKK", "USD")]
        public async Task Given_ValidExchangeRateService_When_GettingExchangeRateForCurrency_Then_ReturnValidExchangeRate(string currentCode, string otherCode)
        {
            // Arrange
            var sut = _fixture.Create<ExchangeRateService>();

            // Act
            var result = sut.GetExchangeRate(Currency.FromCode(currentCode), Currency.FromCode(otherCode));

            // Assert
            result.Should().NotBeNull();
        }

        // In this specific case, the default is a precision of 2 decimals. So 57.7 DKK in USD would actually be 8.47613, but we round up.
        [DataTestMethod]
        [DataRow("EUR", "USD", 100d, 109.23d)]
        [DataRow("DKK", "USD", 57.7d, 8.48d)]
        public async Task Given_ValidExchangeRateService_When_UsingValidExchangeRate_Then_ReturnValidExchangedCurrency(string currentCode, string otherCode, double amount, double expected)
        {
            // Arrange
            var sut = _fixture.Create<ExchangeRateService>();
            var money = new Money(amount, Currency.FromCode(currentCode));

            // Act
            var result = sut.ExchangeMoney(money, Currency.FromCode(otherCode));

            // Assert
            result.Currency.Should().Be(Currency.FromCode(otherCode));
            result.Amount.Should().Be((decimal)expected);
        }

        [TestMethod]
        public async Task Given_ValidExchangeRateService_When_GettingExchangeRateForInvalidCurrency_Then_ThrowException()
        {
            // Arrange
            var sut = _fixture.Create<ExchangeRateService>();

            // Act
            var action = () => sut.GetExchangeRate(Currency.FromCode("DKK"), Currency.FromCode("JPY"));

            // Assert
            action.Should().Throw<ArgumentException>();
        }

        // And so many more positive / negative path unit tests... You get the idea!
    }
}
