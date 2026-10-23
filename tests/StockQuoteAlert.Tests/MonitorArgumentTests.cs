using StockQuoteAlert;

namespace StockQuoteAlert.Tests;

public class MonitorArgumentsTests
{
    [Fact]
    public void Argumentos_validos_sao_convertidos_e_ticker_fica_em_maiusculas()
    {
        var arguments = MonitorArguments.Parse(["petr4", "22.67", "22.59"]);

        Assert.Equal("PETR4", arguments.Ticker);
        Assert.Equal(22.67m, arguments.SellPrice);
        Assert.Equal(22.59m, arguments.BuyPrice);
    }

    [Theory]
    [InlineData("PETR4", "abc", "22.59")]   // venda não numérica
    [InlineData("PETR4", "22.67", "def")]   // compra não numérica
    [InlineData("PETR4", "22,67", "22.59")] // vírgula como separador decimal
    [InlineData("PETR4", "22.59", "22.67")] // venda menor que a compra
    [InlineData("PETR4", "22.67", "22.67")] // venda igual à compra
    [InlineData("PETR4", "22.67", "0.00")]  // preço zero
    [InlineData("PETR4", "22.67", "-1.00")] // preço negativo
    public void Argumentos_invalidos_lancam_ArgumentException(string ticker, string sell, string buy)
    {
        Assert.Throws<ArgumentException>(() => MonitorArguments.Parse([ticker, sell, buy]));
    }

    [Fact]
    public void Quantidade_errada_de_argumentos_lanca_ArgumentException()
    {
        var ex = Assert.Throws<ArgumentException>(() => MonitorArguments.Parse(["PETR4", "22.67"]));

        Assert.StartsWith("Uso:", ex.Message);
    }
}