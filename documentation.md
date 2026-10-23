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
- Implementado o `BrapiQuoteProvider`, que cumpre o contrato `IQuoteProvider`:
  - recebe o `HttpClient` pelo construtor (**injeção de dependência**), em vez de criá-lo internamente; quem usa a classe decide qual cliente HTTP passar;
  - usa construtor primário (`class BrapiQuoteProvider(HttpClient httpClient)`), sintaxe moderna do C#;
  - monta a URL com `Uri.EscapeDataString(ticker)` para evitar que caracteres especiais quebrem a requisição;
  - usa `GetFromJsonAsync<BrapiResponse>` para fazer a requisição e desserializar o JSON em uma única chamada;
  - `JsonSerializerDefaults.Web` para aceitar o camelCase do JSON nas propriedades em PascalCase;
  - valida a resposta com *pattern matching* (`response?.Results is not { Length: > 0 }`), cobrindo resposta nula e lista vazia em uma única condição, e lança `InvalidOperationException` com mensagem clara;
  - converte `BrapiQuote` em `Quote`, mantendo os tipos da brapi isolados dentro do provedor.
  - O `Program.cs` passou a buscar e exibir a cotação atual:
  - `using var` no `HttpClient` para liberar os recursos de rede ao final;
  - a variável é declarada com o tipo da interface (`IQuoteProvider`), de modo que o programa depende apenas do contrato;
  - horário convertido de UTC para o fuso local (`ToLocalTime()`) e formatado no padrão brasileiro;
  - `catch ... when` captura apenas os erros esperados (`HttpRequestException`, `InvalidOperationException`); erros inesperados não são escondidos.

**Testes manuais:**

| Entrada | Resultado |
|---|---|
| `PETR4 58.30 58.10` | `PETR4: R$ 57.75 (negociado em ... 18:30:30)` |
| `XXXX9 58.30 58.10` | `Erro ao buscar cotação: ... 401 (Unauthorized)` |

- **Achado:** sem token, a brapi responde 401 para qualquer ativo fora dos quatro gratuitos (PETR4, VALE3, MGLU3, ITUB4). Assim, não é possível distinguir um ticker inexistente de um ticker que exige autenticação, e a mensagem exibida é técnica e em inglês. **Encaminhamento:** token opcional na configuração e mensagens amigáveis por código HTTP (Passo 5).
- **Limitação observada:** fora do horário de pregão o preço não se altera; no plano gratuito há também atraso de cerca de 15 minutos.
- Implementado o monitoramento contínuo:
  - `PeriodicTimer` com intervalo de 10 segundos (fixo por enquanto; será movido para a configuração);
  - `do ... while` para que a primeira busca aconteça imediatamente, sem esperar o primeiro intervalo;
  - `CancellationTokenSource` + `Console.CancelKeyPress` para encerramento limpo com Ctrl+C: o evento cancela o token em vez de matar o processo, o `PeriodicTimer` lança `OperationCanceledException` e o programa sai com a mensagem "Monitoramento encerrado." e código 0;
  - o mesmo token é repassado ao `GetQuoteAsync`, permitindo interromper uma requisição em andamento.
- **Decisão:** falhas na busca (rede, API) não encerram o programa. O erro é exibido e uma nova tentativa ocorre na próxima batida do timer, já que em monitoramento contínuo uma falha momentânea não deve interromper o serviço.
- **Bug corrigido:** uma primeira versão tinha um `do ... while` dentro do `catch`. Após o primeiro erro, o programa ficava preso repetindo a mesma mensagem e nunca voltava a buscar a cotação. O laço interno foi removido: o `catch` apenas exibe o erro e o laço externo cuida das novas tentativas.
- `catch (Exception)` substituído por `catch ... when (HttpRequestException or InvalidOperationException)`, para que o cancelamento por Ctrl+C durante uma requisição não seja tratado como erro de cotação e bugs não sejam escondidos.

**Teste de resiliência (Wi-Fi desligado e religado durante a execução):**

```
PETR4: R$ 57.75 (negociado em 21/10/2026 18:30:30)
Erro ao obter a cotação: An error occurred while sending the request.
Erro ao obter a cotação: No such host is known. (brapi.dev:443)
Erro ao obter a cotação: No such host is known. (brapi.dev:443)
PETR4: R$ 57.75 (negociado em 21/10/2026 18:30:30)
PETR4: R$ 57.75 (negociado em 21/10/2026 18:30:30)
Monitoramento encerrado.
```
---

## Fase 2 — Núcleo

### Passo 5 — Arquivo de configuração

- Código padronizado em inglês com o "renomear símbolo" (F2) do VS Code: `PrecoVenda`/`PrecoCompra` → `SellPrice`/`BuyPrice`. Mensagens ao usuário seguem em português.
- Pacotes adicionados: `Microsoft.Extensions.Configuration.Json`, `.Binder`, `.UserSecrets` e `.EnvironmentVariables`.
- `appsettings.json` com quatro seções:
  - `Email:To`: destinatário dos alertas (exigido pelo enunciado);
  - `Smtp`: `Host`, `Port`, `Username` e `Password` do servidor de envio (exigido pelo enunciado);
  - `Brapi:Token`: token opcional da API de cotação;
  - `Monitor:IntervalSeconds`: intervalo entre consultas.
- O arquivo é copiado para a pasta de saída (`CopyToOutputDirectory`) e lido a partir de `AppContext.BaseDirectory`, para funcionar independentemente da pasta de onde o programa é executado.
- **Configuração em camadas** com `ConfigurationBuilder`, em que cada camada sobrescreve a anterior:
  1. `appsettings.json` (versionado, sem segredos);
  2. user-secrets (segredos locais, fora do repositório);
  3. variáveis de ambiente com prefixo `STOCKALERT_` (ex.: `STOCKALERT_Smtp__Password`).
- Classes de configuração em `AppSettings.cs`, preenchidas pelo *binder* (`configuration.Get<AppSettings>()`). Usam `class` com `get; set;` em vez de `record` porque o binder preenche as propriedades depois de criar o objeto. Os valores após `=` são padrões usados quando o campo está ausente.
- **Validação** em `AppSettings.Validate()`, que devolve uma **lista** de erros em vez de lançar exceção no primeiro problema, para que o usuário veja todos os campos a corrigir de uma vez. Regras: e-mail de destino com `@`, host e usuário preenchidos, porta entre 1 e 65535, senha preenchida e intervalo de no mínimo 5 segundos. As mensagens usam a notação de chave da configuração (`Smtp:Password`).
- **Intervalo padrão de 60 segundos:** no plano gratuito da brapi os dados já têm atraso de cerca de 15 minutos, então consultas mais frequentes trazem o mesmo preço e consomem a cota de requisições sem ganho real. O mínimo de 5 segundos impede configurações que sobrecarregariam a API.
- **Cuidado multiplataforma:** o arquivo havia sido criado como `AppSettings.json`. No Windows funciona, mas no Linux/macOS o nome é sensível a maiúsculas e o programa não o encontraria. Renomeado para `appsettings.json` com `git mv`.
- **Segredos com user-secrets:** a senha de app do Gmail (`Smtp:Password`) e o token da brapi (`Brapi:Token`) são armazenados com `dotnet user-secrets`, em um arquivo na pasta do usuário, fora do repositório. O `.csproj` recebe apenas o `UserSecretsId`, que não é sensível. Um arquivo `appsettings.example.json` foi considerado e descartado: o próprio `appsettings.json` é versionado sem segredos e serve de modelo.
- **Token da brapi no provedor:** o `BrapiQuoteProvider` passou a receber o token (opcional) e enviá-lo no cabeçalho `Authorization: Bearer`. A chamada foi dividida em etapas (`HttpRequestMessage` → `SendAsync` → checagem de status → `ReadFromJsonAsync`) para permitir o cabeçalho e o tratamento de erros próprio.
- **Bug encontrado:** o token estava salvo e era lido da configuração, mas o provedor era criado sem ele (`new BrapiQuoteProvider(httpClient)`). Como o parâmetro é opcional, o compilador não acusou a ausência. Corrigido passando `settings.Brapi.Token`.
- Na inicialização, o programa informa se o token está configurado, sem exibir o valor.
- **Mensagens por status HTTP** com `switch` expression: 401 (acesso negado, orienta a configurar o token), 404 (ticker não encontrado), 429 (limite de requisições) e genérica com código e `ReasonPhrase` para os demais.
- **Erros permanentes x temporários:**

| Situação | Comportamento | Código de saída |
|---|---|---|
| Erro temporário (rede, 5xx, 429) | exibe e tenta novamente no próximo intervalo | continua |
| Erro permanente (401, 404) | exibe e encerra | 1 |
| Ctrl+C | encerra de forma limpa | 0 |

  Erros permanentes não se resolvem com novas tentativas; insistir geraria um erro a cada intervalo sem que ninguém percebesse. Isso vale também para um token que expire durante uma execução longa. Os `catch` estão ordenados do mais específico para o mais genérico.

**Testes manuais:**

| Cenário | Resultado |
|---|---|
| `XXXX9` (ticker inexistente) | encerra com "Ticker 'XXXX9' não encontrado" |
| `BBAS3` com token inválido via `STOCKALERT_Brapi__Token` | encerra com acesso negado (confirma que variáveis de ambiente sobrescrevem os user-secrets) |
| `BBAS3` com token válido | cotação exibida |
| `PETR4` | cotação exibida |
| Wi-Fi desligado durante a execução | erro exibido, monitoramento continua e se recupera |
### Passo 6 — Envio de e-mail

- Biblioteca **MailKit**, recomendada pela Microsoft no lugar do `System.Net.Mail.SmtpClient`, que não é indicado para código novo.
- Interface `INotifier` com `Task SendAsync(string subject, string body, CancellationToken)`. O notificador é **genérico**: envia um assunto e um corpo sem conhecer ativos ou alertas. Montar o texto do alerta é responsabilidade de outra parte do programa, o que permite trocar o canal (por exemplo, Telegram) sem alterar a lógica.
- Implementação `EmailNotifier`, que recebe as seções `Smtp` e `Email` da configuração pelo construtor (injeção de dependência):
  - remetente exibido como "Stock Quote Alert", usando a conta `Smtp:Username`;
  - `SecureSocketOptions.Auto` escolhe a criptografia pela porta (STARTTLS na 587, SSL na 465), dispensando um campo extra de configuração.
- Testado com o Gmail usando senha de app (exige verificação em duas etapas na conta).
- **Erro encontrado:** a interface foi declarada inicialmente com retorno `Task<Task>` em vez de `Task`, e a implementação não satisfazia o contrato (CS0738). `Task` sozinho representa uma operação assíncrona sem valor de retorno.
## Commits

| Commit | Descrição |
|---|---|
| `chore: setup inicial da solution` | Solution, projeto console, `.gitignore` e `AssemblyName` |
| `feat: leitura dos argumentos da linha de comando` | Leitura e validação da quantidade de argumentos |
| `fix: rejeita vírgula como separador decimal nos preços` | Correção do bug `22,67` → `2267` |
| `feat: validação das regras dos argumentos` | Preços positivos, venda > compra, ticker em maiúsculas |
| `refactor: extrai validação de argumentos para classe própria` | Validação movida para `MonitorArguments`; `Program.cs` vira orquestrador |
| `Fix: Adicionando progresso do documentation.md` | Adicionando comentarios do commit passado (tinha esquecido)
| `feat: leitura e validação do arquivo de configuração` | Configuração em camadas, validação e rename para inglês |
| `chore: configura user-secrets para dados sensíveis` | `UserSecretsId` no projeto |
| `feat: token da brapi e mensagens de erro por status HTTP` | Token no cabeçalho e mensagens amigáveis |
| `feat: encerra o monitoramento em erros permanentes da API` | 401/404 encerram; erros temporários continuam |
| `feat: envio de e-mail via SMTP com MailKit` | `INotifier` + `EmailNotifier` |