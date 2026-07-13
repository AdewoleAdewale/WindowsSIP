using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Maui.Controls;
using SipCoreMobile.ViewModels;
using Microsoft.Extensions.Logging;

namespace SipCoreMobile;

public partial class App : Application
{
    private readonly IServiceProvider _services;

    public App(IServiceProvider services)
    {
        // Guard initialization to log any early startup failures (e.g., XAML parsing)
        _services = services;
        try
        {
            InitializeComponent();
        }
        catch (Exception ex)
        {
            try
            {
                var logger = services.GetService<ILogger<App>>();
                logger?.LogError(ex, "Exception during InitializeComponent in App ctor.");
            }
            catch
            {
                // Swallow logger acquisition errors to preserve original exception
            }

            throw;
        }
    }

    protected override Window CreateWindow(IActivationState? activationState)
    {
        var shell = _services.GetRequiredService<AppShell>();
        var window = new Window(shell);

        // Port of MainActivity.onCreate()'s tryAutoLogin() call -- attempt silent
        // re-registration from saved credentials before the user sees the login screen.
        window.Created += async (_, _) =>
        {
            var logger = _services.GetRequiredService<ILogger<App>>();
            try
            {
                var appState = _services.GetRequiredService<AppStateViewModel>();
                await appState.TryAutoLoginAsync();
            }
            catch (Exception ex)
            {
                // Guard against unobserved exceptions from this async void handler
                logger.LogError(ex, "Error while performing TryAutoLoginAsync during window creation.");
            }
        };

        return window;
    }
}
