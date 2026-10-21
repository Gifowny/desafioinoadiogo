using StockQuoteAlert;

MonitorArguments arguments;

try
{
    arguments = MonitorArguments.Parse(args);
}
catch (ArgumentException ex)
{
    Console.Error.WriteLine(ex.Message);
    return 1;
}

Console.WriteLine($"Monitorando {arguments.Ticker} | venda > {arguments.PrecoVenda} | compra < {arguments.PrecoCompra}");

return 0;
