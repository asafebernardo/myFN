using Microsoft.EntityFrameworkCore;
using MyFn.Application.Abstractions;
using MyFn.Application.Contracts;
using MyFn.Application.Services;
using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;
using MyFn.Infrastructure.Persistence;

namespace MyFn.Tests;

public class InstallmentServiceTests
{
    [Fact]
    public async Task CreateAsync_gera_parcelas_no_banco()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var db = new FinanceDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var user = new User { Id = WellKnownIds.DefaultUserId, Name = "Teste", CreatedAt = DateTime.UtcNow };
        var card = new CreditCard
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Name = "BB",
            Bank = "BB",
            Limit = 4000,
            ClosingDay = 10,
            DueDay = 17,
            IsActive = true
        };
        db.Users.Add(user);
        db.CreditCards.Add(card);
        await db.SaveChangesAsync();

        ICurrentUser current = new StubUser();
        var service = new InstallmentService(db, current);
        var created = await service.CreateAsync(new InstallmentPurchaseDto
        {
            Description = "PC1 (BB)",
            TotalAmount = 1542.86m,
            InstallmentCount = 12,
            CurrentInstallment = 2,
            CreditCardId = card.Id,
            PurchaseDate = new DateOnly(2026, 10, 12),
            FirstInstallmentDate = new DateOnly(2026, 10, 17)
        });

        Assert.Equal(12, created.Installments.Count);
        Assert.Equal(InstallmentStatus.Paid, created.Installments[0].Status);
        Assert.Equal(InstallmentStatus.Open, created.Installments[1].Status);
        Assert.Equal(1542.86m, created.Installments.Sum(i => i.Amount));
    }

    private sealed class StubUser : ICurrentUser
    {
        public Guid UserId => WellKnownIds.DefaultUserId;
    }
}
