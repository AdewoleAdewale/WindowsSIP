using System.ComponentModel;
using SipCoreMobile.Models;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile;

public partial class AppShell : Shell
{
    private readonly AppStateViewModel _appState;

    /// <summary>
    /// Screen → route map. Add an entry here the same turn a new page is built (mirrors
    /// registering a route for a new screen in Compose's `when(state.currentScreen)`).
    /// Screens not yet present just log instead of crashing on an unregistered route.
    /// </summary>
    private static readonly Dictionary<Screen, string> Routes = new()
    {
        [Screen.Login] = "//login",
        [Screen.Home] = "//home",
        [Screen.DialPad] = "//dialpad",
        [Screen.IncomingCall] = "//incomingcall",
        [Screen.ActiveCall] = "//activecall",
        [Screen.Conversations] = "//conversations",
        [Screen.CallHistory] = "//callhistory",
        [Screen.Contacts] = "//contacts",
        [Screen.Profile] = "//profile",
        [Screen.Groups] = "//groups",
        [Screen.Settings] = "//settings",
        [Screen.MeetingHome] = "//meetinghome"

    };

    public AppShell(AppStateViewModel appState)
    {
        InitializeComponent();
        _appState = appState;
        BindingContext = appState;
        _appState.PropertyChanged += OnAppStatePropertyChanged;
    }

    private async void OnAppStatePropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName != nameof(AppStateViewModel.CurrentScreen)) return;

        if (Routes.TryGetValue(_appState.CurrentScreen, out var route))
        {
            await GoToAsync(route);
        }
        else
        {
            System.Diagnostics.Debug.WriteLine(
                $"AppShell: no route registered yet for screen '{_appState.CurrentScreen}'.");
        }
    }
}
