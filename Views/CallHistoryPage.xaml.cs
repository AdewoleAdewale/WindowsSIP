using System.Collections.Specialized;
using SipCoreMobile.Models;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class CallHistoryPage : ContentPage
{
    private readonly AppStateViewModel _viewModel;

    public CallHistoryPage(AppStateViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        _viewModel.CallLogs.CollectionChanged += OnCallLogsChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Rebuild();
    }

    private void OnCallLogsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();

    /// <summary>Port of CallHistoryScreen's sortedLogs/groupedLogs derivation.</summary>
    private void Rebuild()
    {
        var logs = _viewModel.CallLogs.OrderByDescending(l => NormalizeTime(l.Time)).ToList();

        ClearButton.IsVisible = logs.Count > 0;
        EmptyState.IsVisible = logs.Count == 0;
        HistoryList.IsVisible = logs.Count > 0;

        var rows = new List<CallHistoryRowItem>();
        string? currentHeader = null;

        foreach (var log in logs)
        {
            var header = FormatCallDate(log.Time);
            if (header != currentHeader)
            {
                rows.Add(new CallHistoryRowItem { IsHeader = true, HeaderText = header });
                currentHeader = header;
            }

            var extension = CallExtension(log.Number);
            var displayName = CallDisplayName(log.Number, extension);

            var statusText = log.IsMissed ? "Missed call" : log.IsIncoming ? "Incoming call" : "Outgoing call";
            var (glyph, color, bg) = log.IsMissed
                ? ("\uF082", (Color)Application.Current!.Resources["SIPCoreDanger"], Color.FromArgb("#FFEBEE"))
                : log.IsIncoming
                    ? ("\uF081", (Color)Application.Current!.Resources["SIPCoreSuccess"], Color.FromArgb("#E8F5E9"))
                    : ("\uF081", (Color)Application.Current!.Resources["SIPCorePrimary"], Color.FromArgb("#E3F2FD"));

            rows.Add(new CallHistoryRowItem
            {
                DisplayName = displayName,
                Extension = extension,
                StatusText = statusText,
                Subtitle = BuildHistorySubtitle(log),
                IsMissed = log.IsMissed,
                IsIncoming = log.IsIncoming,
                IconGlyph = glyph,
                IconColor = color,
                IconBackgroundColor = bg
            });
        }

        HistoryList.ItemsSource = rows;
    }

    /// <summary>Port of CallHistoryScreen's callDisplayName().</summary>
    private string CallDisplayName(string number, string cleanNumber)
    {
        var contact = _viewModel.Contacts.FirstOrDefault(c => c.Extension == cleanNumber);
        return contact is not null && !string.IsNullOrEmpty(contact.DisplayName)
            ? $"{contact.DisplayName} ({cleanNumber})"
            : cleanNumber;
    }

    /// <summary>Port of CallHistoryScreen's callExtension().</summary>
    private static string CallExtension(string number)
    {
        var openIdx = number.IndexOf('(');
        var closeIdx = number.IndexOf(')');
        var extracted = (openIdx >= 0 && closeIdx > openIdx) ? number[(openIdx + 1)..closeIdx] : "";
        return string.IsNullOrWhiteSpace(extracted) ? number.Trim() : extracted.Trim();
    }

    private static long NormalizeTime(long raw) => raw <= 0 ? 0 : (raw < 10_000_000_000L ? raw * 1000 : raw);

    /// <summary>Port of CallHistoryScreen's formatCallDate() (Today/Yesterday/EEE, dd MMM yyyy).</summary>
    private static string FormatCallDate(long time)
    {
        var safeTime = NormalizeTime(time);
        if (safeTime <= 0) return "Unknown date";

        var date = DateTimeOffset.FromUnixTimeMilliseconds(safeTime).ToLocalTime().Date;
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);

        if (date == today) return "Today";
        if (date == yesterday) return "Yesterday";
        return date.ToString("ddd, dd MMM yyyy");
    }

    /// <summary>Port of buildHistorySubtitle(): "dd MMM yyyy • hh:mm a" plus duration if present.</summary>
    private static string BuildHistorySubtitle(CallLogUi log)
    {
        var safeTime = NormalizeTime(log.Time);
        var time = safeTime > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(safeTime).ToLocalTime().ToString("dd MMM yyyy • hh:mm tt")
            : "";

        var duration = log.DurationSeconds > 0
            ? $" • {log.DurationSeconds / 60:D2}:{log.DurationSeconds % 60:D2}"
            : "";

        return time + duration;
    }

    private void OnDrawerRequested(object? sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    private async void OnClearHistoryClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlert("Clear Call History", "This will remove all call history. Continue?", "Clear", "Cancel");
        if (!confirmed) return;

        await _viewModel.ClearCallLogsAsync();
    }

    private async void OnCallBackClicked(object? sender, EventArgs e)
    {
        if (sender is ImageButton { CommandParameter: string extension } && !string.IsNullOrEmpty(extension))
        {
            await _viewModel.MakeOutgoingCallAsync(extension);
        }
    }

    private void OnDialerTapped(object? sender, EventArgs e) => _viewModel.NavigateSafely(Screen.DialPad);
    private void OnContactsTapped(object? sender, EventArgs e) => _viewModel.NavigateSafely(Screen.Contacts);
    private void OnMessagesTapped(object? sender, EventArgs e) => _viewModel.NavigateSafely(Screen.Conversations);
}
