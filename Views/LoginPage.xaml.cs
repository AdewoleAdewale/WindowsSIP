using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class LoginPage : ContentPage
{
    private readonly AppStateViewModel _viewModel;

    public LoginPage(AppStateViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;
    }

    /// <summary>
    /// Original hardcoded domain = "4.206.202.181" as a local val inside LoginScreen
    /// (not a user-editable field) -- preserved here rather than exposing a domain Entry.
    /// </summary>
    private const string Domain = "4.206.202.181";

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        if (_viewModel.IsLoggingIn) return;

        await _viewModel.LoginAsync(ExtensionEntry.Text ?? "", PasswordEntry.Text ?? "", Domain);
    }
}
