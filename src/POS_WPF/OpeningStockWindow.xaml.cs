using System.Globalization;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class OpeningStockWindow : Window
{
    private readonly OpeningStockService _openingStock; private readonly IDbContextFactory<AppDbContext> _dbFactory; private readonly SessionContext _session;
    public OpeningStockWindow(OpeningStockService openingStock, IDbContextFactory<AppDbContext> dbFactory, SessionContext session) { InitializeComponent(); _openingStock = openingStock; _dbFactory = dbFactory; _session = session; }
    private async void Post_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            await using var db = await _dbFactory.CreateDbContextAsync(); var branch = await db.Branches.OrderBy(x => x.Code).FirstAsync(); var warehouse = await db.Warehouses.Where(x => x.BranchId == branch.Id).OrderBy(x => x.Code).FirstAsync(); if (_session.CurrentUser is null) throw new InvalidOperationException("Session expired."); if (!Guid.TryParse(ProductIdBox.Text, out var productId) || !Guid.TryParse(UnitIdBox.Text, out var unitId) || !decimal.TryParse(QuantityBox.Text, NumberStyles.Number, CultureInfo.CurrentCulture, out var quantity) || quantity <= 0) throw new InvalidOperationException("Enter valid product, unit and quantity identifiers."); var id = await _openingStock.AddAsync(branch.Id, warehouse.Id, _session.CurrentUser.Id, productId, unitId, quantity, $"OPEN-{DateTime.UtcNow:yyyyMMddHHmmssfff}"); StatusText.Text = $"Opening stock posted: {id}";
        }
        catch (Exception ex) { StatusText.Text = ex.Message; }
    }
}
