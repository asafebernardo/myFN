namespace MyFn.Domain.Entities;

public class Income
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public Guid CategoryId { get; set; }
    public bool IsRecurring { get; set; }
    public DateOnly? RecurrenceEndDate { get; set; }
    public string? Notes { get; set; }

    public User? User { get; set; }
    public Category? Category { get; set; }
}
