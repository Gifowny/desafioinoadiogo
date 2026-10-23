# Stock Quote Alert

![CI](https://github.com/Gifowny/desafioinoadiogo/actions/workflows/ci.yml/badge.svg)

Aplicação de console em C# (.NET 10) que monitora continuamente a cotação de um ativo da B3 e envia um alerta por e-mail quando o preço sobe acima de um nível de referência para **venda** ou cai abaixo de um nível de referência para **compra**.

```
stock-quote-alert.exe PETR4 22.67 22.59
```

| Parâmetro | Descrição |
|---|---|
| `PETR4` | Ativo a ser monitorado |
| `22.67` | Preço de referência para **venda**: alerta quando a cotação fica acima |
| `22.59` | Preço de referência para **compra**: alerta quando a cotação fica abaixo |

Os preços usam **ponto** como separador decimal, e o preço de venda deve ser maior que o de compra.

---

## Como funciona

A cada intervalo configurado, o programa consulta a cotação na [brapi.dev](https://brapi.dev) e classifica o preço em uma de três zonas:

| Zona | Condição |
|---|---|
| Acima da venda | preço **>** referência de venda |
| Dentro da faixa | entre as duas referências (inclusive) |
| Abaixo da compra | preço **<** referência de compra |

**O e-mail é enviado quando o preço muda de zona**, e não a cada consulta. Se o preço passa da referência de venda, um alerta de venda é enviado uma única vez; enquanto ele permanecer acima, nenhum novo e-mail é disparado. Voltar para dentro da faixa apenas **rearma** o alerta. Isso evita que o usuário receba dezenas de e-mails idênticos enquanto o preço permanece fora da faixa.

Outros comportamentos:

- **Primeira consulta fora da faixa:** o alerta é enviado imediatamente.
- **Histerese:** para evitar alertas repetidos quando o preço oscila em torno da referência (ex.: 22.66 ↔ 22.68), o alerta só é rearmado quando o preço volta para dentro da faixa com uma folga percentual (padrão: 0,5%), limitada ao ponto médio da faixa. A passagem para a zona oposta continua disparando imediatamente.
- **Falha no envio do e-mail:** o alerta é rearmado e o envio é tentado novamente na próxima consulta, para que o aviso não se perca.
- **Erros da API de cotação:**

| Situação | Comportamento |
|---|---|
| Erro temporário (sem internet, API fora do ar, limite de requisições) | exibe o erro e tenta novamente no próximo intervalo |
| Erro permanente (ticker inexistente, token ausente ou inválido) | exibe o erro e encerra com código de saída 1 |
| Ctrl+C | encerra de forma limpa, com código de saída 0 |

---

## Pré-requisitos

- [.NET 10 SDK](https://dotnet.microsoft.com/download)
- Uma conta de e-mail com acesso SMTP. Para o Gmail, é necessária uma **senha de app** (exige verificação em duas etapas): acesse [myaccount.google.com/apppasswords](https://myaccount.google.com/apppasswords), crie uma senha e use-a no lugar da senha da conta.
- *(Opcional)* Um token da [brapi.dev](https://brapi.dev). Sem token, apenas os ativos de teste da brapi funcionam (PETR4, VALE3, MGLU3 e ITUB4).

---

## Configuração

As configurações ficam em `src/StockQuoteAlert/appsettings.json`:

```json
{
  "Email": {
    "To": "destino@exemplo.com"
  },
  "Smtp": {
    "Host": "smtp.gmail.com",
    "Port": 587,
    "Username": "seu-email@gmail.com",
    "Password": ""
  },
  "Brapi": {
    "Token": ""
  },
  "Monitor": {
    "IntervalSeconds": 60,
    "HysteresisPercent": 0.5
  }
}
```

| Campo | Descrição |
|---|---|
| `Email:To` | E-mail que recebe os alertas |
| `Smtp:Host` / `Smtp:Port` | Servidor SMTP de envio (Gmail: `smtp.gmail.com`, porta `587`) |
| `Smtp:Username` | Conta que envia os alertas |
| `Smtp:Password` | Senha da conta (no Gmail, a senha de app). **Não preencha no arquivo**, veja abaixo |
| `Brapi:Token` | Token da brapi (opcional). **Não preencha no arquivo**, veja abaixo |
| `Monitor:IntervalSeconds` | Intervalo entre consultas, em segundos (mínimo 5, padrão 60) |
| `Monitor:HysteresisPercent` | Margem de rearme em %, de 0 a 10 (padrão 0,5; 0 desativa) |

### Senha e token

Para não expor dados sensíveis no repositório, a senha e o token devem ser informados fora do `appsettings.json`, de uma das duas formas abaixo. A configuração é lida em camadas (arquivo → user-secrets → variáveis de ambiente), e cada camada sobrescreve a anterior.

**User-secrets** (recomendado para desenvolvimento, fica armazenado na pasta do usuário):

```bash
dotnet user-secrets set "Smtp:Password" "sua-senha-de-app" --project src/StockQuoteAlert
dotnet user-secrets set "Brapi:Token" "seu-token" --project src/StockQuoteAlert
```

**Variáveis de ambiente**, com o prefixo `STOCKALERT_` e `__` (dois sublinhados) separando as seções:

```bash
export STOCKALERT_Smtp__Password="sua-senha-de-app"
export STOCKALERT_Brapi__Token="seu-token"
```

No PowerShell: `$env:STOCKALERT_Smtp__Password = "sua-senha-de-app"`.

Qualquer campo pode ser sobrescrito da mesma forma, por exemplo `STOCKALERT_Monitor__IntervalSeconds=10` para testes.

Se a configuração estiver incompleta ou inválida, o programa lista todos os problemas antes de iniciar.

---

## Como executar

A partir da raiz do repositório:

```bash
dotnet run --project src/StockQuoteAlert -- PETR4 22.67 22.59
```

Para gerar o executável:

```bash
dotnet publish src/StockQuoteAlert -c Release -o publish
./publish/stock-quote-alert PETR4 22.67 22.59
```

No Windows: `publish\stock-quote-alert.exe PETR4 22.67 22.59`.

Exemplo de saída:

```
23/10/2026 13:49:13 info: StockQuoteAlert[0] Monitorando PETR4 | venda > 58.50 | compra < 58.20
23/10/2026 13:49:13 info: StockQuoteAlert[0] Consultando a cada 60s. Pressione Ctrl+C para encerrar.
23/10/2026 13:49:13 info: StockQuoteAlert[0] Margem de rearme (histerese): 0.5%
23/10/2026 13:49:13 info: StockQuoteAlert[0] Token da brapi: configurado
23/10/2026 13:49:13 info: StockQuoteAlert[0] Alertas serão enviados para: destino@exemplo.com
23/10/2026 13:49:14 info: StockQuoteAlert[0] PETR4: R$ 58.84 (negociado em 23/10/2026 13:48:30)
23/10/2026 13:49:15 info: StockQuoteAlert[0] Alerta de VENDA enviado por e-mail.
```

Para encerrar, pressione **Ctrl+C**.

---

## Testes

```bash
dotnet test
```

São 16 testes unitários (xUnit) cobrindo:

- **`AlertEvaluator`:** a tabela de decisão das regras de alerta, comparações nos limites, rearme manual e histerese (faixa larga, faixa estreita e passagem para a zona oposta);
- **`MonitorArguments`:** conversão dos argumentos e rejeição de entradas inválidas (texto, vírgula, preços invertidos ou iguais, zero, negativo, quantidade errada).

O build e os testes são executados automaticamente pelo **GitHub Actions** a cada push na `main`, em ambiente Linux.

---

## Estrutura do projeto

```
├── .github/workflows/ci.yml        # build e testes automatizados
├── src/StockQuoteAlert/
│   ├── Program.cs                  # orquestração: configuração, loop de monitoramento, logs
│   ├── MonitorArguments.cs         # leitura e validação dos argumentos
│   ├── AppSettings.cs              # classes de configuração e validação
│   ├── IQuoteProvider.cs           # contrato do provedor de cotações
│   ├── BrapiQuoteProvider.cs       # implementação com a API da brapi
│   ├── BrapiModels.cs              # modelos do JSON da brapi
│   ├── Quote.cs                    # modelo de cotação independente da API
│   ├── AlertEvaluator.cs           # regra de alerta (zonas, mudança de estado, histerese)
│   ├── AlertMessage.cs             # texto dos e-mails de alerta
│   ├── INotifier.cs                # contrato do canal de notificação
│   ├── EmailNotifier.cs            # envio por SMTP com MailKit
│   └── appsettings.json
├── tests/StockQuoteAlert.Tests/    # testes unitários
└── documentation.md                # diário de desenvolvimento passo a passo
```

---

## Decisões técnicas

- **Interfaces para cotação e notificação** (`IQuoteProvider`, `INotifier`): permitem trocar a API ou o canal de alerta sem alterar o restante do código.
- **Lógica de alerta isolada** em `AlertEvaluator`, sem acesso a rede, e-mail ou console, o que permite testá-la de forma unitária.
- **`decimal`** para valores monetários, evitando erros de arredondamento de `double`.
- **`CultureInfo.InvariantCulture`** na leitura dos preços, para que o comportamento não dependa do idioma do sistema; a vírgula é rejeitada, pois seria interpretada como separador de milhar (`22,67` → `2267`).
- **Formatação pt-BR fixa** nos e-mails (`R$ 58,84`), independente do idioma da máquina.
- **Segredos fora do repositório**, com configuração em camadas (`appsettings.json` → user-secrets → variáveis de ambiente).
- **Intervalo padrão de 60 segundos:** os dados gratuitos podem ter atraso, e consultas mais frequentes consumiriam a cota de requisições sem ganho real.
- **MailKit** para SMTP, no lugar do `System.Net.Mail.SmtpClient`, que não é recomendado para código novo.
- **Logs estruturados** com `ILogger`, com horário e nível em cada linha.

O raciocínio completo de cada decisão, incluindo alternativas consideradas e bugs encontrados, está em [`documentation.md`](documentation.md).

---

## Limitações conhecidas

- As cotações dependem da brapi e podem ter atraso, conforme o plano utilizado.
- Fora do horário de pregão o preço não se altera; o programa continua consultando normalmente.
- Uma falha permanente no envio de e-mail (por exemplo, senha incorreta) gera uma nova tentativa e uma mensagem de erro a cada consulta.
- O estado dos alertas fica em memória: ao reiniciar o programa, a primeira consulta fora da faixa gera um novo alerta.

---

## Uso de IA

Utilizei o **Claude (Anthropic)** como mentor durante todo o desenvolvimento, de forma interativa e passo a passo:

- **Explicação de conceitos** de C# e .NET que eu ainda não dominava (records, interfaces, `async`/`await`, injeção de dependência, configuração em camadas, testes com xUnit) e **revisão do código** que escrevi, apontando bugs e melhorias.
- **Código fornecido pela IA**, revisado, testado e integrado por mim: principalmente a infraestrutura (envio de e-mail com MailKit, leitura da configuração, cliente HTTP com token e tratamento de status, logger, workflow do GitHub Actions e as versões integradas do `Program.cs`), além da implementação final da histerese e de parte dos testes.
- **Decisões e lógica próprias**, tomadas a partir de cenários discutidos com a IA: a regra de alerta por mudança de zona (definida por mim em uma tabela de decisão), a rejeição da vírgula como separador decimal, o encerramento em erros permanentes da API, o rearme do alerta em caso de falha no envio, a escolha da histerese em vez de cooldown e o limite da linha de rearme no meio da faixa. Escrevi as primeiras versões de `MonitorArguments`, `AlertEvaluator`, dos modelos, da validação de configuração e de parte dos testes.
- **Documentação:** este README e o `documentation.md` foram redigidos com auxílio da IA a partir do histórico do desenvolvimento.

O processo completo, incluindo os erros encontrados e as alternativas consideradas em cada etapa, está registrado em [`documentation.md`](documentation.md) e no histórico de commits.