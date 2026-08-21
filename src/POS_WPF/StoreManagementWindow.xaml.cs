using System.Windows;
using System.Windows.Media.Imaging;
using Microsoft.EntityFrameworkCore;
using Microsoft.Win32;
using POS_WPF.Data;
using POS_WPF.Domain.Settings;

namespace POS_WPF;

public partial class StoreManagementWindow : Window
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory; private Guid? _settingsId; private string? _logoPath;
    private static readonly CurrencyOption[] Currencies = [new("JOD","JOD — Jordanian Dinar"),new("USD","USD — US Dollar"),new("EUR","EUR — Euro"),new("GBP","GBP — British Pound"),new("SAR","SAR — Saudi Riyal"),new("AED","AED — UAE Dirham"),new("ILS","ILS — Israeli Shekel"),new("EGP","EGP — Egyptian Pound")];
    public StoreManagementWindow(IDbContextFactory<AppDbContext> dbFactory) { InitializeComponent(); _dbFactory = dbFactory; CurrencyBox.ItemsSource = Currencies; Loaded += async (_, _) => await LoadAsync(); }
    private async Task LoadAsync()
    {
        await using var db = await _dbFactory.CreateDbContextAsync(); var branch = await db.Branches.Where(x => x.IsActive).OrderBy(x => x.Code).FirstOrDefaultAsync(); BranchText.Text = branch is null ? "Not configured" : $"{branch.Code} — {branch.Name}"; var settings = branch is null ? await db.StoreSettings.FirstOrDefaultAsync() : await db.StoreSettings.FirstOrDefaultAsync(x => x.BranchId == branch.Id); if (settings is null) return; _settingsId = settings.Id; StoreNameBox.Text = settings.StoreName; ArabicNameBox.Text = settings.StoreNameArabic ?? string.Empty; CurrencyBox.SelectedValue = settings.CurrencyCode; CountryBox.Text = settings.Country; CityBox.Text = settings.City; PhoneBox.Text = settings.Phone; MobileBox.Text = settings.Mobile; EmailBox.Text = settings.Email; WebsiteBox.Text = settings.Website; AddressBox.Text = settings.Address; _logoPath = settings.LogoPath; LoadLogo();
    }
    private void Logo_Click(object sender, RoutedEventArgs e) { var dialog = new OpenFileDialog { Filter = "Image files|*.png;*.jpg;*.jpeg;*.bmp;*.webp" }; if (dialog.ShowDialog() != true) return; _logoPath = dialog.FileName; LoadLogo(); }
    private void LoadLogo() { LogoPathText.Text = string.IsNullOrWhiteSpace(_logoPath) ? "No logo selected" : _logoPath; if (!string.IsNullOrWhiteSpace(_logoPath) && System.IO.File.Exists(_logoPath)) { try { LogoImage.Source = new BitmapImage(new Uri(_logoPath, UriKind.Absolute)); } catch { LogoImage.Source = null; } } }
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try { if (string.IsNullOrWhiteSpace(StoreNameBox.Text)) throw new InvalidOperationException("Store name is required."); if (CurrencyBox.SelectedValue is not string currency) throw new InvalidOperationException("Select a currency."); await using var db = await _dbFactory.CreateDbContextAsync(); var branch = await db.Branches.Where(x => x.IsActive).OrderBy(x => x.Code).FirstOrDefaultAsync() ?? throw new InvalidOperationException("No active branch is configured."); var settings = await db.StoreSettings.FirstOrDefaultAsync(x => x.BranchId == branch.Id); if (settings is null) { settings = new StoreSettings(StoreNameBox.Text, currency); settings.AssignBranch(branch.Id); db.StoreSettings.Add(settings); } settings.ConfigureIdentity(StoreNameBox.Text, currency, ArabicNameBox.Text); settings.ConfigureLogo(_logoPath); typeof(StoreSettings).GetProperty(nameof(StoreSettings.Country))?.SetValue(settings, CountryBox.Text.Trim()); typeof(StoreSettings).GetProperty(nameof(StoreSettings.City))?.SetValue(settings, CityBox.Text.Trim()); typeof(StoreSettings).GetProperty(nameof(StoreSettings.Phone))?.SetValue(settings, PhoneBox.Text.Trim()); typeof(StoreSettings).GetProperty(nameof(StoreSettings.Mobile))?.SetValue(settings, MobileBox.Text.Trim()); typeof(StoreSettings).GetProperty(nameof(StoreSettings.Email))?.SetValue(settings, EmailBox.Text.Trim()); typeof(StoreSettings).GetProperty(nameof(StoreSettings.Website))?.SetValue(settings, WebsiteBox.Text.Trim()); typeof(StoreSettings).GetProperty(nameof(StoreSettings.Address))?.SetValue(settings, AddressBox.Text.Trim()); await db.SaveChangesAsync(); StatusText.Text = "Store settings saved successfully."; } catch (Exception ex) { StatusText.Text = ex.InnerException?.Message ?? ex.Message; }
    }
    private sealed record CurrencyOption(string Code, string Display);
}