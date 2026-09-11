using Microsoft.EntityFrameworkCore;
using MyFn.Application.Abstractions;
using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Finance;

namespace MyFn.Application.Services;

internal sealed class FinanceData
{
    public required AppSetting Settings { get; init; }
    public required List<Income> Incomes { get; init; }
    public required List<Expense> Expenses { get; init; }
    public required List<RecurringExpense> Recurring { get; init; }
    public required List<Installment> Installments { get; init; }
    public required List<Category> Categories { get; init; }
    public required List<CreditCard> Cards { get; init; }
}

internal static class FinanceDataLoader
{
    public static async Task<AppSetting> SettingsAsync(IAppDbContext db, Guid userId, CancellationToken ct)
    {
        var settings = await db.Settings.AsNoTracking().FirstOrDefaultAsync(s => s.UserId == userId, ct);
        return settings ?? new AppSetting
        {
            UserId = userId,
            DefaultSalary = 0,
            CurrencyCode = "BRL",
            FinancialMonthStartDay = 1,
            HealthyCommitmentMaxPercent = 70m,
            WarningCommitmentMaxPercent = 90m,
            ShowDashboardCharts = true
        };
    }

    public static async Task<FinanceData> LoadAsync(IAppDbContext db, Guid userId, CancellationToken ct)
    {
        var settings = await SettingsAsync(db, userId, ct);

        return new FinanceData
        {
            Settings = settings,
            Incomes = await db.Incomes.AsNoTracking().Where(x => x.UserId == userId).ToListAsync(ct),
            Expenses = await db.Expenses.AsNoTracking().Where(x => x.UserId == userId).ToListAsync(ct),
            Recurring = await db.RecurringExpenses.AsNoTracking().Where(x => x.UserId == userId).ToListAsync(ct),
            Installments = await db.Installments
                .AsNoTracking()
                .Include(i => i.Purchase)
                .Where(i => i.Purchase!.UserId == userId)
                .ToListAsync(ct),
            Categories = await db.Categories.AsNoTracking().Where(x => x.UserId == userId).ToListAsync(ct),
            Cards = await db.CreditCards.AsNoTracking().Where(x => x.UserId == userId).ToListAsync(ct)
        };
    }

    public static MonthlySummary Month(FinanceData data, FinancialMonth month, DateOnly today) =>
        LedgerAssembler.Assemble(
            month,
            data.Incomes,
            data.Expenses,
            data.Recurring,
            data.Installments,
            data.Settings.DefaultSalary,
            data.Settings.HealthyCommitmentMaxPercent,
            data.Settings.WarningCommitmentMaxPercent,
            today);
}
