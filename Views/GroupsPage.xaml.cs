using System.Collections.Specialized;
using SipCoreMobile.Models;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class GroupsPage : ContentPage
{
    private readonly AppStateViewModel _appState;
    private readonly GroupsViewModel _groupsViewModel;

    public GroupsPage(AppStateViewModel appState, GroupsViewModel groupsViewModel)
    {
        InitializeComponent();
        _appState = appState;
        _groupsViewModel = groupsViewModel;
        BindingContext = groupsViewModel;

        _groupsViewModel.Groups.CollectionChanged += OnGroupsChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _groupsViewModel.LoadGroupsAsync();
        UpdateEmptyState();
    }

    private void OnGroupsChanged(object? sender, NotifyCollectionChangedEventArgs e) => UpdateEmptyState();

    private void UpdateEmptyState()
    {
        EmptyLabel.IsVisible = _groupsViewModel.Groups.Count == 0;
        GroupsList.IsVisible = _groupsViewModel.Groups.Count > 0;
    }

    private void OnDrawerRequested(object? sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    private async void OnCreateGroupTapped(object? sender, TappedEventArgs e)
    {
        var name = await DisplayPromptAsync("Create Group", "Group name");
        if (string.IsNullOrWhiteSpace(name)) return;

        await _groupsViewModel.CreateGroupAsync(name.Trim());
    }

    private async void OnGroupTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border { BindingContext: GroupUi group })
        {
            await _groupsViewModel.OpenGroupAsync(group.Id, group.Name);
            await Navigation.PushAsync(new GroupDetailsPage(_appState, _groupsViewModel));
        }
    }

    private void OnDialerTapped(object? sender, EventArgs e) => _appState.NavigateSafely(Screen.DialPad);
    private void OnContactsTapped(object? sender, EventArgs e) => _appState.NavigateSafely(Screen.Contacts);
    private void OnMessagesTapped(object? sender, EventArgs e) => _appState.NavigateSafely(Screen.Conversations);
    private void OnHistoryTapped(object? sender, EventArgs e) => _appState.NavigateSafely(Screen.CallHistory);
}
