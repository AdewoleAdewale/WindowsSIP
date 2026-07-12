using SipCoreMobile.Models;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class AddGroupMemberPage : ContentPage
{
    private readonly AppStateViewModel _appState;
    private readonly GroupsViewModel _groupsViewModel;

    public AddGroupMemberPage(AppStateViewModel appState, GroupsViewModel groupsViewModel)
    {
        InitializeComponent();
        _appState = appState;
        _groupsViewModel = groupsViewModel;
        ApplyFilter("");
    }

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e) => ApplyFilter(e.NewTextValue ?? "");

    private void ApplyFilter(string query)
    {
        var memberExtensions = _groupsViewModel.SelectedGroupMembers.Select(m => m.Extension).ToHashSet();

        var filtered = _appState.Contacts
            .Where(c => !memberExtensions.Contains(c.Extension))
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
            await _groupsViewModel.AddMemberToGroupAsync(contact.Extension);
            await Navigation.PopModalAsync();
        }
    }

    private async void OnCloseClicked(object? sender, EventArgs e) => await Navigation.PopModalAsync();
}
