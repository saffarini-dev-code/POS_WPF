using System.Windows;
using System.Windows.Controls;
using Microsoft.EntityFrameworkCore;
using POS_WPF.Data;
using POS_WPF.Domain.Products;

namespace POS_WPF;

public partial class CategoryManagementWindow : Window
{
    private readonly IDbContextFactory<AppDbContext> _dbFactory; private Guid? _selectedId; private List<CategoryRow> _rows = [];
    public CategoryManagementWindow(IDbContextFactory<AppDbContext> dbFactory) { InitializeComponent(); _dbFactory = dbFactory; Loaded += async (_, _) => { await LoadAsync(); NewCategory(); }; }
    private async Task LoadAsync() { await using var db = await _dbFactory.CreateDbContextAsync(); _rows = await db.Categories.AsNoTracking().OrderBy(x => x.Name).Select(x => new CategoryRow(x.Id, x.Name, x.NameArabic, x.IsActive)).ToListAsync(); CategoriesGrid.ItemsSource = _rows; ParentBox.ItemsSource = _rows.Where(x => x.Id != _selectedId).ToList(); }
    private void Search_Changed(object sender, TextChangedEventArgs e) { var q = SearchBox.Text.Trim(); CategoriesGrid.ItemsSource = string.IsNullOrWhiteSpace(q) ? _rows : _rows.Where(x => x.Name.Contains(q, StringComparison.OrdinalIgnoreCase) || (x.NameArabic?.Contains(q, StringComparison.OrdinalIgnoreCase) ?? false)).ToList(); }
    private async void Refresh_Click(object sender, RoutedEventArgs e) => await LoadAsync();
    private void New_Click(object sender, RoutedEventArgs e) => NewCategory();
    private void NewCategory() { _selectedId = null; CategoriesGrid.SelectedItem = null; NameBox.Clear(); ArabicNameBox.Clear(); ParentBox.SelectedValue = null; StatusText.Text = "New category."; }
    private void Category_Selected(object sender, SelectionChangedEventArgs e) { if (CategoriesGrid.SelectedItem is not CategoryRow row) return; _selectedId = row.Id; NameBox.Text = row.Name; ArabicNameBox.Text = row.NameArabic ?? string.Empty; ParentBox.SelectedValue = null; StatusText.Text = row.IsActive ? "Active category." : "Inactive category."; }
    private async void Save_Click(object sender, RoutedEventArgs e)
    {
        try { if (string.IsNullOrWhiteSpace(NameBox.Text)) throw new InvalidOperationException("Category name is required."); await using var db = await _dbFactory.CreateDbContextAsync(); Category category; if (_selectedId is null) { category = new Category(NameBox.Text, ArabicNameBox.Text); if (ParentBox.SelectedValue is Guid parent) category.SetParent(parent); db.Categories.Add(category); } else { category = await db.Categories.SingleAsync(x => x.Id == _selectedId.Value); category.Rename(NameBox.Text, ArabicNameBox.Text); if (ParentBox.SelectedValue is Guid parent && parent != category.Id) category.SetParent(parent); else category.SetParent(null); } await db.SaveChangesAsync(); await LoadAsync(); StatusText.Text = "Category saved successfully."; }
        catch (Exception ex) { StatusText.Text = ex.InnerException?.Message ?? ex.Message; }
    }
    private async void Deactivate_Click(object sender, RoutedEventArgs e) { if (_selectedId is not Guid id) return; try { await using var db = await _dbFactory.CreateDbContextAsync(); var category = await db.Categories.SingleAsync(x => x.Id == id); category.SetActive(false); await db.SaveChangesAsync(); await LoadAsync(); StatusText.Text = "Category deactivated."; } catch (Exception ex) { StatusText.Text = ex.InnerException?.Message ?? ex.Message; } }
    private sealed record CategoryRow(Guid Id, string Name, string? NameArabic, bool IsActive) { public string Status => IsActive ? "Active" : "Inactive"; }
}