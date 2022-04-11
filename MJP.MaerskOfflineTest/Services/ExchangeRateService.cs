namespace MJP.MaerskOfflineTest.Services
{
    using MJP.MaerskOfflineTest.Services.Interfaces;
    using NodaMoney;

    /// <summary>
    /// An in-memory implementation of the <see cref="IExchangeRateService"/> interface that only handles exchanges between EUR, USD and DKK.
    /// </summary>
    public class ExchangeRateService : IExchangeRateService
    {
        private readonly Dictionary<Currency, Dictionary<Currency, ExchangeRate>> currencyExchangeRates;
        private readonly ILogger<ExchangeRateService> logger;

        public ExchangeRateService(ILogger<ExchangeRateService> logger)
        {
            var EUR = Currency.FromCode("EUR");
            var USD = Currency.FromCode("USD");
            var DKK = Currency.FromCode("DKK");

            //These could even be dependency injected to make it easier for testing
            this.currencyExchangeRates = new Dictionary<Currency, Dictionary<Currency, ExchangeRate>>();
            this.AddNewExchangeRate(EUR, USD, 1.0923m);
            this.AddNewExchangeRate(EUR, DKK, 7.4378m);
            this.AddNewExchangeRate(USD, EUR, 0.9155m);
            //The following 3 can implicitly be calculated from the 3 above, but lets just be explicit to make it easier
            this.AddNewExchangeRate(USD, DKK, 6.8093m);
            this.AddNewExchangeRate(DKK, USD, 0.1469m);
            this.AddNewExchangeRate(DKK, EUR, 0.1344m);

            this.logger = logger;
        }

        private void AddNewExchangeRate(Currency current, Currency other, decimal rate)
        {
            if( !this.currencyExchangeRates.TryGetValue(current, out var currentRates) )
            {
                currentRates = new Dictionary<Currency, ExchangeRate>();
            }

            currentRates[other] = new ExchangeRate(current, other, rate);
            this.currencyExchangeRates[current] = currentRates;
        }

        /// <inheritdoc />
        public Money ExchangeMoney(Money current, Currency other)
        {
            if (current.Currency == other)
            {
                return current;
            }

            return this.GetExchangeRate(current.Currency, other).Convert(current);
        }

        /// <inheritdoc />
        public ExchangeRate GetExchangeRate(Currency current, Currency other)
        {
            if (this.currencyExchangeRates.TryGetValue(current, out var exchangeRates) && exchangeRates.TryGetValue(other, out var specificRate))
            {
                return specificRate;
            }

            this.logger.LogWarning($"Failed to find exchange rate between {current.Code} and {other.Code}");
            throw new ArgumentException($"ERROR! An exchange rate between {current.Code} and {other.Code} could not be found!");
        }
    }
}
