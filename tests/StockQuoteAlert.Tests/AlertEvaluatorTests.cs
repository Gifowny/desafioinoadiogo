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

        Assert.Equal(AlertType.Sell, evaluator.Evaluate(58.35m)); // dispara Sell
        Assert.Equal(AlertType.None, evaluator.Evaluate(58.40m)); // sem rearme: não repete
        evaluator.Rearm();
        Assert.Equal(AlertType.Sell, evaluator.Evaluate(58.40m)); // com rearme: dispara de novo
    }

    [Fact]
    public void Histerese_em_faixa_larga_so_rearma_apos_a_margem()
    {
        // venda 60, margem 0,5%: linha de rearme em 59.70 (o meio da faixa, 58, fica mais longe)
        var evaluator = new AlertEvaluator(sellPrice: 60.00m, buyPrice: 56.00m, hysteresisPercent: 0.5m);

        Assert.Equal(AlertType.Sell, evaluator.Evaluate(60.10m)); // saiu por cima
        Assert.Equal(AlertType.None, evaluator.Evaluate(59.80m)); // voltou pouco: não rearma
        Assert.Equal(AlertType.None, evaluator.Evaluate(60.05m)); // oscilação: sem e-mail repetido
        Assert.Equal(AlertType.None, evaluator.Evaluate(59.60m)); // abaixo de 59.70: rearma
        Assert.Equal(AlertType.Sell, evaluator.Evaluate(60.10m)); // nova saída real: dispara
    }

    [Fact]
    public void Histerese_em_faixa_estreita_rearma_no_meio_da_faixa()
    {
        // faixa de 58.10 a 58.30, menor que a margem de 0,5%: a linha de rearme fica no meio, em 58.20
        var evaluator = new AlertEvaluator(sellPrice: 58.30m, buyPrice: 58.10m, hysteresisPercent: 0.5m);

        Assert.Equal(AlertType.Sell, evaluator.Evaluate(58.31m)); // saiu por cima
        Assert.Equal(AlertType.None, evaluator.Evaluate(58.29m)); // voltou pouco: não rearma
        Assert.Equal(AlertType.None, evaluator.Evaluate(58.31m)); // oscilação: sem e-mail repetido
        Assert.Equal(AlertType.None, evaluator.Evaluate(58.29m));
        Assert.Equal(AlertType.None, evaluator.Evaluate(58.31m));
        Assert.Equal(AlertType.None, evaluator.Evaluate(58.15m)); // abaixo do meio (58.20): rearma
        Assert.Equal(AlertType.Sell, evaluator.Evaluate(58.31m)); // nova saída real: dispara
    }

    [Fact]
    public void Histerese_nao_atrasa_o_alerta_da_zona_oposta()
    {
        var evaluator = new AlertEvaluator(sellPrice: 58.30m, buyPrice: 58.10m, hysteresisPercent: 0.5m);

        Assert.Equal(AlertType.Sell, evaluator.Evaluate(58.35m));
        Assert.Equal(AlertType.None, evaluator.Evaluate(58.25m)); // ainda acima do meio da faixa
        Assert.Equal(AlertType.None, evaluator.Evaluate(58.20m)); // exatamente no meio: rearma
        Assert.Equal(AlertType.None, evaluator.Evaluate(58.10m)); // igual à compra: ainda dentro
        Assert.Equal(AlertType.Buy,  evaluator.Evaluate(58.05m)); // abaixo da compra: dispara na hora
        Assert.Equal(AlertType.None, evaluator.Evaluate(58.00m)); // continua abaixo: não repete
    }
}