using POS_WPF.Domain.Common;
using POS_WPF.Domain.Units;

namespace POS_WPF.Domain.Catalog;

public sealed class Product : Entity
{
    private readonly List<ProductUnit> _units = [];

    private Product()
    {
    }

    public Product(string sku, string name)
    {
        if (string.IsNullOrWhiteSpace(sku))
            throw new ArgumentException("SKU is required.", nameof(sku));
        if (string.IsNullOrWhiteSpace(name))
            throw new ArgumentException("Product name is required.", nameof(name));

        Sku = sku.Trim();
        Name = name.Trim();
    }

    public string Sku { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public string? ArabicName { get; private set; }
    public bool IsActive { get; private set; } = true;

    public IReadOnlyCollection<ProductUnit> Units => _units;

    public ProductUnit SetBaseUnit(ProductUnit unit)
    {
        if (unit.ProductId != Id)
            throw new InvalidOperationException("The unit belongs to another product.");

        foreach (var existing in _units)
            existing.SetBaseUnit(false);

        unit.SetBaseUnit(true);
        return unit;
    }

    public void AddUnit(ProductUnit unit)
    {
        if (unit.ProductId != Id)
            throw new InvalidOperationException("The unit belongs to another product.");

        if (_units.Any(x => string.Equals(x.Abbreviation, unit.Abbreviation, StringComparison.OrdinalIgnoreCase)))
            throw new InvalidOperationException("A product cannot have duplicate unit abbreviations.");

        if (unit.IsBaseUnit && _units.Any(x => x.IsBaseUnit))
            throw new InvalidOperationException("A product must have exactly one base unit.");

        _units.Add(unit);
    }
}
