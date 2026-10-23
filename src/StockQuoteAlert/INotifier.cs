namespace StockQuoteAlert;

public interface INotifier
{
    Task SendAsync(string subject, string body, CancellationToken cancellationToken = default);
}