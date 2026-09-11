using Microsoft.EntityFrameworkCore;
using MyFn.Application.Abstractions;
using MyFn.Application.Contracts;
using MyFn.Application.Validation;
using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;
using MyFn.Domain.Finance;

namespace MyFn.Application.Services;

public interface IRecurringExpenseService
{
    Task<IReadOnlyList<RecurringExpenseDto>> ListAsync(bool? mandatory = null, CancellationToken ct = default);
    Task<RecurringExpenseDto> SaveAsync(RecurringExpenseDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class RecurringExpenseService(IAppDbContext db, ICurrentUser user) : IRecurringExpenseService
{
    public async Task<IReadOnlyList<RecurringExpenseDto>> ListAsync(bool? mandatory = null, CancellationToken ct = default)
    {
        var settings = await FinanceDataLoader.SettingsAsync(db, user.UserId, ct);
        var incomeBase = settings.DefaultSalary;
        var current = FinancialMonth.Current(settings.FinancialMonthStartDay);
        var data = await FinanceDataLoader.LoadAsync(db, user.UserId, ct);
        var monthIncome = FinanceDataLoader.Month(data, current, DateOnly.FromDateTime(DateTime.Today)).Income;
        if (monthIncome > 0)
        {
            incomeBase = monthIncome;
        }

        var query = db.RecurringExpenses.AsNoTracking()
            .Where(r => r.UserId == user.UserId);
        if (mandatory.HasValue)
        {
            query = query.Where(r => r.IsMandatory == mandatory);
        }

        var items = await query.OrderByDescending(r => r.IsMandatory).ThenBy(r => r.Description).ToListAsync(ct);
        var categories = data.Categories.ToDictionary(c => c.Id, c => c.Name);

        return items.Select(r => new RecurringExpenseDto
        {
            Id = r.Id,
            Description = r.Description,
            Amount = r.Amount,
            CategoryId = r.CategoryId,
            CategoryName = categories.GetValueOrDefault(r.CategoryId, string.Empty),
            PaymentMethod = r.PaymentMethod,
            CreditCardId = r.CreditCardId,
            IsMandatory = r.IsMandatory,
            IsActive = r.IsActive,
            StartDate = r.StartDate,
            EndDate = r.EndDate,
            WhatIsIt = r.WhatIsIt,
            Prdv = r.Prdv,
            Notes = r.Notes,
            PercentOfIncome = CommitmentCalculator.PercentOfIncome(r.Amount, incomeBase)
        }).ToList();
    }

    public async Task<RecurringExpenseDto> SaveAsync(RecurringExpenseDto dto, CancellationToken ct = default)
    {
        FinanceValidator.ForMoney(dto.Description, dto.Amount, dto.StartDate).ThrowIfInvalid();
        if (dto.CategoryId == Guid.Empty)
        {
            throw new FinanceValidationException(["Selecione uma categoria."]);
        }

        if (dto.PaymentMethod == PaymentMethod.CreditCard && dto.CreditCardId is null)
        {
            throw new FinanceValidationException(["O cartão é obrigatório para recorrências no crédito."]);
        }

        if (dto.EndDate.HasValue && dto.EndDate < dto.StartDate)
        {
            throw new FinanceValidationException(["A data final deve ser posterior à data inicial."]);
        }

        RecurringExpense entity;
        if (dto.Id == Guid.Empty)
        {
            entity = new RecurringExpense { Id = Guid.NewGuid(), UserId = user.UserId };
            db.RecurringExpenses.Add(entity);
        }
        else
        {
            entity = await db.RecurringExpenses.FirstAsync(r => r.Id == dto.Id && r.UserId == user.UserId, ct);
        }

        entity.Description = dto.Description.Trim();
        entity.Amount = Money.Round(dto.Amount);
        entity.CategoryId = dto.CategoryId;
        entity.PaymentMethod = dto.PaymentMethod;
        entity.CreditCardId = dto.PaymentMethod == PaymentMethod.CreditCard ? dto.CreditCardId : null;
        entity.IsMandatory = dto.IsMandatory;
        entity.IsActive = dto.IsActive;
        entity.StartDate = dto.StartDate;
        entity.EndDate = dto.EndDate;
        entity.WhatIsIt = dto.WhatIsIt;
        entity.Prdv = dto.Prdv;
        entity.Notes = dto.Notes;

        await db.SaveChangesAsync(ct);
        dto.Id = entity.Id;
        return dto;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.RecurringExpenses.FirstOrDefaultAsync(r => r.Id == id && r.UserId == user.UserId, ct)
            ?? throw new FinanceValidationException(["Despesa recorrente não encontrada."]);
        db.RecurringExpenses.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}
