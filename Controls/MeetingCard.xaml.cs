using SipCoreMobile.Models.Api;

namespace SipCoreMobile.Controls;

public partial class MeetingCard : ContentView
{
    public event EventHandler<SipCoreMeetingDto>? MeetingTapped;
    public event EventHandler<string>? JoinTapped;

    public static readonly BindableProperty MeetingProperty = BindableProperty.Create(
        nameof(Meeting), typeof(SipCoreMeetingDto), typeof(MeetingCard), null, propertyChanged: OnMeetingChanged);

    public SipCoreMeetingDto? Meeting
    {
        get => (SipCoreMeetingDto?)GetValue(MeetingProperty);
        set => SetValue(MeetingProperty, value);
    }

    public MeetingCard()
    {
        InitializeComponent();
    }

    private static void OnMeetingChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((MeetingCard)bindable).Refresh();

    private void Refresh()
    {
        if (Meeting is null) return;

        TitleLabel.Text = string.IsNullOrWhiteSpace(Meeting.Title) ? "SIPCore Meeting" : Meeting.Title;
        ConferenceLabel.Text = $"Conference {Meeting.ConferenceNumber}";

        HostLabel.IsVisible = !string.IsNullOrWhiteSpace(Meeting.CreatedByExtension);
        HostLabel.Text = $"Host {Meeting.CreatedByExtension}";

        StatusBadge.Status = Meeting.Status;
    }

    private void OnCardTapped(object? sender, TappedEventArgs e)
    {
        if (Meeting is not null) MeetingTapped?.Invoke(this, Meeting);
    }

    private void OnJoinClicked(object? sender, EventArgs e)
    {
        if (Meeting is not null) JoinTapped?.Invoke(this, Meeting.ConferenceNumber);
    }
}
