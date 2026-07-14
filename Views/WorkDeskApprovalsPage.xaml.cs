using Microsoft.Maui.Controls;
using Microsoft.UI.Xaml.Controls.Primitives;
using SipCoreMobile.Models;
using SipCoreMobile.Models.Api;
using SipCoreMobile.Services;
using SipCoreMobile.ViewModels;
using System;
using System.Collections.Generic;
using System.Linq;

namespace SipCoreMobile.Views;

public partial class WorkDeskApprovalsPage : ContentPage
{
    private readonly AppStateViewModel _appState;
    private readonly WorkDeskViewModel _viewModel;
    private readonly ChatViewModel _chatViewModel;
    private WorkTaskDto? _selectedTask;

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
        ApprovalsList.ItemsSource = approvalTasks;

        // Automatically focus the first request on screen load
        if (_selectedTask == null && approvalTasks.Any())
        {
            ApprovalsList.SelectedItem = approvalTasks.First();
        }
    }

    private void ShowEmpty(string title, string message)
    {
        ApprovalsList.IsVisible = false;
        EmptyState.IsVisible = true;
        DetailContainer.IsVisible = false;
        EmptyTitleLabel.Text = title;
        EmptyMessageLabel.Text = message;
    }

    private void OnApprovalSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (e.CurrentSelection.FirstOrDefault() is not WorkTaskDto task)
            return;

        _selectedTask = task;
        DetailContainer.IsVisible = true;

        var contacts = _appState.Contacts.ToList();

        DetailTitleLabel.Text = string.IsNullOrWhiteSpace(task.Title) ? "Untitled Task" : task.Title;
        DetailSubmitterLabel.Text = $"Created by {WorkDeskContactHelpers.DisplayName(task.CreatedByExtension, contacts)} (Ext. {task.CreatedByExtension})";
        DetailNotesLabel.Text = string.IsNullOrWhiteSpace(task.Description) ? "No description provided." : task.Description;

        bool showActionButtons = CanShowApprovalButtons(task);
        ApproveButton.IsVisible = showActionButtons;
        RejectButton.IsVisible = showActionButtons;
    }

    private async void OnApproveClicked(object sender, EventArgs e)
    {
        if (_selectedTask == null) return;
        await _viewModel.ApproveOrRejectTaskAsync(_selectedTask, true);
        Render();
    }

    private async void OnRejectClicked(object sender, EventArgs e)
    {
        if (_selectedTask == null) return;
        await _viewModel.ApproveOrRejectTaskAsync(_selectedTask, false);
        Render();
    }

    private async void OnOpenDiscussionClicked(object sender, EventArgs e)
    {
        if (_selectedTask == null) return;
        await Navigation.PushAsync(new WorkDeskTaskDetailPage(_appState, _viewModel, _chatViewModel, _selectedTask));
    }

    private void OnSearchTextChanged(object sender, TextChangedEventArgs e)
    {
        var query = e.NewTextValue?.Trim() ?? "";
        var filteredTasks = _viewModel.Tasks
            .Where(t => t.RequiresApproval && string.Equals(t.CreatedByExtension.Trim(), _appState.Extension.Trim(), StringComparison.OrdinalIgnoreCase))
            .Where(t => string.IsNullOrEmpty(query) || t.Title.Contains(query, StringComparison.OrdinalIgnoreCase))
            .ToList();

        ApprovalsList.ItemsSource = filteredTasks;
    }

    private void OnRefreshClicked(object sender, EventArgs e) => OnAppearing();

    private async void OnBackTapped(object? sender, EventArgs e) => await Navigation.PopAsync();
}