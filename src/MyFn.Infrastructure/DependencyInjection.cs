using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using MyFn.Application.Abstractions;
using MyFn.Application.Services;
using MyFn.Infrastructure.ImportExport;
using MyFn.Infrastructure.Persistence;
using MyFn.Infrastructure.Seed;

namespace MyFn.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddMyFnInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("Finance")
            ?? "Data Source=App_Data/myfn.db";

        services.AddDbContext<FinanceDbContext>(options => options.UseSqlite(connectionString));
        services.AddScoped<IAppDbContext>(sp => sp.GetRequiredService<FinanceDbContext>());
        services.AddScoped<ICurrentUser, SingleUserContext>();
        services.AddScoped<IImportService, ImportService>();
        services.AddScoped<IExportService, ExportService>();
        return services;
    }

    public static async Task InitializeDatabaseAsync(this IServiceProvider services, bool seedDevelopment)
    {
        using var scope = services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<FinanceDbContext>();
        await db.Database.MigrateAsync();
        if (seedDevelopment)
        {
            await DevelopmentSeed.ApplyAsync(db);
        }
    }
}
