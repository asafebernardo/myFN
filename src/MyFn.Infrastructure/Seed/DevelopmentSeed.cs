using Microsoft.EntityFrameworkCore;
using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;
using MyFn.Domain.Finance;
using MyFn.Infrastructure.Persistence;

namespace MyFn.Infrastructure.Seed;

/// <summary>
/// Dados de EXEMPLO apenas para ambiente Development. Não usar em produção.
/// </summary>
public static class DevelopmentSeed
{
    public static async Task ApplyAsync(FinanceDbContext db, CancellationToken ct = default)
    {
        if (await db.Users.AnyAsync(ct))
        {
            return;
        }

        var userId = WellKnownIds.DefaultUserId;
        var today = new DateOnly(2026, 9, 1);

        var user = new User
        {
            Id = userId,
            Name = "Usuário local",
            Email = "dev@local",
            CreatedAt = DateTime.UtcNow
        };
        db.Users.Add(user);

        var salaryCat = Cat(userId, "Salário", CategoryType.Income, "#1b7a4e", 1);
        var freelanceCat = Cat(userId, "Freelance", CategoryType.Income, "#2d8a6e", 2);
        var otherInCat = Cat(userId, "Outros", CategoryType.Income, "#4aa882", 3);
        var housing = Cat(userId, "Moradia", CategoryType.Expense, "#6c5ce7", 10);
        var food = Cat(userId, "Alimentação", CategoryType.Expense, "#e17055", 11);
        var transport = Cat(userId, "Transporte", CategoryType.Expense, "#0984e3", 12);
        var education = Cat(userId, "Educação", CategoryType.Expense, "#00cec9", 13);
        var health = Cat(userId, "Saúde", CategoryType.Expense, "#d63031", 14);
        var leisure = Cat(userId, "Lazer", CategoryType.Expense, "#fdcb6e", 15);
        var subs = Cat(userId, "Assinaturas", CategoryType.Expense, "#636e72", 16);
        var otherEx = Cat(userId, "Outros", CategoryType.Expense, "#b2bec3", 17);
        db.Categories.AddRange(salaryCat, freelanceCat, otherInCat, housing, food, transport, education, health, leisure, subs, otherEx);

        db.Settings.Add(new AppSetting
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            DefaultSalary = 3640.00m,
            CurrencyCode = "BRL",
            FinancialMonthStartDay = 1,
            HealthyCommitmentMaxPercent = 70m,
            WarningCommitmentMaxPercent = 90m,
            ShowDashboardCharts = true
        });

        db.Incomes.Add(new Income
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Description = "Salário",
            Amount = 3640.00m,
            Date = today,
            CategoryId = salaryCat.Id,
            IsRecurring = true,
            Notes = "SEED-DEV"
        });

        var bb = new CreditCard
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "BB",
            Bank = "Banco do Brasil",
            Limit = 4000m,
            ClosingDay = 10,
            DueDay = 17,
            IsActive = true
        };
        var nubank = new CreditCard
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Nubank",
            Bank = "Nubank",
            Limit = 2500m,
            ClosingDay = 8,
            DueDay = 15,
            IsActive = true
        };
        var inter = new CreditCard
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Name = "Inter",
            Bank = "Inter",
            Limit = 2000m,
            ClosingDay = 5,
            DueDay = 12,
            IsActive = true
        };
        db.CreditCards.AddRange(bb, nubank, inter);

        Recurring(db, userId, "Dízimo", 360m, otherEx.Id, PaymentMethod.Pix, true, today, "Contribuição", null);
        Recurring(db, userId, "Fatura Claro", 100m, subs.Id, PaymentMethod.Pix, true, today, "Telefone/internet", null);
        Recurring(db, userId, "Combustível", 300m, transport.Id, PaymentMethod.Pix, true, today, "Transporte mensal", null);
        Recurring(db, userId, "Inglês CCAA", 650m, education.Id, PaymentMethod.Pix, true, today, "Curso", "12/2026", end: new DateOnly(2026, 12, 31));
        Recurring(db, userId, "Drive", 15m, subs.Id, PaymentMethod.CreditCard, true, today, "Google One", null, cardId: nubank.Id);
        Recurring(db, userId, "Consórcio", 315m, housing.Id, PaymentMethod.Pix, true, today, "Consórcio", null);
        Recurring(db, userId, "Dentista", 130m, health.Id, PaymentMethod.Pix, true, today, "Tratamento", null);
        Recurring(db, userId, "Teologia", 120m, education.Id, PaymentMethod.Pix, true, today, "Curso", null);

        db.Expenses.AddRange(
            new Expense
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Description = "Mercado",
                Amount = 420.00m,
                Date = new DateOnly(2026, 9, 4),
                CategoryId = food.Id,
                PaymentMethod = PaymentMethod.Debit,
                Kind = ExpenseKind.Debit,
                Notes = "SEED-DEV"
            },
            new Expense
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Description = "Farmácia",
                Amount = 85.00m,
                Date = new DateOnly(2026, 9, 6),
                CategoryId = health.Id,
                PaymentMethod = PaymentMethod.Pix,
                Kind = ExpenseKind.Debit,
                Notes = "SEED-DEV"
            },
            new Expense
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Description = "Padaria e extras",
                Amount = 795.00m,
                Date = new DateOnly(2026, 9, 8),
                CategoryId = food.Id,
                PaymentMethod = PaymentMethod.Debit,
                Kind = ExpenseKind.Debit,
                Notes = "SEED-DEV — débitos de exemplo somando ~R$ 1.300 com os demais"
            });

        var pc = new InstallmentPurchase
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Description = "PC1 (BB)",
            TotalAmount = 1542.86m,
            InstallmentCount = 12,
            CurrentInstallment = 2,
            CreditCardId = bb.Id,
            CategoryId = leisure.Id,
            PurchaseDate = new DateOnly(2026, 8, 12),
            FirstInstallmentDate = new DateOnly(2026, 8, 17),
            Notes = "SEED-DEV",
            CreatedAt = DateTime.UtcNow
        };
        foreach (var installment in InstallmentCalculator.Generate(pc.Id, pc.TotalAmount, pc.InstallmentCount, pc.CurrentInstallment, pc.FirstInstallmentDate))
        {
            pc.Installments.Add(installment);
        }

        var phone = new InstallmentPurchase
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Description = "Celular (Nubank)",
            TotalAmount = 2399.88m,
            InstallmentCount = 10,
            CurrentInstallment = 4,
            CreditCardId = nubank.Id,
            CategoryId = otherEx.Id,
            PurchaseDate = new DateOnly(2026, 6, 2),
            FirstInstallmentDate = new DateOnly(2026, 6, 15),
            Notes = "SEED-DEV",
            CreatedAt = DateTime.UtcNow
        };
        foreach (var installment in InstallmentCalculator.Generate(phone.Id, phone.TotalAmount, phone.InstallmentCount, phone.CurrentInstallment, phone.FirstInstallmentDate))
        {
            phone.Installments.Add(installment);
        }

        db.InstallmentPurchases.AddRange(pc, phone);

        db.Expenses.AddRange(
            new Expense
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Description = "iFood",
                Amount = 54.90m,
                Date = new DateOnly(2026, 8, 22),
                CategoryId = food.Id,
                PaymentMethod = PaymentMethod.CreditCard,
                Kind = ExpenseKind.CreditCash,
                CreditCardId = nubank.Id,
                Notes = "SEED-DEV"
            },
            new Expense
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Description = "Uber",
                Amount = 28.40m,
                Date = new DateOnly(2026, 8, 29),
                CategoryId = transport.Id,
                PaymentMethod = PaymentMethod.CreditCard,
                Kind = ExpenseKind.CreditCash,
                CreditCardId = nubank.Id,
                Notes = "SEED-DEV"
            },
            new Expense
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Description = "Farmácia (crédito)",
                Amount = 67.80m,
                Date = new DateOnly(2026, 9, 3),
                CategoryId = health.Id,
                PaymentMethod = PaymentMethod.CreditCard,
                Kind = ExpenseKind.CreditCash,
                CreditCardId = nubank.Id,
                Notes = "SEED-DEV"
            },
            new Expense
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Description = "Mercado no Nubank",
                Amount = 189.90m,
                Date = new DateOnly(2026, 9, 6),
                CategoryId = food.Id,
                PaymentMethod = PaymentMethod.CreditCard,
                Kind = ExpenseKind.CreditCash,
                CreditCardId = nubank.Id,
                Notes = "SEED-DEV"
            },
            new Expense
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Description = "Fatura Nubank",
                Amount = 950.00m,
                Date = new DateOnly(2026, 9, 15),
                CategoryId = otherEx.Id,
                PaymentMethod = PaymentMethod.Pix,
                Kind = ExpenseKind.InvoicePayment,
                CreditCardId = nubank.Id,
                Notes = "SEED-DEV — valor único cobrado no banco"
            });

        var nubankClosed = CreditCardCalculator.LastClosedCycle(nubank, new DateOnly(2026, 9, 18));
        db.CreditCardInvoices.Add(new CreditCardInvoice
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            CreditCardId = nubank.Id,
            ClosingDate = nubankClosed.ClosingDate,
            DueDate = nubankClosed.DueDate,
            CycleStartExclusive = nubankClosed.StartExclusive,
            StatementAmount = 950.00m,
            Notes = "SEED-DEV"
        });

        db.PlannedPurchases.AddRange(
            new PlannedPurchase
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Description = "Mini PC",
                EstimatedAmount = 1000m,
                Priority = PriorityLevel.High,
                CategoryId = leisure.Id,
                DesiredDate = new DateOnly(2026, 11, 1),
                Notes = "SEED-DEV",
                Status = PlannedPurchaseStatus.Planned
            },
            new PlannedPurchase
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Description = "Controles Xbox",
                EstimatedAmount = 70m,
                Priority = PriorityLevel.Medium,
                CategoryId = leisure.Id,
                DesiredDate = new DateOnly(2026, 10, 1),
                Status = PlannedPurchaseStatus.Planned
            },
            new PlannedPurchase
            {
                Id = Guid.NewGuid(),
                UserId = userId,
                Description = "Celular antigo",
                EstimatedAmount = 60m,
                Priority = PriorityLevel.Low,
                CategoryId = otherEx.Id,
                DesiredDate = new DateOnly(2026, 10, 15),
                Status = PlannedPurchaseStatus.Planned
            });

        await db.SaveChangesAsync(ct);
    }

    private static Category Cat(Guid userId, string name, CategoryType type, string color, int order) => new()
    {
        Id = Guid.NewGuid(),
        UserId = userId,
        Name = name,
        Type = type,
        Color = color,
        IsActive = true,
        SortOrder = order
    };

    private static void Recurring(
        FinanceDbContext db,
        Guid userId,
        string description,
        decimal amount,
        Guid categoryId,
        PaymentMethod method,
        bool mandatory,
        DateOnly start,
        string what,
        string? prdv,
        Guid? cardId = null,
        DateOnly? end = null)
    {
        db.RecurringExpenses.Add(new RecurringExpense
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Description = description,
            Amount = amount,
            CategoryId = categoryId,
            PaymentMethod = method,
            CreditCardId = cardId,
            IsMandatory = mandatory,
            IsActive = true,
            StartDate = start,
            EndDate = end,
            WhatIsIt = what,
            Prdv = prdv,
            Notes = "SEED-DEV"
        });
    }
}
