# Decisões de regras financeiras

Regras ambíguas da especificação, a escolha feita e como alterar.

## 1. Despesa obrigatória vs despesa vs recorrente

**Ambiguidade:** três cadastros parecidos (despesas, obrigatórias, recorrentes).

**Decisão:** um único agregado `RecurringExpense`.
- `IsMandatory = true` → entra no card “Despesas obrigatórias”.
- `IsMandatory = false` → recorrente comum (assinaturas etc.).
- `Expense` é só lançamento **realizado à vista** (débito/PIX/dinheiro/crédito 1x).
- Compra parcelada **nunca** vira um `Expense` único; vira `InstallmentPurchase` + `Installment`.

**Como alterar:** filtrar o dashboard só por `Expense` se no futuro as obrigatórias forem materializadas mês a mês.

## 2. Evitar contagem dupla

Não lance a mesma conta obrigatória também em “Despesas”. O dashboard soma:
- obrigatórias = recorrentes obrigatórias ativas no mês
- débitos = `Expense` tipo débito no mês + recorrentes não obrigatórias em dinheiro/PIX/débito
- crédito à vista = `Expense` crédito à vista + recorrentes não obrigatórias no cartão
- parcelas = `Installment` com vencimento no mês (exceto canceladas)

## 3. Percentual da renda

O exemplo da spec (`1990 / 3640 = 54,67%`) é **obrigatórias / entradas**.

O dashboard mostra os dois:
- % das obrigatórias
- % comprometido total (obrigatórias + débitos + crédito à vista + parcelas) / entradas

Limiares padrão (configuráveis em `AppSetting`):
- saudável (verde): &lt; 70%
- atenção (amarelo): 70% até 90%
- comprometido (vermelho): ≥ 90%

A cor dos cards usa o **% total comprometido**.

## 4. Mês financeiro

Padrão: dia 1 (mês calendário). `AppSetting.FinancialMonthStartDay` permite outro dia (ex.: 10 → “setembro” = 10/09 a 09/10).

## 5. Geração de parcelas

`valorParcela = Round(total / N, 2, AwayFromZero)` nas primeiras N-1; a última recebe o residual para fechar o total.

Se “parcela atual” = 2 de 12, as parcelas 1..1 nascem **Pagas** (histórico anterior ao sistema).

Vencimento: data da 1ª parcela + (n-1) meses. Não há ajuste automático para dia de vencimento do cartão além da data informada pelo usuário.

Parcela **Atrasada** é status **calculado** na leitura (`Aberta` e vencimento &lt; hoje). Não depende de job.

## 6. Entradas recorrentes e salário padrão

Uma entrada marcada como recorrente vale de `Date` até `RecurrenceEndDate` (ou indefinido).

Se um mês futuro não tiver nenhuma entrada aplicável, a projeção usa `AppSetting.DefaultSalary`.

## 7. Limite do cartão

Utilizado = soma das parcelas abertas/atrasadas daquele cartão + créditos à vista do ciclo aberto (após o último fechamento, até hoje).

Disponível = limite − utilizado (mínimo 0 para exibição; o valor bruto pode ser negativo internamente se estourar).

Ciclo: do dia seguinte ao fechamento anterior até o dia de fechamento corrente. Fatura “atual” = parcelas com vencimento nesse ciclo de fatura + créditos à vista no ciclo.

## 8. “Posso comprar?”

Avaliação em dinheiro à vista no mês corrente (a prioridade da lista não muda o cálculo):
- recomendado: sobra saldo e o % comprometido permanece &lt; limiar de atenção
- atenção: cabe no saldo mas empurra o % para a faixa amarela, ou sobra pouco (&lt; 10% da renda)
- não recomendado: saldo insuficiente ou % ≥ limiar vermelho

## 9. PRDV e “O que é?”

Campos livres em `RecurringExpense` (`WhatIsIt`, `Prdv` como texto). PRDV não entra em cálculo até haver regra explícita.

## 10. Importação

Importar **não apaga** dados. Duplicata candidata = mesma descrição (trim, case-insensitive) + mesmo valor + mesma data, por usuário.

## 11. Autenticação

Não há login nesta versão. `ICurrentUser` devolve o usuário seed. Trocar a implementação dessa interface (e adicionar ASP.NET Identity) é o ponto de extensão.
