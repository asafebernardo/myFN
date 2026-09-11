namespace MyFn.Domain.Entities;

public class InstallmentPurchase
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int InstallmentCount { get; set; }
    public int CurrentInstallment { get; set; }
    public Guid CreditCardId { get; set; }
    public Guid? CategoryId { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public DateOnly FirstInstallmentDate { get; set; }
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }

    public User? User { get; set; }
    public CreditCard? CreditCard { get; set; }
    public Category? Category { get; set; }
    public ICollection<Installment> Installments { get; set; } = new List<Installment>();
}
