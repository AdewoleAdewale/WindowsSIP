using SipCoreMobile.Controls;
using SipCoreMobile.Models;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class WorkDeskTeamPage : ContentPage
{
    private readonly AppStateViewModel _appState;
    private readonly ChatViewModel _chatViewModel;

    public WorkDeskTeamPage(AppStateViewModel appState, ChatViewModel chatViewModel)
    {
        InitializeComponent();
        _appState = appState;
        _chatViewModel = chatViewModel;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Render();
    }

    /// <summary>Port of WorkDeskTeamView's myCompanyContacts/externalContacts grouping.</summary>
    private void Render()
    {
        var contacts = _appState.Contacts.Where(c => c.CanUseWorkDesk).ToList();

        SubtitleLabel.Text = $"{contacts.Count} WorkDesk contacts";
        EmptyLabel.IsVisible = contacts.Count == 0;
        TeamList.IsVisible = contacts.Count > 0;

        var rows = new List<ContactListRowItem>();

        var myCompany = contacts.Where(c => !c.IsExternal).OrderBy(c => c.DisplayName).ToList();
        if (myCompany.Count > 0)
        {
            rows.Add(new ContactListRowItem { IsHeader = true, HeaderText = "My Company" });
            rows.AddRange(myCompany.Select(ToRow));
        }

        var external = contacts.Where(c => c.IsExternal)
            .GroupBy(c => string.IsNullOrWhiteSpace(c.CompanyName) ? "External Company" : c.CompanyName)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        foreach (var group in external)
        {
            rows.Add(new ContactListRowItem { IsHeader = true, HeaderText = group.Key });
            rows.AddRange(group.OrderBy(c => c.DisplayName).Select(ToRow));
        }

        TeamList.ItemsSource = rows;
    }

    /// <summary>Port of workDeskContactDisplayName() and WorkDeskTeamMemberRow's presence logic.</summary>
    private static ContactListRowItem ToRow(ContactUi contact)
    {
        var cleanExtension = contact.Extension.Trim();
        var cleanName = contact.DisplayName.Replace($"({contact.Extension})", "").Replace($"({cleanExtension})", "").Trim();
        if (string.IsNullOrEmpty(cleanName)) cleanName = cleanExtension;

        var displayName = $"{cleanName} ({cleanExtension})";
        var initial = cleanName.Length > 0 ? cleanName[..1].ToUpperInvariant() : "";

        var onlineStatuses = new[] { "Online", "Available", "Registered", "Active", "Reachable", "Ready", "Connected" };
        var isOnline = contact.CanViewPresence && onlineStatuses.Any(s => string.Equals(contact.Status, s, StringComparison.OrdinalIgnoreCase));

        var statusText = contact.CanViewPresence ? (string.IsNullOrEmpty(contact.Status) ? "Offline" : contact.Status) : "Presence hidden";
        var statusColor = isOnline ? Color.FromArgb("#22C55E") : Color.FromArgb("#99FFFFFF");

        var showCompanySubtitle = !string.IsNullOrWhiteSpace(contact.CompanyName);
        var companySubtitle = contact.IsExternal ? $"{contact.CompanyName} • External" : contact.CompanyName;

        return new ContactListRowItem
        {
            Contact = contact,
            DisplayName = displayName,
            Initial = initial,
            IsOnline = isOnline,
            StatusText = statusText,
            StatusColor = statusColor,
            CompanySubtitle = companySubtitle,
            ShowCompanySubtitle = showCompanySubtitle
        };
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Navigation.PopAsync();

    private async void OnCallContactClicked(object? sender, EventArgs e)
    {
        if (sender is ImageButton { CommandParameter: ContactUi contact })
        {
            await _appState.MakeOutgoingCallAsync(contact.Extension);
        }
    }

    private async void OnMessageContactClicked(object? sender, EventArgs e)
    {
        if (sender is ImageButton { CommandParameter: ContactUi contact })
        {
            await _chatViewModel.OpenConversationAsync(contact.Extension);
            await Navigation.PushAsync(new ChatPage(_appState, _chatViewModel));
        }
    }
}
