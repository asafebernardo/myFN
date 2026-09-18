using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using MyFn.Domain.Entities;

namespace MyFn.Infrastructure.Persistence;

internal static class MoneyConfig
{
    public static void Money(this PropertyBuilder<decimal> property) =>
        property.HasPrecision(18, 2);
}

internal sealed class UserConfig : IEntityTypeConfiguration<User>
{
    public void Configure(EntityTypeBuilder<User> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(120).IsRequired();
        builder.Property(x => x.Email).HasMaxLength(200);
    }
}

internal sealed class CategoryConfig : IEntityTypeConfiguration<Category>
{
    public void Configure(EntityTypeBuilder<Category> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Color).HasMaxLength(16).IsRequired();
        builder.HasIndex(x => new { x.UserId, x.Name });
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}

internal sealed class IncomeConfig : IEntityTypeConfiguration<Income>
{
    public void Configure(EntityTypeBuilder<Income> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Amount).Money();
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => new { x.UserId, x.Date });
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
    }
}

internal sealed class ExpenseConfig : IEntityTypeConfiguration<Expense>
{
    public void Configure(EntityTypeBuilder<Expense> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Amount).Money();
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => new { x.UserId, x.Date });
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CreditCard).WithMany(x => x.Expenses).HasForeignKey(x => x.CreditCardId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class CreditCardConfig : IEntityTypeConfiguration<CreditCard>
{
    public void Configure(EntityTypeBuilder<CreditCard> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Name).HasMaxLength(80).IsRequired();
        builder.Property(x => x.Bank).HasMaxLength(80);
        builder.Property(x => x.Limit).Money();
        builder.HasIndex(x => x.UserId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}

internal sealed class CreditCardInvoiceConfig : IEntityTypeConfiguration<CreditCardInvoice>
{
    public void Configure(EntityTypeBuilder<CreditCardInvoice> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.StatementAmount).Money();
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => new { x.CreditCardId, x.ClosingDate }).IsUnique();
        builder.HasIndex(x => x.UserId);
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.CreditCard).WithMany(x => x.Invoices).HasForeignKey(x => x.CreditCardId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class InstallmentPurchaseConfig : IEntityTypeConfiguration<InstallmentPurchase>
{
    public void Configure(EntityTypeBuilder<InstallmentPurchase> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(160).IsRequired();
        builder.Property(x => x.TotalAmount).Money();
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => new { x.UserId, x.PurchaseDate });
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.CreditCard).WithMany(x => x.Purchases).HasForeignKey(x => x.CreditCardId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
        builder.HasMany(x => x.Installments).WithOne(x => x.Purchase).HasForeignKey(x => x.InstallmentPurchaseId).OnDelete(DeleteBehavior.Cascade);
    }
}

internal sealed class InstallmentConfig : IEntityTypeConfiguration<Installment>
{
    public void Configure(EntityTypeBuilder<Installment> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Amount).Money();
        builder.HasIndex(x => x.DueDate);
        builder.HasIndex(x => new { x.InstallmentPurchaseId, x.Number }).IsUnique();
    }
}

internal sealed class RecurringExpenseConfig : IEntityTypeConfiguration<RecurringExpense>
{
    public void Configure(EntityTypeBuilder<RecurringExpense> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(160).IsRequired();
        builder.Property(x => x.Amount).Money();
        builder.Property(x => x.WhatIsIt).HasMaxLength(200);
        builder.Property(x => x.Prdv).HasMaxLength(80);
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => new { x.UserId, x.IsMandatory, x.IsActive });
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.Restrict);
        builder.HasOne(x => x.CreditCard).WithMany().HasForeignKey(x => x.CreditCardId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class PlannedPurchaseConfig : IEntityTypeConfiguration<PlannedPurchase>
{
    public void Configure(EntityTypeBuilder<PlannedPurchase> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.Description).HasMaxLength(160).IsRequired();
        builder.Property(x => x.EstimatedAmount).Money();
        builder.Property(x => x.Notes).HasMaxLength(500);
        builder.HasIndex(x => new { x.UserId, x.Status });
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
        builder.HasOne(x => x.Category).WithMany().HasForeignKey(x => x.CategoryId).OnDelete(DeleteBehavior.SetNull);
    }
}

internal sealed class AppSettingConfig : IEntityTypeConfiguration<AppSetting>
{
    public void Configure(EntityTypeBuilder<AppSetting> builder)
    {
        builder.HasKey(x => x.Id);
        builder.Property(x => x.DefaultSalary).Money();
        builder.Property(x => x.CurrencyCode).HasMaxLength(8).IsRequired();
        builder.HasIndex(x => x.UserId).IsUnique();
        builder.HasOne(x => x.User).WithMany().HasForeignKey(x => x.UserId);
    }
}
