using System.Globalization;

namespace StockQuoteAlert;

public static class AlertMessage
{
    private static readonly CultureInfo PtBr = new("pt-BR");

    public static (string Subject, string Body) Build(AlertType alert, Quote quote, MonitorArguments arguments)
    {
        return alert switch
        {
            AlertType.Sell => (
                Subject: string.Create(PtBr, $"Alerta de VENDA: {quote.Ticker} a {quote.Price:C} (acima de {arguments.SellPrice:C})"),
                Body: string.Create(PtBr,
                    $"O preço de {quote.Ticker} subiu acima do preço de referência para venda.\n\n" +
                    $"Preço atual: {quote.Price:C}\n" +
                    $"Referência de venda: {arguments.SellPrice:C}\n" +
                    $"Horário da cotação: {quote.Time.ToLocalTime():dd/MM/yyyy HH:mm:ss}\n\n" +
                    $"Considere avaliar a venda.\n\n" +
                    $"---\nAlerta automático do Stock Quote Alert. As cotações podem ter atraso e este e-mail não é recomendação de investimento.")
            ),
            AlertType.Buy => (
                Subject: string.Create(PtBr, $"Alerta de COMPRA: {quote.Ticker} a {quote.Price:C} (abaixo de {arguments.BuyPrice:C})"),
                Body: string.Create(PtBr,
                    $"O preço de {quote.Ticker} caiu abaixo do preço de referência para compra.\n\n" +
                    $"Preço atual: {quote.Price:C}\n" +
                    $"Referência de compra: {arguments.BuyPrice:C}\n" +
                    $"Horário da cotação: {quote.Time.ToLocalTime():dd/MM/yyyy HH:mm:ss}\n\n" +
                    $"Considere avaliar a compra.\n\n" +
                    $"---\nAlerta automático do Stock Quote Alert. As cotações podem ter atraso e este e-mail não é recomendação de investimento.")
            ),
            AlertType.None => throw new ArgumentException("Tipo de alerta inválido para envio de e-mail."),
            _ => throw new ArgumentOutOfRangeException(nameof(alert), "Tipo de alerta desconhecido.")
        };
    }
}