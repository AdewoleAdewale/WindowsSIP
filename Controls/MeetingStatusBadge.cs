namespace SipCoreMobile.Controls;

/// <summary>Port of MeetingStatusBadge() from MeetingScreens.kt. No XAML needed -- built entirely in code
/// since it's just a styled Label with status-driven colors.</summary>
public class MeetingStatusBadge : Border
{
    public static readonly BindableProperty StatusProperty = BindableProperty.Create(
        nameof(Status), typeof(string), typeof(MeetingStatusBadge), "", propertyChanged: OnStatusChanged);

    public string Status
    {
        get => (string)GetValue(StatusProperty);
        set => SetValue(StatusProperty, value);
    }

    private readonly Label _label = new() { FontSize = 12, FontAttributes = FontAttributes.Bold };

    public MeetingStatusBadge()
    {
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 50 };
        Padding = new Thickness(10, 5);
        Content = _label;
        Refresh();
    }

    private static void OnStatusChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((MeetingStatusBadge)bindable).Refresh();

    private void Refresh()
    {
        var cleanStatus = string.IsNullOrWhiteSpace(Status) ? "Scheduled" : Status;
        _label.Text = cleanStatus;

        var (bg, fg) = cleanStatus.ToLowerInvariant() switch
        {
            "live" => (Color.FromArgb("#2422C55E"), Color.FromArgb("#22C55E")),
            "ended" or "expired" or "cancelled" => (Color.FromArgb("#1FDC2626"), Color.FromArgb("#DC2626")),
            _ => (Color.FromArgb("#1F0057B8"), Color.FromArgb("#0057B8"))
        };

        BackgroundColor = bg;
        _label.TextColor = fg;
    }
}
