using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Products;

namespace POS_WPF.Domain.POS;

public sealed record BarcodeMatch(Product Product, ProductUnit Unit);

public sealed class BarcodeLookupService(IDbContextFactory<AppDbContext> dbFactory)
{
    public async Task<BarcodeMatch?> FindAsync(string barcode, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(barcode)) return null;
        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        var unit = await db.ProductUnits.AsNoTracking()
            .Include(x => x.Product)
            .SingleOrDefaultAsync(x => x.IsActive && x.Product.IsActive && x.Barcode == barcode.Trim(), cancellationToken);
        return unit is null ? null : new BarcodeMatch(unit.Product, unit);
    }
}
