using Microsoft.Extensions.DependencyInjection;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        InitializeComponent();
        _services = services;
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = _services.GetRequiredService<AppShell>();
        var window = new Window(shell);

        // Port of MainActivity.onCreate()'s tryAutoLogin() call -- attempt silent
        // re-registration from saved credentials before the user sees the login screen.
        window.Created += async (_, _) =>
        {
            var appState = _services.GetRequiredService<AppStateViewModel>();
            await appState.TryAutoLoginAsync();
        };

        return window;
    }
}
