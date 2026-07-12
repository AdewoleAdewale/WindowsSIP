using SipCoreMobile.Models;
using SipCoreMobile.Models.Api;
using SipCoreMobile.Services;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

/// <summary>
/// Port of WorkDeskTaskDiscussionView() -- the core comment/reply/attachment flow. Deliberately
/// scoped down from the 1,592-line original (same treatment as ChatPage in Batch 6):
/// - Tap-to-reply instead of long-press (matches the simpler interaction pattern used
///   elsewhere in this port rather than Compose's combinedClickable long-press menu)
/// - No comment options dialog (copy/share/delete) -- original's
///   WorkDeskDiscussionCommentOptionsDialog isn't ported
/// - No attachment options dialog (share/open) -- attachments open directly on tap instead
/// - No @mention autocomplete picker (WorkDeskMentionPicker) -- plain text entry only
/// - No URL linkification in comment text (WorkDeskLinkifiedDiscussionText) -- plain text
/// All of the above are candidates for a future polish pass if you need them.
/// </summary>
public partial class WorkDeskDiscussionPage : ContentPage
{
    private readonly AppStateViewModel _appState;
    private readonly WorkDeskViewModel _viewModel;
    private WorkTaskDto _task;
    private WorkTaskCommentDto? _replyingTo;

    public WorkDeskDiscussionPage(AppStateViewModel appState, WorkDeskViewModel viewModel, WorkTaskDto task)
    {
        InitializeComponent();
        _appState = appState;
        _viewModel = viewModel;
        _task = task;

        TitleLabel.Text = string.IsNullOrWhiteSpace(task.Title) ? "Untitled Task" : task.Title;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadTaskDetailAsync(_task);
        Render();
    }

    private void Render()
    {
        var detail = _viewModel.SelectedTaskDetail;
        if (detail is null) return;

        _task = detail.Task;
        var contacts = _appState.Contacts.ToList();

        RenderAttachments(detail.Attachments, contacts);
        RenderComments(detail.Comments, contacts);
    }

    private void RenderAttachments(List<WorkTaskAttachmentDto> attachments, List<ContactUi> contacts)
    {
        AttachmentsSection.Children.Clear();
        if (attachments.Count == 0) return;

        AttachmentsSection.Children.Add(new Label { Text = "Attachments", TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 14 });

        foreach (var attachment in attachments)
        {
            var row = new Border
            {
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
                BackgroundColor = Color.FromArgb("#14FFFFFF"),
                Padding = new Thickness(12)
            };

            var grid = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10 };
            grid.Add(new Label { Text = "\uF0C6", FontFamily = "MaterialIcons", TextColor = Color.FromArgb("#0057B8"), VerticalOptions = LayoutOptions.Center }, 0);

            var textStack = new VerticalStackLayout();
            textStack.Add(new Label { Text = string.IsNullOrWhiteSpace(attachment.FileName) ? "Attachment" : attachment.FileName, TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 13 });
            textStack.Add(new Label { Text = $"Shared by {WorkDeskContactHelpers.DisplayName(attachment.UploadedByExtension, contacts)}", TextColor = Color.FromArgb("#8CFFFFFF"), FontSize = 11 });
            grid.Add(textStack, 1);

            row.Content = grid;

            var url = attachment.FileUrl;
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) => { if (!string.IsNullOrWhiteSpace(url)) await _viewModel.OpenAttachmentAsync(url); };
            row.GestureRecognizers.Add(tap);

            AttachmentsSection.Children.Add(row);
        }
    }

    private void RenderComments(List<WorkTaskCommentDto> comments, List<ContactUi> contacts)
    {
        ThreadsList.Children.Clear();

        var topLevel = comments.Where(c => string.IsNullOrEmpty(c.ParentCommentId)).ToList();
        EmptyCommentsLabel.IsVisible = topLevel.Count == 0;

        foreach (var comment in topLevel)
        {
            ThreadsList.Children.Add(BuildBubble(comment, contacts, isReply: false));

            foreach (var reply in comment.Replies)
            {
                var replyRow = new HorizontalStackLayout { Spacing = 8 };
                replyRow.Add(new Label { Text = "\u21B3", TextColor = Color.FromArgb("#DC2626"), FontSize = 16, WidthRequest = 24 });
                replyRow.Add(BuildBubble(reply, contacts, isReply: true));
                ThreadsList.Children.Add(replyRow);
            }
        }

        Dispatcher.Dispatch(async () => { await Task.Delay(100); await CommentsScroll.ScrollToAsync(0, CommentsList.Height, true); });
    }

    private View BuildBubble(WorkTaskCommentDto comment, List<ContactUi> contacts, bool isReply)
    {
        var senderExtension = string.IsNullOrEmpty(comment.ExtensionNumber) ? _appState.Extension : comment.ExtensionNumber;
        var isMine = string.Equals(senderExtension.Trim(), _appState.Extension.Trim(), StringComparison.OrdinalIgnoreCase);
        var companyLine = WorkDeskContactHelpers.CompanyLine(senderExtension, contacts);

        var bubble = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 16 },
            BackgroundColor = isMine ? (Color)Application.Current!.Resources["SIPCorePrimary"] : Color.FromArgb("#1AFFFFFF"),
            Padding = new Thickness(12),
            MaximumWidthRequest = isReply ? 260 : 300,
            HorizontalOptions = isMine ? LayoutOptions.End : LayoutOptions.Start
        };

        var stack = new VerticalStackLayout { Spacing = 4 };
        stack.Add(new Label { Text = WorkDeskContactHelpers.DisplayName(senderExtension, contacts), TextColor = Color.FromArgb("#C7FFFFFF"), FontAttributes = FontAttributes.Bold, FontSize = 11 });
        if (!string.IsNullOrEmpty(companyLine)) stack.Add(new Label { Text = companyLine, TextColor = Color.FromArgb("#85FFFFFF"), FontSize = 10 });
        stack.Add(new Label { Text = string.IsNullOrWhiteSpace(comment.Body) ? "No comment text" : comment.Body, TextColor = Colors.White, FontSize = 14 });
        if (!string.IsNullOrEmpty(comment.CreatedAt)) stack.Add(new Label { Text = comment.CreatedAt, TextColor = Color.FromArgb("#8CFFFFFF"), FontSize = 10 });

        if (!isReply)
        {
            var replyLabel = new Label { Text = "\uF112  Tap to reply", TextColor = Color.FromArgb("#99FFFFFF"), FontSize = 10, Margin = new Thickness(0, 4, 0, 0) };
            stack.Add(replyLabel);
        }

        bubble.Content = stack;

        var tap = new TapGestureRecognizer();
        tap.Tapped += (_, _) =>
        {
            if (isReply) return;
            _replyingTo = comment;
            ReplyBarPreview.Text = comment.Body;
            ReplyBar.IsVisible = true;
            MessageEntry.Focus();
        };
        bubble.GestureRecognizers.Add(tap);

        return bubble;
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Navigation.PopAsync();

    private void OnCancelReplyClicked(object? sender, EventArgs e)
    {
        _replyingTo = null;
        ReplyBar.IsVisible = false;
    }

    private async void OnAttachClicked(object? sender, EventArgs e)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync();
            if (file is null) return;

            await using var stream = await file.OpenReadAsync();
            await _viewModel.UploadAttachmentAsync(_task, stream, file.FileName, file.ContentType ?? "application/octet-stream");
            Render();
        }
        catch
        {
            _appState.Status = "Attachment upload failed";
        }
    }

    private async void OnSendTapped(object? sender, TappedEventArgs e)
    {
        var text = MessageEntry.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(text)) return;

        MessageEntry.Text = "";

        if (_replyingTo is not null)
        {
            await _viewModel.AddReplyAsync(_task, _replyingTo, text);
            _replyingTo = null;
            ReplyBar.IsVisible = false;
        }
        else
        {
            await _viewModel.AddCommentAsync(_task, text);
        }

        Render();
    }
}
