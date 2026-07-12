using SipCoreMobile.Models;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

/// <summary>
/// Port of WorkDeskHomeView's `WorkDeskScreen.Dashboard` branch (stat derivation) plus
/// WorkDeskDashboardView/WorkDeskHeroCard/WorkDeskQuickActions/WorkDeskLiveTeamCard/
/// WorkDeskRecentActivityCard. This is the WorkDesk module's entry point; Tasks list is also
/// built this batch. Approvals/CreateTask/EditTask/TaskDetail/Discussion/Team are follow-up
/// sub-batches -- their buttons here show an informational stub for now (see OnComingSoon).
/// </summary>
public partial class WorkDeskDashboardPage : ContentPage
{
    private readonly AppStateViewModel _appState;
    private readonly WorkDeskViewModel _viewModel;
    private readonly ChatViewModel _chatViewModel;

    public WorkDeskDashboardPage(AppStateViewModel appState, WorkDeskViewModel viewModel, ChatViewModel chatViewModel)
    {
        InitializeComponent();
        _appState = appState;
        _viewModel = viewModel;
        _chatViewModel = chatViewModel;
        BindingContext = viewModel;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.OpenWorkDeskHomeAsync();
        Refresh();
    }

    /// <summary>Port of the Dashboard branch's openTaskCount/approvalCount/dueTodayCount/onlineContacts/recentActivities.</summary>
    private void Refresh()
    {
        SubtitleLabel.Text = $"Extension {_appState.Extension}";

        var userName = string.IsNullOrEmpty(_appState.DisplayName) ? _appState.Extension : _appState.DisplayName;
        GreetingLabel.Text = $"Hello, {CleanName(userName, _appState.Extension)} ({_appState.Extension})";
        CompanyLabel.Text = string.IsNullOrEmpty(_appState.CompanyName) ? "No Company" : _appState.CompanyName;

        var dashboard = _viewModel.Dashboard;
        var tasks = _viewModel.Tasks;

        var openTaskCount = dashboard?.Pending ?? tasks.Count(t =>
            !string.Equals(t.Status, "completed", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(t.Status, "done", StringComparison.OrdinalIgnoreCase) &&
            !string.Equals(t.Status, "cancelled", StringComparison.OrdinalIgnoreCase));

        var approvalCount = tasks.Count(t =>
            t.RequiresApproval &&
            (string.Equals(t.ApprovalStatus, "PendingApproval", StringComparison.OrdinalIgnoreCase) ||
             string.Equals(t.ApprovalStatus, "Pending", StringComparison.OrdinalIgnoreCase)) &&
            string.Equals(t.CreatedByExtension.Trim(), _appState.Extension.Trim(), StringComparison.OrdinalIgnoreCase));

        var dueTodayCount = dashboard?.Overdue ?? tasks.Count(t => !string.IsNullOrWhiteSpace(t.DueDate));

        var onlineStatuses = new[] { "Online", "Available", "Registered", "Active", "Reachable", "Ready", "Connected" };
        var onlineContacts = _appState.Contacts.Count(c => c.CanViewPresence &&
            onlineStatuses.Any(s => string.Equals(c.Status, s, StringComparison.OrdinalIgnoreCase)));

        OpenTasksValue.Text = openTaskCount.ToString();
        ApprovalsValue.Text = approvalCount.ToString();
        DueTodayValue.Text = dueTodayCount.ToString();
        TeamOnlineLabel.Text = $"{onlineContacts} WorkDesk contacts online now";

        var recentActivities = _viewModel.TaskDetailsById.Values
            .SelectMany(d => d.Activities)
            .OrderByDescending(a => a.CreatedAt)
            .Take(3)
            .ToList();

        ActivityList.Children.Clear();

        if (recentActivities.Count == 0)
        {
            ActivityList.Children.Add(BuildActivityItem("No recent activity yet", ""));
        }
        else
        {
            foreach (var activity in recentActivities)
            {
                var actor = HomeDisplayName(activity.ActorExtension);
                var companyLine = HomeCompanyLine(activity.ActorExtension);
                var text = $"{(string.IsNullOrEmpty(activity.ActivityText) ? activity.ActivityType : activity.ActivityText)} • {actor}";
                ActivityList.Children.Add(BuildActivityItem(text, companyLine));
            }
        }
    }

    private static View BuildActivityItem(string text, string subText)
    {
        var stack = new HorizontalStackLayout { Spacing = 10 };
        stack.Add(new Ellipse
        {
            Fill = new SolidColorBrush(Color.FromArgb("#0057B8")),
            WidthRequest = 8, HeightRequest = 8, VerticalOptions = LayoutOptions.Start, Margin = new Thickness(0, 5, 0, 0)
        });

        var textStack = new VerticalStackLayout();
        textStack.Add(new Label { Text = text, TextColor = Color.FromArgb("#C2FFFFFF"), FontSize = 13 });
        if (!string.IsNullOrEmpty(subText))
        {
            textStack.Add(new Label { Text = subText, TextColor = Color.FromArgb("#73FFFFFF"), FontSize = 11 });
        }

        stack.Add(textStack);
        return stack;
    }

    /// <summary>Port of workDeskCleanName().</summary>
    private static string CleanName(string name, string extension)
    {
        var cleanExtension = extension.Trim();
        var cleaned = name.Replace($"({cleanExtension})", "").Trim();
        return string.IsNullOrEmpty(cleaned) ? cleanExtension : cleaned;
    }

    /// <summary>Port of workDeskHomeDisplayName()/workDeskContactDisplayName().</summary>
    private string HomeDisplayName(string extension)
    {
        var clean = extension.Trim();
        if (string.IsNullOrEmpty(clean)) return "Unknown";

        var contact = _appState.Contacts.FirstOrDefault(c => string.Equals(c.Extension.Trim(), clean, StringComparison.OrdinalIgnoreCase));
        if (contact is null) return clean;

        var cleanExt = contact.Extension.Trim();
        var cleanName = contact.DisplayName.Replace($"({cleanExt})", "").Trim();
        if (string.IsNullOrEmpty(cleanName)) cleanName = cleanExt;
        return $"{cleanName} ({cleanExt})";
    }

    /// <summary>Port of workDeskHomeCompanyLine().</summary>
    private string HomeCompanyLine(string extension)
    {
        var contact = _appState.Contacts.FirstOrDefault(c => string.Equals(c.Extension.Trim(), extension.Trim(), StringComparison.OrdinalIgnoreCase));
        if (contact is null) return "";

        if (contact.IsExternal && !string.IsNullOrWhiteSpace(contact.CompanyName)) return $"{contact.CompanyName} • External";
        return contact.CompanyName ?? "";
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Navigation.PopAsync();

    private async void OnCreateTaskTapped(object? sender, TappedEventArgs e) =>
        await Navigation.PushAsync(new WorkDeskCreateTaskPage(_appState, _viewModel));

    private async void OnOpenTasksTapped(object? sender, TappedEventArgs e) =>
        await Navigation.PushAsync(new WorkDeskTaskListPage(_appState, _viewModel, _chatViewModel));

    private async void OnOpenApprovalsTapped(object? sender, TappedEventArgs e) =>
        await Navigation.PushAsync(new WorkDeskApprovalsPage(_appState, _viewModel, _chatViewModel));

    private async void OnOpenTeamTapped(object? sender, TappedEventArgs e) =>
        await Navigation.PushAsync(new WorkDeskTeamPage(_appState, _chatViewModel));

    private async Task OnComingSoon(string feature) =>
        await DisplayAlert(feature, $"The WorkDesk {feature} screen is coming in a follow-up update.", "OK");
}