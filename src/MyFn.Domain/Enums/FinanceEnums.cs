namespace MyFn.Domain.Enums;

public enum CategoryType
{
    Income = 1,
    Expense = 2
}

public enum PaymentMethod
{
    Cash = 1,
    Pix = 2,
    Debit = 3,
    CreditCard = 4
}

public enum ExpenseKind
{
    Debit = 1,
    CreditCash = 2,
    InvoicePayment = 3
}

public enum InvoiceMatchStatus
{
    MissingTarget = 1,
    Pending = 2,
    Matched = 3,
    Over = 4
}

public enum InstallmentStatus
{
    Open = 1,
    Paid = 2,
    Overdue = 3,
    Cancelled = 4
}

public enum PlannedPurchaseStatus
{
    Planned = 1,
    Purchased = 2,
    Cancelled = 3
}

public enum PriorityLevel
{
    High = 1,
    Medium = 2,
    Low = 3
}

public enum RecurrenceFrequency
{
    Monthly = 1
}

public enum CommitmentLevel
{
    Healthy = 1,
    Warning = 2,
    Critical = 3
}

public enum AffordabilityLevel
{
    Recommended = 1,
    Caution = 2,
    NotRecommended = 3
}
