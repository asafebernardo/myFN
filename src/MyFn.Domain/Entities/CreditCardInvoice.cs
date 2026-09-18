namespace MyFn.Domain.Entities;

public class CreditCardInvoice
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public Guid CreditCardId { get; set; }
    public DateOnly ClosingDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly CycleStartExclusive { get; set; }
    public decimal StatementAmount { get; set; }
    public string? Notes { get; set; }

    public User? User { get; set; }
    public CreditCard? CreditCard { get; set; }
}
