namespace MJP.MaerskOfflineTest.Services
{
    using MJP.MaerskOfflineTest.Models;
    using MJP.MaerskOfflineTest.Services.Interfaces;
    using NodaMoney;
    using System.Collections.Concurrent;
    using System.Threading.Tasks;

    /// <summary>
    /// An in-memory implementation of the <see cref="IDatabaseService"/>, keeping a sorted list of <see cref="ContainerBooking"/> for each voyage, in descending order as sorted by their timestamp.
    /// </summary>
    public class DatabaseService : IDatabaseService
    {
        private class ContainerBookingAverage
        {
            // Using a list as the in-memory database can prove problematic if limited by memory and adding a huge amount of objects
            // In this particular case, I have no information about the size of the dataset and I'll assume the internal List memory allocations are a non-issue
            // since it was specified in the description that read operations are prioritized
            // and we only encounter this problem when inserting (and only once whenever we pass the capacity of the list)
            public List<ContainerBooking> ContainerBookings { get; }
            public decimal CumulativeAverageUSD { get; set; }

            public ContainerBookingAverage()
            {
                this.ContainerBookings = new List<ContainerBooking>();
            }
        }

        // Use a ConcurrentDictionary for thread-safe purposes. The intention is to use this service as a Singleton because it keeps state, 
        // so we can possibly be called by multiple requests at the same time.
        // It is by no means a fast implementation though because of thread synchronization, but it's fiiiine.
        private readonly ConcurrentDictionary<string, ContainerBookingAverage> database;
        private readonly ILogger<DatabaseService> logger;
        private readonly IExchangeRateService exchangeRateService;

        public DatabaseService(ILogger<DatabaseService> logger, IExchangeRateService exchangeRateService)
        {
            this.database = new ConcurrentDictionary<string, ContainerBookingAverage>();
            this.logger = logger;
            this.exchangeRateService = exchangeRateService;
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentException"></exception>
        /// <exception cref="ArgumentNullException"></exception>
        public async Task AddContainerBookingForVoyage(string voyageCode, ContainerBooking booking)
        {
            if (string.IsNullOrEmpty(voyageCode))
            {
                this.logger.LogError($"Attempted to add container booking for invalid voyageCode of '{voyageCode}'");
                throw new ArgumentException($"'{nameof(voyageCode)}' cannot be null or empty.", nameof(voyageCode));
            }
            if (booking is null)
            {
                this.logger.LogError($"Attempted to add container booking for {nameof(voyageCode)} of '{voyageCode}' with null {nameof(booking)}");
                throw new ArgumentNullException(nameof(booking));
            }

            this.database.AddOrUpdate(voyageCode,
                (key) =>
                {
                    var newEntry = new ContainerBookingAverage();
                    newEntry.ContainerBookings.Add(booking);
                    newEntry.CumulativeAverageUSD = booking.Price.Amount;

                    this.logger.LogTrace($"Added new booking to non-existing voyageCode of '{voyageCode}'");

                    return newEntry;
                },
                (key, existingBookings) =>
                {
                    var exchangedPriceUSD = this.exchangeRateService.ExchangeMoney(booking.Price, Currency.FromCode("USD"));
                    // This should probably be done in a separate database function, since this now breaks the Single-Responsibility principle. This is just more convenient.
                    // Formula for cumulative average is stolen directly from wikipedia: https://en.wikipedia.org/wiki/Moving_average#Cumulative_average
                    existingBookings.CumulativeAverageUSD = (booking.Price.Amount + (existingBookings.ContainerBookings.Count * existingBookings.CumulativeAverageUSD)) 
                                                            / (existingBookings.ContainerBookings.Count + 1);

                    // To optimize toward read speeds for getting the price of the latest container bookings, we keep a sorted list in memory.
                    // This means it takes longer to insert, in return for making read faster.
                    // To do so, we need to figure out the correct index to insert our new ContainerBooking which is done using a BinarySearch to obtain O(log n) time complexity.
                    var index = existingBookings.ContainerBookings.BinarySearch(booking, new ContainerBookingDateComparer());
                    // If BinarySearch returns a negative number, then according to the documentation (https://docs.microsoft.com/en-us/dotnet/api/system.collections.generic.list-1.binarysearch?view=net-6.0)
                    // we can take the bitwise complement (as denoted by the ~ operator), to get the correct index to insert our new object at, for the list to remain sorted.
                    if (index < 0)
                    {
                        index = ~index;
                    }
                    existingBookings.ContainerBookings.Insert(index, booking);

                    this.logger.LogTrace($"Added new booking to existing voyageCode of '{voyageCode}'");

                    return existingBookings;
                });
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public async Task<IEnumerable<ContainerBooking>> GetContainerBookings(string voyageCode)
        {
            if (string.IsNullOrEmpty(voyageCode))
            {
                this.logger.LogError($"Attempted to get container bookings for invalid {nameof(voyageCode)} of '{voyageCode}'");
                throw new ArgumentNullException(nameof(voyageCode));
            }

            var success = this.database.TryGetValue(voyageCode, out var containerBookings);
            if (!success)
            {
                this.logger.LogError($"Attempted to get container bookings for non-existing {nameof(voyageCode)} of '{voyageCode}'");
                throw new ArgumentException($"ERROR! The given {nameof(voyageCode)} of '{voyageCode}' was not found in database.");
            }

            return containerBookings!.ContainerBookings.AsEnumerable();
        }

        /// <inheritdoc />
        /// <exception cref="ArgumentNullException"></exception>
        /// <exception cref="ArgumentException"></exception>
        public async Task<decimal> GetCumulativeAverage(string voyageCode)
        {
            if (string.IsNullOrEmpty(voyageCode))
            {
                this.logger.LogError($"Attempted to get container bookings for invalid {nameof(voyageCode)} of '{voyageCode}'");
                throw new ArgumentNullException(nameof(voyageCode));
            }

            var success = this.database.TryGetValue(voyageCode, out var containerBookings);
            if (!success)
            {
                this.logger.LogError($"Attempted to get cumulative average for non-existing {nameof(voyageCode)} of '{voyageCode}'");
                throw new ArgumentException($"ERROR! The given {nameof(voyageCode)} of '{voyageCode}' was not found in database.");
            }

            return containerBookings!.CumulativeAverageUSD;
        }
    }
}
