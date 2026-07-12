namespace SipCoreMobile.Controls;

public partial class WorkDeskSelectableRow : ContentView
{
    public event EventHandler? Tapped;

    public WorkDeskSelectableRow()
    {
        InitializeComponent();
    }

    public void Configure(string icon, string title, string subtitle, bool selected)
    {
        IconLabel.Text = icon;
        TitleLabel.Text = title;
        SubtitleLabel.Text = subtitle;
        SubtitleLabel.IsVisible = !string.IsNullOrEmpty(subtitle);
        CheckIcon.IsVisible = selected;
        RowBorder.BackgroundColor = selected ? Color.FromArgb("#260057B8") : Color.FromArgb("#14FFFFFF");
    }

    private void OnTapped(object? sender, TappedEventArgs e) => Tapped?.Invoke(this, EventArgs.Empty);
}
