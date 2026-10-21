if (args.Length != 3)
{
    Console.Error.WriteLine("Uso: stock-quote-alert <ATIVO> <PRECO_VENDA> <PRECO_COMPRA>");
    Console.Error.WriteLine("Ex.: stock-quote-alert PETR4 22.67 22.59");
    return 1;
}
Console.WriteLine($"Recebi {args.Length} argumentos");
Console.WriteLine($"Ativo: {args[0]}");
Console.WriteLine($"Venda: {args[1]}");
Console.WriteLine($"Compra: {args[2]}");
return 0;