namespace StockQuoteAlert;

public record Quote(string Ticker, decimal Price, DateTimeOffset Time);