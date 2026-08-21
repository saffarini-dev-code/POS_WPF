namespace POS_WPF.Domain.Products;

public sealed class UnitConversionService
{
    public decimal ToBaseQuantity(decimal transactionQuantity, ProductUnit unit)
    {
        if (transactionQuantity <= 0)
            throw new ArgumentOutOfRangeException(nameof(transactionQuantity), "Quantity must be greater than zero.");

        if (unit.ConversionFactorToBase <= 0)
            throw new InvalidOperationException("Conversion factor must be greater than zero.");

        return transactionQuantity * unit.ConversionFactorToBase;
    }

    public decimal FromBaseQuantity(decimal baseQuantity, ProductUnit unit)
    {
        if (baseQuantity < 0)
            throw new ArgumentOutOfRangeException(nameof(baseQuantity));

        if (unit.ConversionFactorToBase <= 0)
            throw new InvalidOperationException("Conversion factor must be greater than zero.");

        return baseQuantity / unit.ConversionFactorToBase;
    }

    public void ValidateProductUnits(Product product)
    {
        var units = product.Units.Where(x => x.IsActive).ToList();
        var baseUnits = units.Where(x => x.IsBaseUnit).ToList();

        if (baseUnits.Count != 1)
            throw new InvalidOperationException("A product must have exactly one active base unit.");

        if (baseUnits[0].ConversionFactorToBase != 1m)
            throw new InvalidOperationException("The base unit conversion factor must be 1.");

        if (units.Any(x => x.ConversionFactorToBase <= 0))
            throw new InvalidOperationException("Conversion factors must be greater than zero.");

        var duplicateBarcodes = units
            .Where(x => !string.IsNullOrWhiteSpace(x.Barcode))
            .GroupBy(x => x.Barcode, StringComparer.OrdinalIgnoreCase)
            .Any(g => g.Count() > 1);

        if (duplicateBarcodes)
            throw new InvalidOperationException("A product cannot have duplicate unit barcodes.");
    }
}
