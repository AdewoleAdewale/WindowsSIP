using System.Collections.Specialized;
using SipCoreMobile.Controls;
using SipCoreMobile.Models.Api;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class WorkDeskTaskListPage : ContentPage
{
    private readonly AppStateViewModel _appState;
    private readonly WorkDeskViewModel _viewModel;
    private readonly ChatViewModel _chatViewModel;

    public WorkDeskTaskListPage(AppStateViewModel appState, WorkDeskViewModel viewModel, ChatViewModel chatViewModel)
    {
        InitializeComponent();
        _appState = appState;
        _viewModel = viewModel;
        _chatViewModel = chatViewModel;
        BindingContext = viewModel;

        _viewModel.Tasks.CollectionChanged += OnTasksChanged;
    }

    protected override void OnAppearing()
    {
        base.OnAppearing();
        SubtitleLabel.Text = $"Extension {_appState.Extension}";
        ApplyFilter(SearchBox.Text ?? "");
    }

    private void OnTasksChanged(object? sender, NotifyCollectionChangedEventArgs e) => ApplyFilter(SearchBox.Text ?? "");
    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e) => ApplyFilter(e.NewTextValue ?? "");

    /// <summary>Port of WorkDeskTaskListView's filteredTasks derivation.</summary>
    private void ApplyFilter(string search)
    {
        LoadingIndicator.IsRunning = _viewModel.WorkDeskLoading;
        LoadingIndicator.IsVisible = _viewModel.WorkDeskLoading;

        if (_viewModel.WorkDeskLoading)
        {
            TasksList.IsVisible = false;
            EmptyState.IsVisible = false;
            return;
        }

        if (!string.IsNullOrEmpty(_viewModel.WorkDeskError))
        {
            ShowEmpty("Unable to load tasks", _viewModel.WorkDeskError);
            return;
        }

        var query = search.Trim();

        bool Matches(WorkTaskDto task)
        {
            if (string.IsNullOrEmpty(query)) return true;

            var creatorName = DisplayName(task.CreatedByExtension);
            var teamLeadName = DisplayName(task.TeamLeadExtension);
            var creatorCompany = CompanyLine(task.CreatedByExtension);
            var teamLeadCompany = CompanyLine(task.TeamLeadExtension);

            return Contains(task.Title, query) || Contains(task.Description ?? "", query) ||
                   Contains(task.Status, query) || Contains(task.Priority, query) ||
                   Contains(task.ApprovalStatus, query) || Contains(task.CreatedByExtension, query) ||
                   Contains(task.TeamLeadExtension, query) || Contains(creatorName, query) ||
                   Contains(teamLeadName, query) || Contains(creatorCompany, query) || Contains(teamLeadCompany, query);
        }

        var filtered = _viewModel.Tasks.Where(Matches).ToList();

        SubtitleLabel.Text = $"Extension {_appState.Extension} • {filtered.Count} task{(filtered.Count == 1 ? "" : "s")}";

        if (filtered.Count == 0)
        {
            ShowEmpty("No tasks found", string.IsNullOrEmpty(query) ? "Create your first WorkDesk task." : "No task matches your search.");
            return;
        }

        EmptyState.IsVisible = false;
        TasksList.IsVisible = true;
        TasksList.ItemsSource = filtered;
    }

    private void ShowEmpty(string title, string message)
    {
        TasksList.IsVisible = false;
        EmptyState.IsVisible = true;
        EmptyTitleLabel.Text = title;
        EmptyMessageLabel.Text = message;
    }

    private static bool Contains(string source, string query) =>
        source.Contains(query, StringComparison.OrdinalIgnoreCase);

    private string DisplayName(string extension)
    {
        var clean = extension.Trim();
        if (string.IsNullOrEmpty(clean)) return "";
        var contact = _appState.Contacts.FirstOrDefault(c => string.Equals(c.Extension.Trim(), clean, StringComparison.OrdinalIgnoreCase));
        if (contact is null) return clean;
        var cleanExt = contact.Extension.Trim();
        var cleanName = contact.DisplayName.Replace($"({cleanExt})", "").Trim();
        return string.IsNullOrEmpty(cleanName) ? cleanExt : $"{cleanName} ({cleanExt})";
    }

    private string CompanyLine(string extension)
    {
        var contact = _appState.Contacts.FirstOrDefault(c => string.Equals(c.Extension.Trim(), extension.Trim(), StringComparison.OrdinalIgnoreCase));
        if (contact is null) return "";
        return contact.IsExternal && !string.IsNullOrWhiteSpace(contact.CompanyName) ? $"{contact.CompanyName} • External" : contact.CompanyName ?? "";
    }

    private void OnCardLoaded(object? sender, EventArgs e)
    {
        if (sender is not WorkDeskTaskCard { BindingContext: WorkTaskDto task } card) return;

        _viewModel.TaskDetailsById.TryGetValue(task.TaskId, out var detail);
        card.Configure(task, detail, _appState.Contacts.ToList(), _appState.Extension);
        card.CardTapped -= OnCardTapped;
        card.CardTapped += OnCardTapped;
        card.EditTapped -= OnEditTapped;
        card.EditTapped += OnEditTapped;
    }

    private async void OnCardTapped(object? sender, WorkTaskDto task) =>
        await Navigation.PushAsync(new WorkDeskTaskDetailPage(_appState, _viewModel, _chatViewModel, task));

    private async void OnEditTapped(object? sender, WorkTaskDto task)
    {
        _viewModel.TaskDetailsById.TryGetValue(task.TaskId, out var detail);
        await Navigation.PushAsync(new WorkDeskEditTaskPage(_appState, _viewModel, task, detail));
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Navigation.PopAsync();

    private async void OnCreateTaskTapped(object? sender, TappedEventArgs e) =>
        await Navigation.PushAsync(new WorkDeskCreateTaskPage(_appState, _viewModel));
}
