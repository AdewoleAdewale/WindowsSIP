namespace SipCoreMobile.Models.Api;

public class WorkDeskDashboardDto
{
    public int Total { get; set; }
    public int Pending { get; set; }
    public int InProgress { get; set; }
    public int OnHold { get; set; }
    public int Completed { get; set; }
    public int Cancelled { get; set; }
    public int Overdue { get; set; }
    public int PendingApproval { get; set; }
}

public class WorkTaskDto
{
    public int CompanyId { get; set; }
    public string TaskId { get; set; } = "";
    public string Title { get; set; } = "";
    public string? Description { get; set; } = "";
    public string CreatedByExtension { get; set; } = "";
    public string Priority { get; set; } = "Normal";
    public string Status { get; set; } = "Pending";
    public string? DueDate { get; set; }
    public string CreatedAt { get; set; } = "";
    public string UpdatedAt { get; set; } = "";
    public string? CompletedAt { get; set; }
    public bool RequiresApproval { get; set; }
    public string ApprovalStatus { get; set; } = "NotRequired";
    public string TeamLeadExtension { get; set; } = "";
    public bool ReportDownloaded { get; set; }
    public string? ReportDownloadedAt { get; set; }
    public string? ReportDownloadedByExtension { get; set; }
}

public class WorkTaskAssigneeDto
{
    public string ExtensionNumber { get; set; } = "";
    public string Status { get; set; } = "";
    public bool CanComment { get; set; } = true;
    public bool CanUploadFiles { get; set; } = true;
    public bool CanUpdateStatus { get; set; } = true;
    public bool CanEditTask { get; set; }
    public bool CanAssignUsers { get; set; }
    public bool CanCloseTask { get; set; }
}

public class WorkTaskCommentDto
{
    public required string CommentId { get; set; }
    public required string TaskId { get; set; }
    public required string ExtensionNumber { get; set; }
    public required string Body { get; set; }
    public required string CreatedAt { get; set; }
    public string? ParentCommentId { get; set; }
    public List<WorkTaskCommentDto> Replies { get; set; } = new();
}

public class WorkTaskActivityDto
{
    public string ActorExtension { get; set; } = "";
    public string ActivityType { get; set; } = "";
    public string ActivityText { get; set; } = "";
    public string CreatedAt { get; set; } = "";
}

public class WorkTaskChecklistItemDto
{
    public string ChecklistItemId { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public bool IsCompleted { get; set; }
    public string CompletedByExtension { get; set; } = "";
    public string? CompletedAt { get; set; }
    public int SortOrder { get; set; }
}

public class WorkTaskAttachmentDto
{
    public string AttachmentId { get; set; } = "";
    public string FileName { get; set; } = "";
    public string FileType { get; set; } = "";
    public string FileUrl { get; set; } = "";
    public long FileSize { get; set; }
    public string UploadedByExtension { get; set; } = "";
    public string UploadedAt { get; set; } = "";
}

public class WorkTaskWatcherDto
{
    public string ExtensionNumber { get; set; } = "";
    public string AddedByExtension { get; set; } = "";
    public string AddedAt { get; set; } = "";
    public bool IsActive { get; set; } = true;
}

public class WorkTaskDetailResponse
{
    public required WorkTaskDto Task { get; set; }
    public List<WorkTaskAssigneeDto> Assignees { get; set; } = new();
    public List<WorkTaskCommentDto> Comments { get; set; } = new();
    public List<WorkTaskActivityDto> Activities { get; set; } = new();
    public List<WorkTaskChecklistItemDto> Checklist { get; set; } = new();
    public List<WorkTaskAttachmentDto> Attachments { get; set; } = new();
    public List<WorkTaskWatcherDto> Watchers { get; set; } = new();
}

public class AddWorkTaskCommentRequest
{
    public required string ExtensionNumber { get; set; }
    public required string Body { get; set; }
    public string? ParentCommentId { get; set; }
}

public class UpdateWorkTaskStatusRequest
{
    public required string ExtensionNumber { get; set; }
    public required string Status { get; set; }
}

public class ApproveTaskRequest
{
    public required string ActorExtension { get; set; }
    public bool Approved { get; set; }
    public string Note { get; set; } = "";
}

public class CreateWorkTaskChecklistItemRequest
{
    public required string Title { get; set; }
    public string? Description { get; set; }
}

public class CreateWorkTaskRequest
{
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string CreatedByExtension { get; set; }
    public required List<string> AssignedExtensions { get; set; }
    public required string TeamLeadExtension { get; set; }
    public List<string> WatcherExtensions { get; set; } = new();
    public string Priority { get; set; } = "Normal";
    public string? DueDate { get; set; }
    public bool RequiresApproval { get; set; }
    public List<CreateWorkTaskChecklistItemRequest> ChecklistItems { get; set; } = new();
}

public class CompleteChecklistItemRequest
{
    public required string ActorExtension { get; set; }
    public bool IsCompleted { get; set; }
}

public class WorkTaskAttachmentUploadResponse
{
    public bool Success { get; set; }
    public required WorkTaskAttachmentDto Attachment { get; set; }
    public int Pushed { get; set; }
}

public class WorkTaskTemplateDto
{
    public int CompanyId { get; set; }
    public string TemplateId { get; set; } = "";
    public string Name { get; set; } = "";
    public string Description { get; set; } = "";
    public string DefaultPriority { get; set; } = "Normal";
    public int DefaultDueInHours { get; set; } = 24;
}

public class WorkTaskCreatorDto
{
    public string Id { get; set; } = "";
    public string CompanyId { get; set; } = "";
    public string ExtensionNumber { get; set; } = "";
    public string Role { get; set; } = "";
    public bool CanCreateTasks { get; set; }
    public bool CanApproveOwnCreatedTasks { get; set; }
    public bool IsActive { get; set; }
    public string AddedByExtension { get; set; } = "";
    public string CreatedAt { get; set; } = "";
}

public class UpdateWorkTaskRequest
{
    public required string ActorExtension { get; set; }
    public required string Title { get; set; }
    public required string Description { get; set; }
    public required string Priority { get; set; }
    public string? DueDate { get; set; }
    public bool RequiresApproval { get; set; }
}
