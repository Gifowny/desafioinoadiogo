using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;

namespace StockQuoteAlert;

public class BrapiQuoteProvider(HttpClient httpClient, string? token = null) : IQuoteProvider
{
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    public async Task<Quote> GetQuoteAsync(string ticker, CancellationToken cancellationToken = default)
    {
        string url = "https://brapi.dev/api/quote/" + Uri.EscapeDataString(ticker);

        // monta a requisição e, se houver token, envia no cabeçalho Authorization
        using var request = new HttpRequestMessage(HttpMethod.Get, url);

        if (!string.IsNullOrWhiteSpace(token))
        {
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        }

        using HttpResponseMessage httpResponse = await httpClient.SendAsync(request, cancellationToken);

        if (!httpResponse.IsSuccessStatusCode)
        {
            string message = httpResponse.StatusCode switch
            {
                HttpStatusCode.Unauthorized => "Token ausente ou inválido, ou ativo que exige token.",
                HttpStatusCode.NotFound => $"Ticker '{ticker}' não encontrado.",
                HttpStatusCode.TooManyRequests => "Limite de requisições do plano atingido.",
                _ => $"Erro ao obter cotação para o ticker '{ticker}'. Status: {(int)httpResponse.StatusCode} {httpResponse.ReasonPhrase}"
            };

            throw new HttpRequestException(message, null, httpResponse.StatusCode);
        }

        var response = await httpResponse.Content.ReadFromJsonAsync<BrapiResponse>(JsonOptions, cancellationToken);

        if (response?.Results is not { Length: > 0 })
        {
            throw new InvalidOperationException($"Não foi possível obter a cotação para o ticker '{ticker}'.");
        }

        var item = response.Results[0];

        return new Quote(item.Symbol, item.RegularMarketPrice, item.RegularMarketTime);
    }
}