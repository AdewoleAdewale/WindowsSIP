namespace SipCoreMobile.Controls;

public partial class CallControlButton : ContentView
{
    public event EventHandler? Tapped;

    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(CallControlButton), "", propertyChanged: OnAnyChanged);

    public static readonly BindableProperty LabelProperty = BindableProperty.Create(
        nameof(Label), typeof(string), typeof(CallControlButton), "", propertyChanged: OnAnyChanged);

    public static readonly BindableProperty IsActiveProperty = BindableProperty.Create(
        nameof(IsActive), typeof(bool), typeof(CallControlButton), false, propertyChanged: OnAnyChanged);

    public string Icon
    {
        get => (string)GetValue(IconProperty);
        set => SetValue(IconProperty, value);
    }

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public bool IsActive
    {
        get => (bool)GetValue(IsActiveProperty);
        set => SetValue(IsActiveProperty, value);
    }

    public CallControlButton()
    {
        InitializeComponent();
        Refresh();
    }

    private static void OnAnyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((CallControlButton)bindable).Refresh();

    private void Refresh()
    {
        IconLabel.Text = Icon;
        LabelText.Text = Label;
        IconBackground.BackgroundColor = IsActive
            ? (Color)Application.Current!.Resources["SIPCorePrimary"]
            : Color.FromArgb("#26FFFFFF");
    }

    private void OnTapped(object? sender, TappedEventArgs e) => Tapped?.Invoke(this, EventArgs.Empty);
}
