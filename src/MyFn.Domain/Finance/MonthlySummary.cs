using MyFn.Domain.Common;
using MyFn.Domain.Enums;

namespace MyFn.Domain.Finance;

public sealed record MonthlySummary
{
    public FinancialMonth Month { get; init; }
    public decimal Income { get; init; }
    public decimal MandatoryExpenses { get; init; }
    public decimal Debits { get; init; }
    public decimal CashCredit { get; init; }
    public decimal Installments { get; init; }

    public decimal TotalExpenses => Money.Round(MandatoryExpenses + Debits + CashCredit + Installments);

    public decimal Balance => Money.Round(Income - TotalExpenses);

    public decimal MandatoryPercentOfIncome => PercentOfIncome(MandatoryExpenses);

    public decimal CommitmentPercent => PercentOfIncome(TotalExpenses);

    public CommitmentLevel CommitmentLevel { get; init; }

    private decimal PercentOfIncome(decimal part)
    {
        if (Income <= 0)
        {
            return part > 0 ? 100m : 0m;
        }

        return Money.Round(part / Income * 100m);
    }
}

public static class CommitmentCalculator
{
    public static CommitmentLevel Classify(decimal commitmentPercent, decimal healthyMax, decimal warningMax)
    {
        if (commitmentPercent < healthyMax)
        {
            return CommitmentLevel.Healthy;
        }

        if (commitmentPercent < warningMax)
        {
            return CommitmentLevel.Warning;
        }

        return CommitmentLevel.Critical;
    }

    public static decimal PercentOfIncome(decimal amount, decimal income)
    {
        if (income <= 0)
        {
            return amount > 0 ? 100m : 0m;
        }

        return Money.Round(amount / income * 100m);
    }
}
