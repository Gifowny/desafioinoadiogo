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

using var httpClient = new HttpClient();
IQuoteProvider quoteProvider = new BrapiQuoteProvider(httpClient);

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

using var timer = new PeriodicTimer(TimeSpan.FromSeconds(10));

Console.WriteLine("Pressione Ctrl+C para encerrar.");

try
{
    do
    {
        try
        {
            Quote quote = await quoteProvider.GetQuoteAsync(arguments.Ticker, cts.Token);
            Console.WriteLine($"{quote.Ticker}: R$ {quote.Price} (negociado em {quote.Time.ToLocalTime():dd/MM/yyyy HH:mm:ss})");
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            Console.Error.WriteLine($"Erro ao obter a cotação: {ex.Message}");
        }
    }
    while (await timer.WaitForNextTickAsync(cts.Token));
}
catch (OperationCanceledException)
{
    // vazio de proposito*
}

Console.WriteLine("Monitoramento encerrado.");

return 0;