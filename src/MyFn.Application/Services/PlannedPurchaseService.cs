using MyFn.Application.Abstractions;
using MyFn.Application.Contracts;
using MyFn.Application.Validation;
using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;
using MyFn.Domain.Finance;
using Microsoft.EntityFrameworkCore;

namespace MyFn.Application.Services;

public interface IPlannedPurchaseService
{
    Task<IReadOnlyList<PlannedPurchaseDto>> ListAsync(CancellationToken ct = default);
    Task<PlannedPurchaseDto> SaveAsync(PlannedPurchaseDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<AffordabilityResult> EvaluateAsync(decimal estimatedAmount, CancellationToken ct = default);
}

public sealed class PlannedPurchaseService(IAppDbContext db, ICurrentUser user) : IPlannedPurchaseService
{
    public async Task<IReadOnlyList<PlannedPurchaseDto>> ListAsync(CancellationToken ct = default)
    {
        var data = await FinanceDataLoader.LoadAsync(db, user.UserId, ct);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var month = FinancialMonth.Current(data.Settings.FinancialMonthStartDay, today);
        var summary = FinanceDataLoader.Month(data, month, today);

        var items = await db.PlannedPurchases.AsNoTracking()
            .Where(p => p.UserId == user.UserId)
            .OrderBy(p => p.Priority)
            .ThenBy(p => p.DesiredDate)
            .ToListAsync(ct);

        var categories = data.Categories.ToDictionary(c => c.Id, c => c.Name);

        return items.Select(p =>
        {
            var affordability = p.Status == PlannedPurchaseStatus.Planned
                ? AffordabilityCalculator.Evaluate(
                    summary,
                    p.EstimatedAmount,
                    data.Settings.HealthyCommitmentMaxPercent,
                    data.Settings.WarningCommitmentMaxPercent)
                : null;

            return new PlannedPurchaseDto
            {
                Id = p.Id,
                Description = p.Description,
                EstimatedAmount = p.EstimatedAmount,
                Priority = p.Priority,
                CategoryId = p.CategoryId,
                CategoryName = p.CategoryId is Guid cid ? categories.GetValueOrDefault(cid) : null,
                DesiredDate = p.DesiredDate,
                Notes = p.Notes,
                Status = p.Status,
                Affordability = affordability
            };
        }).ToList();
    }

    public async Task<PlannedPurchaseDto> SaveAsync(PlannedPurchaseDto dto, CancellationToken ct = default)
    {
        var date = dto.DesiredDate ?? DateOnly.FromDateTime(DateTime.Today);
        FinanceValidator.ForMoney(dto.Description, dto.EstimatedAmount, date).ThrowIfInvalid();

        PlannedPurchase entity;
        if (dto.Id == Guid.Empty)
        {
            entity = new PlannedPurchase { Id = Guid.NewGuid(), UserId = user.UserId };
            db.PlannedPurchases.Add(entity);
        }
        else
        {
            entity = await db.PlannedPurchases.FirstAsync(p => p.Id == dto.Id && p.UserId == user.UserId, ct);
        }

        entity.Description = dto.Description.Trim();
        entity.EstimatedAmount = Money.Round(dto.EstimatedAmount);
        entity.Priority = dto.Priority;
        entity.CategoryId = dto.CategoryId;
        entity.DesiredDate = dto.DesiredDate;
        entity.Notes = dto.Notes;
        entity.Status = dto.Status;

        await db.SaveChangesAsync(ct);
        dto.Id = entity.Id;
        return dto;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.PlannedPurchases.FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.UserId, ct)
            ?? throw new FinanceValidationException(["Compra planejada não encontrada."]);
        db.PlannedPurchases.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<AffordabilityResult> EvaluateAsync(decimal estimatedAmount, CancellationToken ct = default)
    {
        var data = await FinanceDataLoader.LoadAsync(db, user.UserId, ct);
        var today = DateOnly.FromDateTime(DateTime.Today);
        var month = FinancialMonth.Current(data.Settings.FinancialMonthStartDay, today);
        var summary = FinanceDataLoader.Month(data, month, today);
        return AffordabilityCalculator.Evaluate(
            summary,
            estimatedAmount,
            data.Settings.HealthyCommitmentMaxPercent,
            data.Settings.WarningCommitmentMaxPercent);
    }
}
