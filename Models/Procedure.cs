namespace ProSeq.Models;

public class Procedure
{
    public string Id { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public Dictionary<string, string> Conditions { get; set; } = new();
    public List<string> RequiredTimeFields { get; set; } = new();
    public List<BookingStep> Steps { get; set; } = new();
    public string? AttentionNote { get; set; }
    public bool RequiresBewegungsantrag { get; set; }
}
