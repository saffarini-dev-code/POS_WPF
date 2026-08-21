using POS_WPF.Domain.Common;

namespace POS_WPF.Domain.Promotions;

public enum PromotionType { Percentage, FixedAmount, BuyXGetY, QuantityDiscount, Bundle, Coupon }

public sealed class Promotion : Entity
{
    private Promotion() { }
    public Promotion(string code, string name, PromotionType type, DateTime startsAtUtc, DateTime? endsAtUtc = null)
    {
        Code = code.Trim(); Name = name.Trim(); Type = type; StartsAtUtc = startsAtUtc; EndsAtUtc = endsAtUtc;
        if (EndsAtUtc.HasValue && EndsAtUtc < StartsAtUtc) throw new ArgumentException("Promotion end must be after its start.");
    }
    public string Code { get; private set; } = string.Empty;
    public string Name { get; private set; } = string.Empty;
    public PromotionType Type { get; private set; }
    public decimal Value { get; private set; }
    public decimal MinimumQuantity { get; private set; }
    public decimal RewardQuantity { get; private set; }
    public DateTime StartsAtUtc { get; private set; }
    public DateTime? EndsAtUtc { get; private set; }
    public bool IsActive { get; private set; } = true;
    public bool IsCurrentlyActive(DateTime utcNow) => IsActive && utcNow >= StartsAtUtc && (!EndsAtUtc.HasValue || utcNow <= EndsAtUtc.Value);
}
