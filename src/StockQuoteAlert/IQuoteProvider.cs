namespace StockQuoteAlert;

public interface IQuoteProvider
{
    Task<Quote> GetQuoteAsync(string ticker, CancellationToken cancellationToken = default);
}