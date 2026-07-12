using SipCoreMobile.Models;
using SipCoreMobile.Models.Api;
using SipCoreMobile.Services;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

/// <summary>Port of WorkDeskTaskDetailView() and its ~10 collapsible sub-sections
/// (WorkDeskTaskHeroCard, WorkDeskActionSection, WorkDeskInfoSection, WorkDeskAssigneesSection,
/// WorkDeskCommunicationSection, WorkDeskCommentsPreviewSection, WorkDeskAttachmentsSection,
/// WorkDeskChecklistSection, WorkDeskWatchersSection, WorkDeskTimelineSection).</summary>
public partial class WorkDeskTaskDetailPage : ContentPage
{
    private readonly AppStateViewModel _appState;
    private readonly WorkDeskViewModel _viewModel;
    private readonly ChatViewModel _chatViewModel;
    private WorkTaskDto _task;

    public WorkDeskTaskDetailPage(AppStateViewModel appState, WorkDeskViewModel viewModel, ChatViewModel chatViewModel, WorkTaskDto task)
    {
        InitializeComponent();
        _appState = appState;
        _viewModel = viewModel;
        _chatViewModel = chatViewModel;
        _task = task;

        ActionsSection.SetExpanded(true);
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        SubtitleLabel.Text = $"Extension {_appState.Extension}";

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;
        ContentScroll.IsVisible = false;

        await _viewModel.LoadTaskDetailAsync(_task);
        Render();
    }

    private void Render()
    {
        var detail = _viewModel.SelectedTaskDetail;
        if (detail is null)
        {
            LoadingIndicator.IsRunning = true;
            LoadingIndicator.IsVisible = true;
            ContentScroll.IsVisible = false;
            return;
        }

        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;
        ContentScroll.IsVisible = true;

        var contacts = _appState.Contacts.ToList();
        var displayTask = detail.Task;
        var teamLead = !string.IsNullOrEmpty(displayTask.TeamLeadExtension)
            ? displayTask.TeamLeadExtension
            : detail.Assignees.FirstOrDefault()?.ExtensionNumber ?? "";

        _task = displayTask;

        var isTaskClosed = displayTask.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
                            || displayTask.Status.Equals("Done", StringComparison.OrdinalIgnoreCase)
                            || displayTask.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase)
                            || displayTask.ApprovalStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase);

        var hasDueDate = !string.IsNullOrWhiteSpace(displayTask.DueDate);
        var isTeamLead = string.Equals(teamLead.Trim(), _appState.Extension.Trim(), StringComparison.OrdinalIgnoreCase);
        var isCreator = string.Equals(displayTask.CreatedByExtension.Trim(), _appState.Extension.Trim(), StringComparison.OrdinalIgnoreCase);
        var isApproved = displayTask.ApprovalStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase);
        var isCompleted = displayTask.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
                           || displayTask.Status.Equals("Done", StringComparison.OrdinalIgnoreCase)
                           || displayTask.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase);
        var canDownloadReport = isCreator && !displayTask.ReportDownloaded
                                 && (displayTask.RequiresApproval ? isApproved : isCompleted);

        var (statusColor, priorityColor, progress) = ColorsFor(displayTask);

        RenderHero(displayTask, contacts, statusColor, priorityColor, progress);
        RenderActions(displayTask, isTeamLead, isTaskClosed, hasDueDate, canDownloadReport, displayTask.ReportDownloaded);
        RenderInfo(displayTask, contacts);
        RenderAssignees(detail.Assignees, contacts);
        RenderCommunication(teamLead, contacts);
        RenderCommentsPreview(detail.Comments, contacts);
        RenderAttachments(detail.Attachments, contacts);
        RenderChecklist(displayTask, detail.Checklist, isTaskClosed);
        RenderWatchers(detail.Watchers, contacts);
        RenderTimeline(displayTask, detail.Activities, contacts);
    }

    /// <summary>Port of workDeskStatusColor()/workDeskPriorityColor()/workDeskProgress().</summary>
    private static (Color status, Color priority, double progress) ColorsFor(WorkTaskDto task)
    {
        var status = task.Status.ToLowerInvariant() switch
        {
            "completed" or "done" or "approved" => Color.FromArgb("#22C55E"),
            "inprogress" or "in progress" or "started" => Color.FromArgb("#3B82F6"),
            "hold" or "on hold" => Color.FromArgb("#F59E0B"),
            "rejected" => Color.FromArgb("#EF4444"),
            _ => Color.FromArgb("#0057B8")
        };

        var priority = task.Priority.ToLowerInvariant() switch
        {
            "high" or "urgent" => Color.FromArgb("#EF4444"),
            "medium" => Color.FromArgb("#F59E0B"),
            _ => Color.FromArgb("#22C55E")
        };

        var progress = task.Status.ToLowerInvariant() switch
        {
            "completed" or "done" or "approved" => 1.0,
            "inprogress" or "in progress" or "started" => 0.65,
            "hold" or "on hold" => 0.35,
            "rejected" => 0.1,
            _ => 0.15
        };

        return (status, priority, progress);
    }

    private void RenderHero(WorkTaskDto task, List<ContactUi> contacts, Color statusColor, Color priorityColor, double progress)
    {
        HeroIconBg.BackgroundColor = statusColor.WithAlpha(0.16f);
        HeroIcon.TextColor = statusColor;
        HeroTitleLabel.Text = string.IsNullOrWhiteSpace(task.Title) ? "Untitled Task" : task.Title;
        HeroCreatorLabel.Text = $"Created by {WorkDeskContactHelpers.DisplayName(task.CreatedByExtension, contacts)}";

        var creatorCompany = WorkDeskContactHelpers.CompanyLine(task.CreatedByExtension, contacts);
        HeroCreatorCompanyLabel.IsVisible = !string.IsNullOrEmpty(creatorCompany);
        HeroCreatorCompanyLabel.Text = creatorCompany;

        HeroDescriptionLabel.Text = string.IsNullOrWhiteSpace(task.Description) ? "No description provided." : task.Description;

        HeroStatusChip.BackgroundColor = statusColor.WithAlpha(0.16f);
        HeroStatusLabel.TextColor = statusColor;
        HeroStatusLabel.Text = task.Status;

        HeroPriorityChip.BackgroundColor = priorityColor.WithAlpha(0.16f);
        HeroPriorityLabel.TextColor = priorityColor;
        HeroPriorityLabel.Text = task.Priority;

        HeroApprovalChip.IsVisible = task.RequiresApproval;
        HeroApprovalLabel.Text = task.ApprovalStatus;

        HeroProgressBar.Progress = progress;
        HeroProgressBar.ProgressColor = statusColor;
        HeroProgressLabel.Text = $"{(int)(progress * 100)}% Progress";
    }

    private void RenderActions(WorkTaskDto task, bool isTeamLead, bool isTaskClosed, bool hasDueDate, bool canDownloadReport, bool isReportDownloaded)
    {
        var content = ActionsSection.Content;
        content.Children.Clear();

        var disableWorkflow = isTaskClosed || !hasDueDate;

        if (!hasDueDate && !isTaskClosed)
        {
            content.Children.Add(new Label
            {
                Text = "Start, Hold, and Done are disabled because this task has no due date.",
                TextColor = Color.FromArgb("#FACC15"), FontSize = 12, Margin = new Thickness(0, 0, 0, 8)
            });
        }

        var row1 = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10 };
        row1.Add(BuildActionButton("Start", "#3B82F6", !disableWorkflow, async () => { await _viewModel.StartTaskAsync(task); Render(); }), 0);
        row1.Add(BuildActionButton("Hold", "#F59E0B", !disableWorkflow, async () => { await _viewModel.HoldTaskAsync(task); Render(); }), 1);
        content.Children.Add(row1);

        var row2 = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10, Margin = new Thickness(0, 10, 0, 0) };
        row2.Add(BuildActionButton("Done", "#22C55E", !disableWorkflow && isTeamLead, async () => { await _viewModel.CompleteTaskAsync(task); Render(); }), 0);
        row2.Add(BuildActionButton("Upload", "#0057B8", !isTaskClosed, async () => await UploadAttachmentAsync(task)), 1);
        content.Children.Add(row2);

        content.Children.Add(BuildActionButton("Open Discussion", "#A855F7", true,
            () => Navigation.PushAsync(new WorkDeskDiscussionPage(_appState, _viewModel, task)), fullWidth: true, margin: new Thickness(0, 10, 0, 0)));

        if (_viewModel.IsDownloadingTaskReport)
        {
            content.Children.Add(BuildDisabledButton("Downloading..."));
        }
        else if (isReportDownloaded)
        {
            content.Children.Add(BuildDisabledButton("Report Downloaded"));
        }
        else if (canDownloadReport)
        {
            content.Children.Add(BuildActionButton("Download Task Report", "#22C55E", true, async () => await DownloadReportAsync(task), fullWidth: true, margin: new Thickness(0, 10, 0, 0)));
        }
    }

    private static Border BuildDisabledButton(string title) => new()
    {
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
        BackgroundColor = Color.FromArgb("#14FFFFFF"),
        Padding = new Thickness(0, 12),
        Margin = new Thickness(0, 10, 0, 0),
        Content = new Label { Text = title, TextColor = Color.FromArgb("#80FFFFFF"), HorizontalOptions = LayoutOptions.Center, FontAttributes = FontAttributes.Bold }
    };

    private Border BuildActionButton(string title, string colorHex, bool enabled, Func<Task> onClick, bool fullWidth = false, Thickness? margin = null)
    {
        var color = Color.FromArgb(colorHex);
        var border = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
            BackgroundColor = enabled ? color.WithAlpha(0.85f) : Color.FromArgb("#14FFFFFF"),
            Padding = new Thickness(0, 12),
            Margin = margin ?? new Thickness(0),
            Content = new Label
            {
                Text = title,
                TextColor = enabled ? Colors.White : Color.FromArgb("#80FFFFFF"),
                HorizontalOptions = LayoutOptions.Center,
                FontAttributes = FontAttributes.Bold
            }
        };

        if (enabled)
        {
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) => await onClick();
            border.GestureRecognizers.Add(tap);
        }

        return border;
    }

    private async Task UploadAttachmentAsync(WorkTaskDto task)
    {
        try
        {
            var file = await FilePicker.Default.PickAsync();
            if (file is null) return;

            await using var stream = await file.OpenReadAsync();
            await _viewModel.UploadAttachmentAsync(task, stream, file.FileName, file.ContentType ?? "application/octet-stream");
            Render();
        }
        catch
        {
            _appState.Status = "Attachment upload failed";
        }
    }

    private async Task DownloadReportAsync(WorkTaskDto task)
    {
        var stream = await _viewModel.DownloadTaskReportAsync(task);
        if (stream is null) return;

        try
        {
            var fileName = $"{task.Title.Replace(' ', '_')}_report.pdf";
            var targetPath = Path.Combine(FileSystem.CacheDirectory, fileName);
            await using (var fileStream = File.Create(targetPath))
            {
                await stream.CopyToAsync(fileStream);
            }

            await Launcher.Default.OpenAsync(new OpenFileRequest(fileName, new ReadOnlyFile(targetPath)));
        }
        catch
        {
            _appState.Status = "Unable to open downloaded report";
        }

        Render();
    }

    private void RenderInfo(WorkTaskDto task, List<ContactUi> contacts)
    {
        var content = InfoSection.Content;
        content.Children.Clear();

        content.Children.Add(BuildInfoRow("\uF1AD", "Workspace", "SIPCore WorkDesk"));
        content.Children.Add(BuildPersonRow("\uF007", "Created By", task.CreatedByExtension, contacts));
        content.Children.Add(BuildPersonRow("\uF0E8", "Team Lead", task.TeamLeadExtension, contacts));
        content.Children.Add(BuildInfoRow("\uF017", "Due Date", string.IsNullOrWhiteSpace(task.DueDate) ? "No Due Date" : task.DueDate));
        content.Children.Add(BuildInfoRow("\uF021", "Updated", string.IsNullOrWhiteSpace(task.UpdatedAt) ? "Not Updated" : task.UpdatedAt));
    }

    private static View BuildInfoRow(string icon, string label, string value)
    {
        var row = new HorizontalStackLayout { Spacing = 10, Margin = new Thickness(0, 6) };
        row.Add(new Label { Text = icon, FontFamily = "MaterialIcons", TextColor = Color.FromArgb("#0057B8"), WidthRequest = 20 });
        var textStack = new VerticalStackLayout();
        textStack.Add(new Label { Text = label, TextColor = Color.FromArgb("#99FFFFFF"), FontSize = 11 });
        textStack.Add(new Label { Text = value, TextColor = Colors.White, FontSize = 13, FontAttributes = FontAttributes.Bold });
        row.Add(textStack);
        return row;
    }

    private View BuildPersonRow(string icon, string label, string extension, List<ContactUi> contacts)
    {
        var stack = new VerticalStackLayout();
        stack.Add(BuildInfoRow(icon, label, WorkDeskContactHelpers.DisplayName(extension, contacts)));
        var company = WorkDeskContactHelpers.CompanyLine(extension, contacts);
        if (!string.IsNullOrEmpty(company))
        {
            stack.Add(new Label { Text = company, TextColor = Color.FromArgb("#73FFFFFF"), FontSize = 11, Margin = new Thickness(30, -4, 0, 0) });
        }
        return stack;
    }

    private void RenderAssignees(List<WorkTaskAssigneeDto> assignees, List<ContactUi> contacts)
    {
        var content = AssigneesSection.Content;
        content.Children.Clear();

        if (assignees.Count == 0)
        {
            content.Children.Add(EmptyLabel("No assignees found."));
            return;
        }

        foreach (var assignee in assignees)
        {
            var roleText = assignee.Status.Equals("Creator", StringComparison.OrdinalIgnoreCase) ? "Creator"
                : assignee.CanCloseTask ? "Team Lead" : "Contributor";

            var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 10, Margin = new Thickness(0, 6) };
            row.Add(new Label { Text = assignee.CanCloseTask ? "\uF091" : "\uF007", FontFamily = "MaterialIcons", TextColor = assignee.CanCloseTask ? Color.FromArgb("#FACC15") : Color.FromArgb("#0057B8") }, 0);

            var textStack = new VerticalStackLayout();
            textStack.Add(new Label { Text = WorkDeskContactHelpers.DisplayName(assignee.ExtensionNumber, contacts), TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 13 });
            var company = WorkDeskContactHelpers.CompanyLine(assignee.ExtensionNumber, contacts);
            if (!string.IsNullOrEmpty(company)) textStack.Add(new Label { Text = company, TextColor = Color.FromArgb("#73FFFFFF"), FontSize = 11 });
            textStack.Add(new Label { Text = roleText, TextColor = Color.FromArgb("#8CFFFFFF"), FontSize = 11 });
            row.Add(textStack, 1);

            var contact = WorkDeskContactHelpers.FindContact(assignee.ExtensionNumber, contacts);
            if (contact?.CanCall != false)
            {
                var callButton = new Label { Text = "\uF095", FontFamily = "MaterialIcons", TextColor = Color.FromArgb("#22C55E"), VerticalOptions = LayoutOptions.Center };
                var tap = new TapGestureRecognizer();
                var ext = assignee.ExtensionNumber;
                tap.Tapped += async (_, _) => await _appState.MakeOutgoingCallAsync(ext);
                callButton.GestureRecognizers.Add(tap);
                row.Add(callButton, 2);
            }

            content.Children.Add(row);
        }
    }

    private void RenderCommunication(string teamLeadExtension, List<ContactUi> contacts)
    {
        var content = CommunicationSection.Content;
        content.Children.Clear();

        if (string.IsNullOrWhiteSpace(teamLeadExtension))
        {
            content.Children.Add(EmptyLabel("No team lead assigned to this task."));
            return;
        }

        var contact = WorkDeskContactHelpers.FindContact(teamLeadExtension, contacts);

        content.Children.Add(new Label { Text = WorkDeskContactHelpers.DisplayName(teamLeadExtension, contacts), TextColor = Colors.White, FontAttributes = FontAttributes.Bold });
        var company = WorkDeskContactHelpers.CompanyLine(teamLeadExtension, contacts);
        if (!string.IsNullOrEmpty(company)) content.Children.Add(new Label { Text = company, TextColor = Color.FromArgb("#8CFFFFFF"), FontSize = 11 });

        var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10, Margin = new Thickness(0, 10, 0, 0) };

        if (contact?.CanCall != false)
        {
            row.Add(BuildActionButton("Call", "#22C55E", true, async () => await _appState.MakeOutgoingCallAsync(teamLeadExtension)), 0);
        }
        if (contact?.CanMessage != false)
        {
            row.Add(BuildActionButton("Message", "#0057B8", true, async () =>
            {
                await _chatViewModel.OpenConversationAsync(teamLeadExtension);
                await Navigation.PushAsync(new ChatPage(_appState, _chatViewModel));
            }), 1);
        }
        content.Children.Add(row);

        if (contact is not null && !contact.CanCall && !contact.CanMessage)
        {
            content.Children.Add(new Label { Text = "Calling and messaging are not enabled for this related company.", TextColor = Color.FromArgb("#8CFFFFFF"), FontSize = 11, Margin = new Thickness(0, 8, 0, 0) });
        }
    }

    private void RenderCommentsPreview(List<WorkTaskCommentDto> comments, List<ContactUi> contacts)
    {
        var content = CommentsPreviewSection.Content;
        content.Children.Clear();

        if (comments.Count == 0)
        {
            content.Children.Add(EmptyLabel("No comments yet."));
        }
        else
        {
            foreach (var comment in comments.Take(3))
            {
                var stack = new VerticalStackLayout { Margin = new Thickness(0, 6) };
                stack.Add(new Label { Text = string.IsNullOrWhiteSpace(comment.Body) ? "No comment text" : comment.Body, TextColor = Colors.White, FontSize = 13 });
                stack.Add(new Label { Text = $"{WorkDeskContactHelpers.DisplayName(comment.ExtensionNumber, contacts)} • {comment.CreatedAt}", TextColor = Color.FromArgb("#8CFFFFFF"), FontSize = 11 });
                content.Children.Add(stack);
            }
        }

        content.Children.Add(BuildActionButton("View All Comments", "#A855F7", true,
            () => Navigation.PushAsync(new WorkDeskDiscussionPage(_appState, _viewModel, _task)), fullWidth: true, margin: new Thickness(0, 10, 0, 0)));
    }

    private void RenderAttachments(List<WorkTaskAttachmentDto> attachments, List<ContactUi> contacts)
    {
        var content = AttachmentsSection.Content;
        content.Children.Clear();

        if (attachments.Count == 0)
        {
            content.Children.Add(EmptyLabel("No attachments uploaded."));
            return;
        }

        foreach (var attachment in attachments)
        {
            var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 10, Margin = new Thickness(0, 6) };
            row.Add(new Label { Text = "\uF0C6", FontFamily = "MaterialIcons", TextColor = Color.FromArgb("#0057B8") }, 0);

            var textStack = new VerticalStackLayout();
            textStack.Add(new Label { Text = string.IsNullOrWhiteSpace(attachment.FileName) ? "Attachment" : attachment.FileName, TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 13 });
            textStack.Add(new Label { Text = $"Uploaded by {WorkDeskContactHelpers.DisplayName(attachment.UploadedByExtension, contacts)}", TextColor = Color.FromArgb("#8CFFFFFF"), FontSize = 11 });
            textStack.Add(new Label { Text = "Tap to open attachment", TextColor = Color.FromArgb("#0057B8"), FontSize = 11, FontAttributes = FontAttributes.Bold });
            row.Add(textStack, 1);
            row.Add(new Label { Text = "\uF08E", FontFamily = "MaterialIcons", TextColor = Color.FromArgb("#B3FFFFFF") }, 2);

            var url = attachment.FileUrl;
            var tap = new TapGestureRecognizer();
            tap.Tapped += async (_, _) => { if (!string.IsNullOrWhiteSpace(url)) await _viewModel.OpenAttachmentAsync(url); };
            row.GestureRecognizers.Add(tap);

            content.Children.Add(row);
        }
    }

    private void RenderChecklist(WorkTaskDto task, List<WorkTaskChecklistItemDto> checklist, bool isTaskClosed)
    {
        var content = ChecklistSection.Content;
        content.Children.Clear();

        if (checklist.Count == 0)
        {
            content.Children.Add(EmptyLabel("No checklist items."));
            return;
        }

        foreach (var item in checklist)
        {
            var row = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10, Margin = new Thickness(0, 6) };

            var checkbox = new CheckBox { IsChecked = item.IsCompleted, IsEnabled = !isTaskClosed };
            checkbox.CheckedChanged += async (_, e) =>
            {
                if (isTaskClosed) return;
                await _viewModel.ToggleChecklistItemAsync(task, item, e.Value);
                Render();
            };
            row.Add(checkbox, 0);

            var textStack = new VerticalStackLayout();
            textStack.Add(new Label
            {
                Text = string.IsNullOrWhiteSpace(item.Title) ? "Checklist item" : item.Title,
                TextColor = isTaskClosed ? Color.FromArgb("#8CFFFFFF") : Colors.White,
                FontAttributes = item.IsCompleted ? FontAttributes.None : FontAttributes.Bold,
                FontSize = 13
            });
            if (!string.IsNullOrWhiteSpace(item.Description))
                textStack.Add(new Label { Text = item.Description, TextColor = Color.FromArgb("#8CFFFFFF"), FontSize = 11 });
            if (!string.IsNullOrWhiteSpace(item.CompletedByExtension))
                textStack.Add(new Label { Text = $"Completed by {item.CompletedByExtension}", TextColor = Color.FromArgb("#73FFFFFF"), FontSize = 11 });
            row.Add(textStack, 1);

            content.Children.Add(row);
        }
    }

    private void RenderWatchers(List<WorkTaskWatcherDto> watchers, List<ContactUi> contacts)
    {
        var content = WatchersSection.Content;
        content.Children.Clear();

        if (watchers.Count == 0)
        {
            content.Children.Add(EmptyLabel("No watchers."));
            return;
        }

        foreach (var watcher in watchers)
        {
            var stack = new VerticalStackLayout { Margin = new Thickness(0, 6) };
            stack.Add(BuildInfoRow("\uF06E", WorkDeskContactHelpers.DisplayName(watcher.ExtensionNumber, contacts), watcher.IsActive ? "Watching" : "Inactive"));
            var company = WorkDeskContactHelpers.CompanyLine(watcher.ExtensionNumber, contacts);
            if (!string.IsNullOrEmpty(company)) stack.Add(new Label { Text = company, TextColor = Color.FromArgb("#73FFFFFF"), FontSize = 11, Margin = new Thickness(30, -4, 0, 0) });
            content.Children.Add(stack);
        }
    }

    private void RenderTimeline(WorkTaskDto task, List<WorkTaskActivityDto> activities, List<ContactUi> contacts)
    {
        var content = TimelineSection.Content;
        content.Children.Clear();

        content.Children.Add(BuildInfoRow("\uF067", "Task Created", string.IsNullOrWhiteSpace(task.CreatedAt) ? "No creation time" : task.CreatedAt));
        content.Children.Add(BuildInfoRow("\uF021", "Last Updated", string.IsNullOrWhiteSpace(task.UpdatedAt) ? "No update time" : task.UpdatedAt));

        if (!string.IsNullOrWhiteSpace(task.CompletedAt))
            content.Children.Add(BuildInfoRow("\uF058", "Completed", task.CompletedAt));

        if (task.RequiresApproval)
            content.Children.Add(BuildInfoRow("\uF0AE", "Approval Status", task.ApprovalStatus));

        foreach (var activity in activities)
        {
            var title = string.IsNullOrWhiteSpace(activity.ActivityText)
                ? (string.IsNullOrWhiteSpace(activity.ActivityType) ? "Activity" : activity.ActivityType)
                : activity.ActivityText;
            content.Children.Add(BuildInfoRow("\uF1DA", title, $"{WorkDeskContactHelpers.DisplayName(activity.ActorExtension, contacts)} • {activity.CreatedAt}"));
        }
    }

    private static Label EmptyLabel(string text) => new() { Text = text, TextColor = Color.FromArgb("#A6FFFFFF"), FontSize = 13 };

    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Navigation.PopAsync();
}
