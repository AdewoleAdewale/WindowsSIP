namespace SipCoreMobile.Models;

/// <summary>
/// Row model for CallHistoryPage's CollectionView, which flattens the original's grouped
/// `groupedLogs.forEach { (dateTitle, logsForDate) -> ... }` structure into a single list of
/// either a date-header row or a call-log row (via IsHeader), since CollectionView doesn't
/// group as naturally as Compose's LazyColumn did here.
/// </summary>
public class CallHistoryRowItem
{
    public bool IsHeader { get; init; }
    public string HeaderText { get; init; } = "";

    public string DisplayName { get; init; } = "";
    public string Extension { get; init; } = "";
    public string StatusText { get; init; } = "";
    public string Subtitle { get; init; } = "";
    public bool IsMissed { get; init; }
    public bool IsIncoming { get; init; }

    public string IconGlyph { get; init; } = "";
    public Color IconColor { get; init; } = Colors.Black;
    public Color IconBackgroundColor { get; init; } = Colors.Transparent;
}
