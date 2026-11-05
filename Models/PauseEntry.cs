namespace ProSeq.Models;

/// <summary>User-defined pause interval (excluded from working time).</summary>
public class PauseEntry
{
    public string Start { get; set; } = "12:00";
    public string End { get; set; } = "12:30";
    public string Display => $"{Start} – {End}";
}
