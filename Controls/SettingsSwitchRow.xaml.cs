namespace SipCoreMobile.Controls;

public partial class SettingsSwitchRow : ContentView
{
    public event EventHandler<bool>? Toggled;

    public static readonly BindableProperty TitleProperty = BindableProperty.Create(
        nameof(Title), typeof(string), typeof(SettingsSwitchRow), "", propertyChanged: OnTitleChanged);

    public static readonly BindableProperty DescriptionProperty = BindableProperty.Create(
        nameof(Description), typeof(string), typeof(SettingsSwitchRow), "", propertyChanged: OnDescriptionChanged);

    public string Title
    {
        get => (string)GetValue(TitleProperty);
        set => SetValue(TitleProperty, value);
    }

    public string Description
    {
        get => (string)GetValue(DescriptionProperty);
        set => SetValue(DescriptionProperty, value);
    }

    public bool IsChecked
    {
        get => ToggleSwitch.IsToggled;
        set => ToggleSwitch.IsToggled = value;
    }

    public SettingsSwitchRow()
    {
        InitializeComponent();
    }

    private static void OnTitleChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((SettingsSwitchRow)bindable).TitleLabel.Text = (string)newValue;

    private static void OnDescriptionChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((SettingsSwitchRow)bindable).DescriptionLabel.Text = (string)newValue;

    private void OnSwitchToggled(object? sender, ToggledEventArgs e) => Toggled?.Invoke(this, e.Value);
}
