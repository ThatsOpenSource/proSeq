namespace ProSeq;

/// <summary>
/// Static application metadata for proSeq.
/// </summary>
/// <remarks>
/// Author: eGovernment
/// Copyright: © 2026 Lenny Kunz
/// Product: proSeq – Buchungssequenzen
/// </remarks>
public static class AppInfo
{
    /// <summary>Display name of the application.</summary>
    public const string Name = "proSeq";

    /// <summary>Semantic version string.</summary>
    public const string Version = "1.0.0";

    /// <summary>Author / organization responsible for this tool.</summary>
    public const string Author = "eGovernment";

    /// <summary>Short product description (German UI).</summary>
    public const string Description = "Buchungssequenzen – Generator für Zeitwirtschaft";

    /// <summary>Copyright notice.</summary>
    public const string Copyright = "© 2026 Lenny Kunz";

    /// <summary>Company or publisher name.</summary>
    public const string Company = "eGovernment";

    /// <summary>Combined footer text for the main window.</summary>
    public static string FooterText => $"{Name} v{Version}  ·  {Author}  ·  {Copyright}";
}
