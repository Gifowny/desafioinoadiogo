namespace StockQuoteAlert;

public enum PriceZone
{
    Inside,
    AboveSell,
    BelowBuy
}

public enum AlertType
{
    None,
    Sell,
    Buy
}

public class AlertEvaluator(decimal sellPrice, decimal buyPrice)
{
    private PriceZone _lastZone = PriceZone.Inside;

    public AlertType Evaluate(decimal price)
    {
        if (price > sellPrice)
        {
            if (_lastZone == PriceZone.AboveSell)
                return AlertType.None;

            _lastZone = PriceZone.AboveSell;
            return AlertType.Sell;
        }
        else if (price < buyPrice)
        {
            if (_lastZone == PriceZone.BelowBuy)
                return AlertType.None;

            _lastZone = PriceZone.BelowBuy;
            return AlertType.Buy;
        }
        else
        {
            // voltar para a faixa só rearma o alerta, não manda e-mail
            _lastZone = PriceZone.Inside;
            return AlertType.None;
        }
    }
}