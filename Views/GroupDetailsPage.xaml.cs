using System.Collections.Specialized;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class GroupDetailsPage : ContentPage
{
    private readonly AppStateViewModel _appState;
    private readonly GroupsViewModel _groupsViewModel;

    public GroupDetailsPage(AppStateViewModel appState, GroupsViewModel groupsViewModel)
    {
        InitializeComponent();
        _appState = appState;
        _groupsViewModel = groupsViewModel;
        BindingContext = groupsViewModel;

        _groupsViewModel.SelectedGroupMembers.CollectionChanged += OnMembersChanged;
        Refresh();
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Refresh();
    }

    private void OnMembersChanged(object? sender, NotifyCollectionChangedEventArgs e) => Refresh();

    private void Refresh()
    {
        GroupNameLabel.Text = _groupsViewModel.SelectedGroupName;
        MemberCountLabel.Text = $"{_groupsViewModel.SelectedGroupMembers.Count} members";
        EmptyMembersLabel.IsVisible = _groupsViewModel.SelectedGroupMembers.Count == 0;
        MembersList.IsVisible = _groupsViewModel.SelectedGroupMembers.Count > 0;
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Navigation.PopAsync();

    private async void OnAddMemberClicked(object? sender, EventArgs e)
    {
        await Navigation.PushModalAsync(new AddGroupMemberPage(_appState, _groupsViewModel));
    }

    private async void OnRemoveMemberClicked(object? sender, EventArgs e)
    {
        if (sender is ImageButton { CommandParameter: string extension })
        {
            await _groupsViewModel.RemoveMemberFromGroupAsync(extension);
        }
    }

    private async void OnCallGroupClicked(object? sender, EventArgs e)
    {
        await _groupsViewModel.CallGroupAsync(_groupsViewModel.SelectedGroupId);
        _appState.NavigateSafely(Models.Screen.ActiveCall);
    }

    private async void OnGroupChatClicked(object? sender, EventArgs e)
    {
        await Navigation.PushAsync(new GroupChatPage(_groupsViewModel));
    }
}
