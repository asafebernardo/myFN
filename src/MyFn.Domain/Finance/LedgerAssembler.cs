using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;

namespace MyFn.Domain.Finance;

public static class LedgerAssembler
{
    public static MonthlySummary Assemble(
        FinancialMonth month,
        IReadOnlyList<Income> incomes,
        IReadOnlyList<Expense> expenses,
        IReadOnlyList<RecurringExpense> recurring,
        IReadOnlyList<Installment> installments,
        decimal defaultSalary,
        decimal healthyMaxPercent,
        decimal warningMaxPercent,
        DateOnly today)
    {
        var isFuture = month.Start > today;

        var income = Money.Round(incomes.Where(i => RecurrenceCalendar.IncomeApplies(i, month)).Sum(i => i.Amount));
        if (income <= 0 && isFuture)
        {
            income = Money.Round(defaultSalary);
        }

        var mandatory = Money.Round(recurring
            .Where(r => r.IsMandatory && RecurrenceCalendar.RecurringApplies(r, month))
            .Sum(r => r.Amount));

        var realizedDebits = isFuture
            ? 0m
            : expenses.Where(e => e.Kind == ExpenseKind.Debit && month.Contains(e.Date)).Sum(e => e.Amount);

        var recurringDebits = recurring
            .Where(r => !r.IsMandatory
                        && RecurrenceCalendar.RecurringApplies(r, month)
                        && RecurrenceCalendar.IsDebitMethod(r.PaymentMethod))
            .Sum(r => r.Amount);

        var realizedCashCredit = isFuture
            ? 0m
            : expenses.Where(e => e.Kind == ExpenseKind.CreditCash && month.Contains(e.Date)).Sum(e => e.Amount);

        var recurringCashCredit = recurring
            .Where(r => !r.IsMandatory
                        && RecurrenceCalendar.RecurringApplies(r, month)
                        && r.PaymentMethod == PaymentMethod.CreditCard)
            .Sum(r => r.Amount);

        var installmentTotal = Money.Round(installments
            .Where(i => month.Contains(i.DueDate) && i.Status != InstallmentStatus.Cancelled)
            .Sum(i => i.Amount));

        var summary = new MonthlySummary
        {
            Month = month,
            Income = income,
            MandatoryExpenses = mandatory,
            Debits = Money.Round(realizedDebits + recurringDebits),
            CashCredit = Money.Round(realizedCashCredit + recurringCashCredit),
            Installments = installmentTotal
        };

        return summary with
        {
            CommitmentLevel = CommitmentCalculator.Classify(
                summary.CommitmentPercent,
                healthyMaxPercent,
                warningMaxPercent)
        };
    }
}
