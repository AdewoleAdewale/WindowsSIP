using SipCoreMobile.Models;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class ProfilePage : ContentPage
{
    private readonly AppStateViewModel _viewModel;

    public ProfilePage(AppStateViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadWorkTaskCreatorsAsync();
        Refresh();
    }

    /// <summary>Port of ProfileScreen's top-of-function derived values (extension/companyName/
    /// displayName fallbacks and the workDeskAccess computation).</summary>
    private void Refresh()
    {
        var extension = string.IsNullOrEmpty(_viewModel.Extension) ? "Not logged in" : _viewModel.Extension;
        var companyName = string.IsNullOrEmpty(_viewModel.CompanyName) ? "No company assigned" : _viewModel.CompanyName;
        var displayName = string.IsNullOrEmpty(_viewModel.DisplayName) ? "SIPCore User" : _viewModel.DisplayName;

        var creatorRecord = _viewModel.WorkTaskCreators.FirstOrDefault(c =>
            string.Equals(c.ExtensionNumber.Trim(), _viewModel.Extension.Trim(), StringComparison.OrdinalIgnoreCase));

        string workDeskAccess;
        if (creatorRecord is { IsActive: true, CanCreateTasks: true })
            workDeskAccess = "Creator";
        else if (_viewModel.CompanyId > 0)
            workDeskAccess = "Self Task Only";
        else
            workDeskAccess = "No WorkDesk Access";

        AvatarLabel.Text = extension.Length >= 2 ? extension[^2..] : (extension == "Not logged in" ? "SC" : extension);
        NameLabel.Text = displayName;
        ExtensionLabel.Text = $"Extension {extension}";
        CompanyBadgeLabel.Text = companyName;

        NameRow.Value = displayName;
        ExtensionRow.Value = extension;
        CompanyRow.Value = companyName;
        DomainRow.Value = "softworks-us.net";
        StatusRow.Value = string.IsNullOrEmpty(_viewModel.Status) ? "Unknown" : _viewModel.Status;
        ProductRow.Value = "SIPCore Softphone";

        WorkDeskAccessRow.Value = workDeskAccess;
    }

    private void OnDrawerRequested(object? sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    private void OnBackToDialerClicked(object? sender, EventArgs e) => _viewModel.NavigateSafely(Screen.DialPad);

    private async void OnLogoutClicked(object? sender, EventArgs e)
    {
        var confirmed = await DisplayAlert("Logout", "Are you sure you want to log out?", "Logout", "Cancel");
        if (!confirmed) return;

        await _viewModel.LogoutAsync();
    }
}
