namespace SipCoreMobile.Controls;

public partial class WorkDeskCollapsibleSection : ContentView
{
    private bool _expanded;

    public WorkDeskCollapsibleSection()
    {
        InitializeComponent();
    }

    public string Title
    {
        get => TitleLabel.Text;
        set => TitleLabel.Text = value;
    }

    /// <summary>The layout to fill with section content -- add children to this from the host page.</summary>
    public VerticalStackLayout Content => ContentArea;

    public void SetExpanded(bool expanded)
    {
        _expanded = expanded;
        ContentArea.IsVisible = expanded;
        ChevronLabel.Text = expanded ? "\uF077" : "\uF078"; // chevron-up / chevron-down
    }

    private void OnHeaderTapped(object? sender, TappedEventArgs e) => SetExpanded(!_expanded);
}
