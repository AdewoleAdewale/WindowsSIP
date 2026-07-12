using System.ComponentModel;
using Microsoft.Extensions.DependencyInjection;
using SipCoreMobile.Models;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Controls;

public partial class SIPCoreDrawerContent : ContentView
{
    private AppStateViewModel? _viewModel;

    public SIPCoreDrawerContent()
    {
        InitializeComponent();
        BindingContextChanged += OnBindingContextChanged;
    }

    private void OnBindingContextChanged(object? sender, EventArgs e)
    {
        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged -= OnViewModelPropertyChanged;
        }

        _viewModel = BindingContext as AppStateViewModel;

        if (_viewModel is not null)
        {
            _viewModel.PropertyChanged += OnViewModelPropertyChanged;
            Refresh();
        }
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e) => Refresh();

    /// <summary>Port of the status-card text/color logic and each NavigationDrawerItem's `selected` expression.</summary>
    private void Refresh()
    {
        if (_viewModel is null) return;

        ExtensionLabel.Text = string.IsNullOrEmpty(_viewModel.Extension) ? "-" : _viewModel.Extension;

        var successColor = (Color)Application.Current!.Resources["SIPCoreSuccess"];
        var dangerColor = (Color)Application.Current!.Resources["SIPCoreDanger"];

        StatusDot.IsLive = _viewModel.Registered;
        RegisteredLabel.Text = _viewModel.Registered ? "Registered" : "Not Registered";
        RegisteredLabel.TextColor = _viewModel.Registered ? successColor : dangerColor;

        var status = _viewModel.Status;
        var showExtraStatus = !string.IsNullOrEmpty(status) && status != "Registered" && status != "Not Registered";
        ExtraStatusLabel.IsVisible = showExtraStatus;
        ExtraStatusLabel.Text = status;

        var screen = _viewModel.CurrentScreen;
        HomeItem.IsSelected = screen == Screen.Home;
        GroupsItem.IsSelected = screen == Screen.Groups;
        EmailItem.IsSelected = screen == Screen.Email;
        MeetingsItem.IsSelected = screen is Screen.MeetingHome or Screen.MeetingCreate or Screen.MeetingDetail;
        WorkDeskItem.IsSelected = screen is Screen.WorkDesk or Screen.WorkDeskTask or Screen.WorkDeskCreateTask
            or Screen.WorkDeskApprovals or Screen.WorkDeskEditTask;
        // Original: Profile/Settings NavigationDrawerItems always had selected = false (dead code, preserved as-is).
        ProfileItem.IsSelected = false;
        SettingsItem.IsSelected = false;
    }

    /// <summary>Port of closeThen(): close the drawer, then perform the navigation.</summary>
    private void CloseThen(Action action)
    {
        Shell.Current.FlyoutIsPresented = false;
        action();
    }

    private void OnHomeTapped(object? sender, EventArgs e) => CloseThen(() => _viewModel?.NavigateSafely(Screen.Home));
    private void OnGroupsTapped(object? sender, EventArgs e) => CloseThen(() => _viewModel?.NavigateSafely(Screen.Groups));
    private void OnEmailTapped(object? sender, EventArgs e) => CloseThen(() => _viewModel?.NavigateSafely(Screen.Email));
    private void OnMeetingsTapped(object? sender, EventArgs e) => CloseThen(() => _viewModel?.NavigateSafely(Screen.MeetingHome));
    private void OnWorkDeskTapped(object? sender, EventArgs e) => CloseThen(async () =>
    {
        var services = Application.Current?.Handler?.MauiContext?.Services;
        var page = services?.GetService(typeof(Views.WorkDeskDashboardPage)) as Page;
        var hostPage = Application.Current?.Windows.Count > 0 ? Application.Current.Windows[0].Page : null;

        if (page is not null && hostPage is not null)
        {
            await hostPage.Navigation.PushAsync(page);
        }
        else
        {
            _viewModel?.NavigateSafely(Screen.WorkDesk);
        }
    });
    private void OnProfileTapped(object? sender, EventArgs e) => CloseThen(() => _viewModel?.NavigateSafely(Screen.Profile));
    private void OnSettingsTapped(object? sender, EventArgs e) => CloseThen(() => _viewModel?.NavigateSafely(Screen.Settings));

    private void OnLogoutTapped(object? sender, EventArgs e) => CloseThen(async () =>
    {
        if (_viewModel is null) return;

        var confirmed = await Application.Current!.Windows[0].Page!.DisplayAlert(
            "Logout", "Are you sure you want to log out?", "Logout", "Cancel");

        if (confirmed) await _viewModel.LogoutAsync();
    });
}
