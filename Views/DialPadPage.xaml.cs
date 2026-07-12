using SipCoreMobile.Models;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class DialPadPage : ContentPage
{
    private readonly AppStateViewModel _viewModel;
    private string _number = "";

    public DialPadPage(AppStateViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private void OnDrawerRequested(object? sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    private void OnKeyTapped(object? sender, string digit)
    {
        _number += digit;
        NumberDisplay.Text = _number;
    }

    private void OnKeyLongPressed(object? sender, string altChar)
    {
        _number += altChar;
        NumberDisplay.Text = _number;
    }

    private void OnBackspaceTapped(object? sender, EventArgs e)
    {
        if (_number.Length == 0) return;
        _number = _number[..^1];
        NumberDisplay.Text = _number;
    }

    private async void OnCallTapped(object? sender, TappedEventArgs e)
    {
        if (string.IsNullOrEmpty(_number)) return;
        await _viewModel.MakeOutgoingCallAsync(_number);
    }

    private async void OnVideoTapped(object? sender, EventArgs e)
    {
        await DisplayAlert("Video Calling",
            "Video calling is currently under development and will be available in a future SIPCore release.",
            "OK");
    }

    private void OnContactsTapped(object? sender, EventArgs e) => _viewModel.NavigateSafely(Screen.Contacts);
    private void OnMessagesTapped(object? sender, EventArgs e) => _viewModel.NavigateSafely(Screen.Conversations);
    private void OnHistoryTapped(object? sender, EventArgs e) => _viewModel.NavigateSafely(Screen.CallHistory);
}
