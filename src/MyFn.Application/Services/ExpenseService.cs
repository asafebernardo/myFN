using Microsoft.EntityFrameworkCore;
using MyFn.Application.Abstractions;
using MyFn.Application.Contracts;
using MyFn.Application.Validation;
using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;

namespace MyFn.Application.Services;

public interface IExpenseService
{
    Task<IReadOnlyList<ExpenseDto>> ListAsync(CancellationToken ct = default);
    Task<ExpenseDto> SaveAsync(ExpenseDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
    Task<QuickExpenseResult> CreateQuickAsync(QuickExpenseRequest request, CancellationToken ct = default);
}

public sealed class ExpenseService(IAppDbContext db, ICurrentUser user, IInstallmentService installments) : IExpenseService
{
    public async Task<IReadOnlyList<ExpenseDto>> ListAsync(CancellationToken ct = default)
    {
        return await db.Expenses.AsNoTracking()
            .Where(e => e.UserId == user.UserId)
            .OrderByDescending(e => e.Date)
            .Select(e => new ExpenseDto
            {
                Id = e.Id,
                Description = e.Description,
                Amount = e.Amount,
                Date = e.Date,
                CategoryId = e.CategoryId,
                CategoryName = e.Category != null ? e.Category.Name : string.Empty,
                PaymentMethod = e.PaymentMethod,
                Kind = e.Kind,
                CreditCardId = e.CreditCardId,
                CreditCardName = e.CreditCard != null ? e.CreditCard.Name : null,
                Notes = e.Notes
            })
            .ToListAsync(ct);
    }

    public async Task<ExpenseDto> SaveAsync(ExpenseDto dto, CancellationToken ct = default)
    {
        FinanceValidator.ForMoney(dto.Description, dto.Amount, dto.Date).ThrowIfInvalid();
        FinanceValidator.ForExpensePayment(dto.PaymentMethod, dto.CreditCardId, 1).ThrowIfInvalid();
        if (dto.CategoryId == Guid.Empty)
        {
            throw new FinanceValidationException(["Selecione uma categoria."]);
        }

        Expense entity;
        if (dto.Id == Guid.Empty)
        {
            entity = new Expense { Id = Guid.NewGuid(), UserId = user.UserId };
            db.Expenses.Add(entity);
        }
        else
        {
            entity = await db.Expenses.FirstAsync(e => e.Id == dto.Id && e.UserId == user.UserId, ct);
        }

        entity.Description = dto.Description.Trim();
        entity.Amount = Money.Round(dto.Amount);
        entity.Date = dto.Date;
        entity.CategoryId = dto.CategoryId;
        entity.PaymentMethod = dto.PaymentMethod;
        entity.Kind = dto.PaymentMethod == PaymentMethod.CreditCard ? ExpenseKind.CreditCash : ExpenseKind.Debit;
        entity.CreditCardId = dto.PaymentMethod == PaymentMethod.CreditCard ? dto.CreditCardId : null;
        entity.Notes = dto.Notes;

        await db.SaveChangesAsync(ct);
        dto.Id = entity.Id;
        dto.Kind = entity.Kind;
        return dto;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var entity = await db.Expenses.FirstOrDefaultAsync(e => e.Id == id && e.UserId == user.UserId, ct)
            ?? throw new FinanceValidationException(["Despesa não encontrada."]);
        db.Expenses.Remove(entity);
        await db.SaveChangesAsync(ct);
    }

    public async Task<QuickExpenseResult> CreateQuickAsync(QuickExpenseRequest request, CancellationToken ct = default)
    {
        var installmentsCount = request.InstallmentCount <= 0 ? 1 : request.InstallmentCount;
        FinanceValidator.ForMoney(request.Description, request.Amount, request.Date).ThrowIfInvalid();
        FinanceValidator.ForExpensePayment(request.PaymentMethod, request.CreditCardId, installmentsCount).ThrowIfInvalid();

        var categoryId = request.CategoryId ?? await DefaultExpenseCategoryAsync(ct);

        if (request.PaymentMethod == PaymentMethod.CreditCard && installmentsCount > 1)
        {
            var firstDue = request.FirstInstallmentDate ?? request.Date;
            var purchase = await installments.CreateAsync(new InstallmentPurchaseDto
            {
                Description = request.Description,
                TotalAmount = request.Amount,
                InstallmentCount = installmentsCount,
                CurrentInstallment = request.CurrentInstallment <= 0 ? 1 : request.CurrentInstallment,
                CreditCardId = request.CreditCardId!.Value,
                CategoryId = categoryId,
                PurchaseDate = request.Date,
                FirstInstallmentDate = firstDue,
                Notes = request.Notes
            }, ct);

            return new QuickExpenseResult
            {
                CreatedInstallmentPurchase = true,
                GeneratedInstallments = purchase.InstallmentCount,
                EntityId = purchase.Id,
                Message = $"Compra criada. {purchase.InstallmentCount} parcelas geradas."
            };
        }

        var saved = await SaveAsync(new ExpenseDto
        {
            Description = request.Description,
            Amount = request.Amount,
            Date = request.Date,
            CategoryId = categoryId,
            PaymentMethod = request.PaymentMethod,
            CreditCardId = request.CreditCardId,
            Notes = request.Notes
        }, ct);

        return new QuickExpenseResult
        {
            CreatedInstallmentPurchase = false,
            GeneratedInstallments = 0,
            EntityId = saved.Id,
            Message = "Despesa lançada."
        };
    }

    private async Task<Guid> DefaultExpenseCategoryAsync(CancellationToken ct)
    {
        var existing = await db.Categories.AsNoTracking()
            .Where(c => c.UserId == user.UserId && c.Type == CategoryType.Expense && c.IsActive)
            .OrderBy(c => c.SortOrder)
            .Select(c => c.Id)
            .FirstOrDefaultAsync(ct);

        if (existing != Guid.Empty)
        {
            return existing;
        }

        throw new FinanceValidationException(["Cadastre uma categoria de despesa antes de lançar."]);
    }
}
