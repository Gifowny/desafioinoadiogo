namespace StockQuoteAlert;

public record BrapiResponse(BrapiQuote[] Results);

public record BrapiQuote(string Symbol, decimal RegularMarketPrice, DateTimeOffset RegularMarketTime);
