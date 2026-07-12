using System.ComponentModel;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class MeetingDetailPage : ContentPage
{
    private readonly AppStateViewModel _appState;
    private readonly MeetingsViewModel _viewModel;

    public MeetingDetailPage(AppStateViewModel appState, MeetingsViewModel viewModel)
    {
        InitializeComponent();
        _appState = appState;
        _viewModel = viewModel;
        BindingContext = viewModel;

        _viewModel.PropertyChanged += OnViewModelPropertyChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
    }

    private void OnViewModelPropertyChanged(object? sender, PropertyChangedEventArgs e)
    {
        if (e.PropertyName is nameof(MeetingsViewModel.SelectedMeeting) or nameof(MeetingsViewModel.LiveMembersText))
        {
            Refresh();
        }
    }

    /// <summary>Port of MeetingDetailView's title/number/status/isHost derived values.</summary>
    private void Refresh()
    {
        var meeting = _viewModel.SelectedMeeting;

        var title = string.IsNullOrWhiteSpace(meeting?.Title) ? "SIPCore Meeting" : meeting.Title;
        var number = string.IsNullOrWhiteSpace(meeting?.ConferenceNumber)
            ? _appState.SelectedMeetingConferenceNumber
            : meeting.ConferenceNumber;
        var status = meeting?.Status ?? "";
        var isHost = meeting?.CreatedByExtension == _appState.Extension;

        TitleLabel.Text = title;
        MeetingIdLabel.Text = $"Meeting ID: {_appState.SelectedMeetingId}";
        ConferenceNumberLabel.Text = string.IsNullOrEmpty(number) ? "-" : number;
        StatusBadge.Status = status;
        ParticipantPinLabel.Text = $"Participant PIN: {meeting?.ParticipantPin ?? "-"}";

        HostControls.IsVisible = isHost;

        LiveMembersLabel.Text = string.IsNullOrWhiteSpace(_viewModel.LiveMembersText)
            ? "No live member data yet."
            : _viewModel.LiveMembersText;
    }

    private async void OnJoinClicked(object? sender, EventArgs e)
    {
        var number = string.IsNullOrWhiteSpace(_viewModel.SelectedMeeting?.ConferenceNumber)
            ? _appState.SelectedMeetingConferenceNumber
            : _viewModel.SelectedMeeting.ConferenceNumber;

        await _viewModel.JoinMeetingAsync(number);
    }

    private async void OnStartClicked(object? sender, EventArgs e) => await _viewModel.StartMeetingAsync();
    private async void OnEndClicked(object? sender, EventArgs e) => await _viewModel.EndMeetingAsync();
    private async void OnLockClicked(object? sender, EventArgs e) => await _viewModel.LockMeetingAsync();
    private async void OnUnlockClicked(object? sender, EventArgs e) => await _viewModel.UnlockMeetingAsync();
    private async void OnRefreshLiveClicked(object? sender, EventArgs e) => await _viewModel.RefreshMeetingLiveAsync();

    private async void OnBackClicked(object? sender, EventArgs e) => await Navigation.PopAsync();
}
