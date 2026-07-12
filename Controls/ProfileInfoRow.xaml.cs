namespace SipCoreMobile.Controls;

public partial class ProfileInfoRow : ContentView
{
    public static readonly BindableProperty LabelProperty = BindableProperty.Create(
        nameof(Label), typeof(string), typeof(ProfileInfoRow), "", propertyChanged: OnLabelChanged);

    public static readonly BindableProperty ValueProperty = BindableProperty.Create(
        nameof(Value), typeof(string), typeof(ProfileInfoRow), "", propertyChanged: OnValueChanged);

    public string Label
    {
        get => (string)GetValue(LabelProperty);
        set => SetValue(LabelProperty, value);
    }

    public string Value
    {
        get => (string)GetValue(ValueProperty);
        set => SetValue(ValueProperty, value);
    }

    public ProfileInfoRow()
    {
        InitializeComponent();
    }

    private static void OnLabelChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ProfileInfoRow)bindable).LabelText.Text = (string)newValue;

    private static void OnValueChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((ProfileInfoRow)bindable).ValueText.Text = (string)newValue;
}
