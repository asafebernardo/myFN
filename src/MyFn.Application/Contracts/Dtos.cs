using MyFn.Domain.Common;
using MyFn.Domain.Enums;
using MyFn.Domain.Finance;

namespace MyFn.Application.Contracts;

public sealed class CategoryDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public CategoryType Type { get; set; }
    public string Color { get; set; } = "#1b7a4e";
    public bool IsActive { get; set; } = true;
}

public sealed class IncomeDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public bool IsRecurring { get; set; }
    public DateOnly? RecurrenceEndDate { get; set; }
    public string? Notes { get; set; }
}

public sealed class ExpenseDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public ExpenseKind Kind { get; set; }
    public Guid? CreditCardId { get; set; }
    public string? CreditCardName { get; set; }
    public string? Notes { get; set; }
    public bool IsInvoicePayment { get; set; }
}

public sealed class CreditCardDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Bank { get; set; } = string.Empty;
    public decimal Limit { get; set; }
    public int ClosingDay { get; set; }
    public int DueDay { get; set; }
    public bool IsActive { get; set; }
    public decimal UsedLimit { get; set; }
    public decimal AvailableLimit { get; set; }
    public decimal CurrentInvoice { get; set; }
    public decimal? ClosedInvoiceStatement { get; set; }
    public decimal ClosedInvoiceDetailed { get; set; }
    public decimal ClosedInvoiceRemaining { get; set; }
    public InvoiceMatchStatus ClosedInvoiceStatus { get; set; }
    public IReadOnlyList<InvoiceMonthDto> UpcomingInvoices { get; set; } = [];
}

public sealed class InvoiceMonthDto
{
    public string Label { get; set; } = string.Empty;
    public DateOnly DueDate { get; set; }
    public decimal Amount { get; set; }
}

public sealed class InstallmentPurchaseDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal TotalAmount { get; set; }
    public int InstallmentCount { get; set; }
    public int CurrentInstallment { get; set; }
    public Guid CreditCardId { get; set; }
    public string CreditCardName { get; set; } = string.Empty;
    public Guid? CategoryId { get; set; }
    public DateOnly PurchaseDate { get; set; }
    public DateOnly FirstInstallmentDate { get; set; }
    public string? Notes { get; set; }
    public decimal RemainingAmount { get; set; }
    public DateOnly? LastDueDate { get; set; }
    public IReadOnlyList<InstallmentDto> Installments { get; set; } = [];
}

public sealed class InstallmentDto
{
    public Guid Id { get; set; }
    public Guid PurchaseId { get; set; }
    public string PurchaseDescription { get; set; } = string.Empty;
    public int Number { get; set; }
    public int InstallmentCount { get; set; }
    public decimal Amount { get; set; }
    public DateOnly DueDate { get; set; }
    public InstallmentStatus Status { get; set; }
    public string CreditCardName { get; set; } = string.Empty;
}

public sealed class RecurringExpenseDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public Guid CategoryId { get; set; }
    public string CategoryName { get; set; } = string.Empty;
    public PaymentMethod PaymentMethod { get; set; }
    public Guid? CreditCardId { get; set; }
    public bool IsMandatory { get; set; }
    public bool IsActive { get; set; }
    public DateOnly StartDate { get; set; }
    public DateOnly? EndDate { get; set; }
    public string? WhatIsIt { get; set; }
    public string? Prdv { get; set; }
    public string? Notes { get; set; }
    public decimal PercentOfIncome { get; set; }
}

public sealed class PlannedPurchaseDto
{
    public Guid Id { get; set; }
    public string Description { get; set; } = string.Empty;
    public decimal EstimatedAmount { get; set; }
    public PriorityLevel Priority { get; set; }
    public Guid? CategoryId { get; set; }
    public string? CategoryName { get; set; }
    public DateOnly? DesiredDate { get; set; }
    public string? Notes { get; set; }
    public PlannedPurchaseStatus Status { get; set; }
    public AffordabilityResult? Affordability { get; set; }
}

public sealed class SettingsDto
{
    public decimal DefaultSalary { get; set; }
    public string CurrencyCode { get; set; } = "BRL";
    public int FinancialMonthStartDay { get; set; } = 1;
    public decimal HealthyCommitmentMaxPercent { get; set; } = 70m;
    public decimal WarningCommitmentMaxPercent { get; set; } = 90m;
    public bool ShowDashboardCharts { get; set; } = true;
}

public sealed class QuickExpenseRequest
{
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly Date { get; set; }
    public Guid? CategoryId { get; set; }
    public PaymentMethod PaymentMethod { get; set; } = PaymentMethod.Pix;
    public Guid? CreditCardId { get; set; }
    public int InstallmentCount { get; set; } = 1;
    public int CurrentInstallment { get; set; } = 1;
    public DateOnly? FirstInstallmentDate { get; set; }
    public string? Notes { get; set; }
    public bool IsInvoicePayment { get; set; }
}

public sealed class QuickExpenseResult
{
    public bool CreatedInstallmentPurchase { get; set; }
    public int GeneratedInstallments { get; set; }
    public Guid EntityId { get; set; }
    public string Message { get; set; } = string.Empty;
}

public sealed class DashboardDto
{
    public MonthlySummary Summary { get; set; } = null!;
    public IReadOnlyList<CategorySliceDto> ExpensesByCategory { get; set; } = [];
    public IReadOnlyList<MonthlySummary> Evolution { get; set; } = [];
    public IReadOnlyList<InstallmentDto> EndingSoon { get; set; } = [];
    public IReadOnlyList<NamedAmountDto> TopExpenses { get; set; } = [];
}

public sealed class CategorySliceDto
{
    public string Name { get; set; } = string.Empty;
    public string Color { get; set; } = "#888";
    public decimal Amount { get; set; }
}

public sealed class NamedAmountDto
{
    public string Name { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Detail { get; set; } = string.Empty;
}

public sealed class ImportPreviewRow
{
    public int Line { get; set; }
    public string Entity { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateOnly? Date { get; set; }
    public bool PossibleDuplicate { get; set; }
    public string? Warning { get; set; }
}

public sealed class ImportColumnMap
{
    public string Description { get; set; } = "Descricao";
    public string Amount { get; set; } = "Valor";
    public string Date { get; set; } = "Data";
    public string Category { get; set; } = "Categoria";
    public string Type { get; set; } = "Tipo";
}

public sealed class ReportFilter
{
    public DateOnly From { get; set; }
    public DateOnly To { get; set; }
    public Guid? CategoryId { get; set; }
    public Guid? CreditCardId { get; set; }
    public ExpenseKind? ExpenseKind { get; set; }
    public InstallmentStatus? InstallmentStatus { get; set; }
    public PlannedPurchaseStatus? PlannedStatus { get; set; }
}

public sealed class ReportBundle
{
    public IReadOnlyList<CategorySliceDto> ByCategory { get; set; } = [];
    public IReadOnlyList<MonthlySummary> ByMonth { get; set; } = [];
    public IReadOnlyList<NamedAmountDto> BalanceEvolution { get; set; } = [];
    public IReadOnlyList<InstallmentDto> FutureInstallments { get; set; } = [];
    public IReadOnlyList<RecurringExpenseDto> Recurring { get; set; } = [];
    public IReadOnlyList<CreditCardDto> Cards { get; set; } = [];
    public IReadOnlyList<PlannedPurchaseDto> Planned { get; set; } = [];
}

public sealed class LedgerEntryDto
{
    public DateOnly Date { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
}

public sealed class BillingCycleDto
{
    public DateOnly ClosingDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly CycleStartExclusive { get; set; }
    public bool IsCurrent { get; set; }
    public string Label { get; set; } = string.Empty;
}

public sealed class InvoiceItemDto
{
    public string Source { get; set; } = string.Empty;
    public Guid SourceId { get; set; }
    public DateOnly Date { get; set; }
    public string Kind { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public string Category { get; set; } = string.Empty;
}

public sealed class CreditInvoiceDto
{
    public Guid? Id { get; set; }
    public Guid CreditCardId { get; set; }
    public string CardName { get; set; } = string.Empty;
    public string Bank { get; set; } = string.Empty;
    public DateOnly ClosingDate { get; set; }
    public DateOnly DueDate { get; set; }
    public DateOnly CycleStartExclusive { get; set; }
    public decimal? StatementAmount { get; set; }
    public decimal DetailedAmount { get; set; }
    public decimal Remaining { get; set; }
    public InvoiceMatchStatus Status { get; set; }
    public IReadOnlyList<InvoiceItemDto> Items { get; set; } = [];
    public IReadOnlyList<BillingCycleDto> Cycles { get; set; } = [];
}

