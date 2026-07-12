namespace SipCoreMobile.Controls;

public partial class MeetingOptionRow : ContentView
{
    public event EventHandler<bool>? CheckedChanged;

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(MeetingOptionRow), "", propertyChanged: OnTitleChanged);

    public static readonly BindableProperty SubtitleProperty = BindableProperty.Create(
        nameof(Subtitle), typeof(string), typeof(MeetingOptionRow), "", propertyChanged: OnSubtitleChanged);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Subtitle
    {
        get => (string)GetValue(SubtitleProperty);
        set => SetValue(SubtitleProperty, value);
    }

    public bool IsChecked
    {
        get => Checkbox.IsChecked;
        set => Checkbox.IsChecked = value;
    }

    public MeetingOptionRow()
    {
        InitializeComponent();
    }

    private static void OnTitleChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((MeetingOptionRow)bindable).TitleLabel.Text = (string)newValue;

    private static void OnSubtitleChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((MeetingOptionRow)bindable).SubtitleLabel.Text = (string)newValue;

    private void OnCheckedChanged(object? sender, CheckedChangedEventArgs e) => CheckedChanged?.Invoke(this, e.Value);
}
