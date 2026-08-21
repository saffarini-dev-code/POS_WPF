using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using POS_WPF.Data;
using POS_WPF.Domain.Inventory;
using POS_WPF.Domain.Pricing;
using POS_WPF.Domain.Products;
using POS_WPF.Infrastructure.Localization;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((context, services) =>
            {
                var provider = context.Configuration["Database:Provider"] ?? "Sqlite";
                var connectionString = context.Configuration["Database:ConnectionString"] ?? "Data Source=pos-local.db";
                services.AddDbContextFactory<AppDbContext>(options =>
                {
                    if (provider.Equals("SqlServer", StringComparison.OrdinalIgnoreCase)) options.UseSqlServer(connectionString);
                    else options.UseSqlite(connectionString);
                });
                services.AddSingleton<UnitConversionService>();
                services.AddSingleton<InventoryService>();
                services.AddSingleton<InventoryBalanceService>();
                services.AddSingleton<PricingCalculator>();
                services.AddSingleton<LocalizationService>();
                services.AddSingleton<IPasswordHasher, PasswordHasher>();
                services.AddSingleton<MainWindow>();
                services.AddLogging(builder => builder.AddConsole());
            })
            .Build();

        await _host.StartAsync();
        await using (var scope = _host.Services.CreateAsyncScope())
        {
            var factory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<AppDbContext>>();
            await using var db = await factory.CreateDbContextAsync();
            await db.Database.EnsureCreatedAsync();
        }
        _host.Services.GetRequiredService<MainWindow>().Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null) { await _host.StopAsync(); _host.Dispose(); }
        base.OnExit(e);
    }
}
