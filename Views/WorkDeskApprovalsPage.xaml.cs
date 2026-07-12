using SipCoreMobile.Models;
using SipCoreMobile.Models.Api;
using SipCoreMobile.Services;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

/// <summary>Port of WorkDeskApprovalsView(). Approval cards are built in code (same
/// "dynamic rows" approach used throughout the WorkDesk batches) rather than a separate
/// XAML control, since the original's WorkDeskApprovalCard is only used in this one place.</summary>
public partial class WorkDeskApprovalsPage : ContentPage
{
    private readonly AppStateViewModel _appState;
    private readonly WorkDeskViewModel _viewModel;
    private readonly ChatViewModel _chatViewModel;

    public WorkDeskApprovalsPage(AppStateViewModel appState, WorkDeskViewModel viewModel, ChatViewModel chatViewModel)
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
        SubtitleLabel.Text = $"Extension {_appState.Extension}";

        LoadingIndicator.IsRunning = true;
        LoadingIndicator.IsVisible = true;

        await _viewModel.LoadTasksAsync();
        await _viewModel.LoadDashboardAsync();

        LoadingIndicator.IsRunning = false;
        LoadingIndicator.IsVisible = false;

        Render();
    }

    /// <summary>Port of canShowApprovalButtons().</summary>
    private bool CanShowApprovalButtons(WorkTaskDto task)
    {
        var status = task.Status.Trim().ToLowerInvariant();
        var approvalStatus = task.ApprovalStatus.Trim().ToLowerInvariant();
        var createdByMe = string.Equals(task.CreatedByExtension.Trim(), _appState.Extension.Trim(), StringComparison.OrdinalIgnoreCase);
        var awaitingApproval = status is "awaitingapproval" or "awaiting approval" or "pendingapproval" or "pending approval";
        var notProcessed = approvalStatus != "approved" && approvalStatus != "rejected";

        return createdByMe && task.RequiresApproval && awaitingApproval && notProcessed;
    }

    private void Render()
    {
        var approvalTasks = _viewModel.Tasks
            .Where(t => t.RequiresApproval && string.Equals(t.CreatedByExtension.Trim(), _appState.Extension.Trim(), StringComparison.OrdinalIgnoreCase))
            .ToList();

        var pendingCount = approvalTasks.Count(CanShowApprovalButtons);
        var approvedCount = approvalTasks.Count(t => t.ApprovalStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase));
        var rejectedCount = approvalTasks.Count(t => t.ApprovalStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase));

        PendingValue.Text = pendingCount.ToString();
        ApprovedValue.Text = approvedCount.ToString();
        RejectedValue.Text = rejectedCount.ToString();

        if (!string.IsNullOrEmpty(_viewModel.WorkDeskError))
        {
            ShowEmpty("Unable to load approvals", _viewModel.WorkDeskError);
            return;
        }

        if (approvalTasks.Count == 0)
        {
            ShowEmpty("No approval tasks", "Tasks you created that require approval will appear here.");
            return;
        }

        EmptyState.IsVisible = false;
        ApprovalsList.IsVisible = true;
        ApprovalsList.ItemsSource = approvalTasks.Select(BuildCard).ToList();
    }

    private void ShowEmpty(string title, string message)
    {
        ApprovalsList.IsVisible = false;
        EmptyState.IsVisible = true;
        EmptyTitleLabel.Text = title;
        EmptyMessageLabel.Text = message;
    }

    private View BuildCard(WorkTaskDto task)
    {
        var showButtons = CanShowApprovalButtons(task);
        var contacts = _appState.Contacts.ToList();

        var approvalColor = task.ApprovalStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) ? Color.FromArgb("#22C55E")
            : task.ApprovalStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase) ? Color.FromArgb("#EF4444")
            : showButtons ? Color.FromArgb("#A855F7") : Color.FromArgb("#F59E0B");

        var card = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 24 },
            BackgroundColor = Color.FromArgb("#14FFFFFF"),
            Padding = new Thickness(16)
        };

        var outer = new VerticalStackLayout { Spacing = 10 };

        var header = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Auto), new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Auto) }, ColumnSpacing = 12 };
        header.Add(new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 23 },
            BackgroundColor = approvalColor.WithAlpha(0.16f),
            WidthRequest = 46, HeightRequest = 46,
            Content = new Label { Text = "\uF0AE", FontFamily = "MaterialIcons", TextColor = approvalColor, HorizontalOptions = LayoutOptions.Center, VerticalOptions = LayoutOptions.Center }
        }, 0);

        var titleStack = new VerticalStackLayout();
        titleStack.Add(new Label { Text = string.IsNullOrWhiteSpace(task.Title) ? "Untitled Task" : task.Title, TextColor = Colors.White, FontAttributes = FontAttributes.Bold, FontSize = 16 });
        titleStack.Add(new Label { Text = $"Created by {WorkDeskContactHelpers.DisplayName(task.CreatedByExtension, contacts)}", TextColor = Color.FromArgb("#99FFFFFF"), FontSize = 12 });
        var company = WorkDeskContactHelpers.CompanyLine(task.CreatedByExtension, contacts);
        if (!string.IsNullOrEmpty(company)) titleStack.Add(new Label { Text = company, TextColor = Color.FromArgb("#73FFFFFF"), FontSize = 11 });
        header.Add(titleStack, 1);

        header.Add(new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 50 },
            BackgroundColor = approvalColor.WithAlpha(0.18f),
            Padding = new Thickness(10, 4),
            Content = new Label { Text = string.IsNullOrWhiteSpace(task.ApprovalStatus) ? task.Status : task.ApprovalStatus, TextColor = approvalColor, FontSize = 11, FontAttributes = FontAttributes.Bold }
        }, 2);

        outer.Add(header);
        outer.Add(new Label
        {
            Text = string.IsNullOrWhiteSpace(task.Description) ? "No description provided." : task.Description,
            TextColor = Color.FromArgb("#BDFFFFFF"), FontSize = 13, MaxLines = 2, LineBreakMode = LineBreakMode.TailTruncation
        });

        var badges = new HorizontalStackLayout { Spacing = 8 };
        badges.Add(MiniBadge(task.Status));
        badges.Add(MiniBadge(task.Priority));
        badges.Add(MiniBadge(string.IsNullOrWhiteSpace(task.DueDate) ? "No due date" : task.DueDate));
        outer.Add(badges);

        if (showButtons)
        {
            var buttonRow = new Grid { ColumnDefinitions = { new ColumnDefinition(GridLength.Star), new ColumnDefinition(GridLength.Star) }, ColumnSpacing = 10 };
            buttonRow.Add(ApprovalButton("Reject", "#EF4444", async () => { await _viewModel.ApproveOrRejectTaskAsync(task, false); Render(); }), 0);
            buttonRow.Add(ApprovalButton("Approve", "#22C55E", async () => { await _viewModel.ApproveOrRejectTaskAsync(task, true); Render(); }), 1);
            outer.Add(buttonRow);
        }
        else
        {
            var message = task.ApprovalStatus.Equals("Approved", StringComparison.OrdinalIgnoreCase) ? "This task has already been approved."
                : task.ApprovalStatus.Equals("Rejected", StringComparison.OrdinalIgnoreCase) ? "This task has already been rejected."
                : "Approve and Reject buttons appear when task status is Awaiting Approval.";
            outer.Add(new Label { Text = message, TextColor = Color.FromArgb("#94FFFFFF"), FontSize = 11 });
        }

        card.Content = outer;

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await Navigation.PushAsync(new WorkDeskTaskDetailPage(_appState, _viewModel, _chatViewModel, task));
        card.GestureRecognizers.Add(tap);

        return card;
    }

    private static Border MiniBadge(string text) => new()
    {
        StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 50 },
        BackgroundColor = Color.FromArgb("#14FFFFFF"),
        Padding = new Thickness(8, 4),
        Content = new Label { Text = text, TextColor = Color.FromArgb("#C2FFFFFF"), FontSize = 10 }
    };

    private static Border ApprovalButton(string title, string colorHex, Func<Task> onClick)
    {
        var color = Color.FromArgb(colorHex);
        var button = new Border
        {
            StrokeShape = new Microsoft.Maui.Controls.Shapes.RoundRectangle { CornerRadius = 14 },
            BackgroundColor = color.WithAlpha(0.85f),
            Padding = new Thickness(0, 10),
            Content = new Label { Text = title, TextColor = Colors.White, FontAttributes = FontAttributes.Bold, HorizontalOptions = LayoutOptions.Center }
        };

        var tap = new TapGestureRecognizer();
        tap.Tapped += async (_, _) => await onClick();
        button.GestureRecognizers.Add(tap);

        return button;
    }

    private async void OnBackTapped(object? sender, TappedEventArgs e) => await Navigation.PopAsync();
}
