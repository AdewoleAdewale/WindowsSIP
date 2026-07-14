using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class DesktopCallsPage : ContentPage
{
    private readonly AppStateViewModel _appState;

    public DesktopCallsPage(AppStateViewModel appState)
    {
        InitializeComponent();
        _appState = appState;
        BindingContext = appState;

        _appState.CallLogs.CollectionChanged += (_, _) =>
            MainThread.BeginInvokeOnMainThread(RefreshHistory);
    }

    public async Task InitializeHostedAsync()
    {
        _appState.PropertyChanged += (_, args) =>
        {
            if (args.PropertyName == nameof(AppStateViewModel.Status))
                MainThread.BeginInvokeOnMainThread(() =>
                {
                    StatusRow.Value = _appState.Status;
                    CallStateTitle.Text = string.IsNullOrEmpty(_appState.Status) || _appState.Status == "Registered"
                        ? "No active call"
                        : _appState.Status;
                });
        };
        ExtensionRow.Label = "Extension";
        ExtensionRow.Value = _appState.Extension;

        RegistrationRow.Label = "Registration";
        RegistrationRow.Value = _appState.Registered ? "Registered" : "Not registered";

        StatusRow.Label = "Status";
        StatusRow.Value = _appState.Status;
        RefreshHistory();
    }

    // ── ADAPT HERE: map your call-log model to display fields ──
    private static (string Title, string Subtitle, string Glyph, Color GlyphColor) Describe(object logObj)
    {
        dynamic log = logObj;
        string number = log.Number ?? "";          // ← your property name
        string direction = log.Direction ?? "";      // e.g. "Incoming"/"Outgoing"/"Missed"
        string when = log.Time?.ToString() ?? ""; // timestamp/duration line

        return direction.ToLowerInvariant() switch
        {
            "missed" => (number, $"{when} • Missed", "↙", Color.FromArgb("#F04438")),
            "incoming" => (number, when, "↙", Color.FromArgb("#12B76A")),
            _ => (number, when, "↗", Color.FromArgb("#155EEF")),
        };
    }

    private void RefreshHistory()
    {
        var q = HistorySearchBox.Text?.Trim() ?? "";
        var logs = _appState.CallLogs
            .Where(l => string.IsNullOrEmpty(q) || Describe(l).Title.Contains(q, StringComparison.OrdinalIgnoreCase))
            .ToList();

        HistoryCountLabel.Text = $"{logs.Count} call(s)";
        HistoryEmpty.IsVisible = logs.Count == 0;

        var rows = new List<View>();
        foreach (var log in logs)
        {
            var (title, subtitle, glyph, glyphColor) = Describe(log);
            rows.Add(BuildHistoryRow(title, subtitle, glyph, glyphColor));
        }
        HistoryList.ItemsSource = null;
        HistoryList.ItemsSource = rows;
        HistoryList.ItemTemplate = new DataTemplate(() =>
        {
            var cv = new ContentView();
            cv.SetBinding(ContentView.ContentProperty, ".");
            return cv;
        });
    }

    private View BuildHistoryRow(string title, string subtitle, string glyph, Color glyphColor)
    {
        var callBtn = new Button
        {
            Text = "📞",
            FontSize = 12,
            WidthRequest = 36,
            HeightRequest = 36,
            Padding = 0,
            CornerRadius = 8,
            BackgroundColor = Color.FromArgb("#1A14B8A6"),
            TextColor = Colors.Black
        };
        callBtn.Clicked += async (_, _) => await _appState.MakeOutgoingCallAsync(title);

        var grid = new Grid
        {
            ColumnDefinitions = { new(GridLength.Auto), new(GridLength.Star), new(GridLength.Auto) },
            ColumnSpacing = 12,
            Padding = new Thickness(12, 10)
        };
        grid.Add(new Label { Text = glyph, TextColor = glyphColor, FontSize = 15, VerticalOptions = LayoutOptions.Center });
        var textStack = new VerticalStackLayout { Spacing = 1, VerticalOptions = LayoutOptions.Center };
        textStack.Add(new Label
        {
            Text = title,
            FontSize = 14,
            FontAttributes = FontAttributes.Bold,
            TextColor = Color.FromArgb("#101828"),
            LineBreakMode = LineBreakMode.TailTruncation
        });
        textStack.Add(new Label { Text = subtitle, FontSize = 11.5, TextColor = Color.FromArgb("#667085") });
        grid.Add(textStack, 1);
        grid.Add(callBtn, 2);

        return new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 10 },
            StrokeThickness = 1,
            Stroke = Color.FromArgb("#E4E7EC"),
            BackgroundColor = Colors.White,
            Content = grid
        };
    }

    // ── Dialpad ──
    private void OnKeyTapped(object? sender, string digit) => NumberEntry.Text += digit;
    private void OnKeyLongPressed(object? sender, string alt) => NumberEntry.Text += alt;
    private void OnBackspaceTapped(object? sender, EventArgs e)
    {
        var t = NumberEntry.Text ?? "";
        if (t.Length > 0) NumberEntry.Text = t[..^1];
    }
    private async void OnCallClicked(object? sender, EventArgs e)
    {
        var number = NumberEntry.Text?.Trim();
        if (!string.IsNullOrEmpty(number))
            await _appState.MakeOutgoingCallAsync(number);
    }
    private void OnHistorySearchChanged(object? sender, TextChangedEventArgs e) => RefreshHistory();
}