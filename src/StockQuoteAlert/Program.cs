using Microsoft.Extensions.Configuration;
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

AppSettings settings;

try
{
    IConfiguration configuration = new ConfigurationBuilder()
        .SetBasePath(AppContext.BaseDirectory)
        .AddJsonFile("appsettings.json", optional: false)
        .AddUserSecrets<Program>(optional: true)
        .AddEnvironmentVariables(prefix: "STOCKALERT_")
        .Build();

    settings = configuration.Get<AppSettings>() ?? new AppSettings();
}
catch (FileNotFoundException)
{
    Console.Error.WriteLine("Arquivo appsettings.json não encontrado ao lado do executável.");
    return 1;
}

List<string> configErrors = settings.Validate();

if (configErrors.Count > 0)
{
    Console.Error.WriteLine("Configuração inválida:");
    foreach (string error in configErrors)
    {
        Console.Error.WriteLine($"  - {error}");
    }
    return 1;
}

Console.WriteLine($"Monitorando {arguments.Ticker} | venda > {arguments.SellPrice} | compra < {arguments.BuyPrice}");

using var httpClient = new HttpClient();
IQuoteProvider quoteProvider = new BrapiQuoteProvider(httpClient);

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.Monitor.IntervalSeconds));

Console.WriteLine($"Consultando a cada {settings.Monitor.IntervalSeconds}s. Pressione Ctrl+C para encerrar.");

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
    // Ctrl+C pressionado: não é um erro, só saímos do loop
}

Console.WriteLine("Monitoramento encerrado.");

return 0;