using MyFn.Domain.Common;
using MyFn.Domain.Enums;

namespace MyFn.Domain.Finance;

public sealed record AffordabilityResult(
    AffordabilityLevel Level,
    bool IsRecommended,
    decimal BalanceAfter,
    decimal CommitmentAfter,
    IReadOnlyList<string> Reasons);

public static class AffordabilityCalculator
{
    public static AffordabilityResult Evaluate(
        MonthlySummary currentMonth,
        decimal estimatedAmount,
        decimal healthyMaxPercent,
        decimal warningMaxPercent)
    {
        estimatedAmount = Money.Round(estimatedAmount);
        var reasons = new List<string>();

        if (estimatedAmount <= 0)
        {
            reasons.Add("Informe um valor estimado positivo.");
            return new AffordabilityResult(AffordabilityLevel.NotRecommended, false, currentMonth.Balance, currentMonth.CommitmentPercent, reasons);
        }

        var balanceAfter = Money.Round(currentMonth.Balance - estimatedAmount);
        var committedAfter = Money.Round(currentMonth.TotalExpenses + estimatedAmount);
        var percentAfter = CommitmentCalculator.PercentOfIncome(committedAfter, currentMonth.Income);
        var levelAfter = CommitmentCalculator.Classify(percentAfter, healthyMaxPercent, warningMaxPercent);

        if (currentMonth.Income <= 0)
        {
            reasons.Add("Não há entradas cadastradas neste mês para avaliar a compra.");
            return new AffordabilityResult(AffordabilityLevel.NotRecommended, false, balanceAfter, percentAfter, reasons);
        }

        if (balanceAfter < 0)
        {
            reasons.Add("O saldo do mês não cobre o valor estimado, considerando obrigatórias, débitos, crédito à vista e parcelas.");
            return new AffordabilityResult(AffordabilityLevel.NotRecommended, false, balanceAfter, percentAfter, reasons);
        }

        if (levelAfter == CommitmentLevel.Critical)
        {
            reasons.Add($"A compra elevaria o comprometimento da renda para {percentAfter:N2}%, acima do limite de {warningMaxPercent:N0}%.");
            return new AffordabilityResult(AffordabilityLevel.NotRecommended, false, balanceAfter, percentAfter, reasons);
        }

        var thinBuffer = currentMonth.Income > 0 && balanceAfter < Money.Round(currentMonth.Income * 0.10m);

        if (levelAfter == CommitmentLevel.Warning || thinBuffer)
        {
            if (levelAfter == CommitmentLevel.Warning)
            {
                reasons.Add($"Cabe no saldo, mas o comprometimento iria para {percentAfter:N2}% (faixa de atenção).");
            }

            if (thinBuffer)
            {
                reasons.Add("A sobra após a compra ficaria abaixo de 10% da renda.");
            }

            return new AffordabilityResult(AffordabilityLevel.Caution, false, balanceAfter, percentAfter, reasons);
        }

        reasons.Add("Há saldo no mês e o comprometimento permaneceria em nível saudável.");
        return new AffordabilityResult(AffordabilityLevel.Recommended, true, balanceAfter, percentAfter, reasons);
    }
}
