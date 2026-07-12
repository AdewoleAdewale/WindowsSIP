namespace SipCoreMobile.Controls;

public partial class SipCoreHeader : ContentView
{
    public event EventHandler? DrawerRequested;

    public SipCoreHeader()
    {
        InitializeComponent();
    }

    private void OnLogoTapped(object? sender, TappedEventArgs e) => DrawerRequested?.Invoke(this, EventArgs.Empty);
}
