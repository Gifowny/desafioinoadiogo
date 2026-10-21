# Diário de desenvolvimento — Stock Quote Alert

Registro passo a passo do desenvolvimento do desafio técnico: uma aplicação de console em C# que monitora a cotação de um ativo da B3 e envia alertas por e-mail quando o preço sobe acima de um nível de venda ou cai abaixo de um nível de compra.

**Uso esperado:**

```
stock-quote-alert.exe PETR4 22.67 22.59
```

| Parâmetro | Significado |
|---|---|
| `PETR4` | Ativo a ser monitorado |
| `22.67` | Preço de referência para **venda** (alerta quando o preço fica acima) |
| `22.59` | Preço de referência para **compra** (alerta quando o preço fica abaixo) |

---

## Fase 1 — Fundação

### Passo 1 — Setup do ambiente

- Repositório criado e clonado via GitHub Desktop; branch renomeada de `master` para `main` (padrão atual do GitHub).
- **Problema:** a máquina tinha apenas o **runtime** do .NET (que executa aplicações), sem o **SDK** (que cria e compila projetos). Erro: `No .NET SDKs were found`.
- **Solução:** instalação do .NET 10 SDK (versão LTS).
- Estrutura criada:
  ```bash
  dotnet new sln -n StockQuoteAlert
  dotnet new console -n StockQuoteAlert -o src/StockQuoteAlert
  dotnet sln add src/StockQuoteAlert
  dotnet new gitignore
  ```
- A solution foi gerada como `StockQuoteAlert.slnx`, o novo formato padrão do .NET 10 (XML, mais legível que o antigo `.sln`).
- `<AssemblyName>stock-quote-alert</AssemblyName>` definido no `.csproj` para que o executável tenha exatamente o nome pedido no enunciado (`stock-quote-alert.exe`).
- Código organizado em `src/`, deixando espaço para um futuro projeto de testes em `tests/`.
- `bin/` e `obj/` ignorados pelo `.gitignore` do .NET.

**Estrutura do repositório:**

```
desafioinoadiogo/
├── src/
│   └── StockQuoteAlert/
│       ├── Program.cs
│       └── StockQuoteAlert.csproj
├── StockQuoteAlert.slnx
├── .gitignore
├── README.md
└── documentation.md
```

#### Problemas de ambiente resolvidos

- A extensão **Code Runner** (botão ▶ do VS Code) tenta executar C# com o `scriptcs`, que não está instalado. **Solução:** executar via `dotnet run` no terminal.
- O Git Bash do VS Code inseria `[200~` ao colar comandos (*bracketed paste*). **Solução:** desativar no `~/.inputrc` (`set enable-bracketed-paste off`) ou usar o PowerShell.
- Terminal aberto antes da instalação do SDK não reconhecia o comando `dotnet`. **Solução:** reiniciar o VS Code.

**Como executar durante o desenvolvimento:**

```bash
dotnet run --project src/StockQuoteAlert -- PETR4 22.67 22.59
```

O `--` separa os argumentos do `dotnet` dos argumentos do programa.

---

### Passo 2 — Leitura e validação dos argumentos

- Validação da quantidade de argumentos (exatamente 3), com mensagem de uso e código de saída `1` em caso de erro.
- Mensagens de erro escritas em `Console.Error` (stderr), separadas da saída normal.
- `return 0` ao final do programa: com um `return 1` no código, o C# exige que todos os caminhos retornem um valor (erro `CS0161`).
- Conversão dos preços com `decimal.TryParse` + `CultureInfo.InvariantCulture`:
  - `decimal` em vez de `double` para evitar erros de arredondamento com valores monetários;
  - `TryParse` em vez de `Parse` para tratar entradas inválidas sem exceções;
  - `InvariantCulture` para que o comportamento não dependa do idioma do sistema operacional.
- **Bug encontrado:** com `NumberStyles.Number`, a entrada `22,67` era aceita como `2267`, porque na cultura invariante a vírgula é separador de milhar. Nenhum erro era exibido, e a validação "venda > compra" também passaria (`2267 > 22.59`).
- **Decisão:** usar `NumberStyles.AllowDecimalPoint`, que aceita apenas dígitos e ponto. A vírgula passa a ser rejeitada com uma mensagem que orienta o formato correto (`Use ponto como separador decimal (ex.: 22.67)`). Alternativa considerada: trocar vírgula por ponto automaticamente, descartada por gerar ambiguidade em valores como `1,234.56`.
- Validação de regras de negócio: preços maiores que zero e preço de venda maior que o de compra.
- Ticker normalizado para maiúsculas com `ToUpperInvariant()` (ex.: `petr4` → `PETR4`).
- Mensagem final de confirmação: `Monitorando PETR4 | venda > 22.67 | compra < 22.59`.
- Uso de **guard clauses** (`if` sequenciais com saída antecipada) em vez de `if` aninhados, mantendo o código linear e legível.

**Testes manuais realizados:**

| Entrada | Resultado |
|---|---|
| `PETR4 22.67 22.59` | Monitorando PETR4 \| venda > 22.67 \| compra < 22.59 |
| (sem argumentos) | Mensagem de uso |
| `PETR4 abc 22.59` | Preço de venda inválido |
| `PETR4 22.67 xyz` | Preço de compra inválido |
| `PETR4 22,67 22.59` | Preço de venda inválido (orienta usar ponto) |
| `petr4 22.67 22.59` | Ticker convertido para PETR4 |
| `PETR4 22.59 22.67` | Venda deve ser maior que compra |
| `PETR4 0 22.59` | Preços devem ser maiores que zero |

---

### Passo 3 — Refatoração da validação

- **Motivação:** com todo o código no `Program.cs`, o arquivo cresceria demais com a API de cotação, o envio de e-mail e o loop de monitoramento. Além disso, código solto no `Program.cs` não pode ser coberto por testes unitários.
- Criado o arquivo `MonitorArguments.cs` com um `record MonitorArguments(string Ticker, decimal PrecoVenda, decimal PrecoCompra)`:
  - `record` foi escolhido por ser imutável: os preços de referência não podem ser alterados por engano durante a execução;
  - a validação foi movida para o método `static Parse(string[] args)`, que devolve os dados prontos.
- Os erros passaram a ser sinalizados com `throw new ArgumentException(...)` em vez de `Console.Error.WriteLine` + `return 1`.
- **Separação de responsabilidades:** a classe apenas valida e lança a exceção; imprimir a mensagem e encerrar o programa é responsabilidade do `Program.cs`. Uma primeira versão imprimia o erro dentro da classe e também no `Program.cs`, o que duplicaria a mensagem; a impressão foi removida da classe.
- O `Program.cs` foi reduzido a um orquestrador: chama `MonitorArguments.Parse` dentro de um `try/catch` e, em caso de `ArgumentException`, exibe a mensagem em stderr e sai com código `1`.
- **Validação:** os 8 testes manuais do Passo 2 foram repetidos e produziram saídas idênticas, confirmando que a refatoração não alterou o comportamento.

### Passo 4 — Consulta de cotação

- API escolhida: **brapi.dev**, especializada em ativos da B3. Os ativos PETR4, VALE3, MGLU3 e ITUB4 funcionam sem token, o que facilita testes; no plano gratuito as cotações têm atraso de cerca de 15 minutos.
- Endpoint: `GET https://brapi.dev/api/quote/{ticker}`. O preço fica em `results[0].regularMarketPrice`.
- Modelos de desserialização em `BrapiModels.cs`:
  - `BrapiResponse(BrapiQuote[] Results)` representa a raiz do JSON;
  - `BrapiQuote(string Symbol, decimal RegularMarketPrice, DateTimeOffset RegularMarketTime)` representa cada ativo.
- Apenas os campos necessários foram declarados (princípio **YAGNI**). Máxima e mínima do dia foram consideradas para o corpo do e-mail, mas ficaram de fora por enquanto.
- Array em vez de `List`, já que a coleção só é lida.
- Propriedades em PascalCase, seguindo a convenção do C#; a leitura do JSON (em camelCase) será configurada para ignorar diferença de maiúsculas/minúsculas.
- `DateTimeOffset` para o horário da cotação, preservando o fuso horário informado pela API.
- Criado o modelo de domínio `Quote(string Ticker, decimal Price, DateTimeOffset Time)`, independente da brapi. O restante do programa trabalha apenas com `Quote`, e não com os tipos específicos da API.
- Criada a interface `IQuoteProvider` com o método `Task<Quote> GetQuoteAsync(string ticker, CancellationToken cancellationToken = default)`:
  - permite trocar a fonte de cotações sem alterar o restante do código;
  - permite criar um provedor falso nos testes unitários, sem depender da internet;
  - `CancellationToken` para interromper requisições em andamento (será ligado ao Ctrl+C).
- **Decisão de nomenclatura:** código em inglês (padrão da linguagem e do mercado); textos exibidos ao usuário em português.
- **Erro encontrado:** uma primeira versão do `Quote.cs` continha variáveis soltas em vez de um `record`. Como só um arquivo do projeto pode ter *top-level statements* (o `Program.cs`), o compilador acusou erro e a extensão do VS Code passou a marcar erros falsos em outros arquivos. Também havia um namespace incorreto (`IQuoteProvider` em vez de `StockQuoteAlert`). O `dotnet build` foi usado para identificar os erros reais.

## Commits

| Commit | Descrição |
|---|---|
| `chore: setup inicial da solution` | Solution, projeto console, `.gitignore` e `AssemblyName` |
| `feat: leitura dos argumentos da linha de comando` | Leitura e validação da quantidade de argumentos |
| `fix: rejeita vírgula como separador decimal nos preços` | Correção do bug `22,67` → `2267` |
| `feat: validação das regras dos argumentos` | Preços positivos, venda > compra, ticker em maiúsculas |
| `refactor: extrai validação de argumentos para classe própria` | Validação movida para `MonitorArguments`; `Program.cs` vira orquestrador |