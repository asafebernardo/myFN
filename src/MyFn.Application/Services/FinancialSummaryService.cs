using Microsoft.EntityFrameworkCore;
using MyFn.Application.Abstractions;
using MyFn.Application.Contracts;
using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;
using MyFn.Domain.Finance;

namespace MyFn.Application.Services;

public interface IFinancialSummaryService
{
    Task<MonthlySummary> GetMonthAsync(int year, int month, CancellationToken ct = default);
    Task<DashboardDto> GetDashboardAsync(int year, int month, CancellationToken ct = default);
    Task<IReadOnlyList<LedgerEntryDto>> GetLedgerAsync(int year, int month, CancellationToken ct = default);
}

public interface IProjectionService
{
    Task<IReadOnlyList<MonthlySummary>> ProjectAsync(int startYear, int startMonth, int monthCount, CancellationToken ct = default);
}

public sealed class FinancialSummaryService(IAppDbContext db, ICurrentUser user) : IFinancialSummaryService
{
    public async Task<MonthlySummary> GetMonthAsync(int year, int month, CancellationToken ct = default)
    {
        var data = await FinanceDataLoader.LoadAsync(db, user.UserId, ct);
        var fm = new FinancialMonth(year, month, data.Settings.FinancialMonthStartDay);
        return FinanceDataLoader.Month(data, fm, DateOnly.FromDateTime(DateTime.Today));
    }

    public async Task<DashboardDto> GetDashboardAsync(int year, int month, CancellationToken ct = default)
    {
        var data = await FinanceDataLoader.LoadAsync(db, user.UserId, ct);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var current = new FinancialMonth(year, month, data.Settings.FinancialMonthStartDay);
        var summary = FinanceDataLoader.Month(data, current, today);

        var evolution = Enumerable.Range(-5, 6)
            .Select(offset => FinanceDataLoader.Month(data, current.AddMonths(offset), today))
            .ToList();

        var categoryAmounts = new Dictionary<Guid, decimal>();
        void Add(Guid? categoryId, decimal amount)
        {
            if (amount <= 0 || categoryId is null || categoryId == Guid.Empty)
            {
                return;
            }

            categoryAmounts[categoryId.Value] = categoryAmounts.GetValueOrDefault(categoryId.Value) + amount;
        }

        foreach (var rec in data.Recurring.Where(r => RecurrenceCalendar.RecurringApplies(r, current)))
        {
            Add(rec.CategoryId, rec.Amount);
        }

        if (current.Start <= today)
        {
            foreach (var expense in data.Expenses.Where(e => current.Contains(e.Date) && e.Kind != ExpenseKind.InvoicePayment))
            {
                Add(expense.CategoryId, expense.Amount);
            }
        }

        foreach (var installment in data.Installments.Where(i => current.Contains(i.DueDate) && i.Status != InstallmentStatus.Cancelled))
        {
            Add(installment.Purchase?.CategoryId, installment.Amount);
        }

        var slices = categoryAmounts
            .Select(kv =>
            {
                var cat = data.Categories.FirstOrDefault(c => c.Id == kv.Key);
                return new CategorySliceDto
                {
                    Name = cat?.Name ?? "Outros",
                    Color = cat?.Color ?? "#6c757d",
                    Amount = Money.Round(kv.Value)
                };
            })
            .OrderByDescending(s => s.Amount)
            .ToList();

        var endingSoon = data.Installments
            .Where(i => i.Status != InstallmentStatus.Cancelled && i.Purchase is not null)
            .GroupBy(i => i.InstallmentPurchaseId)
            .Select(g =>
            {
                var last = g.OrderBy(x => x.Number).Last();
                var purchase = last.Purchase!;
                return new { last, purchase, remaining = g.Count(x => InstallmentCalculator.EffectiveStatus(x, today) is InstallmentStatus.Open or InstallmentStatus.Overdue) };
            })
            .Where(x => x.remaining is > 0 and <= 3)
            .OrderBy(x => x.last.DueDate)
            .Take(8)
            .Select(x => new InstallmentDto
            {
                Id = x.last.Id,
                PurchaseId = x.purchase.Id,
                PurchaseDescription = x.purchase.Description,
                Number = x.last.Number,
                InstallmentCount = x.purchase.InstallmentCount,
                Amount = x.last.Amount,
                DueDate = x.last.DueDate,
                Status = InstallmentCalculator.EffectiveStatus(x.last, today),
                CreditCardName = data.Cards.FirstOrDefault(c => c.Id == x.purchase.CreditCardId)?.Name ?? string.Empty
            })
            .ToList();

        var top = new List<NamedAmountDto>();
        top.AddRange(data.Recurring
            .Where(r => r.IsMandatory && RecurrenceCalendar.RecurringApplies(r, current))
            .Select(r => new NamedAmountDto { Name = r.Description, Amount = r.Amount, Detail = "Obrigatória" }));
        if (current.Start <= today)
        {
            top.AddRange(data.Expenses.Where(e => current.Contains(e.Date) && e.Kind != ExpenseKind.InvoicePayment)
                .Select(e => new NamedAmountDto { Name = e.Description, Amount = e.Amount, Detail = Labels.Kind(e.Kind) }));
        }

        top.AddRange(data.Installments
            .Where(i => current.Contains(i.DueDate) && i.Status != InstallmentStatus.Cancelled)
            .Select(i => new NamedAmountDto
            {
                Name = i.Purchase?.Description ?? "Parcela",
                Amount = i.Amount,
                Detail = $"{i.Number}/{i.Purchase?.InstallmentCount}"
            }));

        return new DashboardDto
        {
            Summary = summary,
            ExpensesByCategory = slices,
            Evolution = evolution,
            EndingSoon = endingSoon,
            TopExpenses = top.OrderByDescending(t => t.Amount).Take(8).ToList()
        };
    }

    public async Task<IReadOnlyList<LedgerEntryDto>> GetLedgerAsync(int year, int month, CancellationToken ct = default)
    {
        var data = await FinanceDataLoader.LoadAsync(db, user.UserId, ct);
        var fm = new FinancialMonth(year, month, data.Settings.FinancialMonthStartDay);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var categories = data.Categories.ToDictionary(c => c.Id, c => c.Name);
        var entries = new List<LedgerEntryDto>();

        foreach (var income in data.Incomes.Where(i => RecurrenceCalendar.IncomeApplies(i, fm)))
        {
            entries.Add(new LedgerEntryDto
            {
                Date = fm.Contains(income.Date) ? income.Date : fm.Start,
                Kind = "Entrada",
                Description = income.Description,
                Amount = income.Amount,
                Category = categories.GetValueOrDefault(income.CategoryId, string.Empty)
            });
        }

        foreach (var rec in data.Recurring.Where(r => RecurrenceCalendar.RecurringApplies(r, fm)))
        {
            entries.Add(new LedgerEntryDto
            {
                Date = fm.Start,
                Kind = rec.IsMandatory ? "Obrigatória" : "Recorrente",
                Description = rec.Description,
                Amount = -rec.Amount,
                Category = categories.GetValueOrDefault(rec.CategoryId, string.Empty)
            });
        }

        if (fm.Start <= today)
        {
            foreach (var expense in data.Expenses.Where(e => fm.Contains(e.Date)))
            {
                entries.Add(new LedgerEntryDto
                {
                    Date = expense.Date,
                    Kind = Labels.Kind(expense.Kind),
                    Description = expense.Description,
                    Amount = -expense.Amount,
                    Category = categories.GetValueOrDefault(expense.CategoryId, string.Empty)
                });
            }
        }

        foreach (var installment in data.Installments.Where(i => fm.Contains(i.DueDate) && i.Status != InstallmentStatus.Cancelled))
        {
            entries.Add(new LedgerEntryDto
            {
                Date = installment.DueDate,
                Kind = "Parcela",
                Description = $"{installment.Purchase?.Description} ({installment.Number}/{installment.Purchase?.InstallmentCount})",
                Amount = -installment.Amount,
                Category = installment.Purchase?.CategoryId is Guid cid ? categories.GetValueOrDefault(cid, string.Empty) : string.Empty
            });
        }

        return entries.OrderBy(e => e.Date).ThenBy(e => e.Description).ToList();
    }
}

public sealed class ProjectionService(IAppDbContext db, ICurrentUser user) : IProjectionService
{
    public async Task<IReadOnlyList<MonthlySummary>> ProjectAsync(int startYear, int startMonth, int monthCount, CancellationToken ct = default)
    {
        if (monthCount is < 1 or > 36)
        {
            monthCount = 6;
        }

        var data = await FinanceDataLoader.LoadAsync(db, user.UserId, ct);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var start = new FinancialMonth(startYear, startMonth, data.Settings.FinancialMonthStartDay);

        return Enumerable.Range(0, monthCount)
            .Select(offset => FinanceDataLoader.Month(data, start.AddMonths(offset), today))
            .ToList();
    }
}
