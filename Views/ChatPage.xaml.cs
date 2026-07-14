using System.Collections.Specialized;
using SipCoreMobile.Models;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

/// <summary>
/// Port of ChatScreen(). Deliberately scoped down from the 1,508-line original -- covers
/// text messaging (send/receive/status ticks/date-agnostic list), the New-Message recipient
/// entry, and a reply-context bar. NOT covered in this pass (see README caveats for this
/// batch): voice-note recording/playback, the long-press message context menu (reactions/
/// delete/reply), media/attachment message rendering, and per-day date-separator headers in
/// the message list.
/// </summary>
public partial class ChatPage : ContentPage
{
    private readonly AppStateViewModel _appState;
    private readonly ChatViewModel _chatViewModel;
    private ChatMessage? _replyingTo;
    public void RefreshHeaderPublic() => RefreshHeader();
    public ChatPage(AppStateViewModel appState, ChatViewModel chatViewModel)
    {
        InitializeComponent();
        _appState = appState;
        _chatViewModel = chatViewModel;
        BindingContext = chatViewModel;

        _chatViewModel.Messages.CollectionChanged += OnMessagesChanged;
        RefreshHeader();
    }

    protected override void OnDisappearing()
    {
        base.OnDisappearing();
        _chatViewModel.StopChatRefresh();
    }

    private void OnMessagesChanged(object? sender, NotifyCollectionChangedEventArgs e)
    {
        if (_chatViewModel.Messages.Count == 0) return;

        Dispatcher.Dispatch(() => MessagesList.ScrollTo(_chatViewModel.Messages.Count - 1, animate: true));
    }

    private void RefreshHeader()
    {
        var isNewMessage = string.IsNullOrEmpty(_chatViewModel.ActiveExtension);

        NewMessageHeader.IsVisible = isNewMessage;
        ConversationHeader.IsVisible = !isNewMessage;

        if (isNewMessage)
        {
            AvatarLabel.Text = "NM";
            return;
        }

        var extension = _chatViewModel.ActiveExtension;
        AvatarLabel.Text = extension.Length >= 2 ? extension[^2..] : extension;

        var contact = _appState.Contacts.FirstOrDefault(c => c.Extension == extension);
        ContactNameLabel.Text = contact is not null && !string.IsNullOrEmpty(contact.DisplayName)
            ? $"{contact.DisplayName} ({extension})"
            : extension;

        string statusText;
        Color statusColor;
        if (contact is null)
        {
            statusText = "Unknown";
            statusColor = Colors.Gray;
        }
        else if (!string.Equals(contact.Status, "Active", StringComparison.OrdinalIgnoreCase))
        {
            statusText = contact.Status;
            statusColor = Colors.Red;
        }
        else if (contact.IsOnline)
        {
            statusText = "Online";
            statusColor = (Color)Application.Current!.Resources["SIPCoreSuccess"];
        }
        else
        {
            statusText = "Offline";
            statusColor = Colors.Gray;
        }

        ContactStatusLabel.Text = statusText;
        ContactStatusLabel.TextColor = statusColor;
    }

    private async void OnBackClicked(object? sender, EventArgs e)
    {
        _chatViewModel.StopChatRefresh();
        await Navigation.PopAsync();
    }

    private async void OnNewRecipientUnfocused(object? sender, FocusEventArgs e)
    {
        var clean = NewRecipientEntry.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(clean)) return;

        await _chatViewModel.OpenConversationAsync(clean);
        RefreshHeader();
    }

    private async void OnCallClicked(object? sender, EventArgs e)
    {
        if (string.IsNullOrEmpty(_chatViewModel.ActiveExtension)) return;
        await _appState.MakeOutgoingCallAsync(_chatViewModel.ActiveExtension);
    }

    private async void OnSendTapped(object? sender, TappedEventArgs e)
    {
        var text = MessageEntry.Text?.Trim() ?? "";
        var to = string.IsNullOrEmpty(_chatViewModel.ActiveExtension)
            ? NewRecipientEntry.Text?.Trim() ?? ""
            : _chatViewModel.ActiveExtension;

        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(to)) return;

        MessageEntry.Text = "";
        await _chatViewModel.SendTextMessageAsync(to, text);
        _replyingTo = null;
        ReplyBar.IsVisible = false;
    }

    private void OnMessageTapped(object? sender, TappedEventArgs e)
    {
        if (sender is Border { BindingContext: ChatMessage message })
        {
            _replyingTo = message;
            ReplyBarPreview.Text = message.IsDeleted ? "This message was deleted" : message.Body;
            ReplyBar.IsVisible = true;
        }
    }

    private void OnCancelReplyClicked(object? sender, EventArgs e)
    {
        _replyingTo = null;
        ReplyBar.IsVisible = false;
    }

    private async void OnMicClicked(object? sender, EventArgs e)
    {
        // Voice-note recording/playback UI is deferred -- see class remarks.
        await DisplayAlert("Voice Notes", "Voice note recording is coming in a follow-up update.", "OK");
    }

    private async void OnAttachClicked(object? sender, EventArgs e)
    {
        // Attachment picking/upload is deferred -- see class remarks.
        await DisplayAlert("Attachments", "File attachments are coming in a follow-up update.", "OK");
    }

    private async void Button_Clicked(object sender, EventArgs e)
    {
        var text = MessageEntry.Text?.Trim() ?? "";
        var to = string.IsNullOrEmpty(_chatViewModel.ActiveExtension)
            ? NewRecipientEntry.Text?.Trim() ?? ""
            : _chatViewModel.ActiveExtension;

        if (string.IsNullOrEmpty(text) || string.IsNullOrEmpty(to)) return;

        MessageEntry.Text = "";
        await _chatViewModel.SendTextMessageAsync(to, text);
        _replyingTo = null;
        ReplyBar.IsVisible = false;
    }
}
