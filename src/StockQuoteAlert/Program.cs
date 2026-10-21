using System.Globalization;

if (args.Length != 3)
{
    Console.Error.WriteLine("Uso: stock-quote-alert <ATIVO> <PRECO_VENDA> <PRECO_COMPRA>");
    Console.Error.WriteLine("Ex.: stock-quote-alert PETR4 22.67 22.59");
    return 1;
}

if (!decimal.TryParse(args[1], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal precoVenda))
{
    Console.Error.WriteLine($"Preço de venda inválido: '{args[1]}'. Use ponto como separador decimal (ex.: 22.67)");
    return 1;
}

if (!decimal.TryParse(args[2], NumberStyles.AllowDecimalPoint, CultureInfo.InvariantCulture, out decimal precoCompra))
{
    Console.Error.WriteLine($"Preço de compra inválido: '{args[2]}'. Use ponto como separador decimal (ex.: 22.59)");
    return 1;
}

if (precoVenda <= 0 || precoCompra <= 0)
{
    Console.Error.WriteLine("Os preços devem ser maiores que zero.");
    return 1;
}

if (precoVenda <= precoCompra)
{
    Console.Error.WriteLine("O preço de venda deve ser maior que o preço de compra.");
    return 1;
}
string ticker = args[0].ToUpperInvariant();

Console.WriteLine($"Monitorando {ticker} | venda > {precoVenda} | compra < {precoCompra}");

return 0;

