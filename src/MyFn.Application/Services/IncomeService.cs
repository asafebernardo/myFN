using Microsoft.EntityFrameworkCore;
using MyFn.Application.Abstractions;
using MyFn.Application.Contracts;
using MyFn.Application.Validation;
using MyFn.Domain.Common;
using MyFn.Domain.Entities;

namespace MyFn.Application.Services;

public interface IIncomeService
{
    Task<IReadOnlyList<IncomeDto>> ListAsync(CancellationToken ct = default);
    Task<IncomeDto> SaveAsync(IncomeDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class IncomeService(IAppDbContext db, ICurrentUser user) : IIncomeService
{
    public async Task<IReadOnlyList<IncomeDto>> ListAsync(CancellationToken ct = default)
    {
        return await db.Incomes.AsNoTracking()
            .Where(i => i.UserId == user.UserId)
            .OrderByDescending(i => i.Date)
            .Select(i => new IncomeDto
            {
                Id = i.Id,
                Description = i.Description,
                Amount = i.Amount,
                Date = i.Date,
                CategoryId = i.CategoryId,
                CategoryName = i.Category != null ? i.Category.Name : string.Empty,
                IsRecurring = i.IsRecurring,
                RecurrenceEndDate = i.RecurrenceEndDate,
                Notes = i.Notes
            })
            .ToListAsync(ct);
    }

    public async Task<IncomeDto> SaveAsync(IncomeDto dto, CancellationToken ct = default)
    {
        FinanceValidator.ForMoney(dto.Description, dto.Amount, dto.Date).ThrowIfInvalid();
        if (dto.CategoryId == Guid.Empty)
        {
            throw new FinanceValidationException(["Selecione uma categoria."]);
        }

        Income entity;
        if (dto.Id == Guid.Empty)
        {
            entity = new Income { Id = Guid.NewGuid(), UserId = user.UserId };
            db.Incomes.Add(entity);
        }
        else
        {
            entity = await db.Incomes.FirstAsync(i => i.Id == dto.Id && i.UserId == user.UserId, ct);
        }

        entity.Description = dto.Description.Trim();
        entity.Amount = Money.Round(dto.Amount);
        entity.Date = dto.Date;
        entity.CategoryId = dto.CategoryId;
        entity.IsRecurring = dto.IsRecurring;
        entity.RecurrenceEndDate = dto.RecurrenceEndDate;
        entity.Notes = dto.Notes;

        await db.SaveChangesAsync(ct);
        dto.Id = entity.Id;
        return dto;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Incomes.FirstOrDefaultAsync(i => i.Id == id && i.UserId == user.UserId, ct)
            ?? throw new FinanceValidationException(["Entrada não encontrada."]);
        db.Incomes.Remove(entity);
        await db.SaveChangesAsync(ct);
    }
}
