using System.Collections.Specialized;
using SipCoreMobile.Models;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class ConversationsPage : ContentPage
{
    private readonly AppStateViewModel _appState;
    private readonly ChatViewModel _chatViewModel;

    public ConversationsPage(AppStateViewModel appState, ChatViewModel chatViewModel)
    {
        InitializeComponent();
        _appState = appState;
        _chatViewModel = chatViewModel;
        BindingContext = appState;

        _appState.Conversations.CollectionChanged += OnConversationsChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        ApplyFilter(SearchBox.Text ?? "");
    }

    private void OnConversationsChanged(object? sender, NotifyCollectionChangedEventArgs e) =>
        ApplyFilter(SearchBox.Text ?? "");

    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e) => ApplyFilter(e.NewTextValue ?? "");

    /// <summary>Port of ConversationsScreen's `filtered` derivation (search + sort by lastMessageAt).</summary>
    private void ApplyFilter(string search)
    {
        bool DisplayNameMatches(ConversationUi c)
        {
            var contact = _appState.Contacts.FirstOrDefault(x => x.Extension == c.Extension);
            var name = contact is not null && !string.IsNullOrEmpty(contact.DisplayName)
                ? $"{contact.DisplayName} ({c.Extension})"
                : c.Extension;
            return name.Contains(search, StringComparison.OrdinalIgnoreCase);
        }

        var filtered = _appState.Conversations
            .Where(c => c.Extension.Contains(search, StringComparison.OrdinalIgnoreCase)
                     || c.LastMessage.Contains(search, StringComparison.OrdinalIgnoreCase)
                     || DisplayNameMatches(c))
            .OrderByDescending(c => NormalizeTime(c.LastMessageAt))
            .ToList();

        ConversationsList.ItemsSource = filtered;
        EmptyLabel.IsVisible = filtered.Count == 0;
        ConversationsList.IsVisible = filtered.Count > 0;
    }

    /// <summary>Port of ConversationsScreen's normalizedConversationTime() (seconds → ms heuristic).</summary>
    private static long NormalizeTime(long raw) => raw <= 0 ? 0 : (raw < 10_000_000_000L ? raw * 1000 : raw);

    private void OnDrawerRequested(object? sender, EventArgs e)
    {
        Shell.Current.FlyoutIsPresented = true;
    }

    private async void OnConversationTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border { BindingContext: ConversationUi conversation })
        {
            await _chatViewModel.OpenConversationAsync(conversation.Extension);
            _appState.NavigateSafely(Screen.Chat);
            await Navigation.PushAsync(new ChatPage(_appState, _chatViewModel));
        }
    }

    private async void OnComposeTapped(object? sender, TappedEventArgs e)
    {
        _chatViewModel.StartNewMessage();
        await Navigation.PushAsync(new ChatPage(_appState, _chatViewModel));
    }

    private void OnDialerTapped(object? sender, EventArgs e) => _appState.NavigateSafely(Screen.DialPad);
    private void OnContactsTapped(object? sender, EventArgs e) => _appState.NavigateSafely(Screen.Contacts);
    private void OnHistoryTapped(object? sender, EventArgs e) => _appState.NavigateSafely(Screen.CallHistory);
}
