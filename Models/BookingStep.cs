namespace ProSeq.Models;

public class BookingStep
{
    public string Action { get; set; } = string.Empty;
    public string? TimeKey { get; set; }
    public string? FixedTime { get; set; }
    public string? Comment { get; set; }
    public string? Condition { get; set; }
}
