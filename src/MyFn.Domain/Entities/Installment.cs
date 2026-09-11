using MyFn.Domain.Enums;

namespace MyFn.Domain.Entities;

public class Installment
{
    public Guid Id { get; set; }
    public Guid InstallmentPurchaseId { get; set; }
    public int Number { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public InstallmentStatus Status { get; set; }
    public DateOnly? PaidAt { get; set; }

    public InstallmentPurchase? Purchase { get; set; }
}
