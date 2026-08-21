using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Markup;
using POS_WPF.Infrastructure.Security;

namespace POS_WPF;

public partial class PosWindow
{
    protected override void OnInitialized(EventArgs e)
    {
        base.OnInitialized(e);
        ApplyCashierLayoutPolish();
    }

    private void ApplyCashierLayoutPolish()
    {
        if (Content is not Grid root) return;

        // Keep the cashier screen proportional: product area gets the flexible space,
        // receipt and cart keep stable widths similar to the approved POS reference.
        var mainGrid = root.Children.OfType<Grid>()
            .FirstOrDefault(x => x.RowDefinitions.Count == 0 && x.ColumnDefinitions.Count == 3);
        if (mainGrid is not null)
        {
            mainGrid.ColumnDefinitions[0].Width = new GridLength(1, GridUnitType.Star);
            mainGrid.ColumnDefinitions[0].MinWidth = 420;
            mainGrid.ColumnDefinitions[1].Width = new GridLength(270);
            mainGrid.ColumnDefinitions[1].MinWidth = 250;
            mainGrid.ColumnDefinitions[1].MaxWidth = 300;
            mainGrid.ColumnDefinitions[2].Width = new GridLength(390);
            mainGrid.ColumnDefinitions[2].MinWidth = 360;
            mainGrid.ColumnDefinitions[2].MaxWidth = 430;
        }

        ApplyRoundedBorders(root);
        ApplyRoundedButtons(root);
        AddProtectedCloseButton(root);
    }

    private static void ApplyRoundedBorders(DependencyObject parent)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
        {
            if (child is Border border && border.BorderThickness != new Thickness(0))
                border.CornerRadius = new CornerRadius(5);
            ApplyRoundedBorders(child);
        }
    }

    private static void ApplyRoundedButtons(DependencyObject parent)
    {
        foreach (var child in LogicalTreeHelper.GetChildren(parent).OfType<DependencyObject>())
        {
            if (child is Button button && button.Template is null)
                button.Template = CreateRoundedButtonTemplate();
            else if (child is Button button2 && button2.Template is not null)
                button2.Template = CreateRoundedButtonTemplate();

            ApplyRoundedButtons(child);
        }
    }

    private static ControlTemplate CreateRoundedButtonTemplate()
    {
        const string template = @"
<ControlTemplate xmlns='http://schemas.microsoft.com/winfx/2006/xaml/presentation' TargetType='{x:Type Button}'>
  <Border Background='{TemplateBinding Background}' BorderBrush='{TemplateBinding BorderBrush}' BorderThickness='{TemplateBinding BorderThickness}' CornerRadius='5' Padding='{TemplateBinding Padding}'>
    <ContentPresenter HorizontalAlignment='{TemplateBinding HorizontalContentAlignment}' VerticalAlignment='{TemplateBinding VerticalContentAlignment}' Content='{TemplateBinding Content}' ContentTemplate='{TemplateBinding ContentTemplate}' />
  </Border>
</ControlTemplate>";
        return (ControlTemplate)XamlReader.Parse(template);
    }

    private void AddProtectedCloseButton(Grid root)
    {
        if (root.Children.OfType<Button>().Any(x => Equals(x.Tag, "ProtectedPosClose"))) return;

        var close = new Button
        {
            Tag = "ProtectedPosClose",
            Content = "⏻  Close POS",
            Width = 118,
            Height = 26,
            Margin = new Thickness(10, 3, 0, 3),
            Padding = new Thickness(10, 3, 10, 3),
            HorizontalAlignment = HorizontalAlignment.Left,
            VerticalAlignment = VerticalAlignment.Center,
            Background = new SolidColorBrush(Color.FromRgb(248, 250, 252)),
            BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
            BorderThickness = new Thickness(1),
            Foreground = new SolidColorBrush(Color.FromRgb(71, 85, 105)),
            FontSize = 10,
            FontWeight = FontWeights.SemiBold,
            Cursor = System.Windows.Input.Cursors.Hand,
            ToolTip = "Close the cashier screen. Password required."
        };
        close.Click += ProtectedClose_Click;
        Grid.SetRow(close, 2);
        Panel.SetZIndex(close, 1000);
        root.Children.Add(close);
    }

    private void ProtectedClose_Click(object sender, RoutedEventArgs e)
    {
        if (_session.CurrentUser is null)
        {
            MessageBox.Show("The authenticated session has expired.", "Close POS", MessageBoxButton.OK, MessageBoxImage.Warning);
            return;
        }

        var dialog = new Window
        {
            Title = "Close POS — Authorization",
            Width = 390,
            Height = 245,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Owner = this,
            ResizeMode = ResizeMode.NoResize,
            WindowStyle = WindowStyle.ToolWindow,
            ShowInTaskbar = false,
            Background = new SolidColorBrush(Color.FromRgb(246, 248, 251))
        };

        var panel = new Grid { Margin = new Thickness(22) };
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
        panel.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
        panel.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });

        panel.Children.Add(new TextBlock
        {
            Text = "Close Cashier",
            FontSize = 20,
            FontWeight = FontWeights.Bold,
            Foreground = new SolidColorBrush(Color.FromRgb(15, 23, 42))
        });

        var hint = new TextBlock
        {
            Text = "Enter your current password to close the POS screen.",
            Margin = new Thickness(0, 5, 0, 16),
            FontSize = 11,
            Foreground = new SolidColorBrush(Color.FromRgb(100, 116, 139))
        };
        Grid.SetRow(hint, 1);
        panel.Children.Add(hint);

        var password = new PasswordBox
        {
            Height = 36,
            Padding = new Thickness(10, 7, 10, 7),
            FontSize = 13,
            BorderThickness = new Thickness(1),
            BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225))
        };
        Grid.SetRow(password, 2);
        panel.Children.Add(password);

        var error = new TextBlock
        {
            Foreground = new SolidColorBrush(Color.FromRgb(185, 28, 28)),
            FontSize = 10,
            Margin = new Thickness(0, 7, 0, 0)
        };
        Grid.SetRow(error, 3);
        panel.Children.Add(error);

        var buttons = new StackPanel
        {
            Orientation = Orientation.Horizontal,
            HorizontalAlignment = HorizontalAlignment.Right
        };
        var cancel = new Button
        {
            Content = "Cancel",
            Width = 82,
            Height = 34,
            Margin = new Thickness(0, 0, 8, 0),
            Background = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(203, 213, 225)),
            BorderThickness = new Thickness(1)
        };
        cancel.Click += (_, _) => dialog.DialogResult = false;

        var confirm = new Button
        {
            Content = "Close POS",
            Width = 100,
            Height = 34,
            Background = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
            Foreground = Brushes.White,
            BorderBrush = new SolidColorBrush(Color.FromRgb(220, 38, 38)),
            BorderThickness = new Thickness(1),
            FontWeight = FontWeights.SemiBold
        };
        confirm.Click += (_, _) =>
        {
            var user = _session.CurrentUser;
            var valid = user is not null && new PasswordHasher().Verify(password.Password, user.PasswordHash);
            if (!valid)
            {
                error.Text = "Incorrect password.";
                password.Clear();
                password.Focus();
                return;
            }
            dialog.DialogResult = true;
        };

        buttons.Children.Add(cancel);
        buttons.Children.Add(confirm);
        Grid.SetRow(buttons, 4);
        panel.Children.Add(buttons);

        dialog.Content = panel;
        dialog.Loaded += (_, _) => password.Focus();
        dialog.ShowDialog();
        if (dialog.DialogResult == true)
            Close();
    }
}
