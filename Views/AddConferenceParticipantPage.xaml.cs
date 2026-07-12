using SipCoreMobile.Models;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class AddConferenceParticipantPage : ContentPage
{
    private readonly AppStateViewModel _viewModel;

    public AddConferenceParticipantPage(AppStateViewModel viewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        ApplyFilter("");
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e) => ApplyFilter(e.NewTextValue ?? "");

    private void ApplyFilter(string query)
    {
        var filtered = _viewModel.Contacts
            .Where(c => c.Extension.Contains(query, StringComparison.OrdinalIgnoreCase)
                     || c.DisplayName.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        ContactsList.ItemsSource = filtered;
        EmptyLabel.IsVisible = filtered.Count == 0;
        ContactsList.IsVisible = filtered.Count > 0;
    }

    private async void OnContactTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Grid { BindingContext: ContactUi contact })
        {
            await _viewModel.AddConferenceParticipantAsync(contact.Extension);
            await Navigation.PopModalAsync();
        }
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}
