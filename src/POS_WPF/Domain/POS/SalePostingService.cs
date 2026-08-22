using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Audit;
using POS_WPF.Domain.Customers;
using POS_WPF.Domain.Finance;
using POS_WPF.Domain.Inventory;
using POS_WPF.Domain.Sales;
using POS_WPF.Domain.Settings;
using POS_WPF.Domain.Sync;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF.Domain.POS;

public sealed record SaleLineRequest(Guid ProductId, Guid UnitId, decimal Quantity, decimal UnitPrice, decimal Discount, decimal Tax);
public sealed record SalePaymentRequest(string Method, decimal Amount);
public sealed record SalePostingRequest(Guid BranchId, Guid WarehouseId, Guid TerminalId, Guid RegisterId, Guid CashierId, string Number, Guid? CustomerId, IReadOnlyList<SaleLineRequest> Lines, IReadOnlyList<SalePaymentRequest> Payments);
public sealed record SalePostingResult(Guid SaleId, decimal Total, decimal Change);

public sealed class SalePostingService(IDbContextFactory<AppDbContext> dbFactory, PermissionService permissions)
{
    public async Task<SalePostingResult> PostAsync(SalePostingRequest request, CancellationToken cancellationToken = default)
    {
        await permissions.DemandAsync("Sales.Create", cancellationToken);
        if (request.Lines.Count == 0) throw new InvalidOperationException("The sale must contain at least one line.");
        if (request.Payments.Count == 0) throw new InvalidOperationException("The sale must contain a payment.");

        await using var db = await dbFactory.CreateDbContextAsync(cancellationToken);
        await using var transaction = await db.Database.BeginTransactionAsync(cancellationToken);

        try
        {
            var taxSettings = await db.TaxSettings.AsNoTracking().FirstOrDefaultAsync(cancellationToken);
            var taxEnabled = taxSettings?.IsEnabled == true && taxSettings.Rate > 0m;

            var sale = new Sale(request.BranchId, request.TerminalId, request.CashierId, request.Number);
            sale.SetCustomer(request.CustomerId);

            foreach (var line in request.Lines)
            {
                var product = await db.Products
                    .Include(x => x.Units)
                    .SingleAsync(x => x.Id == line.ProductId && x.IsActive, cancellationToken);

                var unit = product.Units.Single(x => x.Id == line.UnitId && x.IsActive && x.CanSell);
                var available = await db.InventoryTransactions
                    .Where(x => x.ProductId == product.Id && x.WarehouseId == request.WarehouseId)
                    .SumAsync(x => (decimal?)x.BaseQuantity, cancellationToken) ?? 0m;
                var required = line.Quantity * unit.ConversionFactorToBase;

                if (required > available)
                    throw new InvalidOperationException($"Insufficient stock for {product.Name}.");

                // Tax is calculated once on the invoice taxable total, not per product line.
                var tax = 0m;

                sale.AddLine(
                    product.Id,
                    unit.Id,
                    product.Name,
                    line.Quantity,
                    unit.ConversionFactorToBase,
                    line.UnitPrice,
                    line.Discount,
                    tax);

                db.InventoryTransactions.Add(new InventoryTransaction
                {
                    ProductId = product.Id,
                    UnitId = unit.Id,
                    TransactionQuantity = line.Quantity,
                    ConversionFactor = unit.ConversionFactorToBase,
                    BaseQuantity = -required,
                    TransactionType = InventoryTransactionType.Sale,
                    Reference = request.Number,
                    WarehouseId = request.WarehouseId,
                    BranchId = request.BranchId,
                    UserId = request.CashierId
                });
            }

            var invoiceTaxableAmount = Math.Max(0m, sale.Subtotal - sale.Discount);
            var invoiceTax = taxEnabled
                ? CalculateTax(invoiceTaxableAmount, taxSettings!.Rate, taxSettings.PricesIncludeTax)
                : 0m;
            sale.SetTax(invoiceTax);

            foreach (var payment in request.Payments)
                sale.AddPayment(payment.Method, payment.Amount);

            sale.Complete();

            var creditAmount = request.Payments
                .Where(x => string.Equals(x.Method, "Credit", StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.Amount);

            if (creditAmount > 0)
            {
                if (!request.CustomerId.HasValue)
                    throw new InvalidOperationException("A customer is required for credit sales.");

                var customer = await db.Customers.SingleAsync(
                    x => x.Id == request.CustomerId.Value && x.IsActive,
                    cancellationToken);
                customer.ApplyBalance(creditAmount);
                db.AccountTransactions.Add(new AccountTransaction(
                    AccountPartyType.Customer,
                    customer.Id,
                    AccountTransactionType.Invoice,
                    creditAmount,
                    0m,
                    request.Number));
            }

            db.Sales.Add(sale);
            db.CashMovements.AddRange(
                request.Payments
                    .Where(x => string.Equals(x.Method, "Cash", StringComparison.OrdinalIgnoreCase))
                    .Select(x => new CashMovement(
                        request.RegisterId,
                        CashMovementType.Sale,
                        x.Amount,
                        request.Number,
                        request.CashierId)));

            db.AuditEntries.Add(new AuditEntry(
                request.CashierId,
                "Sale.Completed",
                nameof(Sale),
                sale.Id,
                null,
                $"Total={sale.Total};Tax={sale.Tax};Credit={creditAmount}",
                null,
                request.TerminalId,
                request.Number));

            db.SyncQueueEntries.Add(new SyncQueueEntry(nameof(Sale), sale.Id, "Create", request.Number));
            await db.SaveChangesAsync(cancellationToken);
            await transaction.CommitAsync(cancellationToken);

            var paid = request.Payments
                .Where(x => !string.Equals(x.Method, "Credit", StringComparison.OrdinalIgnoreCase))
                .Sum(x => x.Amount);

            return new SalePostingResult(
                sale.Id,
                sale.Total,
                Math.Max(0m, paid - (sale.Total - creditAmount)));
        }
        catch
        {
            await transaction.RollbackAsync(cancellationToken);
            throw;
        }
    }

    private static decimal CalculateTax(decimal taxableAmount, decimal rate, bool pricesIncludeTax)
    {
        if (taxableAmount <= 0m || rate <= 0m) return 0m;

        if (pricesIncludeTax)
        {
            return Math.Round(
                taxableAmount - taxableAmount / (1m + rate / 100m),
                2,
                MidpointRounding.AwayFromZero);
        }

        return Math.Round(
            taxableAmount * (rate / 100m),
            2,
            MidpointRounding.AwayFromZero);
    }
}
