using MyFn.Domain.Enums;

namespace MyFn.Domain.Entities;

public class PlannedPurchase
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal EstimatedAmount { get; set; }
    public PriorityLevel Priority { get; set; }
    public Guid? CategoryId { get; set; }
    public DateOnly? DesiredDate { get; set; }
    public string? Notes { get; set; }
    public PlannedPurchaseStatus Status { get; set; } = PlannedPurchaseStatus.Planned;

    public User? User { get; set; }
    public Category? Category { get; set; }
}
