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
        var parcels = installments
            .Where(i => i.Purchase?.CreditCardId == card.Id)
            .Where(i => i.Status != InstallmentStatus.Cancelled)
            .Where(i => i.DueDate > cycle.StartExclusive && i.DueDate <= cycle.DueDate)
            .Sum(i => i.Amount);

        var cash = cashCreditExpenses
            .Where(e => e.CreditCardId == card.Id && e.Kind == ExpenseKind.CreditCash)
            .Where(e => e.Date > cycle.StartExclusive && e.Date <= cycle.ClosingDate)
            .Sum(e => e.Amount);

        return Money.Round(parcels + cash);
    }

    private static DateOnly SafeDay(int year, int month, int day)
    {
        var last = DateTime.DaysInMonth(year, month);
        return new DateOnly(year, month, Math.Clamp(day, 1, last));
    }
}
