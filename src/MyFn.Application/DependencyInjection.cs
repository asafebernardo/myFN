using Microsoft.Extensions.DependencyInjection;
using MyFn.Application.Services;

namespace MyFn.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddMyFnApplication(this IServiceCollection services)
    {
        services.AddScoped<ICategoryService, CategoryService>();
        services.AddScoped<IIncomeService, IncomeService>();
        services.AddScoped<IExpenseService, ExpenseService>();
        services.AddScoped<IInstallmentService, InstallmentService>();
        services.AddScoped<ICreditCardService, CreditCardService>();
        services.AddScoped<ICreditInvoiceService, CreditInvoiceService>();
        services.AddScoped<IRecurringExpenseService, RecurringExpenseService>();
        services.AddScoped<IPlannedPurchaseService, PlannedPurchaseService>();
        services.AddScoped<IFinancialSummaryService, FinancialSummaryService>();
        services.AddScoped<IProjectionService, ProjectionService>();
        services.AddScoped<ISettingsService, SettingsService>();
        services.AddScoped<IReportService, ReportService>();
        services.AddScoped<IFinanceResetService, FinanceResetService>();
        return services;
    }
}
