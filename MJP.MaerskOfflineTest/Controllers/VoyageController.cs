namespace MJP.MaerskOfflineTest.Controllers
{
    using Microsoft.AspNetCore.Mvc;
    using MJP.MaerskOfflineTest.Models;
    using MJP.MaerskOfflineTest.Services.Interfaces;
    using NodaMoney;

    [Route("api/[controller]")]
    [ApiController]
    public class VoyageController : ControllerBase
    {
        private readonly ILogger<VoyageController> logger;
        private readonly IDatabaseService databaseService;
        private readonly IExchangeRateService exchangeRateService;

        public VoyageController(ILogger<VoyageController> logger, IDatabaseService databaseService, IExchangeRateService exchangeRateService)
        {
            this.logger = logger;
            this.databaseService = databaseService;
            this.exchangeRateService = exchangeRateService;
        }

        /// <summary>
        /// Adds a new ContainerBooking to the given voyageCode.
        /// </summary>
        /// <param name="voyageCode">The key for looking up in the database. Must not be null or empty.</param>
        /// <param name="price">The price of the container booking.</param>
        /// <param name="currency">The currency of the price for the container booking. Cannot be null or empty.</param>
        /// <param name="timestamp">The timestamp to use for the container booking. Must be of the format "yyyy'-'MM'-'dd'T'HH':'mm':'ss'.'fffffffzzz".</param>
        /// <exception cref="ArgumentException"></exception>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult> UpdatePrice(string voyageCode, decimal price, CurrencyEnum currency, DateTimeOffset timestamp)
        {
            if (string.IsNullOrEmpty(voyageCode))
            {
                throw new ArgumentException($"'{nameof(voyageCode)}' cannot be null or empty.", nameof(voyageCode));
            }
            // I'm going to assume that a timestamp that carries the default value is invalid. 
            if (timestamp == default)
            {
                throw new ArgumentException($"'{nameof(timestamp)}' cannot be null or default.", nameof(timestamp));
            }

            try
            {
                var newCurrency = Currency.FromCode(Enum.GetName(typeof(CurrencyEnum), currency));
                var newContainerBooking = new ContainerBooking(voyageCode, timestamp, new Money(price, newCurrency));
                await this.databaseService.AddContainerBookingForVoyage(voyageCode, newContainerBooking);

                return Ok();
            }
            catch (Exception e) when (e is not ArgumentException and not ArgumentNullException)
            {
                this.logger.LogError($"An unexpected error occurred while attempting to add container booking to {nameof(voyageCode)} of '{voyageCode}'");
                throw;
            }
        }

        /// <summary>
        /// Returns the average price for all container bookings for the given voyageCode.
        /// </summary>
        /// <param name="voyageCode">The key for looking up in the database. Must not be null or empty.</param>
        /// <param name="currency">The currency of the price for the container booking. Cannot be null or empty.</param>
        /// <returns>The cumulative average denoted by USD, for all container bookings of the given voyageCode.</returns>
        /// <exception cref="ArgumentException"></exception>
        [HttpGet("GetAverage")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<decimal>> GetAverage(string voyageCode, CurrencyEnum currency)
        {
            if (string.IsNullOrEmpty(voyageCode))
            {
                throw new ArgumentException($"'{nameof(voyageCode)}' cannot be null or empty.", nameof(voyageCode));
            }

            try
            {
                var newCurrency = Currency.FromCode(Enum.GetName(typeof(CurrencyEnum), currency));
                var cumulativeAverage = await this.databaseService.GetCumulativeAverage(voyageCode);
                var exchangedMoney = this.exchangeRateService.ExchangeMoney(new Money(cumulativeAverage, Currency.FromCode("USD")), newCurrency).Amount;

                return Ok(exchangedMoney);
            }
            catch (Exception e) when (e is not ArgumentException and not ArgumentNullException)
            {
                this.logger.LogError($"An unexpected error occurred while attempting to get the cumulative average with {nameof(voyageCode)} of '{voyageCode}'");
                throw;
            }
            catch (Exception)
            {
                return BadRequest("Invalid input parameters");
            }
        }

        /// <summary>
        /// Returns the last n prices of container bookings for the given voyageCode, ordered in descending order by a booking's timestamp. 
        /// </summary>
        /// <param name="voyageCode">The key for looking up in the database. Must not be null or empty.</param>
        /// <param name="currency">The currency of the price for the container booking. Cannot be null or empty.</param>
        /// <param name="count">The amount of prices to return, starting from the newest ContainerBooking.</param>
        /// <returns>The first n prices from the given voyageCode, ordered in descending order by their timestamp, converted to the given currency.</returns>
        /// <exception cref="ArgumentException"></exception>
        [HttpGet("GetLastPrices")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        public async Task<ActionResult<decimal[]>> GetLastPrices(string voyageCode, CurrencyEnum currency, int count = 10)
        {
            if (string.IsNullOrEmpty(voyageCode))
            {
                throw new ArgumentException($"'{nameof(voyageCode)}' cannot be null or empty.", nameof(voyageCode));
            }

            try
            {
                var newCurrency = Currency.FromCode(Enum.GetName(typeof(CurrencyEnum), currency));
                var containerBookings = await this.databaseService.GetContainerBookings(voyageCode);

                return Ok(containerBookings
                    .Take(count)
                    .Select(booking => booking.Price)
                    .Select(price => this.exchangeRateService.ExchangeMoney(price, newCurrency).Amount)
                    .ToArray());
            }
            catch (Exception e) when (e is not ArgumentException and not ArgumentNullException)
            {
                this.logger.LogError($"An unexpected error occurred while attempting to get the last prices for {nameof(voyageCode)} of '{voyageCode}'");
                throw;
            }
        }
    }
}
