using Microsoft.EntityFrameworkCore;
using MyFn.Application.Abstractions;
using MyFn.Application.Contracts;
using MyFn.Domain.Common;
using MyFn.Domain.Enums;
using MyFn.Domain.Finance;

namespace MyFn.Application.Services;

public interface ISettingsService
{
    Task<SettingsDto> GetAsync(CancellationToken ct = default);
    Task SaveAsync(SettingsDto dto, CancellationToken ct = default);
}

public sealed class SettingsService(IAppDbContext db, ICurrentUser user) : ISettingsService
{
    public async Task<SettingsDto> GetAsync(CancellationToken ct = default)
    {
        var settings = await FinanceDataLoader.SettingsAsync(db, user.UserId, ct);
        return new SettingsDto
        {
            DefaultSalary = settings.DefaultSalary,
            CurrencyCode = settings.CurrencyCode,
            FinancialMonthStartDay = settings.FinancialMonthStartDay,
            HealthyCommitmentMaxPercent = settings.HealthyCommitmentMaxPercent,
            WarningCommitmentMaxPercent = settings.WarningCommitmentMaxPercent,
            ShowDashboardCharts = settings.ShowDashboardCharts
        };
    }

    public async Task SaveAsync(SettingsDto dto, CancellationToken ct = default)
    {
        var settings = await db.Settings.FirstOrDefaultAsync(s => s.UserId == user.UserId, ct);
        if (settings is null)
        {
            settings = new Domain.Entities.AppSetting { Id = Guid.NewGuid(), UserId = user.UserId };
            db.Settings.Add(settings);
        }

        settings.DefaultSalary = Money.Round(Math.Max(0, dto.DefaultSalary));
        settings.CurrencyCode = string.IsNullOrWhiteSpace(dto.CurrencyCode) ? "BRL" : dto.CurrencyCode.Trim().ToUpperInvariant();
        settings.FinancialMonthStartDay = Math.Clamp(dto.FinancialMonthStartDay, 1, 28);
        settings.HealthyCommitmentMaxPercent = Math.Clamp(dto.HealthyCommitmentMaxPercent, 1, 100);
        settings.WarningCommitmentMaxPercent = Math.Clamp(dto.WarningCommitmentMaxPercent, settings.HealthyCommitmentMaxPercent, 100);
        settings.ShowDashboardCharts = dto.ShowDashboardCharts;
        await db.SaveChangesAsync(ct);
    }
}

public interface IReportService
{
    Task<ReportBundle> GetAsync(ReportFilter filter, CancellationToken ct = default);
}

public sealed class ReportService(IAppDbContext db, ICurrentUser user, ICreditCardService cards, IPlannedPurchaseService planned) : IReportService
{
    public async Task<ReportBundle> GetAsync(ReportFilter filter, CancellationToken ct = default)
    {
        var data = await FinanceDataLoader.LoadAsync(db, user.UserId, ct);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var startMonth = FinancialMonth.FromDate(filter.From, data.Settings.FinancialMonthStartDay);
        var endMonth = FinancialMonth.FromDate(filter.To, data.Settings.FinancialMonthStartDay);

        var months = new List<MonthlySummary>();
        for (var cursor = startMonth; cursor.Start <= endMonth.Start; cursor = cursor.AddMonths(1))
        {
            months.Add(FinanceDataLoader.Month(data, cursor, today));
        }

        var dashboard = months.Count == 0
            ? FinanceDataLoader.Month(data, startMonth, today)
            : months[^1];

        var byCategory = new Dictionary<string, (string Color, decimal Amount)>();
        foreach (var rec in data.Recurring.Where(r => RecurrenceCalendar.AppliesToMonth(r.StartDate, r.EndDate, r.IsActive, startMonth) || r.EndDate is null || r.EndDate >= filter.From))
        {
            if (filter.CategoryId.HasValue && rec.CategoryId != filter.CategoryId)
            {
                continue;
            }

            var cat = data.Categories.FirstOrDefault(c => c.Id == rec.CategoryId);
            var key = cat?.Name ?? "Outros";
            var current = byCategory.GetValueOrDefault(key);
            byCategory[key] = (cat?.Color ?? "#6c757d", current.Amount + rec.Amount);
        }

        var expenses = data.Expenses.Where(e => e.Date >= filter.From && e.Date <= filter.To);
        if (filter.CategoryId.HasValue)
        {
            expenses = expenses.Where(e => e.CategoryId == filter.CategoryId);
        }

        if (filter.CreditCardId.HasValue)
        {
            expenses = expenses.Where(e => e.CreditCardId == filter.CreditCardId);
        }

        if (filter.ExpenseKind.HasValue)
        {
            expenses = expenses.Where(e => e.Kind == filter.ExpenseKind);
        }

        foreach (var expense in expenses)
        {
            var cat = data.Categories.FirstOrDefault(c => c.Id == expense.CategoryId);
            var key = cat?.Name ?? "Outros";
            var current = byCategory.GetValueOrDefault(key);
            byCategory[key] = (cat?.Color ?? "#6c757d", current.Amount + expense.Amount);
        }

        var futureInstallments = data.Installments
            .Where(i => i.DueDate >= today && i.Status != InstallmentStatus.Cancelled)
            .Where(i => !filter.CreditCardId.HasValue || i.Purchase?.CreditCardId == filter.CreditCardId)
            .Where(i => !filter.InstallmentStatus.HasValue || InstallmentCalculator.EffectiveStatus(i, today) == filter.InstallmentStatus)
            .OrderBy(i => i.DueDate)
            .Select(i => new InstallmentDto
            {
                Id = i.Id,
                PurchaseId = i.InstallmentPurchaseId,
                PurchaseDescription = i.Purchase?.Description ?? string.Empty,
                Number = i.Number,
                InstallmentCount = i.Purchase?.InstallmentCount ?? 0,
                Amount = i.Amount,
                DueDate = i.DueDate,
                Status = InstallmentCalculator.EffectiveStatus(i, today),
                CreditCardName = data.Cards.FirstOrDefault(c => c.Id == i.Purchase!.CreditCardId)?.Name ?? string.Empty
            })
            .ToList();

        var recurringDtos = data.Recurring
            .Where(r => !filter.CategoryId.HasValue || r.CategoryId == filter.CategoryId)
            .Select(r => new RecurringExpenseDto
            {
                Id = r.Id,
                Description = r.Description,
                Amount = r.Amount,
                CategoryId = r.CategoryId,
                CategoryName = data.Categories.FirstOrDefault(c => c.Id == r.CategoryId)?.Name ?? string.Empty,
                PaymentMethod = r.PaymentMethod,
                IsMandatory = r.IsMandatory,
                IsActive = r.IsActive,
                StartDate = r.StartDate,
                EndDate = r.EndDate,
                PercentOfIncome = CommitmentCalculator.PercentOfIncome(r.Amount, dashboard.Income)
            })
            .ToList();

        var plannedItems = await planned.ListAsync(ct);
        if (filter.PlannedStatus.HasValue)
        {
            plannedItems = plannedItems.Where(p => p.Status == filter.PlannedStatus).ToList();
        }

        return new ReportBundle
        {
            ByCategory = byCategory.Select(kv => new CategorySliceDto
            {
                Name = kv.Key,
                Color = kv.Value.Color,
                Amount = Money.Round(kv.Value.Amount)
            }).OrderByDescending(x => x.Amount).ToList(),
            ByMonth = months,
            BalanceEvolution = months.Select(m => new NamedAmountDto
            {
                Name = m.Month.ShortLabel,
                Amount = m.Balance
            }).ToList(),
            FutureInstallments = futureInstallments,
            Recurring = recurringDtos,
            Cards = await cards.ListAsync(ct),
            Planned = plannedItems
        };
    }
}

public interface IImportService
{
    Task<IReadOnlyList<string>> ReadHeadersAsync(Stream excel, CancellationToken ct = default);
    Task<IReadOnlyList<ImportPreviewRow>> PreviewAsync(Stream excel, ImportColumnMap map, CancellationToken ct = default);
    Task<int> ImportAsync(Stream excel, ImportColumnMap map, CancellationToken ct = default);
}

public interface IExportService
{
    Task<byte[]> ExportLedgerExcelAsync(int year, int month, CancellationToken ct = default);
    Task<byte[]> ExportExpensesCsvAsync(CancellationToken ct = default);
    Task<byte[]> ExportInstallmentsCsvAsync(CancellationToken ct = default);
    Task<byte[]> ExportProjectionExcelAsync(int year, int month, int months, CancellationToken ct = default);
    Task<byte[]> ExportReportExcelAsync(ReportFilter filter, CancellationToken ct = default);
}
