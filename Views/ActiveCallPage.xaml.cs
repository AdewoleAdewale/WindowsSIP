using System.Collections.Specialized;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class ActiveCallPage : ContentPage
{
    private readonly AppStateViewModel _viewModel;

    private bool _muted;
    private bool _held;
    private bool _speaker;
    private bool _showDialpad;

    public ActiveCallPage(AppStateViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

        _viewModel.ConferenceParticipants.CollectionChanged += OnConferenceParticipantsChanged;
        UpdateConferenceCardVisibility();
    }

    private void OnConferenceParticipantsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        UpdateConferenceCardVisibility();

    private void UpdateConferenceCardVisibility() =>
        ConferenceCard.IsVisible = _viewModel.ConferenceParticipants.Count > 0;

    private void OnMuteTapped(object? sender, EventArgs e)
    {
        _muted = !_muted;
        MuteButton.IsActive = _muted;
        MuteButton.Label = _muted ? "Unmute" : "Mute";
        _viewModel.Mute(_muted);
    }

    private void OnSpeakerTapped(object? sender, EventArgs e)
    {
        _speaker = !_speaker;
        SpeakerButton.IsActive = _speaker;
        SpeakerButton.Label = _speaker ? "Speaker Off" : "Speaker";
        _viewModel.Speaker(_speaker);
    }

    private async void OnHoldTapped(object? sender, EventArgs e)
    {
        _held = !_held;
        HoldButton.IsActive = _held;
        HoldButton.Label = _held ? "Resume" : "Hold";
        await _viewModel.HoldAsync(_held);
    }

    private void OnKeypadTapped(object? sender, EventArgs e)
    {
        _showDialpad = !_showDialpad;
        KeypadButton.IsActive = _showDialpad;
        Dialpad.IsVisible = _showDialpad;
    }

    private void OnDtmfDigitPressed(object? sender, string digit) => _viewModel.SendDtmf(digit);

    private async void OnAddParticipantClicked(object? sender, EventArgs e)
    {
        var picker = new AddConferenceParticipantPage(_viewModel);
        await Navigation.PushModalAsync(picker);
    }

    private void OnRemoveParticipantClicked(object? sender, EventArgs e)
    {
        if (sender is ImageButton { CommandParameter: string participant })
        {
            _viewModel.RemoveConferenceParticipant(participant);
        }
    }

    private void OnHangupTapped(object? sender, TappedEventArgs e) => _viewModel.HangupCall();
}
