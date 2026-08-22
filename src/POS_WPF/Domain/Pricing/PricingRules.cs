namespace POS_WPF.Domain.Pricing;

public enum DiscountType { FixedAmount, Percentage }

public sealed record DiscountRule(DiscountType Type, decimal Value)
{
    public decimal Calculate(decimal subtotal)
    {
        if (subtotal < 0 || Value < 0) throw new ArgumentOutOfRangeException(nameof(subtotal));
        var discount = Type == DiscountType.Percentage ? subtotal * Value / 100m : Value;
        return Math.Min(subtotal, Math.Max(0m, discount));
    }
}

public sealed record TaxRule(string Code, decimal Rate, bool Inclusive = false)
{
    public decimal Calculate(decimal amount)
    {
        if (amount < 0 || Rate < 0) throw new ArgumentOutOfRangeException(nameof(amount));
        return Inclusive ? amount - amount / (1m + Rate / 100m) : amount * Rate / 100m;
    }
}

public sealed class PricingCalculator
{
    public decimal CalculateTotal(decimal subtotal, DiscountRule? discount = null, TaxRule? tax = null)
    {
        var discountAmount = discount?.Calculate(subtotal) ?? 0m;
        var net = subtotal - discountAmount;
        var taxAmount = tax?.Calculate(net) ?? 0m;
        return Math.Round(net + taxAmount, 2, MidpointRounding.AwayFromZero);
    }
}
