namespace SipCoreMobile.Controls;

public partial class WorkDeskChecklistRow : ContentView
{
    public event EventHandler? RemoveClicked;

    public WorkDeskChecklistRow()
    {
        InitializeComponent();
    }

    public void Configure(string title, string description)
    {
        TitleLabel.Text = title;
        DescriptionLabel.Text = description;
        DescriptionLabel.IsVisible = !string.IsNullOrEmpty(description);
    }

    private void OnRemoveClicked(object? sender, EventArgs e) => RemoveClicked?.Invoke(this, EventArgs.Empty);
}
