using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;
using MyFn.Domain.Finance;

namespace MyFn.Tests;

public class MoneyAndCommitmentTests
{
    [Fact]
    public void Percentual_obrigatorias_sobre_salario()
    {
        var percent = CommitmentCalculator.PercentOfIncome(1990m, 3640m);
        Assert.Equal(54.67m, percent);
    }

    [Fact]
    public void Saldo_do_mes()
    {
        var month = new FinancialMonth(2026, 9, 1);
        var summary = new MonthlySummary
        {
            Month = month,
            Income = 3640m,
            MandatoryExpenses = 1990m,
            Debits = 1300m,
            CashCredit = 0m,
            Installments = 1401.47m,
            CommitmentLevel = CommitmentLevel.Critical
        };

        Assert.Equal(4691.47m, summary.TotalExpenses);
        Assert.Equal(-1051.47m, summary.Balance);
    }

    [Fact]
    public void Comprometimento_faixas()
    {
        Assert.Equal(CommitmentLevel.Healthy, CommitmentCalculator.Classify(54.67m, 70, 90));
        Assert.Equal(CommitmentLevel.Warning, CommitmentCalculator.Classify(75m, 70, 90));
        Assert.Equal(CommitmentLevel.Critical, CommitmentCalculator.Classify(90m, 70, 90));
    }
}

public class InstallmentCalculatorTests
{
    [Fact]
    public void Divide_total_com_residual_na_ultima()
    {
        var parts = InstallmentCalculator.SplitAmount(1542.86m, 12);
        Assert.Equal(12, parts.Count);
        Assert.Equal(1542.86m, parts.Sum());
        Assert.Equal(128.57m, parts[0]);
        Assert.Equal(128.59m, parts[11]);
    }

    [Fact]
    public void Gera_parcelas_e_marca_anteriores_como_pagas()
    {
        var items = InstallmentCalculator.Generate(Guid.NewGuid(), 1542.86m, 12, 2, new DateOnly(2026, 8, 17));
        Assert.Equal(InstallmentStatus.Paid, items[0].Status);
        Assert.Equal(InstallmentStatus.Open, items[1].Status);
        Assert.Equal(new DateOnly(2026, 9, 17), items[1].DueDate);
        Assert.Equal(12, items.Count);
    }

    [Fact]
    public void Parcela_aberta_vencida_fica_atrasada()
    {
        var installment = new Installment
        {
            Status = InstallmentStatus.Open,
            DueDate = new DateOnly(2026, 8, 1)
        };
        Assert.Equal(InstallmentStatus.Overdue, InstallmentCalculator.EffectiveStatus(installment, new DateOnly(2026, 9, 11)));
    }
}

public class RecurrenceAndLedgerTests
{
    [Fact]
    public void Recorrente_vale_ate_data_final()
    {
        var month = new FinancialMonth(2026, 12, 1);
        var rec = new RecurringExpense
        {
            IsActive = true,
            StartDate = new DateOnly(2026, 9, 1),
            EndDate = new DateOnly(2026, 12, 31),
            IsMandatory = true,
            Amount = 650m
        };
        Assert.True(RecurrenceCalendar.RecurringApplies(rec, month));
        Assert.False(RecurrenceCalendar.RecurringApplies(rec, month.AddMonths(1)));
    }

    [Fact]
    public void Ledger_monta_dashboard_do_exemplo()
    {
        var month = new FinancialMonth(2026, 9, 1);
        var today = new DateOnly(2026, 9, 11);
        var incomes = new[]
        {
            new Income { Amount = 3640m, Date = new DateOnly(2026, 9, 1), IsRecurring = true, Description = "Salário" }
        };
        var recurring = new[]
        {
            Rec("Dízimo", 360), Rec("Claro", 100), Rec("Combustível", 300), Rec("Inglês", 650),
            Rec("Drive", 15), Rec("Consórcio", 315), Rec("Dentista", 130), Rec("Teologia", 120)
        };
        var expenses = new[]
        {
            new Expense { Amount = 1300m, Date = new DateOnly(2026, 9, 5), Kind = ExpenseKind.Debit }
        };
        var installments = new[]
        {
            new Installment { Amount = 1401.47m, DueDate = new DateOnly(2026, 9, 17), Status = InstallmentStatus.Open }
        };

        var summary = LedgerAssembler.Assemble(month, incomes, expenses, recurring, installments, 3640m, 70, 90, today);
        Assert.Equal(3640m, summary.Income);
        Assert.Equal(1990m, summary.MandatoryExpenses);
        Assert.Equal(54.67m, summary.MandatoryPercentOfIncome);
        Assert.Equal(1300m, summary.Debits);
        Assert.Equal(1401.47m, summary.Installments);
        Assert.Equal(-1051.47m, summary.Balance);
    }

    [Fact]
    public void Projecao_futura_nao_traz_despesa_avulsa()
    {
        var month = new FinancialMonth(2026, 10, 1);
        var today = new DateOnly(2026, 9, 11);
        var incomes = new[] { new Income { Amount = 3640m, Date = new DateOnly(2026, 9, 1), IsRecurring = true } };
        var recurring = new[] { Rec("Dízimo", 360) };
        var expenses = new[] { new Expense { Amount = 1300m, Date = new DateOnly(2026, 9, 5), Kind = ExpenseKind.Debit } };
        var summary = LedgerAssembler.Assemble(month, incomes, expenses, recurring, [], 0, 70, 90, today);
        Assert.Equal(3640m, summary.Income);
        Assert.Equal(360m, summary.MandatoryExpenses);
        Assert.Equal(0m, summary.Debits);
    }

    private static RecurringExpense Rec(string name, decimal amount) => new()
    {
        Description = name,
        Amount = amount,
        IsMandatory = true,
        IsActive = true,
        StartDate = new DateOnly(2026, 9, 1)
    };
}

public class CreditCardAndAffordabilityTests
{
    [Fact]
    public void Limite_utilizado_soma_parcelas_abertas()
    {
        var card = new CreditCard { Id = Guid.NewGuid(), Limit = 4000m, ClosingDay = 10, DueDay = 17 };
        var purchase = new InstallmentPurchase { CreditCardId = card.Id };
        var installments = new[]
        {
            new Installment { Amount = 128.57m, Status = InstallmentStatus.Open, DueDate = new DateOnly(2026, 9, 17), Purchase = purchase },
            new Installment { Amount = 128.57m, Status = InstallmentStatus.Paid, DueDate = new DateOnly(2026, 8, 17), Purchase = purchase }
        };
        var used = CreditCardCalculator.UsedLimit(card, installments, [], new DateOnly(2026, 9, 11));
        Assert.Equal(128.57m, used);
        Assert.Equal(3871.43m, CreditCardCalculator.AvailableLimit(card.Limit, used));
    }

    [Fact]
    public void Compra_planejada_nao_cabe_no_saldo_negativo()
    {
        var summary = new MonthlySummary
        {
            Month = new FinancialMonth(2026, 9, 1),
            Income = 3640m,
            MandatoryExpenses = 1990m,
            Debits = 1300m,
            Installments = 1401.47m,
            CommitmentLevel = CommitmentLevel.Critical
        };
        var result = AffordabilityCalculator.Evaluate(summary, 1000m, 70, 90);
        Assert.Equal(AffordabilityLevel.NotRecommended, result.Level);
        Assert.False(result.IsRecommended);
    }

    [Fact]
    public void Compra_pequena_pode_ser_recomendada_com_sobra()
    {
        var summary = new MonthlySummary
        {
            Month = new FinancialMonth(2026, 9, 1),
            Income = 3640m,
            MandatoryExpenses = 1990m,
            CommitmentLevel = CommitmentLevel.Healthy
        };
        var result = AffordabilityCalculator.Evaluate(summary, 70m, 70, 90);
        Assert.Equal(AffordabilityLevel.Recommended, result.Level);
        Assert.True(result.IsRecommended);
        Assert.Equal(1580m, result.BalanceAfter);
    }
}

public class InvoiceReconcilerTests
{
    [Fact]
    public void Soma_compras_ate_bater_o_valor_da_fatura()
    {
        var card = new CreditCard { Id = Guid.NewGuid(), ClosingDay = 8, DueDay = 15 };
        var cycle = new BillingCycle(new DateOnly(2026, 8, 8), new DateOnly(2026, 9, 8), new DateOnly(2026, 9, 15));
        var expenses = new[]
        {
            Cash(card.Id, "iFood", 54.90m, new DateOnly(2026, 8, 22)),
            Cash(card.Id, "Uber", 28.40m, new DateOnly(2026, 8, 29)),
            Cash(card.Id, "Mercado", 166.70m, new DateOnly(2026, 9, 6))
        };

        var pending = InvoiceReconciler.Reconcile(card, cycle, 250m, expenses, []);
        Assert.Equal(250m, pending.StatementAmount);
        Assert.Equal(250m, pending.DetailedAmount);
        Assert.Equal(0m, pending.Remaining);
        Assert.Equal(InvoiceMatchStatus.Matched, pending.Status);
    }

    [Fact]
    public void Mostra_quanto_falta_para_chegar_no_valor_do_banco()
    {
        var card = new CreditCard { Id = Guid.NewGuid(), ClosingDay = 8, DueDay = 15 };
        var cycle = new BillingCycle(new DateOnly(2026, 8, 8), new DateOnly(2026, 9, 8), new DateOnly(2026, 9, 15));
        var expenses = new[]
        {
            Cash(card.Id, "iFood", 80m, new DateOnly(2026, 8, 22))
        };

        var recon = InvoiceReconciler.Reconcile(card, cycle, 120m, expenses, []);
        Assert.Equal(InvoiceMatchStatus.Pending, recon.Status);
        Assert.Equal(40m, recon.Remaining);
        Assert.Equal(80m, recon.DetailedAmount);
    }

    [Fact]
    public void Detecta_quando_o_detalhe_passa_da_fatura()
    {
        var card = new CreditCard { Id = Guid.NewGuid(), ClosingDay = 8, DueDay = 15 };
        var cycle = new BillingCycle(new DateOnly(2026, 8, 8), new DateOnly(2026, 9, 8), new DateOnly(2026, 9, 15));
        var expenses = new[]
        {
            Cash(card.Id, "Mercado", 200m, new DateOnly(2026, 9, 1))
        };

        var recon = InvoiceReconciler.Reconcile(card, cycle, 150m, expenses, []);
        Assert.Equal(InvoiceMatchStatus.Over, recon.Status);
        Assert.Equal(-50m, recon.Remaining);
    }

    [Fact]
    public void Pagamento_de_fatura_nao_entra_como_debito_do_mes()
    {
        var month = new FinancialMonth(2026, 9, 1);
        var today = new DateOnly(2026, 9, 18);
        var cardId = Guid.NewGuid();
        var expenses = new[]
        {
            new Expense { Amount = 950m, Date = new DateOnly(2026, 9, 15), Kind = ExpenseKind.InvoicePayment, CreditCardId = cardId },
            new Expense { Amount = 80m, Date = new DateOnly(2026, 9, 6), Kind = ExpenseKind.CreditCash, CreditCardId = cardId }
        };
        var summary = LedgerAssembler.Assemble(month, [], expenses, [], [], 3640m, 70, 90, today);
        Assert.Equal(0m, summary.Debits);
        Assert.Equal(80m, summary.CashCredit);
    }

    private static Expense Cash(Guid cardId, string description, decimal amount, DateOnly date) => new()
    {
        Id = Guid.NewGuid(),
        Description = description,
        Amount = amount,
        Date = date,
        Kind = ExpenseKind.CreditCash,
        CreditCardId = cardId
    };
}
