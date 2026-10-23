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

public class AlertEvaluator(decimal sellPrice, decimal buyPrice, decimal hysteresisPercent = 0m)
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
            decimal margin = hysteresisPercent / 100m;
            decimal midpoint = (sellPrice + buyPrice) / 2;

            // a linha de rearme nunca passa do meio da faixa, para funcionar mesmo com faixas estreitas
            decimal sellRearmLine = Math.Max(sellPrice * (1 - margin), midpoint);
            decimal buyRearmLine = Math.Min(buyPrice * (1 + margin), midpoint);

            // ainda perto da borda de onde saiu: não rearma
            if (_lastZone == PriceZone.AboveSell && price > sellRearmLine)
                return AlertType.None;

            if (_lastZone == PriceZone.BelowBuy && price < buyRearmLine)
                return AlertType.None;

            _lastZone = PriceZone.Inside;
            return AlertType.None;
        }
    }

    public void Rearm()
    {
        _lastZone = PriceZone.Inside;
    }
}