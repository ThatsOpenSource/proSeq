using System;
using Avalonia;

namespace ProSeq;

/// <summary>
/// Application entry point for proSeq.
/// </summary>
/// <remarks>
/// Author: eGovernment · © 2026 Lenny Kunz
/// </remarks>
internal sealed class Program
{
    /// <summary>
    /// Starts the Avalonia desktop application.
    /// </summary>
    /// <param name="args">Command-line arguments.</param>
    [STAThread]
    public static void Main(string[] args)
    {
        BuildAvaloniaApp().StartWithClassicDesktopLifetime(args);
    }

    /// <summary>
    /// Configures the Avalonia <see cref="AppBuilder"/>.
    /// </summary>
    public static AppBuilder BuildAvaloniaApp()
        => AppBuilder.Configure<App>()
            .UsePlatformDetect()
            .WithInterFont()
            .LogToTrace();
}
