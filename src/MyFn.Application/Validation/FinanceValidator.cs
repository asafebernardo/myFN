using MyFn.Domain.Enums;

namespace MyFn.Application.Validation;

public sealed class ValidationResult
{
    public List<string> Errors { get; } = [];
    public bool IsValid => Errors.Count == 0;

    public void Add(string message) => Errors.Add(message);

    public void Ensure(bool condition, string message)
    {
        if (!condition)
        {
            Errors.Add(message);
        }
    }

    public void ThrowIfInvalid()
    {
        if (!IsValid)
        {
            throw new FinanceValidationException(Errors);
        }
    }
}

public sealed class FinanceValidationException : Exception
{
    public IReadOnlyList<string> Errors { get; }

    public FinanceValidationException(IReadOnlyList<string> errors)
        : base(string.Join(" ", errors))
    {
        Errors = errors;
    }
}

public static class FinanceValidator
{
    public static ValidationResult ForMoney(string description, decimal amount, DateOnly date)
    {
        var result = new ValidationResult();
        result.Ensure(!string.IsNullOrWhiteSpace(description), "A descrição é obrigatória.");
        result.Ensure(amount > 0, "O valor deve ser positivo.");
        result.Ensure(date != default, "Informe uma data válida.");
        return result;
    }

    public static ValidationResult ForInstallmentPurchase(
        string description,
        decimal totalAmount,
        int installmentCount,
        int currentInstallment,
        Guid? creditCardId,
        DateOnly purchaseDate,
        DateOnly firstInstallmentDate)
    {
        var result = ForMoney(description, totalAmount, purchaseDate);
        result.Ensure(installmentCount > 0, "O número de parcelas deve ser maior que zero.");
        result.Ensure(currentInstallment >= 1 && currentInstallment <= installmentCount,
            "A parcela atual deve ser menor ou igual ao total de parcelas.");
        result.Ensure(creditCardId.HasValue && creditCardId.Value != Guid.Empty,
            "Selecione o cartão de crédito da compra parcelada.");
        result.Ensure(firstInstallmentDate != default, "Informe a data da primeira parcela.");
        return result;
    }

    public static ValidationResult ForCreditCard(string name, decimal limit, int closingDay, int dueDay)
    {
        var result = new ValidationResult();
        result.Ensure(!string.IsNullOrWhiteSpace(name), "O nome do cartão é obrigatório.");
        result.Ensure(limit > 0, "O limite deve ser positivo.");
        result.Ensure(closingDay is >= 1 and <= 31, "O dia de fechamento deve estar entre 1 e 31.");
        result.Ensure(dueDay is >= 1 and <= 31, "O dia de vencimento deve estar entre 1 e 31.");
        return result;
    }

    public static ValidationResult ForExpensePayment(PaymentMethod method, Guid? creditCardId, int installmentCount)
    {
        var result = new ValidationResult();
        if (method == PaymentMethod.CreditCard || installmentCount > 1)
        {
            result.Ensure(creditCardId.HasValue && creditCardId.Value != Guid.Empty,
                "O cartão é obrigatório para compras no crédito.");
        }

        result.Ensure(installmentCount >= 1, "O número de parcelas deve ser maior que zero.");
        return result;
    }

    public static ValidationResult ForCategory(string name, string color)
    {
        var result = new ValidationResult();
        result.Ensure(!string.IsNullOrWhiteSpace(name), "O nome da categoria é obrigatório.");
        result.Ensure(!string.IsNullOrWhiteSpace(color), "Informe uma cor para a categoria.");
        return result;
    }
}
