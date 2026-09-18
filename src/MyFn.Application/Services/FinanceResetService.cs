using Microsoft.EntityFrameworkCore;
using MyFn.Application.Abstractions;

namespace MyFn.Application.Services;

public interface IFinanceResetService
{
    Task ClearMovementsAsync(CancellationToken ct = default);
}

public sealed class FinanceResetService(IAppDbContext db, ICurrentUser user) : IFinanceResetService
{
    public async Task ClearMovementsAsync(CancellationToken ct = default)
    {
        var uid = user.UserId;
        await db.Installments.Where(i => i.Purchase!.UserId == uid).ExecuteDeleteAsync(ct);
        await db.InstallmentPurchases.Where(p => p.UserId == uid).ExecuteDeleteAsync(ct);
        await db.Expenses.Where(e => e.UserId == uid).ExecuteDeleteAsync(ct);
        await db.Incomes.Where(e => e.UserId == uid).ExecuteDeleteAsync(ct);
        await db.RecurringExpenses.Where(e => e.UserId == uid).ExecuteDeleteAsync(ct);
        await db.PlannedPurchases.Where(e => e.UserId == uid).ExecuteDeleteAsync(ct);
        await db.CreditCardInvoices.Where(e => e.UserId == uid).ExecuteDeleteAsync(ct);
        await db.CreditCards.Where(e => e.UserId == uid).ExecuteDeleteAsync(ct);
    }
}
