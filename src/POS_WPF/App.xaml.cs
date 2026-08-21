using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using POS_WPF.Data;
using POS_WPF.Domain.Finance;
using POS_WPF.Domain.Inventory;
using POS_WPF.Domain.POS;
using POS_WPF.Domain.Pricing;
using POS_WPF.Domain.Products;
using POS_WPF.Domain.Purchasing;
using POS_WPF.Domain.Returns;
using POS_WPF.Domain.Reports;
using POS_WPF.Infrastructure.Backup;
using POS_WPF.Infrastructure.Bootstrap;
using POS_WPF.Infrastructure.Localization;
using POS_WPF.Infrastructure.Printing;
using POS_WPF.Infrastructure.Security;
using POS_WPF.Infrastructure.Sync;

namespace POS_WPF;

public partial class App : Application
{
    private IHost? _host;
    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _host = Host.CreateDefaultBuilder().ConfigureServices((context, services) =>
        {
            var provider = context.Configuration["Database:Provider"] ?? "Sqlite";
            var connectionString = context.Configuration["Database:ConnectionString"] ?? "Data Source=pos-local.db";
            services.AddDbContextFactory<AppDbContext>(options => { if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)) options.UseSqlServer(connectionString, sql => sql.EnableRetryOnFailure()); else options.UseSqlite(connectionString); });
            services.AddSingleton<UnitConversionService>(); services.AddSingleton<InventoryService>(); services.AddSingleton<InventoryBalanceService>(); services.AddSingleton<PricingCalculator>(); services.AddSingleton<LocalizationService>();
            services.AddSingleton<IPasswordHasher, PasswordHasher>(); services.AddSingleton<DatabaseAuthenticationService>(); services.AddSingleton<ApplicationSeeder>(); services.AddSingleton<UserAdministrationService>(); services.AddSingleton<SessionContext>();
            services.AddSingleton<BarcodeLookupService>(); services.AddSingleton<SalePostingService>(); services.AddSingleton<PurchasePostingService>(); services.AddSingleton<SalesReturnPostingService>(); services.AddSingleton<CashRegisterService>(); services.AddSingleton<ReportQueryService>(); services.AddSingleton<DatabaseBackupService>();
            services.AddSingleton<IReceiptPrinter, WindowsPrintService>(); services.AddSingleton<IDocumentPrinter, WindowsPrintService>(); services.AddSingleton<ILabelPrinter, WindowsPrintService>(); services.AddSingleton<ISyncConflictResolver, DefaultSyncConflictResolver>(); services.AddLogging(builder => builder.AddConsole());
            services.AddTransient<LoginWindow>(); services.AddSingleton<MainWindow>();
        }).Build();
        await _host.StartAsync();
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>(); await using var db = await factory.CreateDbContextAsync(); await db.Database.EnsureCreatedAsync(); await scope.ServiceProvider.GetRequiredService<ApplicationSeeder>().SeedAsync();
        }
        _host.Services.GetRequiredService<LoginWindow>().Show();
    }
    protected override async void OnExit(ExitEventArgs e) { if (_host is not null) { await _host.StopAsync(); _host.Dispose(); } base.OnExit(e); }
}
