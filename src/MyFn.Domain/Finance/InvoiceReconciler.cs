using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;

namespace MyFn.Domain.Finance;

public sealed record InvoiceItem(
    string Source,
    Guid SourceId,
    DateOnly Date,
    string Kind,
    string Description,
    decimal Amount,
    string Category);

public sealed record InvoiceReconciliation(
    BillingCycle Cycle,
    decimal? StatementAmount,
    decimal DetailedAmount,
    IReadOnlyList<InvoiceItem> Items)
{
    public decimal Remaining => StatementAmount is decimal target
        ? Money.Round(target - DetailedAmount)
        : 0m;

    public InvoiceMatchStatus Status
    {
        get
        {
            if (StatementAmount is not decimal target || target <= 0)
            {
                return InvoiceMatchStatus.MissingTarget;
            }

            var remaining = Money.Round(target - DetailedAmount);
            if (remaining == 0m)
            {
                return InvoiceMatchStatus.Matched;
            }

            return remaining < 0m ? InvoiceMatchStatus.Over : InvoiceMatchStatus.Pending;
        }
    }
}

public static class InvoiceReconciler
{
    public static InvoiceReconciliation Reconcile(
        CreditCard card,
        BillingCycle cycle,
        decimal? statementAmount,
        IEnumerable<Expense> expenses,
        IEnumerable<Installment> installments,
        IReadOnlyDictionary<Guid, string>? categories = null,
        IEnumerable<RecurringExpense>? recurring = null)
    {
        var cats = categories ?? new Dictionary<Guid, string>();
        var items = new List<InvoiceItem>();

        foreach (var expense in expenses
                     .Where(e => e.CreditCardId == card.Id && e.Kind == ExpenseKind.CreditCash)
                     .Where(e => e.Date > cycle.StartExclusive && e.Date <= cycle.ClosingDate)
                     .OrderBy(e => e.Date)
                     .ThenBy(e => e.Description))
        {
            cats.TryGetValue(expense.CategoryId, out var category);
            items.Add(new InvoiceItem(
                "Cash",
                expense.Id,
                expense.Date,
                "Crédito à vista",
                expense.Description,
                expense.Amount,
                category ?? string.Empty));
        }

        foreach (var installment in installments
                     .Where(i => i.Purchase?.CreditCardId == card.Id && i.Status != InstallmentStatus.Cancelled)
                     .Where(i => i.DueDate > cycle.StartExclusive && i.DueDate <= cycle.DueDate)
                     .OrderBy(i => i.DueDate)
                     .ThenBy(i => i.Number))
        {
            var purchase = installment.Purchase;
            var description = purchase?.Description ?? "Parcela";
            var count = purchase?.InstallmentCount ?? 0;
            var categoryId = purchase?.CategoryId;
            var category = categoryId is Guid id && cats.TryGetValue(id, out var name) ? name : string.Empty;
            items.Add(new InvoiceItem(
                "Installment",
                installment.Id,
                installment.DueDate,
                "Parcela",
                $"{description} ({installment.Number}/{count})",
                installment.Amount,
                category));
        }

        foreach (var rec in (recurring ?? [])
                     .Where(r => r.IsActive && r.CreditCardId == card.Id && r.PaymentMethod == PaymentMethod.CreditCard)
                     .Where(r => r.StartDate <= cycle.ClosingDate && (r.EndDate is null || r.EndDate >= cycle.StartExclusive.AddDays(1)))
                     .OrderBy(r => r.Description))
        {
            cats.TryGetValue(rec.CategoryId, out var category);
            items.Add(new InvoiceItem(
                "Recurring",
                rec.Id,
                cycle.ClosingDate,
                rec.IsMandatory ? "Recorrente obrigatória" : "Recorrente",
                rec.Description,
                rec.Amount,
                category ?? string.Empty));
        }

        decimal? target = statementAmount is decimal amount && amount > 0 ? Money.Round(amount) : null;
        return new InvoiceReconciliation(
            cycle,
            target,
            Money.Round(items.Sum(x => x.Amount)),
            items);
    }
}
