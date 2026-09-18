using MyFn.Domain.Enums;

namespace MyFn.Application.Contracts;

public static class Labels
{
    public static string Payment(PaymentMethod method) => method switch
    {
        PaymentMethod.Cash => "Dinheiro",
        PaymentMethod.Pix => "PIX",
        PaymentMethod.Debit => "Débito",
        PaymentMethod.CreditCard => "Cartão de crédito",
        _ => method.ToString()
    };

    public static string Kind(ExpenseKind kind) => kind switch
    {
        ExpenseKind.Debit => "Débito",
        ExpenseKind.CreditCash => "Crédito à vista",
        ExpenseKind.InvoicePayment => "Pagamento de fatura",
        _ => kind.ToString()
    };

    public static string InvoiceMatch(InvoiceMatchStatus status) => status switch
    {
        InvoiceMatchStatus.MissingTarget => "Informe o valor da fatura",
        InvoiceMatchStatus.Pending => "Falta detalhar",
        InvoiceMatchStatus.Matched => "Fatura conferida",
        InvoiceMatchStatus.Over => "Acima da fatura",
        _ => status.ToString()
    };

    public static string Status(InstallmentStatus status) => status switch
    {
        InstallmentStatus.Open => "Aberta",
        InstallmentStatus.Paid => "Paga",
        InstallmentStatus.Overdue => "Atrasada",
        InstallmentStatus.Cancelled => "Cancelada",
        _ => status.ToString()
    };

    public static string Priority(PriorityLevel level) => level switch
    {
        PriorityLevel.High => "Alta",
        PriorityLevel.Medium => "Média",
        PriorityLevel.Low => "Baixa",
        _ => level.ToString()
    };

    public static string Planned(PlannedPurchaseStatus status) => status switch
    {
        PlannedPurchaseStatus.Planned => "Planejada",
        PlannedPurchaseStatus.Purchased => "Comprada",
        PlannedPurchaseStatus.Cancelled => "Cancelada",
        _ => status.ToString()
    };

    public static string Type(CategoryType type) => type switch
    {
        CategoryType.Income => "Entrada",
        CategoryType.Expense => "Despesa",
        _ => type.ToString()
    };

    public static string Commitment(CommitmentLevel level) => level switch
    {
        CommitmentLevel.Healthy => "Situação saudável",
        CommitmentLevel.Warning => "Atenção",
        CommitmentLevel.Critical => "Orçamento comprometido",
        _ => level.ToString()
    };

    public static string Affordability(AffordabilityLevel level) => level switch
    {
        AffordabilityLevel.Recommended => "Recomendado",
        AffordabilityLevel.Caution => "Atenção",
        AffordabilityLevel.NotRecommended => "Não recomendado",
        _ => level.ToString()
    };

    public static string MonthTitle(DateOnly date) =>
        date.ToString("MMMM yyyy", new System.Globalization.CultureInfo("pt-BR"));
}
