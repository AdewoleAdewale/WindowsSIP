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

    private static readonly Color[] SenderPalette =
    {
    Color.FromArgb("#E91E63"), Color.FromArgb("#7C3AED"), Color.FromArgb("#0E9F6E"),
    Color.FromArgb("#D97706"), Color.FromArgb("#0284C7"), Color.FromArgb("#DC2626"),
    Color.FromArgb("#0D9488"), Color.FromArgb("#9333EA"),
};

    private static Color SenderColor(string extension)
    {
        var hash = 0;
        foreach (var ch in extension) hash = (hash * 31 + ch) & 0x7FFFFFFF;
        return SenderPalette[hash % SenderPalette.Length];
    }

    private void RenderComments(List<WorkTaskCommentDto> comments, List<ContactUi> contacts)
    {
        ThreadsList.Children.Clear();

        var topLevel = comments.Where(c => string.IsNullOrEmpty(c.ParentCommentId)).ToList();
        EmptyCommentsLabel.IsVisible = topLevel.Count == 0;

        foreach (var comment in topLevel)
        {
            ThreadsList.Children.Add(BuildBubble(comment, contacts, parent: null));

            foreach (var reply in comment.Replies)
                ThreadsList.Children.Add(BuildBubble(reply, contacts, parent: comment));
        }

        Dispatcher.Dispatch(async () => { await Task.Delay(100); await CommentsScroll.ScrollToAsync(0, CommentsList.Height, true); });
    }

    private View BuildBubble(WorkTaskCommentDto comment, List<ContactUi> contacts, WorkTaskCommentDto? parent)
    {
        var senderExtension = string.IsNullOrEmpty(comment.ExtensionNumber) ? _appState.Extension : comment.ExtensionNumber;
        var isMine = string.Equals(senderExtension.Trim(), _appState.Extension.Trim(), StringComparison.OrdinalIgnoreCase);
        var isReply = parent is not null;
        var companyLine = WorkDeskContactHelpers.CompanyLine(senderExtension, contacts);

        var bubble = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle
            {
                CornerRadius = isMine ? new CornerRadius(14, 14, 14, 3) : new CornerRadius(14, 14, 3, 14)
            },
            StrokeThickness = 0,
            BackgroundColor = isMine ? Color.FromArgb("#D8E9FF") : Colors.White,
            Padding = new Thickness(12, 9),
            MaximumWidthRequest = 460,
            HorizontalOptions = isMine ? LayoutOptions.End : LayoutOptions.Start,
            Shadow = new Shadow { Brush = Colors.Black, Opacity = 0.05f, Radius = 3, Offset = new Point(0, 1) }
        };

        var stack = new VerticalStackLayout { Spacing = 3 };

        // Sender line (others only — WhatsApp style)
        if (!isMine)
        {
            stack.Add(new Label
            {
                Text = WorkDeskContactHelpers.DisplayName(senderExtension, contacts),
                TextColor = SenderColor(senderExtension.Trim()),
                FontAttributes = FontAttributes.Bold,
                FontSize = 12
            });
            if (!string.IsNullOrEmpty(companyLine))
                stack.Add(new Label { Text = companyLine, TextColor = Color.FromArgb("#98A2B3"), FontSize = 10 });
        }

        // Embedded quote of the parent (WhatsApp reply block)
        if (isReply)
        {
            var quoteGrid = new Grid
            {
                ColumnDefinitions = { new(new GridLength(3)), new(GridLength.Star) },
                ColumnSpacing = 8
            };
            quoteGrid.Add(new BoxView { Color = (Color)Application.Current!.Resources["SIPCorePrimary"], WidthRequest = 3, CornerRadius = 1.5f });
            var quoteStack = new VerticalStackLayout { Spacing = 0 };
            quoteStack.Add(new Label
            {
                Text = WorkDeskContactHelpers.DisplayName(
                    string.IsNullOrEmpty(parent!.ExtensionNumber) ? _appState.Extension : parent.ExtensionNumber, contacts),
                TextColor = (Color)Application.Current!.Resources["SIPCorePrimary"],
                FontSize = 10.5,
                FontAttributes = FontAttributes.Bold
            });
            quoteStack.Add(new Label
            {
                Text = parent.Body,
                FontSize = 11,
                TextColor = Color.FromArgb("#667085"),
                LineBreakMode = LineBreakMode.TailTruncation,
                MaxLines = 2
            });
            quoteGrid.Add(quoteStack, 1);

            stack.Add(new Border
            {
                StrokeThickness = 0,
                BackgroundColor = Color.FromArgb("#0D000000"),
                StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 6 },
                Padding = new Thickness(8, 6),
                Content = quoteGrid
            });
        }

        stack.Add(new Label
        {
            Text = string.IsNullOrWhiteSpace(comment.Body) ? "No comment text" : comment.Body,
            TextColor = Color.FromArgb("#101828"),
            FontSize = 14,
            LineHeight = 1.25
        });

        if (!string.IsNullOrEmpty(comment.CreatedAt))
            stack.Add(new Label
            {
                Text = comment.CreatedAt,
                TextColor = Color.FromArgb("#98A2B3"),
                FontSize = 10,
                HorizontalOptions = LayoutOptions.End
            });

        bubble.Content = stack;

        if (!isReply)
        {
            var tap = new TapGestureRecognizer();
            tap.Tapped += (_, _) =>
            {
                _replyingTo = comment;
                ReplyBarPreview.Text = comment.Body;
                ReplyBar.IsVisible = true;
                MessageEntry.Focus();
            };
            bubble.GestureRecognizers.Add(tap);
        }

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

    private async void TapGestureRecognizer_Tapped(object sender, TappedEventArgs e)
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

    private async void MessageEntry_Completed(object sender, EventArgs e)
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
