using System.Collections.Specialized;
using SipCoreMobile.Models.Api;
using SipCoreMobile.ViewModels;

namespace SipCoreMobile.Views;

public partial class MeetingHomePage : ContentPage
{
    private readonly AppStateViewModel _appState;
    private readonly MeetingsViewModel _viewModel;

    public MeetingHomePage(AppStateViewModel appState, MeetingsViewModel viewModel)
    {
        InitializeComponent();
        _appState = appState;
        _viewModel = viewModel;
        BindingContext = viewModel;

        _viewModel.Meetings.CollectionChanged += OnMeetingsChanged;
    }

    protected override async void OnAppearing()
    {
        base.OnAppearing();
        await _viewModel.LoadMeetingsAsync();
        ApplyFilter(SearchBox.Text ?? "");
    }

    private void OnMeetingsChanged(object? sender, NotifyCollectionChangedEventArgs e) => ApplyFilter(SearchBox.Text ?? "");
    private void OnSearchTextChanged(object? sender, TextChangedEventArgs e) => ApplyFilter(e.NewTextValue ?? "");

    /// <summary>Port of MeetingHomeView's filteredMeetings derivation.</summary>
    private void ApplyFilter(string search)
    {
        var q = search.Trim().ToLowerInvariant();

        var filtered = string.IsNullOrEmpty(q)
            ? _viewModel.Meetings.ToList()
            : _viewModel.Meetings.Where(m =>
                m.Title.ToLowerInvariant().Contains(q) ||
                m.ConferenceNumber.ToLowerInvariant().Contains(q) ||
                m.Status.ToLowerInvariant().Contains(q) ||
                m.CreatedByExtension.ToLowerInvariant().Contains(q)).ToList();

        if (_viewModel.Meetings.Count == 0)
        {
            EmptyState.Title = "No meetings yet";
            EmptyState.Body = "Create a meeting and share the conference number with your team.";
            EmptyState.IsVisible = true;
            MeetingsList.IsVisible = false;
        }
        else if (filtered.Count == 0)
        {
            EmptyState.Title = "No meeting found";
            EmptyState.Body = "Try another search keyword.";
            EmptyState.IsVisible = true;
            MeetingsList.IsVisible = false;
        }
        else
        {
            EmptyState.IsVisible = false;
            MeetingsList.IsVisible = true;
            MeetingsList.ItemsSource = filtered;
        }
    }

    private async void OnCreateMeetingClicked(object? sender, EventArgs e) =>
        await Navigation.PushAsync(new MeetingCreatePage(_appState, _viewModel));

    private async void OnRefreshClicked(object? sender, EventArgs e) => await _viewModel.LoadMeetingsAsync();

    private async void OnMeetingTapped(object? sender, SipCoreMeetingDto meeting)
    {
        _viewModel.OpenMeeting(meeting);
        await Navigation.PushAsync(new MeetingDetailPage(_appState, _viewModel));
    }

    private async void OnJoinTapped(object? sender, string conferenceNumber) =>
        await _viewModel.JoinMeetingAsync(conferenceNumber);
}
