using Microsoft.EntityFrameworkCore;
using MyFn.Application.Abstractions;
using MyFn.Application.Contracts;
using MyFn.Application.Validation;
using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;
using MyFn.Domain.Finance;

namespace MyFn.Application.Services;

public interface IInstallmentService
{
    Task<IReadOnlyList<InstallmentPurchaseDto>> ListPurchasesAsync(CancellationToken ct = default);
    Task<InstallmentPurchaseDto> GetAsync(Guid id, CancellationToken ct = default);
    Task<InstallmentPurchaseDto> CreateAsync(InstallmentPurchaseDto dto, CancellationToken ct = default);
    Task DeletePurchaseAsync(Guid id, CancellationToken ct = default);
    Task SetInstallmentStatusAsync(Guid installmentId, InstallmentStatus status, CancellationToken ct = default);
}

public sealed class InstallmentService(IAppDbContext db, ICurrentUser user) : IInstallmentService
{
    public async Task<IReadOnlyList<InstallmentPurchaseDto>> ListPurchasesAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var purchases = await db.InstallmentPurchases
            .AsNoTracking()
            .Include(p => p.CreditCard)
            .Include(p => p.Installments)
            .Where(p => p.UserId == user.UserId)
            .OrderByDescending(p => p.PurchaseDate)
            .ToListAsync(ct);

        return purchases.Select(p => Map(p, today)).ToList();
    }

    public async Task<InstallmentPurchaseDto> GetAsync(Guid id, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var purchase = await db.InstallmentPurchases
            .AsNoTracking()
            .Include(p => p.CreditCard)
            .Include(p => p.Installments)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.UserId, ct)
            ?? throw new FinanceValidationException(["Compra parcelada não encontrada."]);

        return Map(purchase, today);
    }

    public async Task<InstallmentPurchaseDto> CreateAsync(InstallmentPurchaseDto dto, CancellationToken ct = default)
    {
        FinanceValidator.ForInstallmentPurchase(
            dto.Description,
            dto.TotalAmount,
            dto.InstallmentCount,
            dto.CurrentInstallment,
            dto.CreditCardId,
            dto.PurchaseDate,
            dto.FirstInstallmentDate).ThrowIfInvalid();

        var cardExists = await db.CreditCards.AnyAsync(c => c.Id == dto.CreditCardId && c.UserId == user.UserId, ct);
        if (!cardExists)
        {
            throw new FinanceValidationException(["Cartão não encontrado."]);
        }

        var purchase = new InstallmentPurchase
        {
            Id = Guid.NewGuid(),
            UserId = user.UserId,
            Description = dto.Description.Trim(),
            TotalAmount = Money.Round(dto.TotalAmount),
            InstallmentCount = dto.InstallmentCount,
            CurrentInstallment = dto.CurrentInstallment,
            CreditCardId = dto.CreditCardId,
            CategoryId = dto.CategoryId,
            PurchaseDate = dto.PurchaseDate,
            FirstInstallmentDate = dto.FirstInstallmentDate,
            Notes = dto.Notes,
            CreatedAt = DateTime.UtcNow
        };

        foreach (var installment in InstallmentCalculator.Generate(
                     purchase.Id,
                     purchase.TotalAmount,
                     purchase.InstallmentCount,
                     purchase.CurrentInstallment,
                     purchase.FirstInstallmentDate))
        {
            purchase.Installments.Add(installment);
        }

        db.InstallmentPurchases.Add(purchase);
        await db.SaveChangesAsync(ct);

        return await GetAsync(purchase.Id, ct);
    }

    public async Task DeletePurchaseAsync(Guid id, CancellationToken ct = default)
    {
        var purchase = await db.InstallmentPurchases
            .Include(p => p.Installments)
            .FirstOrDefaultAsync(p => p.Id == id && p.UserId == user.UserId, ct)
            ?? throw new FinanceValidationException(["Compra parcelada não encontrada."]);

        db.Installments.RemoveRange(purchase.Installments);
        db.InstallmentPurchases.Remove(purchase);
        await db.SaveChangesAsync(ct);
    }

    public async Task SetInstallmentStatusAsync(Guid installmentId, InstallmentStatus status, CancellationToken ct = default)
    {
        var installment = await db.Installments
            .Include(i => i.Purchase)
            .FirstOrDefaultAsync(i => i.Id == installmentId && i.Purchase!.UserId == user.UserId, ct)
            ?? throw new FinanceValidationException(["Parcela não encontrada."]);

        installment.Status = status is InstallmentStatus.Overdue ? InstallmentStatus.Open : status;
        installment.PaidAt = status == InstallmentStatus.Paid ? DateOnly.FromDateTime(DateTime.Today) : null;
        await db.SaveChangesAsync(ct);
    }

    private static InstallmentPurchaseDto Map(InstallmentPurchase purchase, DateOnly today)
    {
        var items = purchase.Installments
            .OrderBy(i => i.Number)
            .Select(i => new InstallmentDto
            {
                Id = i.Id,
                PurchaseId = purchase.Id,
                PurchaseDescription = purchase.Description,
                Number = i.Number,
                InstallmentCount = purchase.InstallmentCount,
                Amount = i.Amount,
                DueDate = i.DueDate,
                Status = InstallmentCalculator.EffectiveStatus(i, today),
                CreditCardName = purchase.CreditCard?.Name ?? string.Empty
            })
            .ToList();

        var remaining = items.Where(i => i.Status is InstallmentStatus.Open or InstallmentStatus.Overdue).ToList();

        return new InstallmentPurchaseDto
        {
            Id = purchase.Id,
            Description = purchase.Description,
            TotalAmount = purchase.TotalAmount,
            InstallmentCount = purchase.InstallmentCount,
            CurrentInstallment = purchase.CurrentInstallment,
            CreditCardId = purchase.CreditCardId,
            CreditCardName = purchase.CreditCard?.Name ?? string.Empty,
            CategoryId = purchase.CategoryId,
            PurchaseDate = purchase.PurchaseDate,
            FirstInstallmentDate = purchase.FirstInstallmentDate,
            Notes = purchase.Notes,
            RemainingAmount = Money.Round(remaining.Sum(i => i.Amount)),
            LastDueDate = items.LastOrDefault()?.DueDate,
            Installments = items
        };
    }
}
