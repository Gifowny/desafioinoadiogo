using System.Globalization;

namespace StockQuoteAlert;

public record MonitorArguments(string Ticker, decimal SellPrice, decimal BuyPrice)
{
    public static MonitorArguments Parse(string[] args)
    {
        if (args.Length != 3)
        {
            throw new ArgumentException("Uso: stock-quote-alert <ATIVO> <PRECO_VENDA> <PRECO_COMPRA>\nEx.: stock-quote-alert PETR4 22.67 22.59");
        }

        if (!decimal.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal precoVenda))
        {
            throw new ArgumentException($"Preço de venda inválido: '{args[1]}'. Use ponto como separador decimal (ex.: 22.67)");
        }

        if (!decimal.TryParse(args[2], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal precoCompra))
        {
            throw new ArgumentException($"Preço de compra inválido: '{args[2]}'. Use ponto como separador decimal (ex.: 22.59)");
        }

        if (precoVenda <= 0 || precoCompra <= 0)
        {
            throw new ArgumentException("Os preços devem ser maiores que zero.");
        }

        if (precoVenda <= precoCompra)
        {
            throw new ArgumentException("O preço de venda deve ser maior que o preço de compra.");
        }

        string ticker = args[0].ToUpperInvariant();

        return new MonitorArguments(ticker, precoVenda, precoCompra);
    }
}