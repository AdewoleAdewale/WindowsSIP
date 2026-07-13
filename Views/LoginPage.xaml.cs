using Microsoft.UI.Xaml.Controls.Primitives;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class LoginPage : ContentPage
{
    private const string Domain = "4.206.202.181";
    private const double HeroCollapseWidth = 860;

    private readonly AppStateViewModel _viewModel;

    public LoginPage(AppStateViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        BindingContext = viewModel;

     
        SizeChanged += OnPageSizeChanged;
    }

    /// <summary>Hide the hero pane on narrow windows so the form gets full width.</summary>
    private void OnPageSizeChanged(object? sender, EventArgs e)
    {
        var narrow = Width > 0 && Width < HeroCollapseWidth;
        HeroPane.IsVisible = !narrow;
        RootGrid.ColumnDefinitions[0].Width = narrow ? new GridLength(0) : GridLength.Star;
        RootGrid.ColumnDefinitions[1].Width = narrow ? GridLength.Star : new GridLength(460);
    }

    /// <summary>Fluent-style focus ring: accent border while the field has focus.</summary>
    private void OnFieldFocusChanged(object? sender, FocusEventArgs e)
    {
        var border = sender == ExtensionEntry ? ExtensionBorder : PasswordBorder;
        border.Stroke = e.IsFocused
            ? (Color)Application.Current!.Resources["SIPCorePrimary"]
            : (Color)Application.Current!.Resources["SIPCoreBorder"];
    }

    private void OnTogglePasswordClicked(object? sender, EventArgs e)
    {
        PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
        RevealButton.Text = PasswordEntry.IsPassword ? "👁" : "🙈";
    }

    private void OnRememberMeLabelTapped(object? sender, EventArgs e) =>
        RememberMeCheck.IsChecked = !RememberMeCheck.IsChecked;

    private void OnExtensionCompleted(object? sender, EventArgs e) =>
        PasswordEntry.Focus();

    private async void OnPasswordCompleted(object? sender, EventArgs e) =>
        await AttemptLoginAsync();

    private async void OnLoginClicked(object? sender, EventArgs e) =>
        await AttemptLoginAsync();

    private async Task AttemptLoginAsync()
    {
        if (_viewModel.IsLoggingIn) return;

        var extension = ExtensionEntry.Text?.Trim() ?? "";
        var password = PasswordEntry.Text ?? "";

        if (string.IsNullOrEmpty(extension) || string.IsNullOrEmpty(password))
        {
            await DisplayAlert("Missing details", "Enter your extension and password to sign in.", "OK");
            return;
        }

        await _viewModel.LoginAsync(extension, password, Domain);
    }
}