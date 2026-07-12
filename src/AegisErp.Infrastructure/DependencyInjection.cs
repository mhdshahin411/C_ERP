using AegisErp.Infrastructure.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace AegisErp.Infrastructure;

public static class DependencyInjection
{
    /// <summary>
    /// Registers the DbContext factory (provider chosen by config) and the application services.
    /// Config keys: "Database:Provider" (Sqlite|Postgres) and "ConnectionStrings:{Provider}".
    /// </summary>
    public static IServiceCollection AddAegisInfrastructure(this IServiceCollection services, IConfiguration config)
    {
        var provider = config["Database:Provider"] ?? DatabaseProvider.Sqlite;
        var conn = config.GetConnectionString(provider)
                   ?? (provider == DatabaseProvider.Postgres
                       ? "Host=localhost;Database=aegis_erp;Username=postgres;Password=postgres"
                       : "Data Source=aegis_erp.db");

        services.AddDbContextFactory<AegisDbContext>(options =>
            DatabaseProvider.Configure(options, provider, conn));

        services.AddScoped<ChartOfAccountsService>();
        services.AddScoped<JournalService>();
        services.AddScoped<LedgerService>();
        services.AddScoped<CustomerService>();
        services.AddScoped<SalesInvoiceService>();
        services.AddScoped<ReceiptService>();
        services.AddScoped<CompanyService>();
        return services;
    }
}
