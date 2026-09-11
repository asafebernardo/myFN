using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;

namespace MyFn.Domain.Finance;

public static class InstallmentCalculator
{
    public static IReadOnlyList<decimal> SplitAmount(decimal totalAmount, int installmentCount)
    {
        if (installmentCount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(installmentCount), "O número de parcelas deve ser maior que zero.");
        }

        if (totalAmount <= 0)
        {
            throw new ArgumentOutOfRangeException(nameof(totalAmount), "O valor total deve ser positivo.");
        }

        totalAmount = Money.Round(totalAmount);
        var baseAmount = Money.Round(totalAmount / installmentCount);
        var amounts = new decimal[installmentCount];

        decimal allocated = 0;
        for (var i = 0; i < installmentCount - 1; i++)
        {
            amounts[i] = baseAmount;
            allocated += baseAmount;
        }

        amounts[installmentCount - 1] = Money.Round(totalAmount - allocated);
        return amounts;
    }

    public static IReadOnlyList<Installment> Generate(
        Guid purchaseId,
        decimal totalAmount,
        int installmentCount,
        int currentInstallment,
        DateOnly firstInstallmentDate)
    {
        if (currentInstallment < 1 || currentInstallment > installmentCount)
        {
            throw new ArgumentOutOfRangeException(nameof(currentInstallment), "A parcela atual deve estar entre 1 e o total de parcelas.");
        }

        var amounts = SplitAmount(totalAmount, installmentCount);
        var items = new List<Installment>(installmentCount);

        for (var number = 1; number <= installmentCount; number++)
        {
            var status = number < currentInstallment
                ? InstallmentStatus.Paid
                : InstallmentStatus.Open;

            items.Add(new Installment
            {
                Id = Guid.NewGuid(),
                InstallmentPurchaseId = purchaseId,
                Number = number,
                Amount = amounts[number - 1],
                DueDate = firstInstallmentDate.AddMonths(number - 1),
                Status = status,
                PaidAt = status == InstallmentStatus.Paid ? firstInstallmentDate.AddMonths(number - 1) : null
            });
        }

        return items;
    }

    public static InstallmentStatus EffectiveStatus(Installment installment, DateOnly today)
    {
        if (installment.Status is InstallmentStatus.Paid or InstallmentStatus.Cancelled)
        {
            return installment.Status;
        }

        return installment.DueDate < today ? InstallmentStatus.Overdue : InstallmentStatus.Open;
    }

    public static decimal RemainingOpenAmount(IEnumerable<Installment> installments, DateOnly today) =>
        Money.Round(installments
            .Where(i => EffectiveStatus(i, today) is InstallmentStatus.Open or InstallmentStatus.Overdue)
            .Sum(i => i.Amount));
}
