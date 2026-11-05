using System.Globalization;
using ProSeq.Models;

namespace ProSeq.Services;

public class MatrixEngine
{
    private readonly List<Procedure> _procedures;

    public MatrixEngine(IEnumerable<Procedure> procedures)
    {
        _procedures = procedures.ToList();
    }

    public IEnumerable<Procedure> FindMatching(Dictionary<string, string> selections)
        => _procedures.Where(p => Matches(p, selections));

    public Procedure? FindBestMatch(Dictionary<string, string> selections)
    {
        var matches = FindMatching(selections).ToList();
        if (matches.Count == 0) return null;
        return matches.OrderByDescending(m => m.Conditions.Count).First();
    }

    private static bool Matches(Procedure procedure, Dictionary<string, string> selections)
    {
        foreach (var (key, expected) in procedure.Conditions)
        {
            if (!selections.TryGetValue(key, out var actual))
                return false;
            if (!string.Equals(actual, expected, StringComparison.OrdinalIgnoreCase))
                return false;
        }
        return true;
    }

    public List<(string Action, string Time, string? Comment)> GenerateBookings(
        Procedure procedure,
        Dictionary<string, string> times,
        IReadOnlyList<PauseEntry>? extraPauses = null)
    {
        var result = new List<(string Action, string Time, string? Comment)>();

        foreach (var step in procedure.Steps)
        {
            string timeValue;
            if (!string.IsNullOrEmpty(step.FixedTime))
                timeValue = step.FixedTime;
            else if (!string.IsNullOrEmpty(step.TimeKey) && times.TryGetValue(step.TimeKey, out var t) && !string.IsNullOrWhiteSpace(t))
                timeValue = t.Trim();
            else
                timeValue = "—";

            result.Add((step.Action, timeValue, step.Comment));
        }

        ApplyMinuteCollisions(result);

        // Determine whether mobile-style pause actions are appropriate
        var useMobilePause = result.Any(b =>
            b.Action.Contains("Mob.", StringComparison.OrdinalIgnoreCase));

        // Inject only user-defined pauses (no automatic default / legal min in the sequence)
        if (extraPauses is { Count: > 0 })
            InjectExtraPauses(result, extraPauses, useMobilePause);

        ApplyMinuteCollisions(result);
        return result;
    }

    /// <summary>
    /// Inserts user-defined pauses into the booking sequence.
    /// Context is determined from surrounding bookings:
    /// - Inside an open Dienstgang → Gehen, then Kommen + Dienstgang Beginn on resume
    /// - Mobile context → Mob. Arbeiten Ende / Beginn
    /// - Otherwise → Gehen / Kommen
    /// Pauses never move existing pause entries; only non-pause times are adjusted later.
    /// </summary>
    private static void InjectExtraPauses(
        List<(string Action, string Time, string? Comment)> bookings,
        IReadOnlyList<PauseEntry> pauses,
        bool useMobile)
    {
        foreach (var pause in pauses.OrderBy(p => TryParseTime(p.Start, out var t) ? t : TimeSpan.MaxValue))
        {
            if (!TryParseTime(pause.Start, out var t0) || !TryParseTime(pause.End, out var t1) || t1 <= t0)
                continue;

            var (go, resumeActions) = ResolvePauseActions(bookings, t0, useMobile);

            InsertChronological(bookings, (go, FormatTime(t0), "Pause (Beginn)"));

            // Resume: may be one or two actions (e.g. Kommen then Dienstgang Beginn)
            var first = true;
            foreach (var action in resumeActions)
            {
                var comment = first ? "Pause (Ende)" : "Dienstgang fortsetzen nach Pause";
                InsertChronological(bookings, (action, FormatTime(t1), comment));
                first = false;
            }
        }
    }

    /// <summary>
    /// Determines the correct leave/resume actions for a pause starting at <paramref name="pauseStart"/>.
    /// Scans the timeline to detect an open Dienstgang (Dienstgang Beginn without matching end yet).
    /// </summary>
    private static (string GoAction, string[] ResumeActions) ResolvePauseActions(
        List<(string Action, string Time, string? Comment)> bookings,
        TimeSpan pauseStart,
        bool useMobile)
    {
        bool insideDienstgang = false;
        bool insideMobile = useMobile;

        foreach (var (action, timeStr, _) in bookings)
        {
            if (!TryParseTime(timeStr, out var t) || t > pauseStart)
                break;

            if (action.Equals("Dienstgang Beginn", StringComparison.OrdinalIgnoreCase))
                insideDienstgang = true;
            else if (action.Equals("Dienstgang (Ende außerhalb)", StringComparison.OrdinalIgnoreCase) ||
                     (action.Equals("Kommen", StringComparison.OrdinalIgnoreCase) && insideDienstgang))
                insideDienstgang = false; // return to workplace ends Dienstgang segment
            else if (action.Equals("Mob. Arbeiten Beginn", StringComparison.OrdinalIgnoreCase))
                insideMobile = true;
            else if (action.Equals("Mob. Arbeiten Ende", StringComparison.OrdinalIgnoreCase))
                insideMobile = false;
        }

        if (insideDienstgang)
        {
            // Pause during Dienstgang: leave with Gehen, resume with Kommen then Dienstgang Beginn
            // (matches PDF examples 2, 3, 9, 10, 11)
            return ("Gehen", new[] { "Kommen", "Dienstgang Beginn" });
        }

        if (insideMobile)
            return ("Mob. Arbeiten Ende", new[] { "Mob. Arbeiten Beginn" });

        return ("Gehen", new[] { "Kommen" });
    }

    private static void InsertChronological(
        List<(string Action, string Time, string? Comment)> bookings,
        (string Action, string Time, string? Comment) item)
    {
        if (!TryParseTime(item.Time, out var tNew))
        {
            bookings.Add(item);
            return;
        }

        int idx = bookings.Count;
        for (int i = 0; i < bookings.Count; i++)
        {
            if (!TryParseTime(bookings[i].Time, out var t))
                continue;
            if (t > tNew || (t == tNew && IsPauseStart(bookings[i].Action)))
            {
                idx = i;
                break;
            }
        }
        bookings.Insert(idx, item);
    }

    private static void ApplyMinuteCollisions(List<(string Action, string Time, string? Comment)> bookings)
    {
        TimeSpan? lastTime = null;

        for (int i = 0; i < bookings.Count; i++)
        {
            var (action, timeStr, comment) = bookings[i];

            if (!TryParseTime(timeStr, out var current))
            {
                lastTime = null;
                continue;
            }

            if (lastTime.HasValue && current <= lastTime.Value)
            {
                current = lastTime.Value.Add(TimeSpan.FromMinutes(1));
                var adjusted = FormatTime(current);
                var note = string.IsNullOrEmpty(comment)
                    ? "+1 Min (Kollisionsvermeidung)"
                    : comment + "  ·  +1 Min (Kollisionsvermeidung)";
                bookings[i] = (action, adjusted, note);
            }

            lastTime = current;
        }
    }

    public static bool TryParseTime(string? text, out TimeSpan time)
    {
        time = default;
        if (string.IsNullOrWhiteSpace(text) || text == "—") return false;
        text = text.Trim().Replace('.', ':');
        if (TimeSpan.TryParseExact(text, new[] { @"h\:mm", @"hh\:mm", @"h\:mm\:ss", @"hh\:mm\:ss" },
                CultureInfo.InvariantCulture, out time))
            return true;
        return TimeSpan.TryParse(text, CultureInfo.InvariantCulture, out time);
    }

    public static string FormatTime(TimeSpan t)
        => $"{(int)t.TotalHours:D2}:{t.Minutes:D2}";

    public WorkTimeSummary CalculateWorkTime(
        List<(string Action, string Time, string? Comment)> bookings,
        TimeSpan sollzeit)
    {
        var s = CalculateWorkTimeInternal(bookings);

        // Legal minimum pause (ArbZG-style): only ≥15 min pauses count.
        // Shortfall is NOT inserted into the booking sequence – it is deducted
        // from worked time in the overview as a penalty.
        var credited = TimeSpan.Zero;
        foreach (var (start, end) in s.Intervals)
        {
            var dur = end - start;
            if (dur >= TimeSpan.FromMinutes(15))
                credited += dur;
        }

        var required = TimeSpan.Zero;
        // Threshold is based on raw worked time (before penalty)
        if (s.Gearbeitet >= TimeSpan.FromHours(9))
            required = TimeSpan.FromMinutes(45);
        else if (s.Gearbeitet >= TimeSpan.FromHours(6))
            required = TimeSpan.FromMinutes(30);

        var shortfall = required > credited ? required - credited : TimeSpan.Zero;
        s.MindestpauseAbzug = shortfall;
        s.HasMindestpauseAbzug = shortfall > TimeSpan.Zero;

        if (shortfall > TimeSpan.Zero)
        {
            s.Gearbeitet -= shortfall;
            if (s.Gearbeitet < TimeSpan.Zero)
                s.Gearbeitet = TimeSpan.Zero;
        }

        s.Sollzeit = sollzeit;
        s.Differenz = s.Gearbeitet - sollzeit;
        s.Exceeds10h = s.Gearbeitet > TimeSpan.FromHours(10);

        // Theoretical end: start + Sollzeit + actual pauses + legal shortfall
        if (s.DayStart != default || bookings.Count > 0)
        {
            s.SollEnde = s.DayStart + sollzeit + s.PauseTotal + shortfall;
            s.HasSollEnde = true;
        }

        return s;
    }

    private static WorkTimeSummary CalculateWorkTimeInternal(
        List<(string Action, string Time, string? Comment)> bookings)
    {
        var summary = new WorkTimeSummary();
        var events = new List<(TimeSpan Time, string Action)>();
        foreach (var (action, timeStr, _) in bookings)
        {
            if (TryParseTime(timeStr, out var t))
                events.Add((t, action));
        }

        if (events.Count == 0)
            return summary;

        var dayStart = events[0].Time;
        var dayEnd = events[^1].Time;
        if (dayEnd < dayStart)
            return summary;

        var daySpan = dayEnd - dayStart;
        var pauseTotal = TimeSpan.Zero;
        var pauseIntervals = new List<(TimeSpan Start, TimeSpan End)>();

        for (int i = 0; i < events.Count - 1; i++)
        {
            var (t1, a1) = events[i];
            var (t2, a2) = events[i + 1];
            if (!IsPauseStart(a1) || !IsPauseResume(a2) || t2 <= t1) continue;
            pauseTotal += t2 - t1;
            pauseIntervals.Add((t1, t2));
        }

        var worked = daySpan - pauseTotal;
        if (worked < TimeSpan.Zero) worked = TimeSpan.Zero;

        summary.Gearbeitet = worked;
        summary.PauseTotal = pauseTotal;
        summary.DayStart = dayStart;
        summary.DayEnd = dayEnd;
        summary.Intervals = pauseIntervals;
        return summary;
    }

    private static bool IsPauseStart(string action) =>
        action.Equals("Gehen", StringComparison.OrdinalIgnoreCase) ||
        action.Equals("Mob. Arbeiten Ende", StringComparison.OrdinalIgnoreCase);

    private static bool IsPauseResume(string action) =>
        action.Equals("Kommen", StringComparison.OrdinalIgnoreCase) ||
        action.Equals("Mob. Arbeiten Beginn", StringComparison.OrdinalIgnoreCase) ||
        action.Equals("Dienstgang Beginn", StringComparison.OrdinalIgnoreCase);
}

public class WorkTimeSummary
{
    public TimeSpan Gearbeitet { get; set; }
    public TimeSpan Sollzeit { get; set; }
    public TimeSpan Differenz { get; set; }
    public TimeSpan PauseTotal { get; set; }
    public TimeSpan DayStart { get; set; }
    public TimeSpan DayEnd { get; set; }
    public bool Exceeds10h { get; set; }
    public List<(TimeSpan Start, TimeSpan End)> Intervals { get; set; } = new();

    /// <summary>
    /// Legal minimum pause shortfall deducted from worked time (not inserted as bookings).
    /// Only applied when credited pauses (≥15 min each) fall short of 30/45 min.
    /// </summary>
    public TimeSpan MindestpauseAbzug { get; set; }
    public bool HasMindestpauseAbzug { get; set; }

    public string GearbeitetText => MatrixEngine.FormatTime(Gearbeitet);
    public string SollzeitText => MatrixEngine.FormatTime(Sollzeit);
    public string PauseText => MatrixEngine.FormatTime(PauseTotal);
    public string MindestpauseAbzugText => HasMindestpauseAbzug
        ? $"−{MatrixEngine.FormatTime(MindestpauseAbzug)}"
        : "—";
    public string DifferenzText
    {
        get
        {
            var sign = Differenz >= TimeSpan.Zero ? "+" : "−";
            return $"{sign}{MatrixEngine.FormatTime(Differenz.Duration())}";
        }
    }

    /// <summary>Clock time when the day would end if exactly Sollzeit were worked (start + soll + pauses + legal shortfall).</summary>
    public TimeSpan SollEnde { get; set; }
    public bool HasSollEnde { get; set; }
    public string SollEndeText => HasSollEnde ? MatrixEngine.FormatTime(SollEnde) : "—";
}
