using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

/// <summary>
/// Port of SettingsScreen(). Confirmed against MainActivity.kt/SIPCoreApp.kt's actual dispatch:
/// only onChangePassword is wired to a real handler -- onActivateOutboundNumber/
/// onToggleVoicemail/onToggleCallRecording/onUpgradePremium all use their default no-op `= {}`
/// values, and outboundNumber/voicemailEnabled/callRecordingEnabled/isPremiumUser are read
/// from SipUiState but nothing in the codebase ever sets them non-default. So in the shipped
/// app, Outbound/Voicemail/Premium are permanently in their "inactive" visual state. This port
/// preserves that faithfully rather than inventing backend wiring that was never there --
/// Password is real, the other three tabs are local-only UI exactly like the original.
/// </summary>
public partial class SettingsPage : ContentPage
{
    private readonly AppStateViewModel _viewModel;

    public SettingsPage(AppStateViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        VoicemailSwitch.Title = "Enable Voicemail";
        VoicemailSwitch.Description = "Unanswered calls will be sent to voicemail.";
        CallRecordingSwitch.Title = "Enable Voice Recording";
        CallRecordingSwitch.Description = "Record incoming and outgoing calls.";
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ExtensionLabel.Text = $"Extension: {_viewModel.Extension}";

        // Matches state.outboundNumber always being blank in the shipped app (see class remarks).
        OutboundDescriptionLabel.Text = "Use a SIPCore activation code to enable outbound PSTN calling.";
        OutboundActiveState.IsVisible = false;
        OutboundInactiveState.IsVisible = true;

        // Matches isPremiumUser always being false in the shipped app (see class remarks).
        CallRecordingSwitch.IsVisible = false;
        UpgradePrompt.IsVisible = true;

        SelectTab(0);
    }

    private void SelectTab(int index)
    {
        PasswordTab.IsVisible = index == 0;
        OutboundTab.IsVisible = index == 1;
        VoicemailTab.IsVisible = index == 2;
        PremiumTab.IsVisible = index == 3;

        var selectedColor = (Color)Application.Current!.Resources["SIPCorePrimary"];
        var unselectedColor = (Color)Application.Current!.Resources["SIPCoreTextSecondary"];

        PasswordTabButton.TextColor = index == 0 ? selectedColor : unselectedColor;
        OutboundTabButton.TextColor = index == 1 ? selectedColor : unselectedColor;
        VoicemailTabButton.TextColor = index == 2 ? selectedColor : unselectedColor;
        PremiumTabButton.TextColor = index == 3 ? selectedColor : unselectedColor;
    }

    private void OnDrawerRequested(object? sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    private void OnPasswordTabClicked(object? sender, EventArgs e) => SelectTab(0);
    private void OnOutboundTabClicked(object? sender, EventArgs e) => SelectTab(1);
    private void OnVoicemailTabClicked(object? sender, EventArgs e) => SelectTab(2);
    private void OnPremiumTabClicked(object? sender, EventArgs e) => SelectTab(3);

    private async void OnChangePasswordClicked(object? sender, EventArgs e)
    {
        await _viewModel.ChangePasswordAsync(
            CurrentPasswordEntry.Text ?? "",
            NewPasswordEntry.Text ?? "",
            ConfirmPasswordEntry.Text ?? "");

        CurrentPasswordEntry.Text = "";
        NewPasswordEntry.Text = "";
        ConfirmPasswordEntry.Text = "";
    }

    /// <summary>
    /// Local-only, matching the original: onActivateOutboundNumber was never wired to a real
    /// handler, so activating here just clears the field the same way the Compose version did
    /// without actually calling any API.
    /// </summary>
    private void OnActivateOutboundClicked(object? sender, EventArgs e)
    {
        var code = ActivationCodeEntry.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(code)) return;

        ActivationCodeEntry.Text = "";
    }

    private void OnVoicemailToggled(object? sender, bool enabled)
    {
        // Local-only -- see class remarks.
    }

    private void OnCallRecordingToggled(object? sender, bool enabled)
    {
        // Local-only -- see class remarks.
    }

    private void OnUpgradePremiumClicked(object? sender, EventArgs e)
    {
        // Local-only -- see class remarks.
    }
}
