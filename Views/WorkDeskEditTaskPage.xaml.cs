using SipCoreMobile.Controls;
using SipCoreMobile.Models;
using SipCoreMobile.Models.Api;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

/// <summary>Port of WorkDeskEditTaskView(). Near-identical structure to
/// WorkDeskCreateTaskPage, pre-filled from an existing task/detail and calling
/// WorkDeskViewModel.UpdateTaskAsync() instead of CreateTaskAsync() -- mirrors how the
/// original kept CreateTaskView/EditTaskView as two separate (largely duplicated) files
/// rather than one shared component.</summary>
public partial class WorkDeskEditTaskPage : ContentPage
{
    private readonly AppStateViewModel _appState;
    private readonly WorkDeskViewModel _viewModel;
    private readonly WorkTaskDto _task;
    private readonly WorkTaskDetailResponse? _detail;

    private string _priority;
    private List<string> _selectedAssignees;
    private List<string> _selectedWatchers;
    private readonly List<CreateWorkTaskChecklistItemRequest> _checklistItems;
    private string _dueDate;

    private List<ContactUi> _workDeskContacts = new();
    private readonly bool _isCreator;

    public WorkDeskEditTaskPage(AppStateViewModel appState, WorkDeskViewModel viewModel, WorkTaskDto task, WorkTaskDetailResponse? detail)
    {
        InitializeComponent();
        _appState = appState;
        _viewModel = viewModel;
        _task = task;
        _detail = detail;

        var taskDetail = detail?.Task ?? task;

        _priority = string.IsNullOrWhiteSpace(taskDetail.Priority) ? "Normal" : taskDetail.Priority;
        _dueDate = taskDetail.DueDate ?? "";

        _selectedAssignees = detail?.Assignees.Select(a => a.ExtensionNumber).Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToList() ?? new();
        if (_selectedAssignees.Count == 0) _selectedAssignees = new List<string> { _appState.Extension };

        _selectedWatchers = detail?.Watchers.Where(w => w.IsActive).Select(w => w.ExtensionNumber)
            .Where(e => !string.IsNullOrWhiteSpace(e)).Distinct().ToList() ?? new();

        _checklistItems = detail?.Checklist.Select(c => new CreateWorkTaskChecklistItemRequest
        {
            Title = c.Title,
            Description = string.IsNullOrWhiteSpace(c.Description) ? null : c.Description
        }).ToList() ?? new();

        _isCreator = string.Equals(taskDetail.CreatedByExtension.Trim(), _appState.Extension.Trim(), StringComparison.OrdinalIgnoreCase);

        BindingContext = viewModel;
        TitleEntry.Text = taskDetail.Title;
        DescriptionEditor.Text = taskDetail.Description ?? "";
        RequiresApprovalSwitch.IsToggled = taskDetail.RequiresApproval;

        if (DateTime.TryParse(_dueDate, out var due)) DueDatePicker.Date = due;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _workDeskContacts = _appState.Contacts
            .Where(c => !string.Equals(c.Extension.Trim(), _appState.Extension.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(c => c.CanUseWorkDesk)
            .OrderBy(c => c.IsExternal).ThenBy(c => c.CompanyName).ThenBy(c => c.DisplayName)
            .ToList();

        TemplatesSection.IsVisible = false; // original also has no template selector in edit mode

        RefreshPriorityButtons();
        RefreshAssignmentSection();
        RefreshWatchersButton();
        RefreshChecklist();
        RefreshSubmitState();
    }

    private void RefreshPriorityButtons()
    {
        SetPriorityButtonStyle(NormalPriorityButton, _priority == "Normal", "#22C55E");
        SetPriorityButtonStyle(MediumPriorityButton, _priority == "Medium", "#F59E0B");
        SetPriorityButtonStyle(HighPriorityButton, _priority == "High", "#EF4444");
    }

    private static void SetPriorityButtonStyle(Button button, bool selected, string colorHex)
    {
        button.BackgroundColor = selected ? Color.FromArgb(colorHex) : Color.FromArgb("#14FFFFFF");
        button.TextColor = selected ? Colors.White : Color.FromArgb(colorHex);
    }

    private string FinalTeamLead() => _selectedAssignees.FirstOrDefault() ?? "";

    private List<string> FinalWatchers() =>
        _selectedWatchers.Where(w => !_selectedAssignees.Any(a => string.Equals(a.Trim(), w.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Distinct().ToList();

    private void RefreshAssignmentSection()
    {
        AssignToSelfRow.IsVisible = false; // edit mode always shows the "Assign Others" picker directly
        AssignOthersRow.Configure("\uF0C0", "Assignees", $"{_selectedAssignees.Count} selected", true);
        SelectAssigneesButton.IsVisible = true;

        var teamLead = FinalTeamLead();
        TeamLeadLabel.Text = string.IsNullOrEmpty(teamLead) ? "No assignee selected" : teamLead;

        RefreshWatchersButton();
        RefreshSubmitState();
    }

    private void RefreshWatchersButton() => SelectWatchersButton.Text = $"Select Watchers ({FinalWatchers().Count})";

    private void RefreshChecklist()
    {
        ChecklistItemsList.Children.Clear();

        foreach (var item in _checklistItems)
        {
            var row = new WorkDeskChecklistRow();
            row.Configure(item.Title, item.Description ?? "");
            row.RemoveClicked += (_, _) =>
            {
                _checklistItems.Remove(item);
                RefreshChecklist();
            };
            ChecklistItemsList.Children.Add(row);
        }

        NoChecklistLabel.IsVisible = _checklistItems.Count == 0;
    }

    private void RefreshSubmitState()
    {
        SubmitButton.IsEnabled = !string.IsNullOrWhiteSpace(TitleEntry.Text) && _selectedAssignees.Count > 0 && !_viewModel.IsEditingTask;
    }

    private void OnBackTapped(object? sender, TappedEventArgs e) => Navigation.PopAsync();

    private void OnNormalPriorityClicked(object? sender, EventArgs e) { _priority = "Normal"; RefreshPriorityButtons(); }
    private void OnMediumPriorityClicked(object? sender, EventArgs e) { _priority = "Medium"; RefreshPriorityButtons(); }
    private void OnHighPriorityClicked(object? sender, EventArgs e) { _priority = "High"; RefreshPriorityButtons(); }

    private void OnAssignToSelfTapped(object? sender, EventArgs e) { /* not shown in edit mode */ }

    private async void OnAssignOthersTapped(object? sender, EventArgs e) => await OpenAssigneePickerAsync();
    private async void OnSelectAssigneesClicked(object? sender, EventArgs e) => await OpenAssigneePickerAsync();

    private async Task OpenAssigneePickerAsync()
    {
        _selectedAssignees = await WorkDeskContactPickerPage.ShowAsync(Navigation, "Select Assignees", _workDeskContacts, _selectedAssignees);
        RefreshAssignmentSection();
    }

    private async void OnSelectWatchersClicked(object? sender, EventArgs e)
    {
        _selectedWatchers = await WorkDeskContactPickerPage.ShowAsync(Navigation, "Select Watchers", _workDeskContacts, _selectedWatchers);
        RefreshWatchersButton();
    }

    private void OnAddChecklistItemClicked(object? sender, EventArgs e)
    {
        var title = ChecklistTitleEntry.Text?.Trim() ?? "";
        var description = ChecklistDescriptionEditor.Text?.Trim() ?? "";
        if (string.IsNullOrEmpty(title)) return;

        _checklistItems.Add(new CreateWorkTaskChecklistItemRequest
        {
            Title = title,
            Description = string.IsNullOrEmpty(description) ? null : description
        });

        ChecklistTitleEntry.Text = "";
        ChecklistDescriptionEditor.Text = "";
        RefreshChecklist();
    }

    private void OnDueDateSelected(object? sender, DateChangedEventArgs e) => _dueDate = e.NewDate.ToString("yyyy-MM-dd");
    private void OnClearDueDateClicked(object? sender, EventArgs e) => _dueDate = "";

    private async void OnSubmitClicked(object? sender, EventArgs e)
    {
        await _viewModel.UpdateTaskAsync(
            _task,
            (TitleEntry.Text ?? "").Trim(),
            (DescriptionEditor.Text ?? "").Trim(),
            _priority,
            string.IsNullOrEmpty(_dueDate.Trim()) ? null : _dueDate.Trim(),
            RequiresApprovalSwitch.IsToggled);

        ErrorLabel.IsVisible = !string.IsNullOrEmpty(_viewModel.EditTaskError);
        ErrorLabel.Text = _viewModel.EditTaskError;

        if (string.IsNullOrEmpty(_viewModel.EditTaskError))
        {
            await Navigation.PopAsync();
        }
    }
}
