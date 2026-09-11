using MyFn.Domain.Enums;

namespace MyFn.Domain.Entities;

public class Expense
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public Guid CategoryId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public ExpenseKind Kind { get; set; }
    public Guid? CreditCardId { get; set; }
    public string? Notes { get; set; }

    public User? User { get; set; }
    public Category? Category { get; set; }
    public CreditCard? CreditCard { get; set; }
}
