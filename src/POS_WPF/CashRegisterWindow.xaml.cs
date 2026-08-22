using System.Globalization;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Finance;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class CashRegisterWindow : Window
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory; private readonly PermissionService _permissions; private Guid _registerId;
    public CashRegisterWindow(IDbContextFactory<AppDbContext> dbFactory, PermissionService permissions) { InitializeComponent(); _dbFactory = dbFactory; _permissions = permissions; Loaded += async (_, _) => await RefreshAsync(); }
    private async Task RefreshAsync()
    { await using var db = await _dbFactory.CreateDbContextAsync(); var register = await db.CashRegisters.OrderBy(x => x.Code).FirstOrDefaultAsync(); if (register is null) { StatusText.Text = "No cash register configured."; return; } _registerId = register.Id; StatusText.Text = register.IsOpen ? $"Open: {register.Name}" : $"Closed: {register.Name}"; ExpectedText.Text = (await new CashRegisterService(_dbFactory).GetExpectedCashAsync(register.Id)).ToString("N2", CultureInfo.CurrentCulture); }
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await RefreshAsync();
    private async void Open_Click(object sender, RoutedEventArgs e)
    {
        try { await _permissions.DemandAsync("CashRegister.Open"); if (!decimal.TryParse(OpeningBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var amount) || amount < 0) throw new InvalidOperationException("Enter a valid opening balance."); await using var db = await _dbFactory.CreateDbContextAsync(); var register = await db.CashRegisters.SingleAsync(x => x.Id == _registerId); register.Open(amount); db.CashMovements.Add(new Domain.Finance.CashMovement(register.Id, CashMovementType.Opening, amount, "REGISTER-OPEN")); await db.SaveChangesAsync(); await RefreshAsync(); } catch (Exception ex) { StatusText.Text = ex.Message; }
    }
    private async void Calculate_Click(object sender, RoutedEventArgs e)
    { if (decimal.TryParse(ActualBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var actual)) { var expected = decimal.Parse(ExpectedText.Text, NumberStyles.Number, CultureInfo.CurrentCulture); VarianceText.Text = (actual - expected).ToString("N2", CultureInfo.CurrentCulture); } }
    private async void Close_Click(object sender, RoutedEventArgs e)
    {
        try { await _permissions.DemandAsync("CashRegister.Close"); await using var db = await _dbFactory.CreateDbContextAsync(); var register = await db.CashRegisters.SingleAsync(x => x.Id == _registerId); if (!register.IsOpen) throw new InvalidOperationException("Register is already closed."); if (!decimal.TryParse(ActualBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var actual) || actual < 0) throw new InvalidOperationException("Enter actual cash before closing."); var expected = await new CashRegisterService(_dbFactory).GetExpectedCashAsync(register.Id); db.CashMovements.Add(new CashMovement(register.Id, CashMovementType.ClosingAdjustment, Math.Abs(actual - expected), "REGISTER-CLOSE")); register.Close(); await db.SaveChangesAsync(); await RefreshAsync(); } catch (Exception ex) { StatusText.Text = ex.Message; }
    }
}
