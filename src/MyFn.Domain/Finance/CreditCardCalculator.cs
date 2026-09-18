using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;

namespace MyFn.Domain.Finance;

public sealed record BillingCycle(DateOnly StartExclusive, DateOnly ClosingDate, DateOnly DueDate);

public static class CreditCardCalculator
{
    public static BillingCycle CurrentCycle(CreditCard card, DateOnly today)
    {
        var closingThisMonth = SafeDay(today.Year, today.Month, card.ClosingDay);
        DateOnly cycleEnd;
        DateOnly cycleStartExclusive;

        if (today <= closingThisMonth)
        {
            cycleEnd = closingThisMonth;
            var previous = closingThisMonth.AddMonths(-1);
            cycleStartExclusive = SafeDay(previous.Year, previous.Month, card.ClosingDay);
        }
        else
        {
            cycleStartExclusive = closingThisMonth;
            var next = closingThisMonth.AddMonths(1);
            cycleEnd = SafeDay(next.Year, next.Month, card.ClosingDay);
        }

        var dueMonth = cycleEnd;
        var due = SafeDay(dueMonth.Year, dueMonth.Month, card.DueDay);
        if (due <= cycleEnd)
        {
            var nextDue = cycleEnd.AddMonths(1);
            due = SafeDay(nextDue.Year, nextDue.Month, card.DueDay);
        }

        return new BillingCycle(cycleStartExclusive, cycleEnd, due);
    }

    public static BillingCycle CycleEndingOn(CreditCard card, DateOnly closingDate)
    {
        var previous = closingDate.AddMonths(-1);
        var startExclusive = SafeDay(previous.Year, previous.Month, card.ClosingDay);
        var due = SafeDay(closingDate.Year, closingDate.Month, card.DueDay);
        if (due <= closingDate)
        {
            var nextDue = closingDate.AddMonths(1);
            due = SafeDay(nextDue.Year, nextDue.Month, card.DueDay);
        }

        return new BillingCycle(startExclusive, closingDate, due);
    }

    public static BillingCycle LastClosedCycle(CreditCard card, DateOnly today)
    {
        var current = CurrentCycle(card, today);
        return CycleEndingOn(card, current.StartExclusive);
    }

    public static IReadOnlyList<BillingCycle> RecentCycles(CreditCard card, DateOnly today, int count = 8)
    {
        var current = CurrentCycle(card, today);
        var cycles = new List<BillingCycle>(count) { current };
        var closing = current.StartExclusive;
        for (var i = 1; i < count; i++)
        {
            var cycle = CycleEndingOn(card, closing);
            cycles.Add(cycle);
            closing = cycle.StartExclusive;
        }

        return cycles;
    }

    public static decimal UsedLimit(
        CreditCard card,
        IEnumerable<Installment> installments,
        IEnumerable<Expense> cashCreditExpenses,
        DateOnly today)
    {
        var openInstallments = installments
            .Where(i => i.Purchase?.CreditCardId == card.Id)
            .Where(i => InstallmentCalculator.EffectiveStatus(i, today) is InstallmentStatus.Open or InstallmentStatus.Overdue)
            .Sum(i => i.Amount);

        var cycle = CurrentCycle(card, today);
        var openCycleCash = cashCreditExpenses
            .Where(e => e.CreditCardId == card.Id && e.Kind == ExpenseKind.CreditCash)
            .Where(e => e.Date > cycle.StartExclusive && e.Date <= cycle.ClosingDate)
            .Sum(e => e.Amount);

        return Money.Round(openInstallments + openCycleCash);
    }

    public static decimal AvailableLimit(decimal limit, decimal used) => Money.Round(limit - used);

    public static decimal CurrentInvoice(
        CreditCard card,
        IEnumerable<Installment> installments,
        IEnumerable<Expense> cashCreditExpenses,
        DateOnly today)
    {
        var cycle = CurrentCycle(card, today);
        return InvoiceReconciler.Reconcile(card, cycle, null, cashCreditExpenses, installments).DetailedAmount;
    }

    private static DateOnly SafeDay(int year, int month, int day)
    {
        var last = DateTime.DaysInMonth(year, month);
        return new DateOnly(year, month, Math.Clamp(day, 1, last));
    }
}
