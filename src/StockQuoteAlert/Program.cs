using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
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

using ILoggerFactory loggerFactory = LoggerFactory.Create(builder =>
    builder.AddSimpleConsole(options =>
    {
        options.TimestampFormat = "dd/MM/yyyy HH:mm:ss ";
        options.SingleLine = true;
    }));

ILogger logger = loggerFactory.CreateLogger("StockQuoteAlert");

using var httpClient = new HttpClient();
IQuoteProvider quoteProvider = new BrapiQuoteProvider(httpClient, settings.Brapi.Token);
INotifier notifier = new EmailNotifier(settings.Smtp, settings.Email);
var evaluator = new AlertEvaluator(arguments.SellPrice, arguments.BuyPrice);

using var cts = new CancellationTokenSource();

Console.CancelKeyPress += (_, e) =>
{
    e.Cancel = true;
    cts.Cancel();
};

using var timer = new PeriodicTimer(TimeSpan.FromSeconds(settings.Monitor.IntervalSeconds));

logger.LogInformation("Monitorando {Ticker} | venda > {SellPrice} | compra < {BuyPrice}",
    arguments.Ticker, arguments.SellPrice, arguments.BuyPrice);
logger.LogInformation("Consultando a cada {IntervalSeconds}s. Pressione Ctrl+C para encerrar.",
    settings.Monitor.IntervalSeconds);
logger.LogInformation("Token da brapi: {TokenStatus}",
    string.IsNullOrWhiteSpace(settings.Brapi.Token) ? "não configurado (apenas PETR4, VALE3, MGLU3 e ITUB4)" : "configurado");
logger.LogInformation("Alertas serão enviados para: {EmailTo}", settings.Email.To);

try
{
    do
    {
        try
        {
            Quote quote = await quoteProvider.GetQuoteAsync(arguments.Ticker, cts.Token);
            logger.LogInformation("{Ticker}: R$ {Price} (negociado em {QuoteTime:dd/MM/yyyy HH:mm:ss})",
                quote.Ticker, quote.Price, quote.Time.ToLocalTime());

            AlertType alert = evaluator.Evaluate(quote.Price);

            if (alert != AlertType.None)
            {
                var (subject, body) = AlertMessage.Build(alert, quote, arguments);
                string alertName = alert == AlertType.Sell ? "VENDA" : "COMPRA";

                try
                {
                    await notifier.SendAsync(subject, body, cts.Token);
                    logger.LogInformation("Alerta de {AlertName} enviado por e-mail.", alertName);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    evaluator.Rearm();
                    logger.LogError("Falha ao enviar o alerta de {AlertName}: {ErrorMessage} Nova tentativa na próxima consulta.",
                        alertName, ex.Message);
                }
            }
        }
        catch (HttpRequestException ex) when (ex.StatusCode is HttpStatusCode.Unauthorized or HttpStatusCode.NotFound)
        {
            logger.LogError("Erro permanente: {ErrorMessage} Encerrando o monitoramento.", ex.Message);
            return 1;
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            logger.LogWarning("Erro ao obter a cotação: {ErrorMessage}", ex.Message);
        }
    }
    while (await timer.WaitForNextTickAsync(cts.Token));
}
catch (OperationCanceledException)
{
    // Ctrl+C pressionado: não é um erro, só saímos do loop
}

logger.LogInformation("Monitoramento encerrado.");

return 0;