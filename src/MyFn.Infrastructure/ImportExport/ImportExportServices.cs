using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MyFn.Application.Abstractions;
using MyFn.Application.Contracts;
using MyFn.Application.Services;
using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;
using System.Globalization;
using System.Text;

namespace MyFn.Infrastructure.ImportExport;

public sealed class ImportService(IAppDbContext db, ICurrentUser user) : IImportService
{
    public Task<IReadOnlyList<string>> ReadHeadersAsync(Stream excel, CancellationToken ct = default)
    {
        using var workbook = new XLWorkbook(excel);
        var sheet = workbook.Worksheets.First();
        var headers = sheet.Row(1).CellsUsed().Select(c => c.GetString().Trim()).ToList();
        return Task.FromResult<IReadOnlyList<string>>(headers);
    }

    public async Task<IReadOnlyList<ImportPreviewRow>> PreviewAsync(Stream excel, ImportColumnMap map, CancellationToken ct = default)
    {
        var rows = ReadRows(excel, map);
        var existing = await db.Expenses.AsNoTracking()
            .Where(e => e.UserId == user.UserId)
            .Select(e => new { e.Description, e.Amount, e.Date })
            .ToListAsync(ct);
        var incomes = await db.Incomes.AsNoTracking()
            .Where(e => e.UserId == user.UserId)
            .Select(e => new { e.Description, e.Amount, e.Date })
            .ToListAsync(ct);

        foreach (var row in rows)
        {
            var dupExpense = existing.Any(e => Same(e.Description, row.Description) && e.Amount == row.Amount && e.Date == row.Date);
            var dupIncome = incomes.Any(e => Same(e.Description, row.Description) && e.Amount == row.Amount && e.Date == row.Date);
            row.PossibleDuplicate = dupExpense || dupIncome;
            if (row.PossibleDuplicate)
            {
                row.Warning = "Possível duplicidade (mesma descrição, valor e data).";
            }
        }

        return rows;
    }

    public async Task<int> ImportAsync(Stream excel, ImportColumnMap map, CancellationToken ct = default)
    {
        var rows = await PreviewAsync(excel, map, ct);
        var defaultExpense = await db.Categories.AsNoTracking()
            .Where(c => c.UserId == user.UserId && c.Type == CategoryType.Expense && c.IsActive)
            .Select(c => c.Id)
            .FirstAsync(ct);
        var defaultIncome = await db.Categories.AsNoTracking()
            .Where(c => c.UserId == user.UserId && c.Type == CategoryType.Income && c.IsActive)
            .Select(c => c.Id)
            .FirstAsync(ct);

        var imported = 0;
        foreach (var row in rows.Where(r => !r.PossibleDuplicate && r.Amount > 0 && r.Date.HasValue))
        {
            var isIncome = row.Entity.Contains("entrada", StringComparison.OrdinalIgnoreCase)
                           || row.Entity.Contains("salário", StringComparison.OrdinalIgnoreCase)
                           || row.Entity.Contains("salario", StringComparison.OrdinalIgnoreCase);

            if (isIncome)
            {
                db.Incomes.Add(new Income
                {
                    Id = Guid.NewGuid(),
                    UserId = user.UserId,
                    Description = row.Description,
                    Amount = Money.Round(row.Amount),
                    Date = row.Date!.Value,
                    CategoryId = defaultIncome,
                    Notes = "Importado de planilha"
                });
            }
            else
            {
                db.Expenses.Add(new Expense
                {
                    Id = Guid.NewGuid(),
                    UserId = user.UserId,
                    Description = row.Description,
                    Amount = Money.Round(row.Amount),
                    Date = row.Date!.Value,
                    CategoryId = defaultExpense,
                    PaymentMethod = PaymentMethod.Pix,
                    Kind = ExpenseKind.Debit,
                    Notes = "Importado de planilha"
                });
            }

            imported++;
        }

        await db.SaveChangesAsync(ct);
        return imported;
    }

    private static List<ImportPreviewRow> ReadRows(Stream excel, ImportColumnMap map)
    {
        if (excel.CanSeek)
        {
            excel.Position = 0;
        }

        using var workbook = new XLWorkbook(excel);
        var sheet = workbook.Worksheets.First();
        var header = sheet.Row(1).CellsUsed().ToDictionary(c => c.GetString().Trim(), c => c.Address.ColumnNumber, StringComparer.OrdinalIgnoreCase);

        int Col(string name) => header.TryGetValue(name, out var n) ? n : -1;
        var descCol = Col(map.Description);
        var amountCol = Col(map.Amount);
        var dateCol = Col(map.Date);
        var typeCol = Col(map.Type);

        var rows = new List<ImportPreviewRow>();
        var last = sheet.LastRowUsed()?.RowNumber() ?? 1;
        for (var r = 2; r <= last; r++)
        {
            var description = descCol > 0 ? sheet.Cell(r, descCol).GetString().Trim() : string.Empty;
            if (string.IsNullOrWhiteSpace(description))
            {
                continue;
            }

            var amountCell = amountCol > 0 ? sheet.Cell(r, amountCol) : null;
            decimal amount = 0;
            if (amountCell is not null && amountCell.TryGetValue(out double raw))
            {
                amount = Money.Round((decimal)raw);
            }
            else if (amountCell is not null)
            {
                decimal.TryParse(amountCell.GetString(), NumberStyles.Any, new CultureInfo("pt-BR"), out amount);
                amount = Money.Round(amount);
            }

            DateOnly? date = null;
            if (dateCol > 0)
            {
                var cell = sheet.Cell(r, dateCol);
                if (cell.TryGetValue(out DateTime dt))
                {
                    date = DateOnly.FromDateTime(dt);
                }
                else if (DateOnly.TryParse(cell.GetString(), new CultureInfo("pt-BR"), DateTimeStyles.None, out var parsed))
                {
                    date = parsed;
                }
            }

            rows.Add(new ImportPreviewRow
            {
                Line = r,
                Entity = typeCol > 0 ? sheet.Cell(r, typeCol).GetString() : "Despesa",
                Description = description,
                Amount = amount,
                Date = date
            });
        }

        return rows;
    }

    private static bool Same(string a, string b) =>
        string.Equals(a.Trim(), b.Trim(), StringComparison.OrdinalIgnoreCase);
}

public sealed class ExportService(
    IFinancialSummaryService summary,
    IExpenseService expenses,
    IInstallmentService installments,
    IProjectionService projection,
    IReportService reports) : IExportService
{
    public async Task<byte[]> ExportLedgerExcelAsync(int year, int month, CancellationToken ct = default)
    {
        var ledger = await summary.GetLedgerAsync(year, month, ct);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Lancamentos");
        WriteHeader(sheet, "Data", "Tipo", "Descrição", "Categoria", "Valor");
        var row = 2;
        foreach (var item in ledger)
        {
            sheet.Cell(row, 1).Value = item.Date.ToDateTime(TimeOnly.MinValue);
            sheet.Cell(row, 1).Style.DateFormat.Format = "dd/MM/yyyy";
            sheet.Cell(row, 2).Value = item.Kind;
            sheet.Cell(row, 3).Value = item.Description;
            sheet.Cell(row, 4).Value = item.Category;
            sheet.Cell(row, 5).Value = item.Amount;
            sheet.Cell(row, 5).Style.NumberFormat.Format = "\"R$\" #,##0.00";
            row++;
        }

        return ToBytes(workbook);
    }

    public async Task<byte[]> ExportExpensesCsvAsync(CancellationToken ct = default)
    {
        var items = await expenses.ListAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("Descricao;Valor;Data;Categoria;Pagamento;Tipo;Observacao");
        foreach (var e in items)
        {
            sb.AppendLine(string.Join(';',
                Csv(e.Description),
                e.Amount.ToString("N2", new CultureInfo("pt-BR")),
                e.Date.ToString("dd/MM/yyyy"),
                Csv(e.CategoryName),
                Labels.Payment(e.PaymentMethod),
                Labels.Kind(e.Kind),
                Csv(e.Notes ?? string.Empty)));
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<byte[]> ExportInstallmentsCsvAsync(CancellationToken ct = default)
    {
        var purchases = await installments.ListPurchasesAsync(ct);
        var sb = new StringBuilder();
        sb.AppendLine("Compra;Parcela;Total;Valor;Vencimento;Status;Cartao");
        foreach (var p in purchases)
        {
            foreach (var i in p.Installments)
            {
                sb.AppendLine(string.Join(';',
                    Csv(p.Description),
                    $"{i.Number}/{i.InstallmentCount}",
                    p.InstallmentCount,
                    i.Amount.ToString("N2", new CultureInfo("pt-BR")),
                    i.DueDate.ToString("dd/MM/yyyy"),
                    Labels.Status(i.Status),
                    Csv(i.CreditCardName)));
            }
        }

        return Encoding.UTF8.GetPreamble().Concat(Encoding.UTF8.GetBytes(sb.ToString())).ToArray();
    }

    public async Task<byte[]> ExportProjectionExcelAsync(int year, int month, int months, CancellationToken ct = default)
    {
        var items = await projection.ProjectAsync(year, month, months, ct);
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Projecao");
        WriteHeader(sheet, "Mês", "Entradas", "Obrigatórias", "Débitos", "Crédito à vista", "Parcelas", "Total", "Saldo", "% comprometido");
        var row = 2;
        foreach (var m in items)
        {
            sheet.Cell(row, 1).Value = m.Month.Label;
            MoneyCell(sheet, row, 2, m.Income);
            MoneyCell(sheet, row, 3, m.MandatoryExpenses);
            MoneyCell(sheet, row, 4, m.Debits);
            MoneyCell(sheet, row, 5, m.CashCredit);
            MoneyCell(sheet, row, 6, m.Installments);
            MoneyCell(sheet, row, 7, m.TotalExpenses);
            MoneyCell(sheet, row, 8, m.Balance);
            sheet.Cell(row, 9).Value = m.CommitmentPercent / 100m;
            sheet.Cell(row, 9).Style.NumberFormat.Format = "0.00%";
            row++;
        }

        return ToBytes(workbook);
    }

    public async Task<byte[]> ExportReportExcelAsync(ReportFilter filter, CancellationToken ct = default)
    {
        var report = await reports.GetAsync(filter, ct);
        using var workbook = new XLWorkbook();
        var cat = workbook.Worksheets.Add("Por categoria");
        WriteHeader(cat, "Categoria", "Valor");
        var r = 2;
        foreach (var item in report.ByCategory)
        {
            cat.Cell(r, 1).Value = item.Name;
            MoneyCell(cat, r, 2, item.Amount);
            r++;
        }

        var months = workbook.Worksheets.Add("Por mes");
        WriteHeader(months, "Mês", "Entradas", "Despesas", "Saldo");
        r = 2;
        foreach (var m in report.ByMonth)
        {
            months.Cell(r, 1).Value = m.Month.Label;
            MoneyCell(months, r, 2, m.Income);
            MoneyCell(months, r, 3, m.TotalExpenses);
            MoneyCell(months, r, 4, m.Balance);
            r++;
        }

        return ToBytes(workbook);
    }

    private static void WriteHeader(IXLWorksheet sheet, params string[] headers)
    {
        for (var i = 0; i < headers.Length; i++)
        {
            sheet.Cell(1, i + 1).Value = headers[i];
            sheet.Cell(1, i + 1).Style.Font.Bold = true;
        }
    }

    private static void MoneyCell(IXLWorksheet sheet, int row, int col, decimal value)
    {
        sheet.Cell(row, col).Value = value;
        sheet.Cell(row, col).Style.NumberFormat.Format = "\"R$\" #,##0.00";
    }

    private static byte[] ToBytes(XLWorkbook workbook)
    {
        using var stream = new MemoryStream();
        workbook.SaveAs(stream);
        return stream.ToArray();
    }

    private static string Csv(string value) =>
        "\"" + value.Replace("\"", "\"\"") + "\"";
}
