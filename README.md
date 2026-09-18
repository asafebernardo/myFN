# myFN

Sistema web de controle financeiro pessoal em **.NET 10 + Blazor Web App**, para substituir a planilha mensal.

## Como executar

Requisitos: SDK .NET 10.

```bash
dotnet restore
dotnet ef database update --project src/MyFn.Infrastructure --startup-project src/MyFn.Web
dotnet run --project src/MyFn.Web
```

Em Development o banco SQLite (`src/MyFn.Web/App_Data/myfn.db`) recebe **dados de exemplo** (salário R$ 3.640, obrigatórias, cartões, parcelamentos e fatura Nubank para conferir). Esses registros têm observação `SEED-DEV`.

A tela **Faturas** compara o valor único cobrado no banco com a soma dos gastos no crédito daquele ciclo.

## Testes

```bash
dotnet test
```

## Arquitetura

Ver `docs/ARQUITETURA.md` e `docs/DECISOES.md`.
