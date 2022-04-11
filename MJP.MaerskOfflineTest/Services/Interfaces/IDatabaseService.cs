namespace MJP.MaerskOfflineTest.Services.Interfaces
{
    using MJP.MaerskOfflineTest.Models;
    using NodaMoney;

    /// <summary>
    /// Interface for the DatabaseService to be used by the API.
    /// </summary>
    public interface IDatabaseService
    {
        /// <summary>
        /// Add a new ContainerBooking to the Voyage with the given voyageCode in the database.
        /// If no such Voyage exists, insert a new Voyage into the database and add the given booking.
        /// As a side-effect, will update the accumulative average for the voyage in particular.
        /// </summary>
        /// <param name="voyageCode">The key for looking up in the database. Must not be null or empty.</param>
        /// <param name="booking">The ContainerBooking object to insert into the database. Must not be null.</param>
        /// <returns>An empty Task.</returns>
        public Task AddContainerBookingForVoyage(string voyageCode, ContainerBooking booking);
        /// <summary>
        /// Get all booking containers for the given voyageCode.
        /// </summary>
        /// <param name="voyageCode">The key for looking up in the database. Must not be null or empty.</param>
        /// <returns>An empty Task.</returns>
        public Task<IEnumerable<ContainerBooking>> GetContainerBookings(string voyageCode);

        /// <summary>
        /// Get the calculated cumulative average for the given voyageCode.
        /// </summary>
        /// <param name="voyageCode">The key for looking up in the database. Must not be null or empty.</param>
        /// <returns>The cumulative average value. Always denoted in USD.</returns>
        public Task<decimal> GetCumulativeAverage(string voyageCode);
    }
}
