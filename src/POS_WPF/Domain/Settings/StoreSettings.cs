using POS_WPF.Domain.Common;

namespace POS_WPF.Domain.Settings;

public sealed class StoreSettings : Entity
{
    public StoreSettings() { StoreName = "Retail POS"; CurrencyCode = "JOD"; }
    public StoreSettings(string storeName, string currencyCode) { StoreName = storeName.Trim(); CurrencyCode = currencyCode.Trim().ToUpperInvariant(); }
    public string StoreName { get; private set; } = string.Empty; public string? StoreNameArabic { get; private set; } public string? StoreNameEnglish { get; private set; } public string? LogoPath { get; private set; } public string? Address { get; private set; } public string? AddressArabic { get; private set; } public string? AddressEnglish { get; private set; } public string? Phone { get; private set; } public string? Mobile { get; private set; } public string? Email { get; private set; } public string? Website { get; private set; } public string? TaxNumber { get; private set; } public string? CommercialRegistrationNumber { get; private set; } public string CurrencyCode { get; private set; } = "JOD"; public string Country { get; private set; } = string.Empty; public string City { get; private set; } = string.Empty; public Guid? BranchId { get; private set; }
    public void AssignBranch(Guid branchId) { BranchId = branchId; UpdatedAtUtc = DateTime.UtcNow; }
    public void ConfigureIdentity(string name, string currencyCode, string? arabicName) { if (string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(currencyCode)) throw new ArgumentException("Store name and currency are required."); StoreName = name.Trim(); CurrencyCode = currencyCode.Trim().ToUpperInvariant(); StoreNameArabic = arabicName?.Trim(); UpdatedAtUtc = DateTime.UtcNow; }
    public void ConfigureLogo(string? logoPath) { LogoPath = string.IsNullOrWhiteSpace(logoPath) ? null : logoPath.Trim(); UpdatedAtUtc = DateTime.UtcNow; }
    public void ConfigureContact(string? address, string? country, string? city, string? phone, string? mobile, string? email, string? website) { Address = address?.Trim(); Country = country?.Trim() ?? string.Empty; City = city?.Trim() ?? string.Empty; Phone = phone?.Trim(); Mobile = mobile?.Trim(); Email = email?.Trim(); Website = website?.Trim(); UpdatedAtUtc = DateTime.UtcNow; }
}

public sealed class InvoiceSettings : Entity
{
    public InvoiceSettings() { }
    public string? HeaderText { get; private set; } public bool ShowProductName { get; private set; } = true; public bool ShowSku { get; private set; } = true; public bool ShowBarcode { get; private set; } public bool ShowUnit { get; private set; } = true; public bool ShowQuantity { get; private set; } = true; public bool ShowUnitPrice { get; private set; } = true; public bool ShowDiscount { get; private set; } = true; public bool ShowTax { get; private set; } = true; public bool ShowTotal { get; private set; } = true; public ReceiptPaperSize ThermalPaperSize { get; private set; } = ReceiptPaperSize.Millimeter80; public List<InvoiceFooterLine> FooterLines { get; private set; } = [];
}
public enum ReceiptPaperSize { Millimeter58, Millimeter80, A4 }
public sealed class InvoiceFooterLine { private InvoiceFooterLine() { } public InvoiceFooterLine(string text, int sortOrder) { Id = Guid.NewGuid(); Text = text.Trim(); SortOrder = sortOrder; } public Guid Id { get; private set; } public string Text { get; private set; } = string.Empty; public int SortOrder { get; private set; } public bool IsEnabled { get; private set; } = true; }
public sealed class TaxSettings : Entity { public TaxSettings() { } public bool IsEnabled { get; private set; } = true; public decimal Rate { get; private set; } public bool PricesIncludeTax { get; private set; } public bool ShowTaxOnInvoice { get; private set; } = true; public void Configure(bool enabled, decimal rate, bool pricesIncludeTax, bool showTaxOnInvoice) { if (rate < 0 || rate > 100) throw new ArgumentOutOfRangeException(nameof(rate)); IsEnabled = enabled; Rate = rate; PricesIncludeTax = pricesIncludeTax; ShowTaxOnInvoice = showTaxOnInvoice; UpdatedAtUtc = DateTime.UtcNow; } }
