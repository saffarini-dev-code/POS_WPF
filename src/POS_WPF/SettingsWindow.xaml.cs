using System.Globalization;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Settings;
using POS_WPF.Infrastructure.Localization;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class SettingsWindow : Window
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory; private readonly PermissionService _permissions; private readonly LocalizationService _localization;
    public SettingsWindow(IDbContextFactory<AppDbContext> dbFactory, PermissionService permissions, LocalizationService localization) { InitializeComponent(); _dbFactory = dbFactory; _permissions = permissions; _localization = localization; Loaded += async (_, _) => await LoadAsync(); }
    private async Task LoadAsync()
    { await using var db = await _dbFactory.CreateDbContextAsync(); var branch = await db.Branches.OrderBy(x => x.Code).FirstAsync(); var settings = await db.StoreSettings.SingleOrDefaultAsync(x => x.BranchId == branch.Id); var tax = await db.TaxSettings.FirstOrDefaultAsync(); StoreNameBox.Text = settings?.StoreName ?? "Retail POS"; ArabicNameBox.Text = settings?.StoreNameArabic ?? string.Empty; CurrencyBox.Text = settings?.CurrencyCode ?? "JOD"; TaxRateBox.Text = (tax?.Rate ?? 0m).ToString(CultureInfo.InvariantCulture); LanguageBox.SelectedIndex = _localization.CurrentLanguage == AppLanguage.Arabic ? 1 : 0; }
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try { await _permissions.DemandAsync("Settings.Store"); if (!decimal.TryParse(TaxRateBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate < 0 || rate > 100) throw new InvalidOperationException("Invalid tax rate."); await using var db = await _dbFactory.CreateDbContextAsync(); var branch = await db.Branches.OrderBy(x => x.Code).FirstAsync(); var settings = await db.StoreSettings.SingleOrDefaultAsync(x => x.BranchId == branch.Id) ?? new StoreSettings(StoreNameBox.Text, CurrencyBox.Text); settings.AssignBranch(branch.Id); if (settings.StoreName != StoreNameBox.Text) { db.Entry(settings).Property(x => x.StoreName).CurrentValue = StoreNameBox.Text.Trim(); db.Entry(settings).Property(x => x.CurrencyCode).CurrentValue = CurrencyBox.Text.Trim().ToUpperInvariant(); db.Entry(settings).Property(x => x.StoreNameArabic).CurrentValue = ArabicNameBox.Text.Trim(); } if (settings.Id == Guid.Empty) db.StoreSettings.Add(settings); var tax = await db.TaxSettings.FirstOrDefaultAsync() ?? new TaxSettings(); tax.Configure(true, rate, false, true); if (tax.Id == Guid.Empty) db.TaxSettings.Add(tax); await db.SaveChangesAsync(); _localization.SetLanguage(LanguageBox.SelectedIndex == 1 ? AppLanguage.Arabic : AppLanguage.English); StatusText.Text = "Saved."; } catch (Exception ex) { StatusText.Text = ex.Message; }
    }
}
