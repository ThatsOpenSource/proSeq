using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using ProSeq.Models;

namespace ProSeq.Views;

public partial class TemplateDialog : Window
{
    public Procedure? SelectedProcedure { get; private set; }

    public TemplateDialog() : this(null, null) { }

    public TemplateDialog(IEnumerable<Procedure>? templates, Procedure? current)
    {
        InitializeComponent();

        var list = this.FindControl<ListBox>("TemplateList");
        if (list != null && templates != null)
        {
            list.ItemsSource = templates;
            if (current != null)
                list.SelectedItem = current;
            else if (list.ItemCount > 0)
                list.SelectedIndex = 0;
        }

        Opened += (_, _) =>
        {
            try
            {
                // ~50% of owner or primary screen working area
                PixelRect area;
                if (Owner is Window owner && owner.Screens?.ScreenFromVisual(owner) is { } s)
                    area = s.WorkingArea;
                else if (Screens?.Primary is { } primary)
                    area = primary.WorkingArea;
                else
                    return;

                var scale = Screens?.Primary?.Scaling ?? 1.0;
                Width = area.Width * 0.5 / scale;
                Height = area.Height * 0.5 / scale;
            }
            catch
            {
                // keep XAML defaults
            }
        };
    }

    private void InitializeComponent()
    {
        AvaloniaXamlLoader.Load(this);
    }

    private void OnConfirm(object? sender, RoutedEventArgs e)
    {
        var list = this.FindControl<ListBox>("TemplateList");
        SelectedProcedure = list?.SelectedItem as Procedure;
        Close(SelectedProcedure);
    }

    private void OnCancel(object? sender, RoutedEventArgs e)
    {
        SelectedProcedure = null;
        Close(null);
    }
}
