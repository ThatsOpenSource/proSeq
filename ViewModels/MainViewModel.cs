using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ProSeq.Models;
using ProSeq.Services;

namespace ProSeq.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ConfigService _configService;
    private readonly MatrixEngine _engine;
    private readonly ProcedureConfig _config;

    public ObservableCollection<string> OrtBeginnOptions { get; } = new() { "Dienststätte", "Wohnort", "Einsatzort", "Fortbildungsort" };
    public ObservableCollection<string> DienstgangOptions { get; } = new() { "Ja", "Nein" };
    public ObservableCollection<string> DienstgangOrtOptions { get; } = new() { "Gleicher Ort (wie Dienststätte)", "Anderer Ort" };
    public ObservableCollection<string> DienstgangEndeTypOptions { get; } = new() { "Rückkehr + Weiterarbeit", "Ende außerhalb" };
    public ObservableCollection<string> OrtEndeOptions { get; } = new() { "Dienststätte", "Wohnort" };
    public ObservableCollection<string> MitarbeiterTypOptions { get; } = new() { "Angestellte", "Beamte" };
    public ObservableCollection<string> ZusatzMobilOptions { get; } = new() { "Nein", "Ja" };
    public ObservableCollection<string> DienstreiseOptions { get; } = new() { "Nein", "Ja" };

    public ObservableCollection<TimeInputItem> TimeInputs { get; } = new();
    public ObservableCollection<BookingResultItem> BookingResults { get; } = new();
    public ObservableCollection<Procedure> ProcedureTemplates { get; } = new();
    public ObservableCollection<PauseEntry> CustomPauses { get; } = new();

    private bool _suppressHandlers;

    public MainViewModel()
    {
        try
        {
            _configService = new ConfigService();
            _config = _configService.Load();
            _engine = new MatrixEngine(_config.Procedures ?? new List<Procedure>());
        }
        catch (Exception ex)
        {
            Console.ForegroundColor = ConsoleColor.Red;
            Console.WriteLine("[VM] Fehler beim Laden der Konfiguration:");
            Console.WriteLine(ex);
            Console.ResetColor();

            _configService = new ConfigService();
            _config = new ProcedureConfig();
            _engine = new MatrixEngine(Array.Empty<Procedure>());
        }

        foreach (var p in _config.Procedures ?? new List<Procedure>())
            ProcedureTemplates.Add(p);

        SollzeitText = "07:48";

        _suppressHandlers = true;
        SelectedOrtBeginn = "Dienststätte";
        SelectedDienstgang = "Nein";
        SelectedDienstgangOrt = "Gleicher Ort (wie Dienststätte)";
        SelectedDienstgangEndeTyp = "Rückkehr + Weiterarbeit";
        SelectedOrtEnde = "Dienststätte";
        SelectedMitarbeiterTyp = "Angestellte";
        SelectedZusatzMobil = "Nein";
        SelectedDienstreise = "Nein";
        _suppressHandlers = false;

        // Default: first template
        if (ProcedureTemplates.Count > 0)
            SelectedTemplate = ProcedureTemplates[0];
        else
            UpdateFromDropdowns();
    }

    // Template selection (primary UX)
    [ObservableProperty] private Procedure? selectedTemplate;

    // Selections (synced from template or manual)
    [ObservableProperty] private string? selectedOrtBeginn;
    [ObservableProperty] private string? selectedDienstgang;
    [ObservableProperty] private string? selectedDienstgangOrt;
    [ObservableProperty] private string? selectedDienstgangEndeTyp;
    [ObservableProperty] private string? selectedOrtEnde;
    [ObservableProperty] private string? selectedMitarbeiterTyp;
    [ObservableProperty] private string? selectedZusatzMobil;
    [ObservableProperty] private string? selectedDienstreise;

    // Visibility
    [ObservableProperty] private bool showDienstgangDetails;
    [ObservableProperty] private bool showDienstgangOrt;
    [ObservableProperty] private bool showOrtEnde;
    [ObservableProperty] private bool showMitarbeiterTyp;
    [ObservableProperty] private bool showZusatzMobil;
    [ObservableProperty] private bool showDienstreise;
    [ObservableProperty] private bool showFortbildungHint;

    [ObservableProperty] private Procedure? selectedProcedure;
    [ObservableProperty] private string? attentionNote;
    [ObservableProperty] private string statusMessage = "Vorlage wählen oder manuell konfigurieren.";

    // Sollzeit + Summary
    [ObservableProperty] private string sollzeitText = "07:48";
    [ObservableProperty] private string summaryGearbeitet = "—";
    [ObservableProperty] private string summarySollzeit = "07:48";
    [ObservableProperty] private string summaryDifferenz = "—";
    [ObservableProperty] private string summaryPause = "—";
    [ObservableProperty] private string summarySollEnde = "—";
    [ObservableProperty] private string summaryMindestpauseAbzug = "—";
    [ObservableProperty] private bool showMindestpauseAbzug;
    [ObservableProperty] private string summaryWarning = "";
    [ObservableProperty] private bool showSummaryWarning;

    // ── Template selected → apply conditions + times ─────────────────

    partial void OnSelectedTemplateChanged(Procedure? value)
    {
        if (value is null || _suppressHandlers) return;
        ApplyTemplate(value);
    }

    private void ApplyTemplate(Procedure procedure)
    {
        _suppressHandlers = true;
        try
        {
            var c = procedure.Conditions;

            if (c.TryGetValue("OrtBeginn", out var v)) SelectedOrtBeginn = v;
            if (c.TryGetValue("Dienstgang", out v)) SelectedDienstgang = v;
            if (c.TryGetValue("DienstgangOrt", out v)) SelectedDienstgangOrt = v;
            if (c.TryGetValue("DienstgangEndeTyp", out v)) SelectedDienstgangEndeTyp = v;
            if (c.TryGetValue("OrtEnde", out v)) SelectedOrtEnde = v;
            if (c.TryGetValue("MitarbeiterTyp", out v)) SelectedMitarbeiterTyp = v;
            if (c.TryGetValue("ZusatzMobil", out v)) SelectedZusatzMobil = v;
            if (c.TryGetValue("Dienstreise", out v)) SelectedDienstreise = v;

            // Defaults for keys not in this procedure
            if (!c.ContainsKey("DienstgangOrt")) SelectedDienstgangOrt = "Gleicher Ort (wie Dienststätte)";
            if (!c.ContainsKey("DienstgangEndeTyp")) SelectedDienstgangEndeTyp = "Rückkehr + Weiterarbeit";
            if (!c.ContainsKey("MitarbeiterTyp")) SelectedMitarbeiterTyp = "Angestellte";
            if (!c.ContainsKey("ZusatzMobil")) SelectedZusatzMobil = "Nein";
            if (!c.ContainsKey("Dienstreise")) SelectedDienstreise = "Nein";
            if (!c.ContainsKey("OrtEnde"))
            {
                SelectedOrtEnde = SelectedOrtBeginn is "Wohnort" or "Fortbildungsort"
                    ? "Wohnort" : "Dienststätte";
            }

            SelectedProcedure = procedure;
            AttentionNote = procedure.AttentionNote;
            StatusMessage = $"Vorlage: {procedure.Name}";

            // No default/fixed pauses – user defines every pause explicitly
            CustomPauses.Clear();

            UpdateVisibility();
            UpdateTimeFields();
        }
        finally
        {
            _suppressHandlers = false;
        }
    }

    // ── Manual dropdown changes ──────────────────────────────────────

    partial void OnSelectedOrtBeginnChanged(string? value)
    {
        if (_suppressHandlers) return;
        if (value is "Wohnort" or "Fortbildungsort")
            SelectedOrtEnde = "Wohnort";
        else if (value is "Dienststätte" or "Einsatzort")
            SelectedOrtEnde = "Dienststätte";
        UpdateFromDropdowns();
    }

    partial void OnSelectedDienstgangChanged(string? value) { if (!_suppressHandlers) UpdateFromDropdowns(); }
    partial void OnSelectedDienstgangOrtChanged(string? value) { if (!_suppressHandlers) UpdateFromDropdowns(); }
    partial void OnSelectedDienstgangEndeTypChanged(string? value) { if (!_suppressHandlers) UpdateFromDropdowns(); }
    partial void OnSelectedOrtEndeChanged(string? value) { if (!_suppressHandlers) UpdateFromDropdowns(); }
    partial void OnSelectedMitarbeiterTypChanged(string? value) { if (!_suppressHandlers) UpdateFromDropdowns(); }
    partial void OnSelectedZusatzMobilChanged(string? value) { if (!_suppressHandlers) UpdateFromDropdowns(); }
    partial void OnSelectedDienstreiseChanged(string? value) { if (!_suppressHandlers) UpdateFromDropdowns(); }

    private void UpdateFromDropdowns()
    {
        UpdateVisibility();

        var selections = BuildSelections();
        var match = _engine.FindBestMatch(selections);

        // Clear template highlight if user diverged manually
        if (SelectedTemplate != null && match?.Id != SelectedTemplate.Id)
        {
            _suppressHandlers = true;
            SelectedTemplate = match; // may be null
            _suppressHandlers = false;
        }

        SelectedProcedure = match;
        AttentionNote = match?.AttentionNote;
        StatusMessage = match != null
            ? $"Gefunden: {match.Name}"
            : "Keine passende Prozedur für diese Kombination.";

        UpdateTimeFields();
    }

    private void UpdateVisibility()
    {
        ShowDienstgangDetails = SelectedDienstgang == "Ja";
        ShowDienstgangOrt = SelectedDienstgang == "Ja" && SelectedOrtBeginn == "Dienststätte";
        ShowOrtEnde = true;
        ShowMitarbeiterTyp = SelectedOrtBeginn == "Einsatzort"
            || (SelectedOrtBeginn == "Wohnort" && SelectedDienstgang == "Ja");
        ShowZusatzMobil = SelectedOrtBeginn == "Wohnort" && SelectedDienstgang == "Ja";
        ShowDienstreise = SelectedOrtBeginn == "Dienststätte"
            && SelectedDienstgang == "Ja"
            && SelectedDienstgangOrt == "Anderer Ort";
        ShowFortbildungHint = SelectedOrtBeginn == "Fortbildungsort";
    }

    private Dictionary<string, string> BuildSelections()
    {
        var s = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);

        void Add(string key, string? value)
        {
            if (!string.IsNullOrEmpty(value)) s[key] = value;
        }

        Add("OrtBeginn", SelectedOrtBeginn);
        Add("Dienstgang", SelectedDienstgang);
        if (ShowDienstgangDetails) Add("DienstgangEndeTyp", SelectedDienstgangEndeTyp);
        if (ShowDienstgangOrt) Add("DienstgangOrt", SelectedDienstgangOrt);
        Add("OrtEnde", SelectedOrtEnde);
        if (ShowMitarbeiterTyp) Add("MitarbeiterTyp", SelectedMitarbeiterTyp);
        if (ShowZusatzMobil) Add("ZusatzMobil", SelectedZusatzMobil);
        if (ShowDienstreise) Add("Dienstreise", SelectedDienstreise);
        return s;
    }

    private void UpdateTimeFields()
    {
        TimeInputs.Clear();
        if (SelectedProcedure == null) return;

        var labels = _config.TimeFieldLabels ?? new Dictionary<string, string>();
        var defaults = BuildDefaultTimes(SelectedProcedure);

        foreach (var key in SelectedProcedure.RequiredTimeFields ?? new List<string>())
        {
            labels.TryGetValue(key, out var label);
            defaults.TryGetValue(key, out var value);
            TimeInputs.Add(new TimeInputItem
            {
                Key = key,
                Label = label ?? key,
                Value = value ?? GetStaticDefault(key)
            });
        }

        AdjustTimesForPauses();
    }

    /// <summary>
    /// Builds default times from procedure steps.
    /// Actions that immediately follow another booking (e.g. Dienstgang Start after Kommen)
    /// default to previous time + 1 minute, matching real booking practice.
    /// Static example times are used for pauses and planned end times.
    /// </summary>
    private static Dictionary<string, string> BuildDefaultTimes(Procedure procedure)
    {
        var result = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        TimeSpan? lastAssigned = null;

        // Keys that should snap to previous+1min when they appear right after a start/kommen
        // in the step sequence (not independent planned times like PauseBeginn).
        var followOnKeys = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
        {
            "DienstgangStart"
        };

        foreach (var step in procedure.Steps ?? new List<BookingStep>())
        {
            var key = step.TimeKey;
            if (string.IsNullOrEmpty(key)) continue;
            if (result.ContainsKey(key)) continue; // first occurrence wins for shared keys

            string value;
            if (followOnKeys.Contains(key) && lastAssigned.HasValue)
            {
                var next = lastAssigned.Value.Add(TimeSpan.FromMinutes(1));
                value = FormatTs(next);
            }
            else
            {
                value = GetStaticDefault(key);
            }

            result[key] = value;
            if (MatrixEngine.TryParseTime(value, out var ts))
                lastAssigned = ts;
        }

        // Ensure all required fields still get a value
        foreach (var key in procedure.RequiredTimeFields ?? new List<string>())
        {
            if (!result.ContainsKey(key))
                result[key] = GetStaticDefault(key);
        }

        return result;
    }

    private static string FormatTs(TimeSpan t)
        => $"{(int)t.TotalHours:D2}:{t.Minutes:D2}";

    private static string GetStaticDefault(string key) => key switch
    {
        "Arbeitsbeginn" => "07:00",
        // Fallback only if no previous step exists
        "DienstgangStart" => "07:01",
        "DienstgangEnde" => "15:30",
        "Arbeitsende" => "16:00",
        "ArbeitsendeDienststaette" => "15:00",
        "ArbeitsbeginnMobil2" => "17:00",
        "FortbildungBeginn" => "09:00",
        "FortbildungEnde" => "15:45",
        "FahrtBeginnHin" => "07:00",
        "AnkunftOrt" => "08:15",
        "FahrtBeginnRueck" => "16:00",
        "AnkunftWohnort" => "17:30",
        _ => "08:00"
    };

    private TimeSpan ParseSollzeit()
    {
        if (MatrixEngine.TryParseTime(SollzeitText, out var ts))
            return ts;
        return new TimeSpan(7, 48, 0);
    }

    [RelayCommand]
    private void Generate()
    {
        if (SelectedProcedure == null)
        {
            StatusMessage = "Keine Prozedur ausgewählt.";
            return;
        }

        var times = TimeInputs.ToDictionary(t => t.Key, t => t.Value ?? "");
        var bookings = _engine.GenerateBookings(SelectedProcedure, times, CustomPauses.ToList());

        BookingResults.Clear();
        int i = 1;
        foreach (var (action, time, comment) in bookings)
        {
            BookingResults.Add(new BookingResultItem
            {
                Nummer = i++,
                Aktion = action,
                Zeit = time,
                Kommentar = comment ?? ""
            });
        }

        var soll = ParseSollzeit();
        var summary = _engine.CalculateWorkTime(bookings, soll);

        SummaryGearbeitet = summary.GearbeitetText;
        SummarySollzeit = summary.SollzeitText;
        SummaryDifferenz = summary.DifferenzText;
        SummaryPause = summary.PauseText;
        SummarySollEnde = summary.SollEndeText;
        SummaryMindestpauseAbzug = summary.MindestpauseAbzugText;
        ShowMindestpauseAbzug = summary.HasMindestpauseAbzug;
        ShowSummaryWarning = summary.Exceeds10h;
        SummaryWarning = summary.Exceeds10h
            ? "⚠ Arbeitszeit überschreitet die 10-Stunden-Grenze!"
            : "";

        StatusMessage = $"Buchungssequenz generiert ({bookings.Count} Schritte). Gearbeitet: {summary.GearbeitetText}";
        if (SelectedProcedure.RequiresBewegungsantrag)
            StatusMessage += "  ·  Bewegungsantrag ggf. erforderlich.";
        if (summary.HasMindestpauseAbzug)
            StatusMessage += $"  ·  Mindestpause-Abzug {summary.MindestpauseAbzugText}.";
        if (summary.Exceeds10h)
            StatusMessage += "  ·  10h-Limit überschritten.";
    }

    
    /// <summary>
    /// Pushes any time-field values that fall inside a custom pause
    /// to (pause end + 1 minute). Later entries stay outside pause windows.
    /// </summary>
    public void AdjustTimesForPauses()
    {
        if (CustomPauses.Count == 0) return;

        var pauses = new List<(TimeSpan Start, TimeSpan End)>();
        foreach (var p in CustomPauses)
        {
            if (MatrixEngine.TryParseTime(p.Start, out var s) &&
                MatrixEngine.TryParseTime(p.End, out var e) && e > s)
                pauses.Add((s, e));
        }
        if (pauses.Count == 0) return;

        pauses.Sort((a, b) => a.Start.CompareTo(b.Start));

        foreach (var item in TimeInputs)
        {
            if (!MatrixEngine.TryParseTime(item.Value, out var t))
                continue;

            bool changed;
            do
            {
                changed = false;
                foreach (var (ps, pe) in pauses)
                {
                    // Inside pause (start inclusive, end exclusive) → push to end + 1 min
                    if (t >= ps && t < pe)
                    {
                        t = pe.Add(TimeSpan.FromMinutes(1));
                        item.Value = FormatTs(t);
                        changed = true;
                    }
                }
            } while (changed);
        }
    }


    [RelayCommand]
    private void RemovePause(PauseEntry? pause)
    {
        if (pause != null)
            CustomPauses.Remove(pause);
    }

    [RelayCommand]
    private void Reset()
    {
        _suppressHandlers = true;
        SelectedOrtBeginn = "Dienststätte";
        SelectedDienstgang = "Nein";
        SelectedDienstgangOrt = "Gleicher Ort (wie Dienststätte)";
        SelectedDienstgangEndeTyp = "Rückkehr + Weiterarbeit";
        SelectedOrtEnde = "Dienststätte";
        SelectedMitarbeiterTyp = "Angestellte";
        SelectedZusatzMobil = "Nein";
        SelectedDienstreise = "Nein";
        SollzeitText = "07:48";
        _suppressHandlers = false;

        BookingResults.Clear();
        CustomPauses.Clear();
        SummaryGearbeitet = "—";
        SummarySollzeit = "07:48";
        SummaryDifferenz = "—";
        SummaryPause = "—";
        SummarySollEnde = "—";
        SummaryMindestpauseAbzug = "—";
        ShowMindestpauseAbzug = false;
        SummaryWarning = "";
        ShowSummaryWarning = false;

        if (ProcedureTemplates.Count > 0)
            SelectedTemplate = ProcedureTemplates[0];
        else
            UpdateFromDropdowns();

        StatusMessage = "Zurückgesetzt.";
    }
}

public partial class TimeInputItem : ObservableObject
{
    public string Key { get; set; } = "";
    public string Label { get; set; } = "";
    [ObservableProperty] private string? value;
}

public partial class BookingResultItem : ObservableObject
{
    public int Nummer { get; set; }
    public string Aktion { get; set; } = "";
    public string Zeit { get; set; } = "";
    public string Kommentar { get; set; } = "";
}
