using System;
using System.Collections.Specialized;
using System.Linq;
using Microsoft.Maui.Controls;
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

        // Populate standard template labels
        SubtitleLabel.Text = $"Extension {_viewModel.Extension} • Tracking active line logs";

        ApplyFilters();
    }

    private void OnCallLogsChanged(object? sender, NotifyCollectionChangedEventArgs e) => ApplyFilters();

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e) => ApplyFilters();

    /// <summary>
    /// Processes filter selections and search text queries, then updates the desktop list frame.
    /// </summary>
    private void ApplyFilters()
    {
        var searchQuery = SearchBox.Text?.Trim() ?? "";
        var filterSelection = FilterPicker.SelectedItem?.ToString() ?? "All calls";

        var filteredLogs = _viewModel.CallLogs
            .OrderByDescending(l => NormalizeTime(l.Time))
            .Select(log => {
                var ext = CallExtension(log.Number);
                var name = CallDisplayName(log.Number, ext);

                // Generate initials safely for avatar circles
                var initials = !string.IsNullOrEmpty(name) ? name[0].ToString().ToUpper() : "?";

                // Determine directional arrows
                var glyph = log.IsIncoming ? "↙" : "↗";

                var duration = log.DurationSeconds > 0
                    ? $"{log.DurationSeconds / 60:D2}:{log.DurationSeconds % 60:D2}"
                    : "00:00";

                // Return a clean layout-ready presentation container object
                return new
                {
                    RawLog = log,
                    Extension = ext,
                    DisplayName = name,
                    AvatarInitials = initials,
                    DirectionGlyph = glyph,
                    TimestampText = FormatCallDate(log.Time) + " • " + GetTimeOnly(log.Time),
                    DurationText = duration
                };
            })
            .Where(wrapped => {
                if (string.IsNullOrEmpty(searchQuery)) return true;
                return wrapped.DisplayName.Contains(searchQuery, StringComparison.OrdinalIgnoreCase) ||
                       wrapped.Extension.Contains(searchQuery, StringComparison.OrdinalIgnoreCase);
            })
            .Where(wrapped => {
                return filterSelection switch
                {
                    "Incoming" => wrapped.RawLog.IsIncoming && !wrapped.RawLog.IsMissed,
                    "Outgoing" => !wrapped.RawLog.IsIncoming,
                    "Missed" => wrapped.RawLog.IsMissed,
                    _ => true
                };
            })
            .ToList();

        // Fix context naming visibility controls safely
        if (EmptyState != null) EmptyState.IsVisible = filteredLogs.Count == 0;
        if (CallHistoryList != null) CallHistoryList.IsVisible = filteredLogs.Count > 0;

        CallHistoryList.ItemsSource = filteredLogs;

        SubtitleLabel.Text = $"Extension {_viewModel.Extension} • Found {filteredLogs.Count} records matching filters";
    }
    private string CallDisplayName(string number, string cleanNumber)
    {
        var contact = _viewModel.Contacts.FirstOrDefault(c => c.Extension == cleanNumber);
        return contact is not null && !string.IsNullOrEmpty(contact.DisplayName)
            ? contact.DisplayName
            : cleanNumber;
    }

    private static string CallExtension(string number)
    {
        var openIdx = number.IndexOf('(');
        var closeIdx = number.IndexOf(')');
        var extracted = (openIdx >= 0 && closeIdx > openIdx) ? number[(openIdx + 1)..closeIdx] : "";
        return string.IsNullOrWhiteSpace(extracted) ? number.Trim() : extracted.Trim();
    }

    private static long NormalizeTime(long raw) => raw <= 0 ? 0 : (raw < 10_000_000_000L ? raw * 1000 : raw);

    private static string FormatCallDate(long time)
    {
        var safeTime = NormalizeTime(time);
        if (safeTime <= 0) return "Unknown date";

        var date = DateTimeOffset.FromUnixTimeMilliseconds(safeTime).ToLocalTime().Date;
        var today = DateTime.Today;
        var yesterday = today.AddDays(-1);

        if (date == today) return "Today";
        if (date == yesterday) return "Yesterday";
        return date.ToString("dd MMM yyyy");
    }

    private static string GetTimeOnly(long time)
    {
        var safeTime = NormalizeTime(time);
        return safeTime > 0
            ? DateTimeOffset.FromUnixTimeMilliseconds(safeTime).ToLocalTime().ToString("hh:mm tt")
            : "";
    }

    private async void OnClearLogsClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlert("Clear Call History", "This will remove all call history logs permanently. Continue?", "Clear", "Cancel");
        if (!confirmed) return;

        await _viewModel.ClearCallLogsAsync();
        ApplyFilters();
    }

    private async void OnQuickCallClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: CallLogUi log } && !string.IsNullOrEmpty(log.Extension))
        {
            await _viewModel.MakeOutgoingCallAsync(log.Extension);
        }
    }

    private async void OnDeleteLogClicked(object? sender, EventArgs e)
    {
        if (sender is Button { CommandParameter: CallLogUi log })
        {
            // Optional local record deletion logic hook can go here
            _viewModel.CallLogs.Remove(log);
            ApplyFilters();
        }
    }

    private async void OnOpenDialpadClicked(object? sender, EventArgs e)
    {
        // Smoothly bring the user straight back onto your call window workspace pane
        await Navigation.PopAsync();
    }

    private async void OnBackTapped(object sender, EventArgs e) => await Navigation.PopAsync();
}