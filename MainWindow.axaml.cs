using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using ProSeq.Services;
using ProSeq.ViewModels;
using ProSeq.Models;
using ProSeq.Views;

namespace ProSeq;

/// <summary>
/// Main application window: template selection, booking matrix, and time summary.
/// </summary>
/// <remarks>
/// Author: eGovernment · © 2026 Lenny Kunz
/// </remarks>
public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
        RestoreWindowState();
        Closing += OnClosing;
    }

    private void RestoreWindowState()
    {
        var state = WindowStateService.Load();

        if (state.Width >= 400) Width = state.Width;
        if (state.Height >= 300) Height = state.Height;

        if (!double.IsNaN(state.X) && !double.IsNaN(state.Y))
        {
            // Delay position until opened so screens are available
            Opened += (_, _) =>
            {
                try
                {
                    Position = new PixelPoint((int)state.X, (int)state.Y);
                    if (string.Equals(state.WindowState, "Maximized", StringComparison.OrdinalIgnoreCase))
                        WindowState = WindowState.Maximized;
                }
                catch
                {
                    // ignore invalid positions
                }
            };
        }
        else
        {
            WindowStartupLocation = WindowStartupLocation.CenterScreen;
        }
    }

    private void OnClosing(object? sender, WindowClosingEventArgs e)
    {
        try
        {
            var data = new WindowStateData
            {
                Width = Width,
                Height = Height,
                WindowState = WindowState.ToString()
            };

            // Prefer restored bounds when maximized so next open isn't huge
            if (WindowState == WindowState.Normal)
            {
                data.X = Position.X;
                data.Y = Position.Y;
                data.Width = Width;
                data.Height = Height;
            }
            else
            {
                // Keep last known normal size from file if we don't have restore bounds
                var prev = WindowStateService.Load();
                data.X = prev.X;
                data.Y = prev.Y;
                data.Width = prev.Width > 0 ? prev.Width : Width;
                data.Height = prev.Height > 0 ? prev.Height : Height;
                data.WindowState = WindowState.ToString();
            }

            WindowStateService.Save(data);
        }
        catch
        {
            // ignore
        }
    }

    private async void OnOpenTemplateDialog(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var dialog = new TemplateDialog(vm.ProcedureTemplates, vm.SelectedTemplate);
        var result = await dialog.ShowDialog<Models.Procedure?>(this);

        if (result != null)
            vm.SelectedTemplate = result;
    }

    private void OnRemovePause(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;
        if (sender is Button { Tag: PauseEntry pause })
        {
            vm.CustomPauses.Remove(pause);
            vm.AdjustTimesForPauses();
        }
    }

    private async void OnAddPause(object? sender, RoutedEventArgs e)
    {
        if (DataContext is not MainViewModel vm) return;

        var dialog = new PauseDialog(vm.CustomPauses);
        var result = await dialog.ShowDialog<PauseEntry?>(this);
        if (result != null)
        {
            vm.CustomPauses.Add(result);
            vm.AdjustTimesForPauses();
        }
    }
}
