using SipCoreMobile.Services.Sip;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

/// <summary>
/// Port of IncomingCallScreen(). Note this supersedes the standalone IncomingCallViewModel
/// built in the calling-infra batch (Batch 3) -- with AppStateViewModel now owning
/// AnswerIncomingCallAsync()/RejectIncomingCall() (mirroring how MainActivity.kt itself
/// held answerIncomingCall()/rejectIncomingCall(), with IncomingCallActivity.kt only calling
/// into them via SipCoreHolder.manager), this page binds directly to AppStateViewModel rather
/// than the separate ViewModel. Keep IncomingCallViewModel only if you want a genuinely
/// separate always-on-top Window (see its class remarks); otherwise it's redundant with this.
/// </summary>
public partial class IncomingCallPage : ContentPage
{
    private readonly AppStateViewModel _viewModel;

    public IncomingCallPage(AppStateViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private async void OnAnswerTapped(object? sender, TappedEventArgs e)
    {
        RingbackPlayer.Stop();
        await _viewModel.AnswerIncomingCallAsync();
    }

    private void OnRejectTapped(object? sender, TappedEventArgs e)
    {
        RingbackPlayer.Stop();
        _viewModel.RejectIncomingCall();
    }
}
