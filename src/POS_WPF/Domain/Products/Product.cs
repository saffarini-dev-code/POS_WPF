namespace POS_WPF.Domain.Products;

public sealed class Product
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public string Sku { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? NameArabic { get; set; }
    public Guid? CategoryId { get; set; }
    public bool IsActive { get; set; } = true;
    public Guid BaseUnitId { get; set; }
    public ICollection<ProductUnit> Units { get; set; } = new List<ProductUnit>();
}
