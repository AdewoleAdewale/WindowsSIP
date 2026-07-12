using System.Collections.Specialized;
using SipCoreMobile.Controls;
using SipCoreMobile.Models;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class ContactsPage : ContentPage
{
    private readonly AppStateViewModel _viewModel;
    private readonly ChatViewModel _chatViewModel;

    public ContactsPage(AppStateViewModel viewModel, ChatViewModel chatViewModel)
    {
        InitializeComponent();
        _viewModel = viewModel;
        _chatViewModel = chatViewModel;
        BindingContext = viewModel;

        _viewModel.Contacts.CollectionChanged += OnContactsChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        Rebuild();
    }

    private void OnContactsChanged(object? sender, NotifyCollectionChangedEventArgs e) => Rebuild();
    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e) => Rebuild();

    /// <summary>Port of ContactsScreen's filtered/myCompanyContacts/externalContacts derivation.</summary>
    private void Rebuild()
    {
        var search = SearchBox.Text ?? "";

        var filtered = _viewModel.Contacts.Where(c =>
            c.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            c.Extension.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            c.CompanyName.Contains(search, StringComparison.OrdinalIgnoreCase) ||
            c.Status.Contains(search, StringComparison.OrdinalIgnoreCase)).ToList();

        EmptyLabel.IsVisible = filtered.Count == 0;
        ContactsList.IsVisible = filtered.Count > 0;

        var rows = new List<ContactListRowItem>();

        var myCompany = filtered.Where(c => !c.IsExternal).OrderBy(c => c.DisplayName).ToList();
        if (myCompany.Count > 0)
        {
            rows.Add(new ContactListRowItem { IsHeader = true, HeaderText = "My Company" });
            rows.AddRange(myCompany.Select(ToRow));
        }

        var external = filtered.Where(c => c.IsExternal)
            .GroupBy(c => string.IsNullOrWhiteSpace(c.CompanyName) ? "External Company" : c.CompanyName)
            .OrderBy(g => g.Key, StringComparer.Ordinal);

        foreach (var group in external)
        {
            rows.Add(new ContactListRowItem { IsHeader = true, HeaderText = group.Key });
            rows.AddRange(group.OrderBy(c => c.DisplayName).Select(ToRow));
        }

        ContactsList.ItemsSource = rows;
    }

    /// <summary>Port of contactDisplayName()/contactCleanName()/presence-text logic from ContactCard().</summary>
    private static ContactListRowItem ToRow(ContactUi contact)
    {
        var cleanExtension = contact.Extension.Trim();
        var cleanName = contact.DisplayName
            .Replace($"({contact.Extension})", "")
            .Replace($"({cleanExtension})", "")
            .Trim();
        if (string.IsNullOrEmpty(cleanName)) cleanName = cleanExtension;

        var displayName = $"{cleanName} ({cleanExtension})";
        var initial = cleanName.Length > 0 ? cleanName[..1].ToUpperInvariant() : "";

        var isOnline = contact.CanViewPresence && contact.IsOnline;

        string statusText;
        if (!contact.CanViewPresence)
        {
            statusText = "Presence hidden";
        }
        else
        {
            statusText = isOnline
                ? (string.IsNullOrEmpty(contact.Status) ? "Available" : contact.Status)
                : (string.IsNullOrEmpty(contact.Status) ? "Offline" : contact.Status);
        }

        var statusColor = isOnline
            ? (Color)Application.Current!.Resources["SIPCoreSuccess"]
            : Colors.Gray;

        var showCompanySubtitle = !string.IsNullOrWhiteSpace(contact.CompanyName);
        var companySubtitle = contact.IsExternal
            ? $"{contact.CompanyName} • External Company"
            : contact.CompanyName;

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

    private void OnDrawerRequested(object? sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    private async void OnAddContactTapped(object? sender, TappedEventArgs e)
    {
        var name = await DisplayPromptAsync("Create Contact", "Name", placeholder: "e.g. John Doe");
        if (string.IsNullOrWhiteSpace(name)) return;

        var extension = await DisplayPromptAsync("Create Contact", "Extension", placeholder: "e.g. 1001", keyboard: Keyboard.Numeric);
        if (string.IsNullOrWhiteSpace(extension)) return;

        await _viewModel.CreateContactAsync(extension.Trim(), name.Trim());
    }

    private async void OnCallContactClicked(object? sender, EventArgs e)
    {
        if (sender is ImageButton { CommandParameter: ContactUi contact })
        {
            await _viewModel.MakeOutgoingCallAsync(contact.Extension);
        }
    }

    private async void OnMessageContactClicked(object? sender, EventArgs e)
    {
        if (sender is ImageButton { CommandParameter: ContactUi contact })
        {
            await _chatViewModel.OpenConversationAsync(contact.Extension);
            _viewModel.NavigateSafely(Screen.Chat);
            await Navigation.PushAsync(new ChatPage(_viewModel, _chatViewModel));
        }
    }

    private void OnDialerTapped(object? sender, EventArgs e) => _viewModel.NavigateSafely(Screen.DialPad);
    private void OnMessagesTapped(object? sender, EventArgs e) => _viewModel.NavigateSafely(Screen.Conversations);
    private void OnHistoryTapped(object? sender, EventArgs e) => _viewModel.NavigateSafely(Screen.CallHistory);
}
