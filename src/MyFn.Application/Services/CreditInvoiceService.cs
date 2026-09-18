using Microsoft.EntityFrameworkCore;
using MyFn.Application.Abstractions;
using MyFn.Application.Contracts;
using MyFn.Application.Validation;
using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Finance;

namespace MyFn.Application.Services;

public interface ICreditInvoiceService
{
    Task<CreditInvoiceDto> GetAsync(Guid cardId, DateOnly? closingDate = null, CancellationToken ct = default);
    Task<CreditInvoiceDto> SaveStatementAsync(Guid cardId, DateOnly closingDate, decimal statementAmount, CancellationToken ct = default);
}

public sealed class CreditInvoiceService(IAppDbContext db, ICurrentUser user) : ICreditInvoiceService
{
    public async Task<CreditInvoiceDto> GetAsync(Guid cardId, DateOnly? closingDate = null, CancellationToken ct = default)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var data = await FinanceDataLoader.LoadAsync(db, user.UserId, ct);
        var card = data.Cards.FirstOrDefault(c => c.Id == cardId)
                   ?? throw new FinanceValidationException(["Cartão não encontrado."]);

        var cycles = CreditCardCalculator.RecentCycles(card, today);
        var cycle = closingDate is DateOnly closing
            ? cycles.FirstOrDefault(c => c.ClosingDate == closing) ?? CreditCardCalculator.CycleEndingOn(card, closing)
            : CreditCardCalculator.LastClosedCycle(card, today);

        var stored = await db.CreditCardInvoices.AsNoTracking()
            .FirstOrDefaultAsync(i => i.CreditCardId == card.Id && i.ClosingDate == cycle.ClosingDate, ct);

        var categories = data.Categories.ToDictionary(c => c.Id, c => c.Name);
        var recon = InvoiceReconciler.Reconcile(card, cycle, stored?.StatementAmount, data.Expenses, data.Installments, categories, data.Recurring);

        return Map(card, recon, stored?.Id, cycles, today);
    }

    public async Task<CreditInvoiceDto> SaveStatementAsync(Guid cardId, DateOnly closingDate, decimal statementAmount, CancellationToken ct = default)
    {
        FinanceValidator.ForInvoiceStatement(statementAmount).ThrowIfInvalid();

        var card = await db.CreditCards.FirstOrDefaultAsync(c => c.Id == cardId && c.UserId == user.UserId, ct)
                   ?? throw new FinanceValidationException(["Cartão não encontrado."]);

        var cycle = CreditCardCalculator.CycleEndingOn(card, closingDate);
        var entity = await db.CreditCardInvoices
            .FirstOrDefaultAsync(i => i.CreditCardId == card.Id && i.ClosingDate == cycle.ClosingDate, ct);

        if (entity is null)
        {
            entity = new CreditCardInvoice
            {
                Id = Guid.NewGuid(),
                UserId = user.UserId,
                CreditCardId = card.Id
            };
            db.CreditCardInvoices.Add(entity);
        }

        entity.ClosingDate = cycle.ClosingDate;
        entity.DueDate = cycle.DueDate;
        entity.CycleStartExclusive = cycle.StartExclusive;
        entity.StatementAmount = Money.Round(statementAmount);

        await db.SaveChangesAsync(ct);
        return await GetAsync(cardId, cycle.ClosingDate, ct);
    }

    private static CreditInvoiceDto Map(
        CreditCard card,
        InvoiceReconciliation recon,
        Guid? invoiceId,
        IReadOnlyList<BillingCycle> cycles,
        DateOnly today)
    {
        var current = CreditCardCalculator.CurrentCycle(card, today);
        return new CreditInvoiceDto
        {
            Id = invoiceId,
            CreditCardId = card.Id,
            CardName = card.Name,
            Bank = card.Bank,
            ClosingDate = recon.Cycle.ClosingDate,
            DueDate = recon.Cycle.DueDate,
            CycleStartExclusive = recon.Cycle.StartExclusive,
            StatementAmount = recon.StatementAmount,
            DetailedAmount = recon.DetailedAmount,
            Remaining = recon.Remaining,
            Status = recon.Status,
            Items = recon.Items.Select(i => new InvoiceItemDto
            {
                Source = i.Source,
                SourceId = i.SourceId,
                Date = i.Date,
                Kind = i.Kind,
                Description = i.Description,
                Amount = i.Amount,
                Category = i.Category
            }).ToList(),
            Cycles = cycles.Select(c => new BillingCycleDto
            {
                ClosingDate = c.ClosingDate,
                DueDate = c.DueDate,
                CycleStartExclusive = c.StartExclusive,
                IsCurrent = c.ClosingDate == current.ClosingDate,
                Label = c.ClosingDate == current.ClosingDate
                    ? $"Fatura aberta · fecha {c.ClosingDate:dd/MM} · vence {c.DueDate:dd/MM}"
                    : $"Fatura fechada {c.ClosingDate:dd/MM/yyyy} · vence {c.DueDate:dd/MM}"
            }).ToList()
        };
    }
}
