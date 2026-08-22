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
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public PromotionType Type { get; set; }
    public Guid? ProductId { get; set; }
    public Guid? UnitId { get; set; }
    public decimal Value { get; set; }
    public decimal MinimumQuantity { get; set; }
    public decimal RewardQuantity { get; set; }
    public decimal? MaxQuantity { get; set; }
    public decimal ConsumedQuantity { get; set; }
    public DateTime StartsAtUtc { get; set; }
    public DateTime? EndsAtUtc { get; set; }
    public bool IsActive { get; set; } = true;
    public bool IsCurrentlyActive(DateTime utcNow) => IsActive && utcNow >= StartsAtUtc && (!EndsAtUtc.HasValue || utcNow <= EndsAtUtc.Value) && (!MaxQuantity.HasValue || ConsumedQuantity < MaxQuantity.Value);
}
