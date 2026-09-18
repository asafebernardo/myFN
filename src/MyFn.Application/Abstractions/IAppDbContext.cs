using MyFn.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace MyFn.Application.Abstractions;

public interface IAppDbContext
{
    DbSet<User> Users { get; }
    DbSet<Category> Categories { get; }
    DbSet<Income> Incomes { get; }
    DbSet<Expense> Expenses { get; }
    DbSet<CreditCard> CreditCards { get; }
    DbSet<CreditCardInvoice> CreditCardInvoices { get; }
    DbSet<InstallmentPurchase> InstallmentPurchases { get; }
    DbSet<Installment> Installments { get; }
    DbSet<RecurringExpense> RecurringExpenses { get; }
    DbSet<PlannedPurchase> PlannedPurchases { get; }
    DbSet<AppSetting> Settings { get; }

    Task<int> SaveChangesAsync(CancellationToken cancellationToken = default);
}

public interface ICurrentUser
{
    Guid UserId { get; }
}
