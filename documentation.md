#Documentacao oficial do projeto!
##Passo 1 — Setup do ambiente
- Repositório criado e clonado via GitHub Desktop; branch renomeada de `master` para `main`.
- Problema: a máquina tinha apenas o **runtime** do .NET (executa apps), sem o **SDK** (cria e compila projetos). Erro: `No .NET SDKs were found`.
- Solução: instalação do .NET 10 SDK (versão LTS).
- Estrutura criada: solution `StockQuoteAlert` + projeto console em `src/StockQuoteAlert`.
- A solution usa o formato `.slnx` (XML), que é o padrão do .NET 10 e substitui o antigo `.sln`.
- `AssemblyName` definido como `stock-quote-alert`, para o executável ter o nome pedido no enunciado.

### Problemas de ambiente resolvidos
- Code Runner (botão ▶ do VS Code) tenta usar `scriptcs` para C#; solução: rodar via `dotnet run` no terminal.
- Git Bash no VS Code inseriu `[200~` ao colar comandos (bracketed paste); solução: desativar no `~/.inputrc` / usar PowerShell.
- Terminal aberto antes da instalação do SDK não reconhecia `dotnet`; solução: reiniciar o VS Code.