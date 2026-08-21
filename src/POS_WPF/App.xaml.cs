using System.Windows;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using POS_WPF.Data;
using POS_WPF.Domain.Inventory;
using POS_WPF.Domain.Products;

namespace POS_WPF;

public partial class App : Application
{
    private IHost? _host;

    protected override async void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        _host = Host.CreateDefaultBuilder()
            .ConfigureServices((_, services) =>
            {
                services.AddDbContextFactory<AppDbContext>(options =>
                {
                    options.UseSqlite("Data Source=pos-local.db");
                });

                services.AddSingleton<UnitConversionService>();
                services.AddSingleton<InventoryService>();
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

        var window = _host.Services.GetRequiredService<MainWindow>();
        window.Show();
    }

    protected override async void OnExit(ExitEventArgs e)
    {
        if (_host is not null)
        {
            await _host.StopAsync();
            _host.Dispose();
        }

        base.OnExit(e);
    }
}
