namespace SipCoreMobile.Controls;

public partial class DialpadKey : Grid
{
    public event EventHandler<string>? Tapped;
    /// <summary>Raised with the long-press alt character (matches the original's onLongClick mapping).</summary>
    public event EventHandler<string>? LongPressed;

    public static readonly BindableProperty DigitProperty = BindableProperty.Create(
        nameof(Digit), typeof(string), typeof(DialpadKey), "", propertyChanged: OnDigitChanged);

    public static readonly BindableProperty LettersProperty = BindableProperty.Create(
        nameof(Letters), typeof(string), typeof(DialpadKey), "", propertyChanged: OnLettersChanged);

    public string Digit
    {
        get => (string)GetValue(DigitProperty);
        set => SetValue(DigitProperty, value);
    }

    public string Letters
    {
        get => (string)GetValue(LettersProperty);
        set => SetValue(LettersProperty, value);
    }

    public DialpadKey()
    {
        InitializeComponent();
    }

    private static void OnDigitChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((DialpadKey)bindable).DigitLabel.Text = (string)newValue;

    private static void OnLettersChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((DialpadKey)bindable).LettersLabel.Text = (string)newValue;

    /// <summary>Port of the onLongClick digit mapping: 0→+, *→',', #→';'.</summary>
    private string LongPressChar => Digit switch
    {
        "0" => "+",
        "*" => ",",
        "#" => ";",
        _ => Digit
    };

    private void OnTapped(object? sender, TappedEventArgs e) => Tapped?.Invoke(this, Digit);

    private void OnLongPressCompleted(object? sender, EventArgs e) => LongPressed?.Invoke(this, LongPressChar);
}
