using System.Net;
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
IQuoteProvider quoteProvider = new BrapiQuoteProvider(httpClient, settings.Brapi.Token);
INotifier notifier = new EmailNotifier(settings.Smtp, settings.Email);              // NOVO
var evaluator = new AlertEvaluator(arguments.SellPrice, arguments.BuyPrice);       // NOVO

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.Monitor.IntervalSeconds));

Console.WriteLine($"Consultando a cada {settings.Monitor.IntervalSeconds}s. Pressione Ctrl+C para encerrar.");
Console.WriteLine($"Token da brapi: {(string.IsNullOrWhiteSpace(settings.Brapi.Token) ? "não configurado (apenas PETR4, VALE3, MGLU3 e ITUB4)" : "configurado")}");
Console.WriteLine($"Alertas serão enviados para: {settings.Email.To}");                // NOVO

try
{
    do
    {
        try
        {
            Quote quote = await quoteProvider.GetQuoteAsync(arguments.Ticker, cts.Token);
            Console.WriteLine($"{quote.Ticker}: R$ {quote.Price} (negociado em {quote.Time.ToLocalTime():dd/MM/yyyy HH:mm:ss})");

            AlertType alert = evaluator.Evaluate(quote.Price);

            if (alert != AlertType.None)
            {
                var (subject, body) = AlertMessage.Build(alert, quote, arguments);
                string alertName = alert == AlertType.Sell ? "VENDA" : "COMPRA";

                try
                {
                    await notifier.SendAsync(subject, body, cts.Token);
                    Console.WriteLine($"Alerta de {alertName} enviado por e-mail.");
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    evaluator.Rearm();
                    Console.Error.WriteLine($"Falha ao enviar o alerta de {alertName}: {ex.Message} Nova tentativa na próxima consulta.");
                }
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
        {
            Console.Error.WriteLine($"Erro permanente: {ex.Message} Encerrando o monitoramento.");
            return 1;
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