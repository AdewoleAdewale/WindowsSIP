using SipCoreMobile.Models;

namespace SipCoreMobile.Controls;

public partial class BottomNavBar : ContentView
{
    public event EventHandler? DialerTapped;
    public event EventHandler? ContactsTapped;
    public event EventHandler? MessagesTapped;
    public event EventHandler? HistoryTapped;

    public static readonly BindableProperty SelectedScreenProperty = BindableProperty.Create(
        nameof(SelectedScreen), typeof(Screen), typeof(BottomNavBar), Screen.Home,
        propertyChanged: OnSelectedScreenChanged);

    public Screen SelectedScreen
    {
        get => (Screen)GetValue(SelectedScreenProperty);
        set => SetValue(SelectedScreenProperty, value);
    }

    public BottomNavBar()
    {
        InitializeComponent();
        UpdateSelection();
    }

    private static void OnSelectedScreenChanged(BindableObject bindable, object oldValue, object newValue) =>
        ((BottomNavBar)bindable).UpdateSelection();

    private void UpdateSelection()
    {
        var selectedColor = (Color)Application.Current!.Resources["SIPCorePrimary"];
        var unselectedColor = (Color)Application.Current!.Resources["SIPCoreTextSecondary"];

        SetItemState(DialerIconBackground, DialerIcon, DialerLabel, SelectedScreen == Screen.DialPad, selectedColor, unselectedColor);
        SetItemState(ContactsIconBackground, ContactsIcon, ContactsLabel, SelectedScreen == Screen.Contacts, selectedColor, unselectedColor);
        SetItemState(MessagesIconBackground, MessagesIcon, MessagesLabel,
            SelectedScreen is Screen.Conversations or Screen.Chat, selectedColor, unselectedColor);
        SetItemState(HistoryIconBackground, HistoryIcon, HistoryLabel, SelectedScreen == Screen.CallHistory, selectedColor, unselectedColor);
    }

    private static void SetItemState(Border iconBackground, Label icon, Label label, bool selected, Color selectedColor, Color unselectedColor)
    {
        iconBackground.BackgroundColor = selected ? selectedColor : Colors.Transparent;
        icon.TextColor = selected ? Colors.White : unselectedColor;
        label.TextColor = selected ? selectedColor : unselectedColor;
        label.FontAttributes = selected ? FontAttributes.Bold : FontAttributes.None;
    }

    private void OnDialerTapped(object? sender, TappedEventArgs e) => DialerTapped?.Invoke(this, EventArgs.Empty);
    private void OnContactsTapped(object? sender, TappedEventArgs e) => ContactsTapped?.Invoke(this, EventArgs.Empty);
    private void OnMessagesTapped(object? sender, TappedEventArgs e) => MessagesTapped?.Invoke(this, EventArgs.Empty);
    private void OnHistoryTapped(object? sender, TappedEventArgs e) => HistoryTapped?.Invoke(this, EventArgs.Empty);
}
