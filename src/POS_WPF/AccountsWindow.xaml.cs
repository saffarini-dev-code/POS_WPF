using System.Globalization;
using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Customers;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class AccountsWindow : Window
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory; private readonly PermissionService _permissions; private readonly SessionContext _session; private Guid? _selectedId;
    public AccountsWindow(IDbContextFactory<AppDbContext> dbFactory, PermissionService permissions, SessionContext session) { InitializeComponent(); _dbFactory = dbFactory; _permissions = permissions; _session = session; PartyTypeBox.SelectedIndex = 0; Loaded += async (_, _) => await LoadAsync(); }
    private async void PartyType_Changed(object sender, SelectionChangedEventArgs e) { if (IsLoaded) await LoadAsync(); }
    private async Task LoadAsync() { await using var db = await _dbFactory.CreateDbContextAsync(); if (PartyTypeBox.SelectedIndex == 1) PartyGrid.ItemsSource = await db.Suppliers.AsNoTracking().OrderBy(x => x.Name).Select(x => new { x.Id, x.Code, x.Name, x.Phone, x.CurrentBalance }).ToListAsync(); else PartyGrid.ItemsSource = await db.Customers.AsNoTracking().OrderBy(x => x.Name).Select(x => new { x.Id, x.Code, x.Name, x.Phone, x.CreditLimit, x.CurrentBalance }).ToListAsync(); }
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
    private async void Party_Selected(object sender, SelectionChangedEventArgs e) { if (PartyGrid.SelectedItem is null) return; var value = PartyGrid.SelectedItem.GetType().GetProperty("Id")?.GetValue(PartyGrid.SelectedItem); _selectedId = value is Guid id ? id : null; if (_selectedId.HasValue) await LoadStatementAsync(); }
    private async Task LoadStatementAsync() { if (!_selectedId.HasValue) return; var service = new AccountStatementService(_dbFactory); var type = PartyTypeBox.SelectedIndex == 1 ? AccountPartyType.Supplier : AccountPartyType.Customer; StatementGrid.ItemsSource = await service.GetAsync(type, _selectedId.Value, DateTime.UtcNow.AddYears(-1), DateTime.UtcNow); }
    private async void Save_Click(object sender, RoutedEventArgs e)
    { try { if (string.IsNullOrWhiteSpace(CodeBox.Text) || string.IsNullOrWhiteSpace(NameBox.Text)) throw new InvalidOperationException("Code and name are required."); await _permissions.DemandAsync(PartyTypeBox.SelectedIndex == 1 ? "Suppliers.Create" : "Customers.Create"); await using var db = await _dbFactory.CreateDbContextAsync(); if (PartyTypeBox.SelectedIndex == 1) db.Suppliers.Add(new Supplier(CodeBox.Text, NameBox.Text)); else { var customer = new Customer(CodeBox.Text, NameBox.Text); if (decimal.TryParse(CreditBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var limit)) customer.SetCreditLimit(limit); db.Customers.Add(customer); } await db.SaveChangesAsync(); StatusText.Text = "Saved."; await LoadAsync(); } catch (Exception ex) { StatusText.Text = ex.Message; } }
    private async void Payment_Click(object sender, RoutedEventArgs e)
    { try { await _permissions.DemandAsync("Payments.Create"); if (!_selectedId.HasValue || _session.CurrentUser is null) throw new InvalidOperationException("Select an account first."); if (!decimal.TryParse(PaymentBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) || amount <= 0) throw new InvalidOperationException("Enter a valid payment."); var reference = $"PAY-{DateTime.UtcNow:yyyyMMddHHmmssfff}"; if (PartyTypeBox.SelectedIndex == 1) await new SupplierPaymentService(_dbFactory).PostPaymentAsync(_selectedId.Value, amount, _session.CurrentUser.Id, reference); else await new CustomerPaymentService(_dbFactory).PostPaymentAsync(_selectedId.Value, amount, _session.CurrentUser.Id, reference); PaymentBox.Clear(); await LoadAsync(); await LoadStatementAsync(); StatusText.Text = "Payment posted."; } catch (Exception ex) { StatusText.Text = ex.Message; } }
}
