using Microsoft.EntityFrameworkCore;
using MyFn.Application.Abstractions;
using MyFn.Application.Contracts;
using MyFn.Application.Validation;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;

namespace MyFn.Application.Services;

public interface ICategoryService
{
    Task<IReadOnlyList<CategoryDto>> ListAsync(CategoryType? type = null, bool activeOnly = false, CancellationToken ct = default);
    Task<CategoryDto> SaveAsync(CategoryDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class CategoryService(IAppDbContext db, ICurrentUser user) : ICategoryService
{
    public async Task<IReadOnlyList<CategoryDto>> ListAsync(CategoryType? type = null, bool activeOnly = false, CancellationToken ct = default)
    {
        var query = db.Categories.AsNoTracking().Where(c => c.UserId == user.UserId);
        if (type.HasValue)
        {
            query = query.Where(c => c.Type == type);
        }

        if (activeOnly)
        {
            query = query.Where(c => c.IsActive);
        }

        return await query.OrderBy(c => c.SortOrder).ThenBy(c => c.Name)
            .Select(c => new CategoryDto
            {
                Id = c.Id,
                Name = c.Name,
                Type = c.Type,
                Color = c.Color,
                IsActive = c.IsActive
            })
            .ToListAsync(ct);
    }

    public async Task<CategoryDto> SaveAsync(CategoryDto dto, CancellationToken ct = default)
    {
        FinanceValidator.ForCategory(dto.Name, dto.Color).ThrowIfInvalid();

        Category entity;
        if (dto.Id == Guid.Empty)
        {
            entity = new Category
            {
                Id = Guid.NewGuid(),
                UserId = user.UserId
            };
            db.Categories.Add(entity);
        }
        else
        {
            entity = await db.Categories.FirstAsync(c => c.Id == dto.Id && c.UserId == user.UserId, ct);
        }

        entity.Name = dto.Name.Trim();
        entity.Type = dto.Type;
        entity.Color = dto.Color;
        entity.IsActive = dto.IsActive;

        await db.SaveChangesAsync(ct);
        dto.Id = entity.Id;
        return dto;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Categories.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.UserId, ct)
            ?? throw new FinanceValidationException(["Categoria não encontrada."]);

        var inUse = await db.Incomes.AnyAsync(x => x.CategoryId == id, ct)
                    || await db.Expenses.AnyAsync(x => x.CategoryId == id, ct)
                    || await db.RecurringExpenses.AnyAsync(x => x.CategoryId == id, ct)
                    || await db.PlannedPurchases.AnyAsync(x => x.CategoryId == id, ct)
                    || await db.InstallmentPurchases.AnyAsync(x => x.CategoryId == id, ct);

        if (inUse)
        {
            entity.IsActive = false;
        }
        else
        {
            db.Categories.Remove(entity);
        }

        await db.SaveChangesAsync(ct);
    }
}
