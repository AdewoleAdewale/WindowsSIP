using SipCoreMobile.Models;
using SipCoreMobile.Models.Api;

namespace SipCoreMobile.Controls;

public partial class WorkDeskTaskCard : ContentView
{
    public event EventHandler<WorkTaskDto>? CardTapped;
    public event EventHandler<WorkTaskDto>? EditTapped;

    private WorkTaskDto? _task;

    public WorkDeskTaskCard()
    {
        InitializeComponent();
    }

    /// <summary>
    /// Port of WorkDeskTaskCard()'s full derivation (statusColor/priorityColor/progress/
    /// isMine/isClosed/canEdit/creatorName/teamLeadName/presence/counts). Called by the host
    /// page instead of using several ordering-sensitive bindable properties.
    /// </summary>
    public void Configure(WorkTaskDto task, WorkTaskDetailResponse? detail, IReadOnlyList<ContactUi> contacts, string currentExtension)
    {
        _task = task;

        var statusColor = task.Status.ToLowerInvariant() switch
        {
            "completed" or "done" or "approved" => Color.FromArgb("#22C55E"),
            "inprogress" or "in progress" or "started" => Color.FromArgb("#3B82F6"),
            "hold" or "on hold" => Color.FromArgb("#F59E0B"),
            "rejected" => Color.FromArgb("#EF4444"),
            _ => (Color)Application.Current!.Resources["SIPCorePrimary"]
        };

        var priorityColor = task.Priority.ToLowerInvariant() switch
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

        var isMine = string.Equals(task.CreatedByExtension.Trim(), currentExtension.Trim(), StringComparison.OrdinalIgnoreCase);
        var isClosed = task.Status.Equals("Completed", StringComparison.OrdinalIgnoreCase)
                       || task.Status.Equals("Done", StringComparison.OrdinalIgnoreCase)
                       || task.Status.Equals("Closed", StringComparison.OrdinalIgnoreCase)
                       || task.ApprovalStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase);
        var canEdit = isMine && !isClosed;

        var teamLeadExtension = !string.IsNullOrEmpty(task.TeamLeadExtension)
            ? task.TeamLeadExtension
            : !string.IsNullOrEmpty(detail?.Task.TeamLeadExtension)
                ? detail!.Task.TeamLeadExtension
                : detail?.Assignees.FirstOrDefault()?.ExtensionNumber ?? "";

        var creatorName = DisplayName(task.CreatedByExtension, contacts);
        var creatorCompany = CompanyLine(task.CreatedByExtension, contacts);
        var teamLeadName = DisplayName(teamLeadExtension, contacts);
        var teamLeadCompany = CompanyLine(teamLeadExtension, contacts);

        IconBg.BackgroundColor = statusColor.WithAlpha(0.16f);
        IconLabel.TextColor = statusColor;

        TitleLabel.Text = string.IsNullOrWhiteSpace(task.Title) ? "Untitled Task" : task.Title;
        CreatorLabel.Text = isMine ? $"Created by you ({currentExtension})" : $"Created by {creatorName}";
        CreatorCompanyLabel.IsVisible = !string.IsNullOrEmpty(creatorCompany);
        CreatorCompanyLabel.Text = creatorCompany;

        EditButton.IsVisible = canEdit;

        StatusChip.BackgroundColor = statusColor.WithAlpha(0.16f);
        StatusLabel.TextColor = statusColor;
        StatusLabel.Text = task.Status;

        PriorityChip.BackgroundColor = priorityColor.WithAlpha(0.16f);
        PriorityLabel.TextColor = priorityColor;
        PriorityLabel.Text = task.Priority;

        ProgressIndicator.Progress = progress;
        ProgressIndicator.ProgressColor = statusColor;

        TeamLeadLabel.Text = string.IsNullOrEmpty(teamLeadName) ? "No team lead" : $"Lead: {teamLeadName}";
        TeamLeadCompanyLabel.IsVisible = !string.IsNullOrEmpty(teamLeadCompany);
        TeamLeadCompanyLabel.Text = teamLeadCompany;

        CommentsCountLabel.Text = $"💬 {detail?.Comments.Count ?? 0}";
        AttachmentsCountLabel.Text = $"📎 {detail?.Attachments.Count ?? 0}";
    }

    private static string DisplayName(string extension, IReadOnlyList<ContactUi> contacts)
    {
        var clean = extension.Trim();
        if (string.IsNullOrEmpty(clean)) return "";

        var contact = contacts.FirstOrDefault(c => string.Equals(c.Extension.Trim(), clean, StringComparison.OrdinalIgnoreCase));
        if (contact is null) return clean;

        var cleanExt = contact.Extension.Trim();
        var cleanName = contact.DisplayName.Replace($"({cleanExt})", "").Trim();
        if (string.IsNullOrEmpty(cleanName)) cleanName = cleanExt;
        return $"{cleanName} ({cleanExt})";
    }

    private static string CompanyLine(string extension, IReadOnlyList<ContactUi> contacts)
    {
        var contact = contacts.FirstOrDefault(c => string.Equals(c.Extension.Trim(), extension.Trim(), StringComparison.OrdinalIgnoreCase));
        if (contact is null) return "";

        if (contact.IsExternal && !string.IsNullOrWhiteSpace(contact.CompanyName)) return $"{contact.CompanyName} • External";
        return contact.CompanyName ?? "";
    }

    private void OnCardTapped(object? sender, TappedEventArgs e)
    {
        if (_task is not null) CardTapped?.Invoke(this, _task);
    }

    private void OnEditClicked(object? sender, EventArgs e)
    {
        if (_task is not null) EditTapped?.Invoke(this, _task);
    }
}
