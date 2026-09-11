using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;

namespace MyFn.Domain.Finance;

public static class RecurrenceCalendar
{
    public static bool AppliesToMonth(DateOnly start, DateOnly? end, bool isActive, FinancialMonth month)
    {
        if (!isActive)
        {
            return false;
        }

        if (start > month.End)
        {
            return false;
        }

        if (end.HasValue && end.Value < month.Start)
        {
            return false;
        }

        return true;
    }

    public static bool IncomeApplies(Income income, FinancialMonth month)
    {
        if (!income.IsRecurring)
        {
            return month.Contains(income.Date);
        }

        if (income.Date > month.End)
        {
            return false;
        }

        if (income.RecurrenceEndDate.HasValue && income.RecurrenceEndDate.Value < month.Start)
        {
            return false;
        }

        return true;
    }

    public static bool RecurringApplies(RecurringExpense expense, FinancialMonth month) =>
        AppliesToMonth(expense.StartDate, expense.EndDate, expense.IsActive, month);

    public static bool IsDebitMethod(PaymentMethod method) =>
        method is PaymentMethod.Cash or PaymentMethod.Pix or PaymentMethod.Debit;
}
