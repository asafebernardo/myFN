using ClosedXML.Excel;
using Microsoft.EntityFrameworkCore;
using MyFn.Application.Abstractions;
using MyFn.Application.Import;
using MyFn.Domain.Common;
using MyFn.Domain.Entities;
using MyFn.Domain.Enums;
using MyFn.Infrastructure.ImportExport;
using MyFn.Infrastructure.Persistence;

namespace MyFn.Tests;

public class SpreadsheetLayoutTests
{
    private static readonly string[] BancoDoBrasilHeaders =
    [
        "Data", "Lançamento", "Detalhes", "Nº documento", "Valor", "Tipo Lançamento"
    ];

    [Fact]
    public void Planilha_do_BB_mapeia_tipo_lancamento_e_nao_a_coluna_lancamento()
    {
        var map = SpreadsheetLayout.GuessColumns(BancoDoBrasilHeaders);

        Assert.Equal("Data", map.Date);
        Assert.Equal("Lançamento", map.Description);
        Assert.Equal("Detalhes", map.Details);
        Assert.Equal("Valor", map.Amount);
        Assert.Equal("Tipo Lançamento", map.Type);
    }

    [Fact]
    public void Planilha_generica_continua_usando_descricao_e_tipo()
    {
        var map = SpreadsheetLayout.GuessColumns(["Descricao", "Valor", "Data", "Tipo"]);

        Assert.Equal("Descricao", map.Description);
        Assert.Equal("Valor", map.Amount);
        Assert.Equal("Data", map.Date);
        Assert.Equal("Tipo", map.Type);
    }

    [Fact]
    public void Pix_recebido_com_entrada_e_receita()
    {
        var row = SpreadsheetLayout.Classify(
            "Pix - Recebido — 01/09 07:49 ADRIANA KOHLER",
            "Entrada",
            300m);

        Assert.False(row.Skip);
        Assert.True(row.IsIncome);
        Assert.False(row.IsInvoicePayment);
        Assert.Equal("Entrada", row.Label);
    }

    [Fact]
    public void Pagto_cartao_credito_e_pagamento_de_fatura_nao_compra()
    {
        var row = SpreadsheetLayout.Classify("Pagto cartão crédito", "Saída", -326.31m);

        Assert.False(row.Skip);
        Assert.False(row.IsIncome);
        Assert.True(row.IsInvoicePayment);
        Assert.False(row.IsCreditPurchase);
        Assert.Equal("Pagamento de fatura", row.Label);
    }

    [Fact]
    public void Saldo_anterior_e_zero_sao_ignorados()
    {
        Assert.True(SpreadsheetLayout.Classify("Saldo Anterior", string.Empty, 0m).Skip);
        Assert.True(SpreadsheetLayout.Classify("Pix - Recebido", "Entrada", 0m).Skip);
    }

    [Fact]
    public void Despesa_generica_positiva_nao_vira_entrada()
    {
        var row = SpreadsheetLayout.Classify("Mercado", "Despesa", 80m);
        Assert.False(row.IsIncome);
        Assert.Equal("Saída", row.Label);
    }
}

public class BancoDoBrasilImportServiceTests
{
    [Fact]
    public async Task Importa_entrada_e_saida_do_extrato_do_banco_do_brasil()
    {
        var options = new DbContextOptionsBuilder<FinanceDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        await using var db = new FinanceDbContext(options);
        await db.Database.OpenConnectionAsync();
        await db.Database.EnsureCreatedAsync();

        var userId = WellKnownIds.DefaultUserId;
        db.Users.Add(new User { Id = userId, Name = "Teste", CreatedAt = DateTime.UtcNow });
        db.Categories.AddRange(
            new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Salário", Type = CategoryType.Income, IsActive = true },
            new Category { Id = Guid.NewGuid(), UserId = userId, Name = "Outros", Type = CategoryType.Expense, IsActive = true });
        await db.SaveChangesAsync();

        var service = new ImportService(db, new StubUser());
        await using var excel = BancoDoBrasilWorkbook();
        var headers = await service.ReadHeadersAsync(excel);
        var map = SpreadsheetLayout.GuessColumns(headers);

        excel.Position = 0;
        var preview = await service.PreviewAsync(excel, map);
        Assert.Equal(2, preview.Count);
        Assert.Contains(preview, r => r.IsIncome && r.Amount == 300m && r.KindLabel == "Entrada");
        Assert.Contains(preview, r => r.IsInvoicePayment && r.Amount == 326.31m && r.KindLabel == "Pagamento de fatura");
        Assert.DoesNotContain(preview, r => r.Description.Contains("Saldo", StringComparison.OrdinalIgnoreCase));

        excel.Position = 0;
        var imported = await service.ImportAsync(excel, map);
        Assert.Equal(2, imported);

        var income = Assert.Single(db.Incomes);
        Assert.Equal(300m, income.Amount);
        Assert.Contains("Pix - Recebido", income.Description);
        Assert.Contains("ADRIANA KOHLER", income.Description);
        Assert.Equal(new DateOnly(2026, 9, 1), income.Date);

        var expense = Assert.Single(db.Expenses);
        Assert.Equal(326.31m, expense.Amount);
        Assert.Equal(ExpenseKind.InvoicePayment, expense.Kind);
        Assert.Contains("Pagto cartão crédito", expense.Description);
        Assert.Equal(new DateOnly(2026, 9, 1), expense.Date);
    }

    private static MemoryStream BancoDoBrasilWorkbook()
    {
        using var workbook = new XLWorkbook();
        var sheet = workbook.Worksheets.Add("Extrato");
        sheet.Cell(1, 1).Value = "Data";
        sheet.Cell(1, 2).Value = "Lançamento";
        sheet.Cell(1, 3).Value = "Detalhes";
        sheet.Cell(1, 4).Value = "Nº documento";
        sheet.Cell(1, 5).Value = "Valor";
        sheet.Cell(1, 6).Value = "Tipo Lançamento";

        sheet.Cell(2, 1).Value = new DateTime(2026, 8, 31);
        sheet.Cell(2, 2).Value = "Saldo Anterior";
        sheet.Cell(2, 5).Value = 0;

        sheet.Cell(3, 1).Value = new DateTime(2026, 9, 1);
        sheet.Cell(3, 2).Value = "Pix - Recebido";
        sheet.Cell(3, 3).Value = "01/09 07:49 71265457972 ADRIANA KOHLER";
        sheet.Cell(3, 4).Value = "10749045104071";
        sheet.Cell(3, 5).Value = 300.00;
        sheet.Cell(3, 6).Value = "Entrada";

        sheet.Cell(4, 1).Value = new DateTime(2026, 9, 1);
        sheet.Cell(4, 2).Value = "Pagto cartão crédito";
        sheet.Cell(4, 4).Value = "40300400028183";
        sheet.Cell(4, 5).Value = -326.31;
        sheet.Cell(4, 6).Value = "Saída";

        var stream = new MemoryStream();
        workbook.SaveAs(stream);
        stream.Position = 0;
        return stream;
    }

    private sealed class StubUser : ICurrentUser
    {
        public Guid UserId => WellKnownIds.DefaultUserId;
    }
}
