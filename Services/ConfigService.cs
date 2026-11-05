using System.Text.Json;
using ProSeq.Models;

namespace ProSeq.Services;

public class ConfigService
{
    private static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = true
    };

    public ProcedureConfig Load()
    {
        var candidates = new[]
        {
            Path.Combine(AppContext.BaseDirectory, "Config", "procedures.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "Config", "procedures.json"),
            Path.Combine(AppContext.BaseDirectory, "procedures.json"),
            Path.Combine(Directory.GetCurrentDirectory(), "procedures.json"),
            // relative from source tree when running under debugger
            Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "..", "..", "..", "Config", "procedures.json")),
            Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "Config", "procedures.json"))
        };

        string? found = null;
        foreach (var path in candidates)
        {
            if (File.Exists(path))
            {
                found = path;
                break;
            }
        }

        if (found is null)
        {
            return new ProcedureConfig();
        }

        var json = File.ReadAllText(found);
        return JsonSerializer.Deserialize<ProcedureConfig>(json, Options)
               ?? new ProcedureConfig();
    }

    public void Save(ProcedureConfig config)
    {
        var path = Path.Combine(AppContext.BaseDirectory, "Config", "procedures.json");
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        var json = JsonSerializer.Serialize(config, Options);
        File.WriteAllText(path, json);
    }
}
