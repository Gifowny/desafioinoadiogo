using StockQuoteAlert;

namespace StockQuoteAlert.Tests;

public class AlertEvaluatorTests
{
    [Fact]
    public void Sequencia_da_tabela_de_decisao_produz_os_alertas_esperados()
    {
        var evaluator = new AlertEvaluator(sellPrice: 58.30m, buyPrice: 58.10m);

        Assert.Equal(AlertType.None, evaluator.Evaluate(58.20m)); // 1: dentro
        Assert.Equal(AlertType.Sell, evaluator.Evaluate(58.35m)); // 2: saiu por cima
        Assert.Equal(AlertType.None, evaluator.Evaluate(58.40m)); // 3: continua acima
        Assert.Equal(AlertType.None, evaluator.Evaluate(58.38m)); // 4: continua acima
        Assert.Equal(AlertType.None, evaluator.Evaluate(58.25m)); // 5: voltou, rearma
        Assert.Equal(AlertType.Sell, evaluator.Evaluate(58.32m)); // 6: saiu de novo
        Assert.Equal(AlertType.Buy,  evaluator.Evaluate(58.05m)); // 7: de acima para abaixo
        Assert.Equal(AlertType.None, evaluator.Evaluate(58.30m)); // 8: igual à venda conta como dentro
    }

    [Fact]
    public void Preco_exatamente_igual_a_compra_na_primeira_consulta_nao_dispara_alerta()
    {
        var evaluator = new AlertEvaluator(sellPrice: 58.30m, buyPrice: 58.10m);

        Assert.Equal(AlertType.None, evaluator.Evaluate(58.10m));
    }

    [Fact]
    public void Primeira_consulta_ja_abaixo_da_compra_dispara_Buy()
    {
        var evaluator = new AlertEvaluator(sellPrice: 58.30m, buyPrice: 58.10m);

        Assert.Equal(AlertType.Buy, evaluator.Evaluate(58.05m));
    }

    [Fact]
    public void Rearm_faz_o_alerta_ser_disparado_de_novo()
    {
        var evaluator = new AlertEvaluator(sellPrice: 58.30m, buyPrice: 58.10m);
        Assert.Equal(AlertType.Sell, evaluator.Evaluate(58.35m));
        Assert.Equal(AlertType.None, evaluator.Evaluate(58.40m));
        evaluator.Rearm();
        Assert.Equal(AlertType.Sell, evaluator.Evaluate(58.40m));
    }
}