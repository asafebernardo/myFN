using Microsoft.EntityFrameworkCore;
using MyFn.Application.Abstractions;
using MyFn.Application.Contracts;
using MyFn.Application.Validation;
using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;
using MyFn.Domain.Finance;

namespace MyFn.Application.Services;

public interface ICreditCardService
{
    Task<IReadOnlyList<CreditCardDto>> ListAsync(CancellationToken ct = default);
    Task<CreditCardDto> SaveAsync(CreditCardDto dto, CancellationToken ct = default);
    Task DeleteAsync(Guid id, CancellationToken ct = default);
}

public sealed class CreditCardService(IAppDbContext db, ICurrentUser user) : ICreditCardService
{
    public async Task<IReadOnlyList<CreditCardDto>> ListAsync(CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var data = await FinanceDataLoader.LoadAsync(db, user.UserId, ct);
        var cards = await db.CreditCards.AsNoTracking()
            .Where(c => c.UserId == user.UserId)
            .OrderBy(c => c.Name)
            .ToListAsync(ct);

        return cards.Select(card => Map(card, data, today)).ToList();
    }

    public async Task<CreditCardDto> SaveAsync(CreditCardDto dto, CancellationToken ct = default)
    {
        FinanceValidator.ForCreditCard(dto.Name, dto.Limit, dto.ClosingDay, dto.DueDay).ThrowIfInvalid();

        CreditCard entity;
        if (dto.Id == Guid.Empty)
        {
            entity = new CreditCard { Id = Guid.NewGuid(), UserId = user.UserId };
            db.CreditCards.Add(entity);
        }
        else
        {
            entity = await db.CreditCards.FirstAsync(c => c.Id == dto.Id && c.UserId == user.UserId, ct);
        }

        entity.Name = dto.Name.Trim();
        entity.Bank = (dto.Bank ?? string.Empty).Trim();
        entity.Limit = Money.Round(dto.Limit);
        entity.ClosingDay = dto.ClosingDay;
        entity.DueDay = dto.DueDay;
        entity.IsActive = dto.IsActive;

        await db.SaveChangesAsync(ct);
        dto.Id = entity.Id;
        return dto;
    }

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var inUse = await db.InstallmentPurchases.AnyAsync(p => p.CreditCardId == id, ct)
                    || await db.Expenses.AnyAsync(e => e.CreditCardId == id, ct);
        if (inUse)
        {
            var entity = await db.CreditCards.FirstAsync(c => c.Id == id && c.UserId == user.UserId, ct);
            entity.IsActive = false;
            await db.SaveChangesAsync(ct);
            return;
        }

        var card = await db.CreditCards.FirstOrDefaultAsync(c => c.Id == id && c.UserId == user.UserId, ct)
            ?? throw new FinanceValidationException(["Cartão não encontrado."]);
        db.CreditCards.Remove(card);
        await db.SaveChangesAsync(ct);
    }

    private static CreditCardDto Map(CreditCard card, FinanceData data, DateOnly today)
    {
        var used = CreditCardCalculator.UsedLimit(card, data.Installments, data.Expenses, today);
        var currentInvoice = CreditCardCalculator.CurrentInvoice(card, data.Installments, data.Expenses, today);

        var upcoming = data.Installments
            .Where(i => i.Purchase?.CreditCardId == card.Id && i.Status != InstallmentStatus.Cancelled)
            .Where(i => i.DueDate >= today)
            .GroupBy(i => new { i.DueDate.Year, i.DueDate.Month })
            .OrderBy(g => g.Key.Year).ThenBy(g => g.Key.Month)
            .Take(6)
            .Select(g => new InvoiceMonthDto
            {
                Label = new DateOnly(g.Key.Year, g.Key.Month, 1).ToString("MMM/yyyy", new System.Globalization.CultureInfo("pt-BR")),
                DueDate = g.Min(x => x.DueDate),
                Amount = Money.Round(g.Sum(x => x.Amount))
            })
            .ToList();

        return new CreditCardDto
        {
            Id = card.Id,
            Name = card.Name,
            Bank = card.Bank,
            Limit = card.Limit,
            ClosingDay = card.ClosingDay,
            DueDay = card.DueDay,
            IsActive = card.IsActive,
            UsedLimit = used,
            AvailableLimit = CreditCardCalculator.AvailableLimit(card.Limit, used),
            CurrentInvoice = currentInvoice,
            UpcomingInvoices = upcoming
        };
    }
}
