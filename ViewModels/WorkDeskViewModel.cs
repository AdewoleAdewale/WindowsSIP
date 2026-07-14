using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SipCoreMobile.Models;
using SipCoreMobile.Models.Api;
using SipCoreMobile.Services.Api;

namespace SipCoreMobile.ViewModels;

/// <summary>
/// Port of the WorkDesk-related slice of MainActivity.kt: loadWorkDeskDashboard(),
/// loadWorkDeskTasks(), loadWorkDeskTaskDetail(), loadWorkDeskCreateMeta(),
/// createWorkDeskTask(), updateWorkDeskTask(), updateWorkDeskTaskStatus(),
/// approveOrRejectWorkDeskTask(), addWorkDeskTaskComment(), addWorkDeskTaskReply(),
/// toggleWorkDeskChecklistItem(), uploadWorkDeskAttachment(), openWorkDeskAttachment(),
/// downloadTaskReportPdf(), and the openWorkDeskHome/Tasks/Approvals/TaskDetail navigation
/// helpers. Same feature-scoped pattern as ChatViewModel/GroupsViewModel/MeetingsViewModel.
///
/// NOTE: this ViewModel is Batch 12's deliverable for the *data layer*. The WorkDesk module's
/// 8 Kotlin files total 7,947 lines (comparable in scope to the entire main UI conversion) --
/// see the README's WorkDesk sub-batch plan for which screens are built vs. still pending.
/// </summary>
public partial class WorkDeskViewModel : ObservableObject
{
    private readonly ISipCoreApiService _api;
    private readonly AppStateViewModel _appState;

    [ObservableProperty] private WorkDeskDashboardDto? dashboard;
    [ObservableProperty] private WorkTaskDetailResponse? selectedTaskDetail;
    [ObservableProperty] private bool workDeskLoading;
    [ObservableProperty] private string workDeskError = "";

    [ObservableProperty] private WorkTaskDto? selectedEditTask;
    [ObservableProperty] private bool isEditingTask;
    [ObservableProperty] private string editTaskError = "";

    [ObservableProperty] private bool isDownloadingTaskReport;

    public ObservableCollection<WorkTaskDto> Tasks { get; } = new();
    public ObservableCollection<WorkTaskTemplateDto> Templates { get; } = new();
    public Dictionary<string, WorkTaskDetailResponse> TaskDetailsById { get; } = new();

    public WorkDeskViewModel(ISipCoreApiService api, AppStateViewModel appState)
    {
        _api = api;
        _appState = appState;
    }

    // ---------------------------------------------------------------------
    // Navigation helpers (port of openWorkDeskHome/Tasks/Approvals/TaskDetail)
    // ---------------------------------------------------------------------

    public async Task OpenWorkDeskHomeAsync()
    {
        await Task.WhenAll(LoadDashboardAsync(), LoadTasksAsync(), LoadCreateMetaAsync());
        _appState.NavigateSafely(Screen.WorkDesk);
    }

    public async Task OpenWorkDeskTasksAsync()
    {
        await LoadTasksAsync();
        _appState.NavigateSafely(Screen.WorkDeskTask);
    }

    public async Task OpenWorkDeskApprovalsAsync()
    {
        await Task.WhenAll(LoadTasksAsync(), LoadDashboardAsync());
        _appState.NavigateSafely(Screen.WorkDeskApprovals);
    }

    public async Task OpenWorkDeskTaskDetailAsync(WorkTaskDto task)
    {
        await LoadTaskDetailAsync(task);
    }

    // ---------------------------------------------------------------------
    // Loaders
    // ---------------------------------------------------------------------

    public async Task LoadDashboardAsync()
    {
        try
        {
            Dashboard = await _api.GetWorkDeskDashboardAsync(_appState.CompanyId, _appState.Extension);
        }
        catch
        {
            Dashboard = null;
            _appState.Status = "Failed to load WorkDesk dashboard";
        }
    }

    public async Task LoadTasksAsync()
    {
        WorkDeskLoading = true;
        WorkDeskError = "";
        Controls.InAppNotifier.Show("Syncing workspace…", "Fetching your tasks and approvals", Controls.SnackKind.Loading, durationMs: 2000);
        try
        {
            var tasks = await _api.GetMyWorkTasksAsync(_appState.CompanyId, _appState.Extension);
            Tasks.Clear();
            foreach (var t in tasks) Tasks.Add(t);

        
            TaskDetailsById.Clear();
            var detailResults = await Task.WhenAll(tasks.Select(async task =>
            {
                try
                {
                    var detail = await _api.GetWorkTaskAsync(_appState.CompanyId, task.TaskId);
                    return (task.TaskId, Detail: detail);
                }
                catch
                {

                    return (task.TaskId, Detail: (WorkTaskDetailResponse?)null);
                }
            }));
        }
        catch (Exception ex)
        {
            WorkDeskError = ex.Message;
        }
        finally
        {
            WorkDeskLoading = false;
        }
    }

    public async Task LoadTaskDetailAsync(WorkTaskDto task)
    {
        WorkDeskLoading = true;
        WorkDeskError = "";

        try
        {
            SelectedTaskDetail = await _api.GetWorkTaskAsync(_appState.CompanyId, task.TaskId);
        }
        catch (Exception ex)
        {
            WorkDeskError = ex.Message;
            SelectedTaskDetail = null;
        }
        finally
        {
            WorkDeskLoading = false;
        }
    }

    public async Task LoadCreateMetaAsync()
    {
        try
        {
            var templates = await _api.GetWorkTaskTemplatesAsync(_appState.CompanyId);
            Templates.Clear();
            foreach (var t in templates) Templates.Add(t);

            await _appState.LoadWorkTaskCreatorsAsync();
        }
        catch
        {
            Templates.Clear();
            _appState.Status = "Failed to load WorkDesk create options";
        }
    }

    // ---------------------------------------------------------------------
    // Mutations
    // ---------------------------------------------------------------------

    public async Task CreateTaskAsync(
        string title, string description, string priority, string dueDate, bool requiresApproval,
        List<string> assignedExtensions, string teamLeadExtension, List<string> watcherExtensions,
        List<CreateWorkTaskChecklistItemRequest> checklistItems)
    {
        WorkDeskLoading = true;
        WorkDeskError = "";

        try
        {
            await _api.CreateWorkTaskAsync(_appState.CompanyId, new CreateWorkTaskRequest
            {
                Title = title,
                Description = description,
                CreatedByExtension = _appState.Extension,
                AssignedExtensions = assignedExtensions,
                TeamLeadExtension = teamLeadExtension,
                WatcherExtensions = watcherExtensions,
                Priority = priority,
                DueDate = string.IsNullOrEmpty(dueDate) ? null : dueDate,
                RequiresApproval = requiresApproval,
                ChecklistItems = checklistItems
            });

            await LoadTasksAsync();
            _appState.Status = "Task created";
        }
        catch (Exception ex)
        {
            WorkDeskError = ex.Message;
            _appState.Status = "Failed to create task";
        }
        finally
        {
            WorkDeskLoading = false;
        }
    }

    public async Task UpdateTaskAsync(
        WorkTaskDto task, string title, string description, string priority, string? dueDate, bool requiresApproval)
    {
        IsEditingTask = true;
        EditTaskError = "";

        try
        {
            await _api.UpdateWorkTaskAsync(_appState.CompanyId, task.TaskId, new UpdateWorkTaskRequest
            {
                ActorExtension = _appState.Extension,
                Title = title,
                Description = description,
                Priority = priority,
                DueDate = dueDate,
                RequiresApproval = requiresApproval
            });

            await LoadTasksAsync();
            _appState.Status = "Task updated";
        }
        catch (Exception ex)
        {
            EditTaskError = ex.Message;
            _appState.Status = "Failed to update task";
        }
        finally
        {
            IsEditingTask = false;
        }
    }

    public async Task UpdateTaskStatusAsync(WorkTaskDto task, string status)
    {
        WorkDeskLoading = true;
        WorkDeskError = "";

        try
        {
            await _api.UpdateWorkTaskStatusAsync(_appState.CompanyId, task.TaskId,
                new UpdateWorkTaskStatusRequest { ExtensionNumber = _appState.Extension, Status = status });

            await LoadTasksAsync();
            await LoadDashboardAsync();
            await LoadTaskDetailAsync(task);

            _appState.Status = "Task updated";
        }
        catch (Exception ex)
        {
            WorkDeskError = ex.Message;
            _appState.Status = "Failed to update task";
        }
        finally
        {
            WorkDeskLoading = false;
        }
    }

    public Task StartTaskAsync(WorkTaskDto task) => UpdateTaskStatusAsync(task, "InProgress");
    public Task HoldTaskAsync(WorkTaskDto task) => UpdateTaskStatusAsync(task, "OnHold");
    public Task CompleteTaskAsync(WorkTaskDto task) => UpdateTaskStatusAsync(task, "Completed");

    public async Task ApproveOrRejectTaskAsync(WorkTaskDto task, bool approved)
    {
        WorkDeskLoading = true;
        WorkDeskError = "";

        try
        {
            await _api.ApproveWorkTaskAsync(_appState.CompanyId, task.TaskId, new ApproveTaskRequest
            {
                ActorExtension = _appState.Extension,
                Approved = approved,
                Note = approved ? "Approved" : "Rejected"
            });

            await LoadTasksAsync();
            await LoadDashboardAsync();

            _appState.Status = approved ? "Task approved" : "Task rejected";
        }
        catch (Exception ex)
        {
            WorkDeskError = ex.Message;
            _appState.Status = "Approval update failed";
        }
        finally
        {
            WorkDeskLoading = false;
        }
    }

    public async Task AddCommentAsync(WorkTaskDto task, string message)
    {
        WorkDeskLoading = true;
        WorkDeskError = "";

        try
        {
            await _api.AddWorkTaskCommentAsync(_appState.CompanyId, task.TaskId,
                new AddWorkTaskCommentRequest { ExtensionNumber = _appState.Extension, Body = message });

            await LoadTasksAsync();
            await LoadDashboardAsync();
            await LoadTaskDetailAsync(task);

            _appState.Status = "Comment added";
        }
        catch (Exception ex)
        {
            WorkDeskError = ex.Message;
            _appState.Status = "Failed to add comment";
        }
        finally
        {
            WorkDeskLoading = false;
        }
    }

    public async Task AddReplyAsync(WorkTaskDto task, WorkTaskCommentDto parentComment, string message)
    {
        WorkDeskLoading = true;
        WorkDeskError = "";

        try
        {
            await _api.AddWorkTaskCommentAsync(_appState.CompanyId, task.TaskId, new AddWorkTaskCommentRequest
            {
                ExtensionNumber = _appState.Extension,
                Body = message,
                ParentCommentId = parentComment.CommentId
            });

            await LoadTasksAsync();
            await LoadDashboardAsync();
            await LoadTaskDetailAsync(task);

            _appState.Status = "Reply added";
        }
        catch (Exception ex)
        {
            WorkDeskError = ex.Message;
            _appState.Status = "Failed to add reply";
        }
        finally
        {
            WorkDeskLoading = false;
        }
    }

    public async Task ToggleChecklistItemAsync(WorkTaskDto task, WorkTaskChecklistItemDto item, bool completed)
    {
        WorkDeskLoading = true;
        WorkDeskError = "";

        try
        {
            await _api.CompleteChecklistItemAsync(_appState.CompanyId, task.TaskId, item.ChecklistItemId,
                new CompleteChecklistItemRequest { ActorExtension = _appState.Extension, IsCompleted = completed });

            await LoadTasksAsync();
            await LoadDashboardAsync();
            await LoadTaskDetailAsync(task);

            _appState.Status = "Checklist updated";
        }
        catch (Exception ex)
        {
            WorkDeskError = ex.Message;
            _appState.Status = "Failed to update checklist";
        }
        finally
        {
            WorkDeskLoading = false;
        }
    }

    /// <summary>
    /// Port of uploadWorkDeskAttachment(). Original read bytes from an Android content:// URI
    /// via ContentResolver; on Windows, use Microsoft.Maui.Storage.FilePicker in the page's
    /// code-behind to get a stream/filename, then call this with the result.
    /// </summary>
    public async Task UploadAttachmentAsync(WorkTaskDto task, Stream fileStream, string fileName, string contentType)
    {
        WorkDeskLoading = true;
        WorkDeskError = "";

        try
        {
            await _api.UploadWorkTaskAttachmentAsync(_appState.CompanyId, task.TaskId, fileStream, fileName, contentType, _appState.Extension);

            await LoadTasksAsync();
            await LoadDashboardAsync();
            await LoadTaskDetailAsync(task);

            _appState.Status = "Attachment uploaded";
        }
        catch (Exception ex)
        {
            WorkDeskError = ex.Message;
            _appState.Status = "Attachment upload failed";
        }
        finally
        {
            WorkDeskLoading = false;
        }
    }

    /// <summary>
    /// Port of openWorkDeskAttachment(): resolves a relative URL against the API base and
    /// opens it externally. Original used an Android ACTION_VIEW Intent; MAUI's
    /// Launcher.OpenAsync is the equivalent (opens in the system default browser).
    /// </summary>
    public async Task OpenAttachmentAsync(string url)
    {
        var cleanUrl = url.Trim();
        if (string.IsNullOrEmpty(cleanUrl))
        {
            _appState.Status = "Attachment link is empty";
            return;
        }

        var browserUrl = cleanUrl.StartsWith("http://", StringComparison.OrdinalIgnoreCase)
                          || cleanUrl.StartsWith("https://", StringComparison.OrdinalIgnoreCase)
            ? cleanUrl
            : cleanUrl.StartsWith('/')
                ? $"https://softworks-us.org{cleanUrl}"
                : $"https://softworks-us.org/{cleanUrl}";

        try
        {
            await Launcher.Default.OpenAsync(browserUrl);
        }
        catch
        {
            _appState.Status = "Unable to open attachment";
        }
    }

    /// <summary>
    /// Port of downloadTaskReportPdf(). Original wrote to Android's MediaStore Downloads
    /// collection; on Windows, save via a FileSaver/SaveFilePicker in the page's code-behind
    /// after this returns the stream, or adapt to write directly to the user's Downloads
    /// folder via Windows.Storage APIs. Returns the PDF stream for the caller to persist.
    /// </summary>
    public async Task<Stream?> DownloadTaskReportAsync(WorkTaskDto task)
    {
        if (IsDownloadingTaskReport) return null;
        IsDownloadingTaskReport = true;

        try
        {
            _appState.Status = "Downloading task report...";
            var stream = await _api.DownloadSingleTaskReportPdfAsync(_appState.CompanyId, task.TaskId, _appState.Extension);

            _appState.Status = "Task report ready";
            await LoadTasksAsync();
            await LoadDashboardAsync();
            await LoadTaskDetailAsync(task);

            return stream;
        }
        catch (Exception ex)
        {
            _appState.Status = ex.Message;
            return null;
        }
        finally
        {
            IsDownloadingTaskReport = false;
        }
    }
}
