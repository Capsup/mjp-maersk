namespace MJP.MaerskOfflineTest.Services.Interfaces
{
    using NodaMoney;

    /// <summary>
    /// Interface for the ExchangeRateService to be used by the API.
    /// </summary>
    public interface IExchangeRateService
    {
        /// <summary>
        /// Get the current <see cref="ExchangeRate"/> with money currently in the <see cref="Currency"/> of <paramref name="current"/>, to be exchanged into <paramref name="other"/>.
        /// </summary>
        /// <param name="current">The Currency to exchange money from.</param>
        /// <param name="other">The Currency to exchange money into.</param>
        /// <returns>An <see cref="ExchangeRate"/> that allows exchanging any amount of Currency of <paramref name="current"/> into <paramref name="other"/>.</returns>
        ExchangeRate GetExchangeRate(Currency current, Currency other);

        /// <summary>
        /// Given a <see cref="Money"/> object denoting a <see cref="Currency"/> and an amount, returns a new <see cref="Money"/> object with the new amount in the currency of <paramref name="other"/>.
        /// </summary>
        /// <param name="current">The Money object to convert to <paramref name="other"/>.</param>
        /// <param name="other">The <see cref="Currency"/> to be used for the new <see cref="Money"/> object.</param>
        /// <returns>A new <see cref="Money"/> object with amount correctly exchanged to the <paramref name="other"/> Currency. If <paramref name="other"/> is equal to the Currency of <paramref name="current"/>, the same <see cref="Money"/> object is returned.</returns>
        Money ExchangeMoney(Money current, Currency other);
    }
}
