using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class GroupChatPage : ContentPage
{
    private readonly GroupsViewModel _groupsViewModel;

    public GroupChatPage(GroupsViewModel groupsViewModel)
    {
        InitializeComponent();
        _groupsViewModel = groupsViewModel;
        BindingContext = groupsViewModel;

        GroupNameLabel.Text = _groupsViewModel.SelectedGroupName;
        MemberCountLabel.Text = $"{_groupsViewModel.SelectedGroupMembers.Count} members";
    }

    private async void OnBackClicked(object? sender, EventArgs e) => await Navigation.PopAsync();

    private async void OnSendTapped(object? sender, TappedEventArgs e)
    {
        var text = MessageEntry.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(text)) return;

        MessageEntry.Text = "";
        await _groupsViewModel.SendGroupMessageAsync(_groupsViewModel.SelectedGroupId, text);
    }
}
