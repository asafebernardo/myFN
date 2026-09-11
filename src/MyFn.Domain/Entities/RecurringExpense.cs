using MyFn.Domain.Enums;

namespace MyFn.Domain.Entities;

public class RecurringExpense
{
    public Guid Id { get; set; }
    public Guid UserId { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid CategoryId { get; set; }
    public PaymentMethod PaymentMethod { get; set; }
    public Guid? CreditCardId { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsActive { get; set; } = true;
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public RecurrenceFrequency Frequency { get; set; } = RecurrenceFrequency.Monthly;
    public string? WhatIsIt { get; set; }
    public string? Prdv { get; set; }
    public string? Notes { get; set; }

    public User? User { get; set; }
    public Category? Category { get; set; }
    public CreditCard? CreditCard { get; set; }
}
