using POS_WPF.Domain.Common;

namespace POS_WPF.Domain.Sales;

public sealed class HeldSale : Entity
{
    private HeldSale() { }
    public HeldSale(string reference, Guid cashierId, string payload)
    {
        Reference = reference.Trim();
        CashierId = cashierId;
        Payload = payload;
    }
    public string Reference { get; set; } = string.Empty;
    public Guid CashierId { get; set; }
    public string Payload { get; set; } = string.Empty;
    public bool IsReleased { get; set; }
}
