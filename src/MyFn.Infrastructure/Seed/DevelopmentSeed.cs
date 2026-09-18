using Microsoft.EntityFrameworkCore;
using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;
using MyFn.Infrastructure.Persistence;

namespace MyFn.Infrastructure.Seed;

/// <summary>
/// Cria só a estrutura vazia (usuário, categorias e configurações). Não lança salário nem gastos de exemplo.
/// </summary>
public static class DevelopmentSeed
{
    public static Task ApplyAsync(FinanceDbContext db, CancellationToken ct = default) =>
        EnsureBaselineAsync(db, ct);

    public static async Task EnsureBaselineAsync(FinanceDbContext db, CancellationToken ct = default)
    {
        var userId = WellKnownIds.DefaultUserId;
        if (!await db.Users.AnyAsync(u => u.Id == userId, ct))
        {
            db.Users.Add(new User
            {
                Id = userId,
                Name = "Usuário local",
                Email = "dev@local",
                CreatedAt = DateTime.UtcNow
            });
        }

        if (!await db.Categories.AnyAsync(c => c.UserId == userId, ct))
        {
            db.Categories.AddRange(
                Cat(userId, "Salário", CategoryType.Income, "#1b7a4e", 1),
                Cat(userId, "Freelance", CategoryType.Income, "#2d8a6e", 2),
                Cat(userId, "Outros", CategoryType.Income, "#4aa882", 3),
                Cat(userId, "Moradia", CategoryType.Expense, "#6c5ce7", 10),
                Cat(userId, "Alimentação", CategoryType.Expense, "#e17055", 11),
                Cat(userId, "Transporte", CategoryType.Expense, "#0984e3", 12),
                Cat(userId, "Educação", CategoryType.Expense, "#00cec9", 13),
                Cat(userId, "Saúde", CategoryType.Expense, "#d63031", 14),
                Cat(userId, "Lazer", CategoryType.Expense, "#fdcb6e", 15),
                Cat(userId, "Assinaturas", CategoryType.Expense, "#636e72", 16),
                Cat(userId, "Outros", CategoryType.Expense, "#b2bec3", 17));
        }

        var settings = await db.Settings.FirstOrDefaultAsync(s => s.UserId == userId, ct);
        if (settings is null)
        {
            db.Settings.Add(new AppSetting
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                DefaultSalary = 0,
                CurrencyCode = "BRL",
                FinancialMonthStartDay = 1,
                HealthyCommitmentMaxPercent = 70m,
                WarningCommitmentMaxPercent = 90m,
                ShowDashboardCharts = true,
                SeedVersion = AppSetting.CurrentSeedVersion
            });
        }

        await db.SaveChangesAsync(ct);
    }

    public static async Task ClearDemoFinanceIfNeededAsync(FinanceDbContext db, CancellationToken ct = default)
    {
        var settings = await db.Settings.FirstOrDefaultAsync(s => s.UserId == WellKnownIds.DefaultUserId, ct);
        if (settings is null || settings.SeedVersion >= AppSetting.CurrentSeedVersion)
        {
            return;
        }

        await ClearMovementsAsync(db, WellKnownIds.DefaultUserId, ct);
        settings.DefaultSalary = 0;
        settings.SeedVersion = AppSetting.CurrentSeedVersion;
        await db.SaveChangesAsync(ct);
    }

    public static async Task ClearMovementsAsync(FinanceDbContext db, Guid userId, CancellationToken ct = default)
    {
        await db.Installments.Where(i => i.Purchase!.UserId == userId).ExecuteDeleteAsync(ct);
        await db.InstallmentPurchases.Where(p => p.UserId == userId).ExecuteDeleteAsync(ct);
        await db.Expenses.Where(e => e.UserId == userId).ExecuteDeleteAsync(ct);
        await db.Incomes.Where(e => e.UserId == userId).ExecuteDeleteAsync(ct);
        await db.RecurringExpenses.Where(e => e.UserId == userId).ExecuteDeleteAsync(ct);
        await db.PlannedPurchases.Where(e => e.UserId == userId).ExecuteDeleteAsync(ct);
        await db.CreditCardInvoices.Where(e => e.UserId == userId).ExecuteDeleteAsync(ct);
        await db.CreditCards.Where(e => e.UserId == userId).ExecuteDeleteAsync(ct);
    }

    private static Category Cat(Guid userId, string name, CategoryType type, string color, int order) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Name = name,
        Type = type,
        Color = color,
        IsActive = true,
        SortOrder = order
    };
}
