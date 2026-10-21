using System.Net.Http.Json;
using System.Text.Json;

namespace StockQuoteAlert;

public class BrapiQuoteProvider(HttpClient httpClient) : IQuoteProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Quote> GetQuoteAsync(string ticker, CancellationToken cancellationToken = default)
    {
        string url = "https://brapi.dev/api/quote/" + Uri.EscapeDataString(ticker);

        var response = await httpClient.GetFromJsonAsync<BrapiResponse>(url, JsonOptions, cancellationToken);

        if (response?.Results is not { Length: > 0 })
        {
            throw new InvalidOperationException($"Não foi possível obter a cotação para o ticker '{ticker}'.");
        }

        var item = response.Results[0];

        return new Quote(item.Symbol, item.RegularMarketPrice, item.RegularMarketTime);
    }
}