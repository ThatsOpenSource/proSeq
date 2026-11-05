using System.Text.Json;

namespace ProSeq.Services;

public class WindowStateData
{
    public double X { get; set; } = double.NaN;
    public double Y { get; set; } = double.NaN;
    public double Width { get; set; } = 1000;
    public double Height { get; set; } = 820;
    public string? WindowState { get; set; } // Normal, Maximized
}

public static class WindowStateService
{
    private static readonly JsonSerializerOptions JsonOptions = new() { WriteIndented = true };

    private static string GetPath()
    {
        var dir = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "proSeq");
        Directory.CreateDirectory(dir);
        return Path.Combine(dir, "window-state.json");
    }

    public static WindowStateData Load()
    {
        try
        {
            var path = GetPath();
            if (!File.Exists(path)) return new WindowStateData();
            var json = File.ReadAllText(path);
            return JsonSerializer.Deserialize<WindowStateData>(json, JsonOptions) ?? new WindowStateData();
        }
        catch
        {
            return new WindowStateData();
        }
    }

    public static void Save(WindowStateData data)
    {
        try
        {
            var path = GetPath();
            File.WriteAllText(path, JsonSerializer.Serialize(data, JsonOptions));
        }
        catch
        {
            // ignore persistence errors
        }
    }
}
