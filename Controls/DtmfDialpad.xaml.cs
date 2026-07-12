namespace SipCoreMobile.Controls;

public partial class DtmfDialpad : ContentView
{
    public event EventHandler<string>? DigitPressed;

    public DtmfDialpad()
    {
        InitializeComponent();
    }

    private void OnDigitTapped(object? sender, TappedEventArgs e)
    {
        if (sender is TapGestureRecognizer { CommandParameter: string digit })
        {
            DigitPressed?.Invoke(this, digit);
        }
    }
}
