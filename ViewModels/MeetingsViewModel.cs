using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using SipCoreMobile.Models;
using SipCoreMobile.Models.Api;
using SipCoreMobile.Services.Api;

namespace SipCoreMobile.ViewModels;

/// <summary>
/// Port of the meeting-related slice of MainActivity.kt: loadMeetings(), createMeeting(),
/// openMeeting(), joinMeeting(), refreshMeetingLive(), startMeeting(), endMeeting(),
/// lockMeeting(), unlockMeeting(). Feature-scoped per the same pattern as ChatViewModel/
/// GroupsViewModel.
/// </summary>
public partial class MeetingsViewModel : ObservableObject
{
    private readonly ISipCoreApiService _api;
    private readonly AppStateViewModel _appState;

    public ObservableCollection<SipCoreMeetingDto> Meetings { get; } = new();

    [ObservableProperty] private SipCoreMeetingDto? selectedMeeting;
    [ObservableProperty] private bool meetingsLoading;
    [ObservableProperty] private string meetingsError = "";
    [ObservableProperty] private string liveMembersText = "";

    public MeetingsViewModel(ISipCoreApiService api, AppStateViewModel appState)
    {
        _api = api;
        _appState = appState;
    }

    public async Task LoadMeetingsAsync()
    {
        MeetingsLoading = true;
        MeetingsError = "";

        try
        {
            var result = await _api.GetMeetingsAsync(_appState.CompanyId, _appState.Extension);
            Meetings.Clear();
            foreach (var m in result.Meetings) Meetings.Add(m);
        }
        catch (Exception ex)
        {
            MeetingsError = ex.Message;
        }
        finally
        {
            MeetingsLoading = false;
        }
    }

    public async Task CreateMeetingAsync(string title, string description, List<string> participants, bool isInstant, bool isRecorded)
    {
        MeetingsLoading = true;
        MeetingsError = "";

        try
        {
            var result = await _api.CreateMeetingAsync(_appState.CompanyId, new CreateMeetingRequest
            {
                Title = title,
                Description = description,
                CreatedByExtension = _appState.Extension,
                ParticipantExtensions = participants,
                IsInstant = isInstant,
                IsRecorded = isRecorded
            });

            _appState.SelectedMeetingId = result.MeetingId;
            _appState.SelectedMeetingTitle = result.Title;
            _appState.SelectedMeetingConferenceNumber = result.ConferenceNumber;
            _appState.NavigateSafely(Screen.MeetingDetail);
            _appState.Status = "Meeting created";

            await LoadMeetingsAsync();
        }
        catch (Exception ex)
        {
            MeetingsError = ex.Message;
        }
        finally
        {
            MeetingsLoading = false;
        }
    }

    public void OpenMeeting(SipCoreMeetingDto meeting)
    {
        SelectedMeeting = meeting;

        _appState.SelectedMeetingId = meeting.MeetingId;
        _appState.SelectedMeetingTitle = meeting.Title;
        _appState.SelectedMeetingConferenceNumber = meeting.ConferenceNumber;
        _appState.NavigateSafely(Screen.MeetingDetail);
        _appState.Status = "Meeting opened";
    }

    /// <summary>
    /// Port of joinMeeting(): original called sipManager.makeCall() directly (bypassing the
    /// contact-active-status check that AppStateViewModel.MakeOutgoingCallAsync does for
    /// regular dialer calls) -- preserved here for the same reason: a conference number isn't
    /// a contact extension, so that check doesn't apply.
    /// </summary>
    public async Task JoinMeetingAsync(string conferenceNumber)
    {
        var number = conferenceNumber.Trim();
        if (string.IsNullOrEmpty(number))
        {
            _appState.Status = "Conference number is empty";
            return;
        }

        await _appState.SipManager.MakeCallAsync(number);

        _appState.NavigateSafely(Screen.ActiveCall);
        _appState.Status = $"Joining meeting {number}";
    }

    public async Task RefreshMeetingLiveAsync()
    {
        try
        {
            var result = await _api.GetLiveMeetingMembersAsync(_appState.CompanyId, _appState.SelectedMeetingId, _appState.Extension);
            LiveMembersText = result.Live;
        }
        catch
        {
            LiveMembersText = "Unable to load live members";
        }
    }

    public async Task StartMeetingAsync()
    {
        try
        {
            var result = await _api.StartMeetingAsync(_appState.CompanyId, _appState.SelectedMeetingId,
                new MeetingActionRequest { ActorExtension = _appState.Extension });
            SelectedMeeting = result.Meeting;
            await LoadMeetingsAsync();
        }
        catch
        {
            MeetingsError = "Failed to start meeting";
        }
    }

    public async Task EndMeetingAsync()
    {
        try
        {
            var result = await _api.EndMeetingAsync(_appState.CompanyId, _appState.SelectedMeetingId,
                new MeetingActionRequest { ActorExtension = _appState.Extension });
            SelectedMeeting = result.Meeting;
            await LoadMeetingsAsync();
        }
        catch
        {
            MeetingsError = "Failed to end meeting";
        }
    }

    public async Task LockMeetingAsync()
    {
        try
        {
            var result = await _api.LockMeetingAsync(_appState.CompanyId, _appState.SelectedMeetingId,
                new MeetingActionRequest { ActorExtension = _appState.Extension });
            SelectedMeeting = result.Meeting;
        }
        catch
        {
            MeetingsError = "Failed to lock meeting";
        }
    }

    public async Task UnlockMeetingAsync()
    {
        try
        {
            var result = await _api.UnlockMeetingAsync(_appState.CompanyId, _appState.SelectedMeetingId,
                new MeetingActionRequest { ActorExtension = _appState.Extension });
            SelectedMeeting = result.Meeting;
        }
        catch
        {
            MeetingsError = "Failed to unlock meeting";
        }
    }
}
