using POS_WPF.Domain.Common;

namespace POS_WPF.Domain.Units;

public sealed class ProductUnit : Entity
{
    private ProductUnit()
    {
    }

    public ProductUnit(Guid productId, string unitName, string abbreviation, decimal conversionFactorToBase)
    {
        if (productId == Guid.Empty)
            throw new ArgumentException("Product is required.", nameof(productId));
        if (string.IsNullOrWhiteSpace(unitName))
            throw new ArgumentException("Unit name is required.", nameof(unitName));
        if (string.IsNullOrWhiteSpace(abbreviation))
            throw new ArgumentException("Unit abbreviation is required.", nameof(abbreviation));
        if (conversionFactorToBase <= 0)
            throw new ArgumentOutOfRangeException(nameof(conversionFactorToBase), "Conversion factor must be greater than zero.");

        ProductId = productId;
        UnitName = unitName.Trim();
        Abbreviation = abbreviation.Trim();
        ConversionFactorToBase = conversionFactorToBase;
    }

    public Guid ProductId { get; private set; }
    public string UnitName { get; private set; } = string.Empty;
    public string Abbreviation { get; private set; } = string.Empty;
    public decimal ConversionFactorToBase { get; private set; }
    public string? Barcode { get; private set; }
    public decimal SellingPrice { get; private set; }
    public decimal PurchasePrice { get; private set; }
    public bool CanSell { get; private set; } = true;
    public bool CanPurchase { get; private set; } = true;
    public bool IsBaseUnit { get; private set; }
    public bool IsActive { get; private set; } = true;

    public void SetBaseUnit(bool value) => IsBaseUnit = value;

    public void UpdateConversionFactor(decimal factor)
    {
        if (factor <= 0)
            throw new ArgumentOutOfRangeException(nameof(factor), "Conversion factor must be greater than zero.");

        ConversionFactorToBase = factor;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void UpdatePricing(decimal sellingPrice, decimal purchasePrice)
    {
        if (sellingPrice < 0 || purchasePrice < 0)
            throw new ArgumentOutOfRangeException(nameof(sellingPrice), "Prices cannot be negative.");

        SellingPrice = sellingPrice;
        PurchasePrice = purchasePrice;
        UpdatedAtUtc = DateTime.UtcNow;
    }

    public void SetBarcode(string? barcode)
    {
        Barcode = string.IsNullOrWhiteSpace(barcode) ? null : barcode.Trim();
        UpdatedAtUtc = DateTime.UtcNow;
    }
}
