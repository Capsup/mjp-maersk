namespace MJP.MaerskOfflineTest.Models
{
    using NodaMoney;

    public class ContainerBooking
    {
        public ContainerBooking(string voyageCode, DateTimeOffset timestamp, Money price)
        {
            if (string.IsNullOrEmpty(voyageCode))
            {
                throw new ArgumentNullException(nameof(voyageCode));
            }
            this.VoyageCode = voyageCode;
            this.Timestamp = timestamp;
            this.Price = price;
        }

        public string VoyageCode { get; set; }
        public DateTimeOffset Timestamp { get; set; }
        public Money Price { get; set; }
    }

    /// <summary>
    /// An implementation of <see cref="IComparer{T}"/> that compares specifically on the Timestamp of two given instances of <see cref="ContainerBooking"/>.
    /// Returns comparisons in descending order.
    /// </summary>
    public class ContainerBookingDateComparer : IComparer<ContainerBooking>
    {
        public int Compare(ContainerBooking? x, ContainerBooking? y)
        {
            if (object.ReferenceEquals(x, y))
            {
                return 0;
            }
            if (x == null)
            {
                return -1;
            }
            if (y == null)
            {
                return 1;
            }

            return -1 * Comparer<DateTimeOffset>.Default.Compare(x.Timestamp, y.Timestamp);
        }
    }
}
