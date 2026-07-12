using SipCoreMobile.Controls;
using SipCoreMobile.Models;
using SipCoreMobile.Models.Api;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

/// <summary>Port of WorkDeskCreateTaskView(). See also WorkDeskEditTaskPage -- the original
/// has two nearly-identical files (CreateTaskView/EditTaskView) rather than one shared
/// component, so this port keeps that same duplication rather than introducing a shared base
/// class that XAML named elements don't cross cleanly.</summary>
public partial class WorkDeskCreateTaskPage : ContentPage
{
    private readonly AppStateViewModel _appState;
    private readonly WorkDeskViewModel _viewModel;

    private string _priority = "Normal";
    private bool _assignToSelf = true;
    private List<string> _selectedAssignees = new();
    private List<string> _selectedWatchers = new();
    private readonly List<CreateWorkTaskChecklistItemRequest> _checklistItems = new();
    private string _dueDate = "";
    private string _selectedTemplateId = "";

    private List<ContactUi> _workDeskContacts = new();
    private bool _isCreator;

    public WorkDeskCreateTaskPage(AppStateViewModel appState, WorkDeskViewModel viewModel)
    {
        InitializeComponent();
        _appState = appState;
        _viewModel = viewModel;
        BindingContext = viewModel;

        _selectedAssignees = new List<string> { _appState.Extension };
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();

        _isCreator = _appState.WorkTaskCreators.Any(c =>
            string.Equals(c.ExtensionNumber.Trim(), _appState.Extension.Trim(), StringComparison.OrdinalIgnoreCase)
            && c.IsActive && c.CanCreateTasks);

        _workDeskContacts = _appState.Contacts
            .Where(c => !string.Equals(c.Extension.Trim(), _appState.Extension.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(c => c.CanUseWorkDesk)
            .OrderBy(c => c.IsExternal).ThenBy(c => c.CompanyName).ThenBy(c => c.DisplayName)
            .ToList();

        RebuildTemplates();
        RefreshPriorityButtons();
        RefreshAssignmentSection();
        RefreshWatchersButton();
        RefreshChecklist();
        RefreshSubmitState();
    }

    private void RebuildTemplates()
    {
        TemplatesList.Children.Clear();
        TemplatesSection.IsVisible = _viewModel.Templates.Count > 0;

        foreach (var template in _viewModel.Templates)
        {
            var row = new WorkDeskSelectableRow();
            row.Configure("\uF15C", string.IsNullOrWhiteSpace(template.Name) ? "Task Template" : template.Name,
                string.IsNullOrWhiteSpace(template.Description) ? $"Priority: {template.DefaultPriority}" : template.Description,
                _selectedTemplateId == template.TemplateId);

            row.Tapped += (_, _) =>
            {
                _selectedTemplateId = template.TemplateId;
                TitleEntry.Text = template.Name;
                DescriptionEditor.Text = template.Description;
                _priority = string.IsNullOrWhiteSpace(template.DefaultPriority) ? "Normal" : template.DefaultPriority;
                RebuildTemplates();
                RefreshPriorityButtons();
            };

            TemplatesList.Children.Add(row);
        }
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

    /// <summary>Port of finalAssignees/finalTeamLead derivation.</summary>
    private List<string> FinalAssignees()
    {
        if (!_isCreator) return new List<string> { _appState.Extension };
        if (_assignToSelf) return new List<string> { _appState.Extension };

        return _selectedAssignees
            .Where(a => !string.Equals(a.Trim(), _appState.Extension.Trim(), StringComparison.OrdinalIgnoreCase))
            .Distinct()
            .ToList();
    }

    private string FinalTeamLead() => FinalAssignees().FirstOrDefault() ?? "";

    private List<string> FinalWatchers() =>
        _selectedWatchers.Where(w => !FinalAssignees().Any(a => string.Equals(a.Trim(), w.Trim(), StringComparison.OrdinalIgnoreCase)))
            .Distinct().ToList();

    private void RefreshAssignmentSection()
    {
        if (!_isCreator)
        {
            AssignToSelfRow.Configure("\uF007", "Assign to Me", "Non-creators can only assign tasks to themselves.", true);
            AssignOthersRow.IsVisible = false;
            SelectAssigneesButton.IsVisible = false;
        }
        else
        {
            AssignToSelfRow.IsVisible = true;
            AssignOthersRow.IsVisible = true;
            AssignToSelfRow.Configure("\uF007", "Assign to Me", "Only you will be assigned.", _assignToSelf);
            AssignOthersRow.Configure("\uF0C0", "Assign Others", $"{FinalAssignees().Count} selected", !_assignToSelf);
            SelectAssigneesButton.IsVisible = !_assignToSelf;
        }

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
        var canSubmit = !string.IsNullOrWhiteSpace(TitleEntry.Text) && FinalAssignees().Count > 0 && !_viewModel.WorkDeskLoading;
        SubmitButton.IsEnabled = canSubmit;
    }

    private void OnBackTapped(object? sender, TappedEventArgs e) => Navigation.PopAsync();

    private void OnNormalPriorityClicked(object? sender, EventArgs e) { _priority = "Normal"; RefreshPriorityButtons(); }
    private void OnMediumPriorityClicked(object? sender, EventArgs e) { _priority = "Medium"; RefreshPriorityButtons(); }
    private void OnHighPriorityClicked(object? sender, EventArgs e) { _priority = "High"; RefreshPriorityButtons(); }

    private void OnAssignToSelfTapped(object? sender, EventArgs e)
    {
        if (!_isCreator) return;
        _assignToSelf = true;
        _selectedAssignees = new List<string> { _appState.Extension };
        _selectedWatchers = _selectedWatchers.Where(w => !string.Equals(w.Trim(), _appState.Extension.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        RefreshAssignmentSection();
    }

    private async void OnAssignOthersTapped(object? sender, EventArgs e)
    {
        if (!_isCreator) return;
        _assignToSelf = false;
        _selectedAssignees = _selectedAssignees.Where(a => !string.Equals(a.Trim(), _appState.Extension.Trim(), StringComparison.OrdinalIgnoreCase)).ToList();
        RefreshAssignmentSection();
        await OpenAssigneePickerAsync();
    }

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
        await _viewModel.CreateTaskAsync(
            (TitleEntry.Text ?? "").Trim(),
            (DescriptionEditor.Text ?? "").Trim(),
            _priority,
            _dueDate.Trim(),
            RequiresApprovalSwitch.IsToggled,
            FinalAssignees(),
            FinalTeamLead(),
            FinalWatchers(),
            _checklistItems);

        ErrorLabel.IsVisible = !string.IsNullOrEmpty(_viewModel.WorkDeskError);
        ErrorLabel.Text = _viewModel.WorkDeskError;

        if (string.IsNullOrEmpty(_viewModel.WorkDeskError))
        {
            await Navigation.PopAsync();
        }
    }
}
