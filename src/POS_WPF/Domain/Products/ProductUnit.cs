namespace POS_WPF.Domain.Products;

public sealed class ProductUnit
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ProductId { get; set; }
    public Product Product { get; set; } = null!;
    public string Name { get; set; } = string.Empty;
    public string Abbreviation { get; set; } = string.Empty;
    public string? Barcode { get; set; }
    public decimal ConversionFactorToBase { get; set; } = 1m;
    public decimal SellingPrice { get; set; }
    public decimal PurchasePrice { get; set; }
    public decimal WholesalePrice { get; set; }
    public decimal WholesaleWholesalePrice { get; set; }
    public bool IsBaseUnit { get; set; }
    public bool CanSell { get; set; } = true;
    public bool CanPurchase { get; set; } = true;
    public bool IsActive { get; set; } = true;
}
