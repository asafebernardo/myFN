# myFN — Arquitetura do sistema financeiro pessoal

Este documento descreve as decisões de arquitetura, o modelo de dados, as regras financeiras e o plano de implementação. A imagem/planilha de referência não veio anexada no repositório; o modelo foi extraído da especificação textual (salário R$ 3.640, obrigatórias R$ 1.990, débitos, parcelamentos e o fluxo mensal descrito).

## 1. Arquitetura proposta

Solução em camadas, com Blazor como único frontend:

| Projeto | Responsabilidade |
|---|---|
| `MyFn.Domain` | Entidades, enums, cálculos puros (sem UI e sem EF) |
| `MyFn.Application` | Contratos, DTOs, validações e serviços de aplicação |
| `MyFn.Infrastructure` | EF Core + SQLite, migrations, seed de desenvolvimento, Excel/CSV |
| `MyFn.Web` | Blazor Web App (.NET 10, Interactive Server), layout e páginas |
| `MyFn.Tests` | Testes unitários das regras financeiras |

Fluxo de dependência: **Web → Application ← Infrastructure**. Domain não depende de ninguém.

Por que não um único projeto monolítico: os cálculos (saldo, parcelas, projeção, comprometimento) precisam ser testáveis sem subir o Blazor. Por que não DDD pesado: é um sistema pessoal — serviços de aplicação + funções puras no Domain são suficientes.

Renderização: **Blazor Interactive Server** em toda a UI (CRUD rápido, filtros e gráficos sem WASM).

## 2. Entidades

- **User** — preparado para autenticação futura; hoje há um usuário local padrão.
- **Category** — nome, tipo (entrada/despesa), cor, ativa.
- **Income** — entradas (salário, freelance…). Recorrente projeta meses futuros.
- **Expense** — despesas realizadas à vista (débito/PIX/dinheiro ou crédito à vista). **Não** representa compra parcelada.
- **CreditCard** — limite, fechamento, vencimento.
- **CreditCardInvoice** — valor cobrado no banco em um ciclo (meta para conferir os gastos detalhados).
- **InstallmentPurchase** — compra parcelada (valor total, N parcelas, parcela atual, cartão).
- **Installment** — cada parcela gerada (número, valor, vencimento, status).
- **RecurringExpense** — impacto mensal enquanto ativa; `IsMandatory` marca despesas obrigatórias.
- **PlannedPurchase** — compras futuras + análise “Posso comprar?”.
- **AppSetting** — salário padrão, dia inicial do mês financeiro, limiares de comprometimento.

`FinancialMonth` **não é tabela**: é um value object calculado (`ano/mês` + dia inicial configurável).

## 3. Relacionamentos

```
User 1──* Category
User 1──* Income, Expense, CreditCard, InstallmentPurchase,
          RecurringExpense, PlannedPurchase, AppSetting

Category 1──* Income, Expense, RecurringExpense, PlannedPurchase, InstallmentPurchase

CreditCard 1──* Expense (crédito à vista ou pagamento de fatura)
CreditCard 1──* InstallmentPurchase
CreditCard 1──* CreditCardInvoice
CreditCard 1──* RecurringExpense (opcional)

InstallmentPurchase 1──* Installment
```

Sem relacionamentos redundantes (não há FK de Installment para CreditCard; o cartão vem da compra).

## 4. Estrutura do banco (SQLite)

- PK `Guid`
- FK com índices em `UserId` e datas de consulta (`Date`, `DueDate`)
- `decimal(18,2)` para dinheiro — **nunca** `float`/`double`
- `DateOnly` para datas civis
- Enums persistidos como `int`

Arquivo padrão: `App_Data/myfn.db` (caminho em `appsettings.json`).

## 5. Estrutura de pastas

```
MyFn.sln
docs/ARQUITETURA.md
docs/DECISOES.md
src/MyFn.Domain/{Common,Enums,Entities,Finance}
src/MyFn.Application/{Abstractions,Contracts,Services,Validation}
src/MyFn.Infrastructure/{Persistence,Seed,ImportExport}
src/MyFn.Web/Components/{Layout,Pages,Shared}
tests/MyFn.Tests
```

## 6. Fluxo principal

1. Cadastrar salário (entrada recorrente) e/ou salário padrão nas configurações.
2. Cadastrar despesas obrigatórias (recorrentes com `IsMandatory`).
3. Cadastrar cartão.
4. Cadastrar compra parcelada → o sistema gera N parcelas, marca as anteriores à “parcela atual” como pagas.
5. Dashboard e Projeção leem os serviços (nunca recalculam no Razor).
6. Marcar parcela como paga atualiza limite utilizado e meses futuros.
7. Compra planejada consulta saldo + compromissos para responder “Posso comprar?”.

## 7. Plano de implementação

1. Domain + Application (regras e serviços)
2. EF Core, migrations, seed de desenvolvimento
3. CRUD categorias, entradas, despesas
4. Cartões + compras parceladas
5. Motor de projeção
6. Dashboard + gráficos
7. Recorrências / obrigatórias
8. Compras planejadas
9. Relatórios
10. Importação xlsx + exportação
11. Fatura do cartão: meta do banco + gastos detalhados até bater o valor
11. Testes e UI responsiva
