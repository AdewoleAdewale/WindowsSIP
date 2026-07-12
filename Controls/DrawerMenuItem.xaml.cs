namespace SipCoreMobile.Controls;

public partial class DrawerMenuItem : ContentView
{
    public event EventHandler? Tapped;

    public static readonly BindableProperty IconProperty = BindableProperty.Create(
        nameof(Icon), typeof(string), typeof(DrawerMenuItem), "", propertyChanged: OnAnyChanged);

    public static readonly BindableProperty LabelProperty = BindableProperty.Create(
        nameof(Label), typeof(string), typeof(DrawerMenuItem), "", propertyChanged: OnAnyChanged);

    public static readonly BindableProperty IsSelectedProperty = BindableProperty.Create(
        nameof(IsSelected), typeof(bool), typeof(DrawerMenuItem), false, propertyChanged: OnAnyChanged);

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

    public bool IsSelected
    {
        get => (bool)GetValue(IsSelectedProperty);
        set => SetValue(IsSelectedProperty, value);
    }

    public DrawerMenuItem()
    {
        InitializeComponent();
        Refresh();
    }

    private static void OnAnyChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((DrawerMenuItem)bindable).Refresh();

    private void Refresh()
    {
        IconLabel.Text = Icon;
        LabelText.Text = Label;

        var selectedColor = Application.Current?.Resources["SIPCorePrimary"] as Color ?? Colors.Blue;
        var textColor = Application.Current?.Resources["SIPCoreTextPrimary"] as Color ?? Colors.Black;
        var selectedBg = Color.FromArgb("#EAF2FF");

        RowBackground.BackgroundColor = IsSelected ? selectedBg : Colors.Transparent;
        IconLabel.TextColor = IsSelected ? selectedColor : textColor;
        LabelText.TextColor = IsSelected ? selectedColor : textColor;
        LabelText.FontAttributes = IsSelected ? FontAttributes.Bold : FontAttributes.None;
    }

    private void OnTapped(object? sender, TappedEventArgs e) => Tapped?.Invoke(this, EventArgs.Empty);
}
