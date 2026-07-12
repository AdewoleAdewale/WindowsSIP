using SipCoreMobile.Models;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class HomePage : ContentPage
{
    private readonly AppStateViewModel _viewModel;

    public HomePage(AppStateViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    private void OnDrawerRequested(object? sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    private void OnOpenWorkDeskTapped(object? sender, TappedEventArgs e) =>
        _viewModel.NavigateSafely(Screen.WorkDesk); // page not built yet -- see AppShell.xaml.cs Routes

    private void OnDialerTapped(object? sender, EventArgs e) => _viewModel.NavigateSafely(Screen.DialPad);
    private void OnContactsTapped(object? sender, EventArgs e) => _viewModel.NavigateSafely(Screen.Contacts);
    private void OnMessagesTapped(object? sender, EventArgs e) => _viewModel.NavigateSafely(Screen.Conversations);
    private void OnHistoryTapped(object? sender, EventArgs e) => _viewModel.NavigateSafely(Screen.CallHistory);
}
