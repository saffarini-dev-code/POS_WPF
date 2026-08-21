using System.Globalization;
using System.IO;
using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using POS_WPF.Data;
using POS_WPF.Domain.Settings;
using POS_WPF.Infrastructure.Localization;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class SettingsWindow : Window
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory; private readonly PermissionService _permissions; private readonly LocalizationService _localization; private string? _selectedLogoPath;
    private static readonly CurrencyOption[] Currencies = [new("JOD", "JOD — Jordanian Dinar"), new("USD", "USD — US Dollar"), new("EUR", "EUR — Euro"), new("GBP", "GBP — British Pound"), new("SAR", "SAR — Saudi Riyal"), new("AED", "AED — UAE Dirham"), new("ILS", "ILS — Israeli Shekel"), new("EGP", "EGP — Egyptian Pound")];
    public SettingsWindow(IDbContextFactory<AppDbContext> dbFactory, PermissionService permissions, LocalizationService localization) { InitializeComponent(); _dbFactory = dbFactory; _permissions = permissions; _localization = localization; CurrencyBox.ItemsSource = Currencies; Loaded += async (_, _) => await LoadAsync(); }
    private async Task LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync(); var branch = await db.Branches.OrderBy(x => x.Code).FirstAsync(); var settings = await db.StoreSettings.SingleOrDefaultAsync(x => x.BranchId == branch.Id); var tax = await db.TaxSettings.FirstOrDefaultAsync();
        StoreNameBox.Text = settings?.StoreName ?? "Retail POS"; ArabicNameBox.Text = settings?.StoreNameArabic ?? string.Empty; CurrencyBox.SelectedValue = settings?.CurrencyCode ?? "JOD"; TaxRateBox.Text = (tax?.Rate ?? 0m).ToString(CultureInfo.InvariantCulture); LanguageBox.SelectedIndex = _localization.CurrentLanguage == AppLanguage.Arabic ? 1 : 0;
        _selectedLogoPath = settings?.LogoPath; LogoPathText.Text = _selectedLogoPath ?? "No logo selected"; LoadLogoPreview(_selectedLogoPath);
    }
    private void ChooseLogo_Click(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFileDialog { Title = "Choose Store Logo", Filter = "Image Files|*.png;*.jpg;*.jpeg;*.bmp|All Files|*.*", CheckFileExists = true };
        if (dialog.ShowDialog() != true) return; _selectedLogoPath = dialog.FileName; LogoPathText.Text = dialog.FileName; LoadLogoPreview(dialog.FileName);
    }
    private void LoadLogoPreview(string? path)
    {
        try { if (string.IsNullOrWhiteSpace(path) || !File.Exists(path)) { LogoPreview.Source = null; return; } var image = new BitmapImage(); image.BeginInit(); image.CacheOption = BitmapCacheOption.OnLoad; image.UriSource = new Uri(path); image.EndInit(); LogoPreview.Source = image; } catch { LogoPreview.Source = null; }
    }
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await _permissions.DemandAsync("Settings.Store");
            if (CurrencyBox.SelectedValue is not string currency || string.IsNullOrWhiteSpace(currency)) throw new InvalidOperationException("Select a currency.");
            if (!decimal.TryParse(TaxRateBox.Text, NumberStyles.Number, CultureInfo.InvariantCulture, out var rate) || rate < 0 || rate > 100) throw new InvalidOperationException("Invalid tax rate.");
            await using var db = await _dbFactory.CreateDbContextAsync(); var branch = await db.Branches.OrderBy(x => x.Code).FirstAsync(); var settings = await db.StoreSettings.SingleOrDefaultAsync(x => x.BranchId == branch.Id);
            string? persistedLogo = null;
            if (!string.IsNullOrWhiteSpace(_selectedLogoPath) && File.Exists(_selectedLogoPath))
            {
                var logoDirectory = Path.Combine(AppContext.BaseDirectory, "Data", "Store"); Directory.CreateDirectory(logoDirectory); var extension = Path.GetExtension(_selectedLogoPath); var target = Path.Combine(logoDirectory, "logo" + extension.ToLowerInvariant()); File.Copy(_selectedLogoPath, target, true); persistedLogo = Path.GetRelativePath(AppContext.BaseDirectory, target);
            }
            else persistedLogo = settings?.LogoPath;
            if (settings is null) { settings = new StoreSettings(StoreNameBox.Text, currency); settings.AssignBranch(branch.Id); settings.ConfigureIdentity(StoreNameBox.Text, currency, ArabicNameBox.Text); settings.ConfigureLogo(persistedLogo); db.StoreSettings.Add(settings); } else { settings.ConfigureIdentity(StoreNameBox.Text, currency, ArabicNameBox.Text); settings.ConfigureLogo(persistedLogo); }
            var tax = await db.TaxSettings.FirstOrDefaultAsync(); if (tax is null) { tax = new TaxSettings(); db.TaxSettings.Add(tax); } tax.Configure(true, rate, false, true);
            await db.SaveChangesAsync(); _localization.SetLanguage(LanguageBox.SelectedIndex == 1 ? AppLanguage.Arabic : AppLanguage.English); StatusText.Text = "Settings saved successfully.";
        }
        catch (Exception ex) { StatusText.Text = ex.InnerException?.Message ?? ex.Message; }
    }
    private sealed record CurrencyOption(string Code, string Display);
}
