using System;
using Microsoft.Maui.Controls;
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

    private const string Domain = "4.206.202.181";

    private async void OnLoginClicked(object? sender, EventArgs e)
    {
        if (_viewModel.IsLoggingIn) return;

        // Perform basic verification before invoking view model mechanics
        if (string.IsNullOrWhiteSpace(ExtensionEntry.Text) || string.IsNullOrWhiteSpace(PasswordEntry.Text))
        {
            _viewModel.Status = "Extension and Password cannot be blank.";
            return;
        }

        // Optional: Save extension info locally here if RememberMeCheck.IsChecked == true

        await _viewModel.LoginAsync(ExtensionEntry.Text.Trim(), PasswordEntry.Text, Domain);
    }

    /// <summary>
    /// Implements standard Windows input behavior allowing users to inspect their typed input.
    /// </summary>
    private void OnTogglePasswordClicked(object sender, EventArgs e)
    {
        if (sender is Button toggleButton)
        {
            PasswordEntry.IsPassword = !PasswordEntry.IsPassword;
            toggleButton.Text = PasswordEntry.IsPassword ? "👁" : "🙈";
        }
    }
}