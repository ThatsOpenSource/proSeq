namespace ProSeq.Models;

public class ProcedureConfig
{
    public List<Procedure> Procedures { get; set; } = new();
    public List<string> AvailableActions { get; set; } = new();
    public Dictionary<string, string> TimeFieldLabels { get; set; } = new();
}
