using MyFn.Application.Contracts;

namespace MyFn.Application.Import;

public sealed class SpreadsheetRowClassification
{
    public bool Skip { get; init; }
    public bool IsIncome { get; init; }
    public bool IsInvoicePayment { get; init; }
    public bool IsCreditPurchase { get; init; }
    public string Label { get; init; } = string.Empty;
}

/// <summary>
/// Mapeia cabeçalhos de extrato (inclui o .xlsx do Banco do Brasil) e classifica Entrada/Saída.
/// </summary>
public static class SpreadsheetLayout
{
    public static ImportColumnMap GuessColumns(IReadOnlyList<string> headers)
    {
        var map = new ImportColumnMap();
        if (headers.Count == 0)
        {
            return map;
        }

        map.Date = First(headers, h => ContainsAny(h, "data", "date")) ?? headers[0];
        map.Amount = First(headers, h => ContainsAny(h, "valor", "amount", "vlr")) ?? headers[0];
        map.Type = First(headers, IsTypeHeader) ?? headers[0];
        map.Description = First(headers, IsDescriptionHeader) ?? headers[0];
        map.Details = First(headers, IsDetailsHeader) ?? string.Empty;
        return map;
    }

    public static string CombineDescription(string launch, string details)
    {
        launch = launch.Trim();
        details = details.Trim();
        if (string.IsNullOrWhiteSpace(details))
        {
            return launch;
        }

        if (string.IsNullOrWhiteSpace(launch))
        {
            return details;
        }

        return $"{launch} — {details}";
    }

    public static SpreadsheetRowClassification Classify(string description, string typeOrEntity, decimal signedAmount)
    {
        var desc = description.Trim();
        var type = typeOrEntity.Trim();

        if (IsBalanceRow(desc) || signedAmount == 0)
        {
            return new SpreadsheetRowClassification
            {
                Skip = true,
                Label = "Ignorado"
            };
        }

        var isInvoicePayment = IsInvoicePayment(desc, type);
        var explicitIncome = IsIncomeType(type);
        var explicitExpense = IsExpenseType(type);
        var receivedByDescription = ContainsAny(desc, "recebido", "salário", "salario")
                                    && !ContainsAny(desc, "pagto", "pagamento");

        var isIncome = explicitIncome
                       || (!explicitExpense && receivedByDescription);

        if (isIncome)
        {
            return new SpreadsheetRowClassification
            {
                IsIncome = true,
                Label = "Entrada"
            };
        }

        if (isInvoicePayment)
        {
            return new SpreadsheetRowClassification
            {
                IsInvoicePayment = true,
                Label = "Pagamento de fatura"
            };
        }

        var isCreditPurchase = IsCreditPurchaseType(type);
        return new SpreadsheetRowClassification
        {
            IsCreditPurchase = isCreditPurchase,
            Label = isCreditPurchase ? "Crédito" : "Saída"
        };
    }

    public static bool IsTypeHeader(string header)
    {
        var h = header.Trim();
        if (ContainsAny(h, "tipo lançamento", "tipo lancamento", "tipo de lançamento", "tipo de lancamento"))
        {
            return true;
        }

        return ContainsAny(h, "tipo", "type");
    }

    public static bool IsDescriptionHeader(string header)
    {
        if (IsTypeHeader(header))
        {
            return false;
        }

        return ContainsAny(header, "descrição", "descricao", "histórico", "historico", "lançamento", "lancamento", "memo")
               || ContainsToken(header, "desc");
    }

    public static bool IsDetailsHeader(string header) =>
        ContainsAny(header, "detalhe", "details", "complemento");

    public static bool IsBalanceRow(string description)
    {
        var d = description.Trim();
        return d.StartsWith("saldo", StringComparison.OrdinalIgnoreCase);
    }

    public static bool ContainsAny(string value, params string[] tokens) =>
        tokens.Any(token => value.Contains(token, StringComparison.OrdinalIgnoreCase));

    private static bool IsIncomeType(string type) =>
        ContainsAny(type, "entrada", "receita", "income", "credit in");

    private static bool IsExpenseType(string type) =>
        ContainsAny(type, "saída", "saida", "despesa", "débito", "debito", "expense", "debit");

    private static bool IsInvoicePayment(string description, string type)
    {
        if (ContainsAny(type, "fatura") || ContainsAny(description, "fatura"))
        {
            return true;
        }

        return ContainsAny(description, "pagto", "pag.", "pagamento")
               && ContainsAny(description, "cartão", "cartao");
    }

    private static bool IsCreditPurchaseType(string type) =>
        ContainsAny(type, "crédito", "credito", "cartão", "cartao", "credit")
        && !IsIncomeType(type)
        && !IsExpenseType(type);

    private static bool ContainsToken(string header, string token)
    {
        var h = header.Trim();
        return h.Equals(token, StringComparison.OrdinalIgnoreCase)
               || h.StartsWith(token, StringComparison.OrdinalIgnoreCase);
    }

    private static string? First(IReadOnlyList<string> headers, Func<string, bool> predicate) =>
        headers.FirstOrDefault(predicate);
}
