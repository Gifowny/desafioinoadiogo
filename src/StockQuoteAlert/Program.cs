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

Console.WriteLine($"Recebi {args.Length} argumentos");
Console.WriteLine($"Ativo: {args[0]}");
Console.WriteLine($"Venda: {precoVenda}");
Console.WriteLine($"Compra: {precoCompra}");
return 0;

