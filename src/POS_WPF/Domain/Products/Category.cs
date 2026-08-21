using POS_WPF.Domain.Common;

namespace POS_WPF.Domain.Products;

public sealed class Category : Entity
{
    private Category() { }
    public Category(string name, string? nameArabic = null)
    {
        if (string.IsNullOrWhiteSpace(name)) throw new ArgumentException("Category name is required.", nameof(name));
        Name = name.Trim(); NameArabic = nameArabic?.Trim();
    }
    public string Name { get; private set; } = string.Empty;
    public string? NameArabic { get; private set; }
    public Guid? ParentCategoryId { get; private set; }
    public bool IsActive { get; private set; } = true;
    public void SetParent(Guid? parentCategoryId) { ParentCategoryId = parentCategoryId; UpdatedAtUtc = DateTime.UtcNow; }
    public void SetActive(bool active) { IsActive = active; UpdatedAtUtc = DateTime.UtcNow; }
}
