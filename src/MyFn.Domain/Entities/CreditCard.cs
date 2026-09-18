namespace MyFn.Domain.Entities;

public class CreditCard
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Bank { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public bool IsActive { get; set; } = true;

    public User? User { get; set; }
    public ICollection<InstallmentPurchase> Purchases { get; set; } = new List<InstallmentPurchase>();
    public ICollection<Expense> Expenses { get; set; } = new List<Expense>();
    public ICollection<CreditCardInvoice> Invoices { get; set; } = new List<CreditCardInvoice>();
}
