using CommunityToolkit.Maui;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.DependencyInjection;
using SipCoreMobile.Data;
using SipCoreMobile.Services.Api;
using SipCoreMobile.ViewModels;
using SipCoreMobile.Views;
#if WINDOWS
using Microsoft.Windows.AppNotifications;
using Microsoft.Windows.AppNotifications.Builder;
#endif

namespace SipCoreMobile;

public static class MauiProgram
{
    public static MauiApp CreateMauiApp()
    {
        var builder = MauiApp.CreateBuilder();

        builder
            .UseMauiApp<App>()
            .UseMauiCommunityToolkit() // required for Controls/DialpadKey.xaml's TouchBehavior long-press (Batch 5)
            .ConfigureFonts(fonts =>
            {
                // MaterialIcons is referenced throughout (BottomNavBar, CallControlButton, WorkDesk
                // icons, etc.) as FontFamily="MaterialIcons". Add the actual .ttf to
                // Resources/Fonts/ and reference it here -- see Resources/Fonts/README.md.
                fonts.AddFont("MaterialIcons-Regular.ttf", "MaterialIcons");
                fonts.AddFont("OpenSans-Regular.ttf", "OpenSansRegular");
                fonts.AddFont("OpenSans-Semibold.ttf", "OpenSansSemibold");
            });
        AppDomain.CurrentDomain.UnhandledException += (_, e) =>
        {
            var path = Path.Combine(FileSystem.AppDataDirectory, "crash.log");
            File.WriteAllText(path, e.ExceptionObject?.ToString() ?? "unknown");
        };

#if DEBUG
        builder.Logging.AddDebug();
#endif

        // ---------------------------------------------------------------
        // Data layer (Batch 1)
        // ---------------------------------------------------------------
        builder.Services.AddSingleton(_ =>
      Task.Run(() => SipCoreDatabase.GetDatabaseAsync()).GetAwaiter().GetResult());
        builder.Services.AddSingleton<Data.ISipCoreRepository, SipCoreRepository>();

        // ---------------------------------------------------------------
        // Remote API (Batch 1)
        // ---------------------------------------------------------------
        builder.Services.AddHttpClient<ISipCoreApiService, SipCoreApiService>(c =>
        {
            c.BaseAddress = new Uri("https://softworks-us.org/");
        });

        // ---------------------------------------------------------------
        // App state / feature ViewModels (Batches 2, 4, 6, 8, 11, 12)
        // Singletons: one shared instance for the app's lifetime, mirroring how the original
        // MainActivity's fields lived for as long as the process did.
        // ---------------------------------------------------------------
        builder.Services.AddSingleton<AppStateViewModel>();
        builder.Services.AddSingleton<ChatViewModel>();
        builder.Services.AddSingleton<GroupsViewModel>();
        builder.Services.AddSingleton<MeetingsViewModel>();
        builder.Services.AddSingleton<WorkDeskViewModel>();

        // ---------------------------------------------------------------
        // Pages -- transient (a fresh instance per navigation), resolved either directly by
        // Shell's {DataTemplate} routes or via the DI container (e.g. the nav drawer's
        // WorkDesk item). Pages that need extra runtime data (a specific WorkTaskDto, an
        // in-progress edit, etc.) are constructed with `new` at the call site instead --
        // see each page's own code-behind for which pattern it uses.
        // ---------------------------------------------------------------
        builder.Services.AddTransient<LoginPage>();
        builder.Services.AddTransient<HomePage>();
        builder.Services.AddTransient<DialPadPage>();
        builder.Services.AddTransient<IncomingCallPage>();
        builder.Services.AddTransient<ActiveCallPage>();
        builder.Services.AddTransient<AddConferenceParticipantPage>();
        builder.Services.AddTransient<ConversationsPage>();
        builder.Services.AddTransient<ChatPage>();
        builder.Services.AddTransient<CallHistoryPage>();
        builder.Services.AddTransient<ContactsPage>();
        builder.Services.AddTransient<ProfilePage>();
        builder.Services.AddTransient<GroupsPage>();
        builder.Services.AddTransient<GroupDetailsPage>();
        builder.Services.AddTransient<GroupChatPage>();
        builder.Services.AddTransient<AddGroupMemberPage>();
        builder.Services.AddTransient<SettingsPage>();
        builder.Services.AddTransient<MeetingHomePage>();
        builder.Services.AddTransient<MeetingCreatePage>();
        builder.Services.AddTransient<MeetingDetailPage>();
        builder.Services.AddTransient<WorkDeskDashboardPage>();
        builder.Services.AddTransient<WorkDeskTaskListPage>();
        builder.Services.AddTransient<WorkDeskCreateTaskPage>();
        builder.Services.AddTransient<WorkDeskApprovalsPage>();
        builder.Services.AddTransient<WorkDeskTeamPage>();
        builder.Services.AddTransient<MainShellPage>();
        builder.Services.AddTransient<DesktopCallsPage>();
        // ---------------------------------------------------------------
        // Shell
        // ---------------------------------------------------------------
        builder.Services.AddSingleton<AppShell>();

        var app = builder.Build();
#if WINDOWS
        try
        {
            AppNotificationManager.Default.Register();

            var notification = new AppNotificationBuilder()
                .AddText("Hello, you can now make calls and send messages with Sipcore!")
                .BuildNotification();

            AppNotificationManager.Default.Show(notification);
        }
        catch (Exception ex)
        {
            System.Diagnostics.Debug.WriteLine($"Windows notification failed: {ex}");
        }
#endif

        return app;
    }
}
