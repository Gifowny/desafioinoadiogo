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
### Passo 7 — Lógica de alerta com estado

- **O comportamento foi decidido antes do código**, a partir de uma sequência de preços hipotética (venda 58.30, compra 58.10):

| Consulta | Preço | Zona | E-mail |
|---|---|---|---|
| 1 | 58.20 | dentro | não |
| 2 | 58.35 | acima | venda |
| 3 | 58.40 | acima | não |
| 4 | 58.38 | acima | não |
| 5 | 58.25 | dentro | não (rearma) |
| 6 | 58.32 | acima | venda |
| 7 | 58.05 | abaixo | compra |
| 8 | 58.30 | dentro | não |

- **Raciocínio:** a primeira versão da regra enviava e-mail em toda consulta fora da faixa. Isso geraria dezenas de e-mails idênticos enquanto o preço permanecesse acima ou abaixo (cerca de 240 em uma tarde, com consultas a cada 60s), sem nenhuma informação nova após o primeiro. A regra foi revista: o e-mail avisa sobre uma **mudança**, não sobre uma situação que continua.
- **Regra final:** o programa guarda a última zona (dentro, acima da venda ou abaixo da compra) e envia e-mail sempre que a zona muda para acima ou abaixo. Voltar para dentro da faixa não gera e-mail, apenas rearma o alerta.
- **Decisões de borda:**
  - comparações estritas (`>` e `<`), conforme o enunciado ("maior que" / "menor que"); um preço exatamente igual ao de referência conta como dentro da faixa;
  - saída da faixa após retorno (consulta 6) gera novo e-mail, pois representa uma nova oportunidade;
  - mudança direta de acima para abaixo (consulta 7) gera o e-mail da nova zona;
  - a zona inicial é "dentro": se a primeira consulta já estiver fora da faixa, o e-mail é enviado imediatamente.
- **Risco conhecido:** oscilação do preço na borda (ex.: 58.29 ↔ 58.31) geraria um e-mail a cada saída. Mitigações possíveis: histerese ou cooldown (avaliadas como extras).
- Implementação em `AlertEvaluator.cs`:
  - `enum PriceZone` (`Inside`, `AboveSell`, `BelowBuy`) e `enum AlertType` (`None`, `Sell`, `Buy`): tipos fechados impedem estados inválidos;
  - campo privado `_lastZone` guarda o estado entre as consultas;
  - a classe não faz I/O (não busca cotação, não envia e-mail, não imprime), o que a torna testável de forma isolada.
- Uma tentativa intermediária com `switch` usava padrões relacionais com variáveis (`> sellPrice`), que o C# aceita apenas com constantes. Mantida a versão com `if / else if / else`, mais legível. A simulação manual da tabela confirmou o comportamento esperado.
### Passo 8 — Integração

- **Montagem das mensagens** em `AlertMessage.Build`, que devolve uma tupla `(Subject, Body)`:
  - o assunto traz ativo, preço atual e referência (ex.: "Alerta de VENDA: PETR4 a R$ 58,88 (acima de R$ 58,50)"), para o alerta ser compreendido sem abrir o e-mail;
  - corpo com preço atual, referência, horário da cotação e aviso de que as cotações podem ter atraso e não constituem recomendação de investimento;
  - moeda e data formatadas com cultura **pt-BR fixa** (`string.Create(PtBr, ...)`). Com o `:C` padrão, o formato dependeria do idioma da máquina (em uma máquina em inglês sairia `$58.88`);
  - `AlertType.None` e valores desconhecidos lançam exceção, já que não há e-mail a montar.
- **Erro encontrado:** `string.Create` exige que todos os trechos concatenados sejam strings interpoladas (`$"..."`); trechos sem `$` impediam a compilação.
- **Integração no loop:** a cada cotação, o `AlertEvaluator` decide se a zona mudou; havendo alerta, a mensagem é montada e enviada pelo `INotifier`.
- **Decisão — falha no envio:** o avaliador atualiza o estado antes do envio; se o e-mail falhasse, o alerta seria perdido (na consulta seguinte a zona seria a mesma). Opções consideradas: aceitar a perda, rearmar o avaliador ou criar uma fila de reenvio. Escolhido **rearmar** com `AlertEvaluator.Rearm()`, que volta o estado para "dentro": se o preço continuar fora da faixa, o envio é tentado novamente na próxima consulta; se já tiver voltado, nada é enviado, pois o aviso deixaria de ser verdadeiro. A solução reaproveita a regra de mudança de estado sem criar um mecanismo novo.
- O `catch` do envio captura qualquer exceção exceto cancelamento, pois o MailKit lança diversos tipos (autenticação, conexão, protocolo) e todos recebem o mesmo tratamento.
- **Limitação conhecida:** uma falha permanente de e-mail (senha errada) gera uma nova tentativa e uma mensagem de erro a cada consulta.

**Testes ponta a ponta** (pregão aberto, PETR4 cotada a R$ 58,88; intervalo reduzido para 10s via `STOCKALERT_Monitor__IntervalSeconds`, sem alterar o arquivo):

| Cenário | Comando | Resultado |
|---|---|---|
| Sem alerta | `PETR4 100 1` | apenas cotações |
| Venda | `PETR4 58.50 58.20` | e-mail de venda na primeira consulta |
| Compra | `PETR4 59.20 59.00` | e-mail de compra na primeira consulta, sem repetição na seguinte |
| Falha de envio | senha inválida via `STOCKALERT_Smtp__Password` | falha exibida e nova tentativa a cada consulta (rearme) |
| Venda sem repetição | `PETR4 58.50 58.20` | um único e-mail de venda; nas consultas seguintes (58.84 → 58.79, ainda acima de 58.50) nenhum novo envio |

- **Observação:** com o token configurado, o horário da cotação avançou a cada minuto, próximo do horário real, bem abaixo do atraso de ~15 minutos do acesso sem cadastro.
---

## Fase 3 — Extras e entrega

### Passo 9 — Testes unitários

- Projeto `tests/StockQuoteAlert.Tests` com **xUnit**, adicionado à solution e referenciando o projeto principal. Execução: `dotnet test`.
- Testes no padrão **Arrange / Act / Assert**, com nomes que descrevem o comportamento esperado.
- `AlertEvaluatorTests` (4 testes):
  - a **tabela de decisão** do Passo 7 inteira, convertida em uma sequência de asserções;
  - primeira consulta já abaixo da compra dispara alerta de compra (zona inicial "dentro");
  - preço exatamente igual à referência não dispara alerta (comparação estrita);
  - sem `Rearm()` o alerta não se repete; com `Rearm()` ele é disparado de novo.
- `MonitorArgumentsTests` (9 testes):
  - argumentos válidos são convertidos e o ticker fica em maiúsculas;
  - `[Theory]` com 7 entradas inválidas (texto, vírgula, venda menor ou igual à compra, zero, negativo), verificando `ArgumentException`;
  - quantidade errada de argumentos, verificando também que a mensagem é a de uso.
- **Achado:** o preço negativo é rejeitado no `TryParse`, pois `NumberStyles.AllowDecimalPoint` não aceita sinal. Com isso, a regra "preços maiores que zero" só é atingida pelo zero.
- As classes testadas não fazem I/O (rede, e-mail, console), o que permite testá-las sem internet e sem servidor SMTP. Esse foi um dos motivos para separá-las do `Program.cs` nos passos anteriores.
- **Resultado:** 13 testes, todos passando.
### Passo 10 — Extras

#### Integração contínua (GitHub Actions)

- Workflow em `.github/workflows/ci.yml`, executado a cada push e pull request na `main`.
- Etapas: checkout, instalação do .NET 10, `dotnet restore`, `dotnet build --configuration Release` e `dotnet test`.
- Executa em **Linux** (`ubuntu-latest`), o que confirma que o projeto compila e os testes passam fora do Windows (incluindo a correção do nome `appsettings.json`, sensível a maiúsculas no Linux).
- Primeira execução: sucesso em 29 segundos.
#### Logs estruturados

- Substituição de `Console.WriteLine` por `ILogger` (`Microsoft.Extensions.Logging.Console`) durante o monitoramento.
- Formato de uma linha por mensagem, com data e hora (`dd/MM/yyyy HH:mm:ss`) e nível.
- Níveis:
  - `Information`: início, cotações e alertas enviados;
  - `Warning`: erros temporários de cotação (o programa se recupera);
  - `Error`: falha no envio de e-mail e erros permanentes da API.
- *Message templates* (`"{Ticker}: R$ {Price}"`) em vez de interpolação: os valores ficam registrados como campos separados, permitindo filtrar e consultar logs em ferramentas de monitoramento (log estruturado).
- Mensagens de argumentos e configuração inválidos continuam em `Console.Error`, sem horário: ocorrem antes do início do monitoramento e a mensagem de uso deve aparecer limpa.
- `ILoggerFactory` declarado com `using`, garantindo que as mensagens pendentes sejam escritas antes do encerramento (o logger de console escreve em segundo plano).

#### Histerese contra oscilação na borda

- **Problema** (identificado no Passo 7): com o preço oscilando em torno da referência (ex.: 58.29 ↔ 58.31 com venda em 58.30), cada saída da faixa gerava um novo e-mail.
- **Alternativas consideradas:** histerese (margem de preço para rearmar) e cooldown (intervalo mínimo entre e-mails). Escolhida a **histerese**, por atacar diretamente a causa (a borda), manter a regra baseada apenas em preço e o `AlertEvaluator` testável sem dependência de relógio.
- **Regra:** após um alerta, voltar para dentro da faixa só rearma o alerta quando o preço se afasta da referência por uma margem percentual: abaixo de `venda × (1 − margem)` para a venda e acima de `compra × (1 + margem)` para a compra. A margem afeta apenas o rearme da mesma zona; a passagem para a zona oposta continua disparando imediatamente.
- **Problema encontrado pelos testes:** com faixas mais estreitas que a margem (ex.: 58.10–58.30 com 0,5%), a linha de rearme da venda ficava abaixo do preço de compra, e o alerta de venda só voltaria após um alerta de compra. Opções avaliadas: aceitar e documentar, limitar a linha de rearme ou recusar a configuração. **Decisão:** limitar a linha de rearme ao **ponto médio da faixa** (`Math.Max`/`Math.Min`), o que mantém o comportamento original em faixas largas e garante o rearme em faixas estreitas, sem restringir o uso.
- Configurável em `Monitor:HysteresisPercent` (padrão 0,5%, cerca de 5 vezes a variação por minuto observada na PETR4; validado entre 0 e 10). Com 0, o comportamento é idêntico ao original, e os testes anteriores continuaram passando sem alteração.
- Novos testes: histerese em faixa larga (rearme pela margem), em faixa estreita (rearme no meio da faixa) e a garantia de que a zona oposta não é atrasada. Total: 16 testes.
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
| `feat: avaliação de alertas por mudança de estado` | `AlertEvaluator` com `PriceZone` e `AlertType` |
| `feat: montagem das mensagens de alerta` | `AlertMessage` com formatação pt-BR |
| `feat: integra avaliação de alertas e envio de e-mail no monitoramento` | Loop completo com rearme em falha de envio |
| `test: testes unitários do AlertEvaluator` | Projeto xUnit e 4 testes da lógica de alerta |
| `test: testes unitários do MonitorArguments` | 9 testes de validação de entrada |