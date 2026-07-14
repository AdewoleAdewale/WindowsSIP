using CommunityToolkit.Mvvm.ComponentModel;
using SipCoreMobile.Data;
using SipCoreMobile.Data.Entities;
using SipCoreMobile.Models;
using SipCoreMobile.Models.Api;
using SipCoreMobile.Services.Api;
using SipCoreMobile.Services.Notifications;
using SipCoreMobile.Services.Sip;
using SipCoreMobile.Services.Storage;
using System.Collections.ObjectModel;
using System.Security.Principal;

namespace SipCoreMobile.ViewModels;


public partial class AppStateViewModel : ObservableObject, ISipEvents
{
    private readonly ISipCoreApiService _api;
    private readonly ISipCoreRepository _repository;

    public SipCoreManager SipManager { get; }

    [ObservableProperty] private bool isLoggingIn;
    [ObservableProperty] private bool registered;
    [ObservableProperty] private string status = "Not registered";
    [ObservableProperty] private Screen currentScreen = Screen.Login;
    [ObservableProperty] private string extension = "";
    [ObservableProperty] private string domain = "";
    [ObservableProperty] private int companyId;
    [ObservableProperty] private string companyName = "";
    [ObservableProperty] private string displayName = "";

    [ObservableProperty] private string caller = "";
    [ObservableProperty] private string activeNumber = "";
    [ObservableProperty] private int callDurationSeconds;

    [ObservableProperty] private string selectedMeetingId = "";
    [ObservableProperty] private string selectedMeetingTitle = "";
    [ObservableProperty] private string selectedMeetingConferenceNumber = "";

    public ObservableCollection<string> ConferenceParticipants { get; } = new();
    public ObservableCollection<ConversationUi> Conversations { get; } = new();
    public ObservableCollection<ContactUi> Contacts { get; } = new();
    public ObservableCollection<CallLogUi> CallLogs { get; } = new();
    public ObservableCollection<WorkTaskCreatorDto> WorkTaskCreators { get; } = new();

    private string _currentCallNumber = "";
    private bool _currentCallIncoming;
    private long _callStartedAt;
    private long _callConnectedAt;

    private CancellationTokenSource? _callTimerCts;
    private CancellationTokenSource? _presenceHeartbeatCts;
    private CancellationTokenSource? _presenceRefreshCts;

    public AppStateViewModel(ISipCoreApiService api, ISipCoreRepository repository)
    {
        _api = api;
        _repository = repository;
        SipManager = new SipCoreManager(this);
        SipCoreHolder.Manager = SipManager;
    }

    private static long NowMs => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

    // ---------------------------------------------------------------------
    // Login / auto-login (port of login(), tryAutoLogin())
    // ---------------------------------------------------------------------

    public async Task LoginAsync(string ext, string pass, string domainInput)
    {
        if (IsLoggingIn) return;

        var cleanExt = ext.Trim();
        var cleanPass = pass.Trim();
        var cleanDomain = domainInput.Trim();

        if (string.IsNullOrEmpty(cleanExt) || string.IsNullOrEmpty(cleanPass) || string.IsNullOrEmpty(cleanDomain))
        {
            Status = "Extension, password, and domain are required";
            return;
        }

        IsLoggingIn = true;
        Status = "Registering...";
        Extension = cleanExt;
        Domain = cleanDomain;
        CurrentScreen = Screen.Login;

        try
        {
            await SecureCredentialStore.SavePasswordAsync(cleanPass);

            await _repository.SaveCredentialsAsync(new SipCredentialEntity
            {
                Extension = cleanExt,
                Password = "", // password is kept in SecureStorage only, never in the DB row
                Domain = cleanDomain
            });

            // Original started a foreground service here; not needed on Windows desktop
            // (see SipBackgroundKeepAlive remarks from the calling-infra batch).

            SipManager.Start(cleanExt, cleanPass, cleanDomain);
        }
        catch
        {
            IsLoggingIn = false;
            CurrentScreen = Screen.Login;
            Registered = false;
            Status = "Login failed";
        }
    }

    public async Task TryAutoLoginAsync()
    {
        try
        {
            var saved = await _repository.GetCredentialsAsync();
            if (saved is null) return;

            Status = "Auto registering...";
            Extension = saved.Extension;
            Domain = saved.Domain;

            var savedPassword = await SecureCredentialStore.GetPasswordAsync();
            if (string.IsNullOrEmpty(savedPassword))
            {
                CurrentScreen = Screen.Login;
                Status = "Saved login is incomplete. Please log in again.";
                return;
            }

            try
            {
                SipManager.Start(saved.Extension, savedPassword, saved.Domain);
            }
            catch
            {
                CurrentScreen = Screen.Login;
                Status = "Auto login failed";
            }
        }
        catch
        {
            Status = "auto login failed";
        }
    }

    /// <summary>Port of the onChangePassword callback wiring (calls API, then forces re-login on success).</summary>
    public async Task ChangePasswordAsync(string currentPassword, string newPassword, string confirmPassword)
    {
        try
        {
            var result = await _api.ChangePasswordAsync(new ChangePasswordRequest
            {
                Extension = Extension,
                CurrentPassword = currentPassword,
                NewPassword = newPassword,
                ConfirmPassword = confirmPassword
            });

            Status = result.Message;

            if (result.Success)
            {
                Status = "Password changed successfully. Please log in again.";
                await Task.Delay(1500);
                await LogoutAsync();
            }
        }
        catch
        {
            Status = "change password failed";
        }
    }

    public async Task LogoutAsync()
    {
        StopPresenceRefresh();

        try
        {
            await _repository.ClearCredentialsAsync();
            SecureCredentialStore.Clear();
        }
        catch
        {
            // best-effort, mirrors original
        }

        try { SipManager.Stop(); } catch { /* best-effort */ }
        try { StopCallTimer(); } catch { /* best-effort */ }

        SipCoreHolder.IsCallActive = false;
        SipCoreHolder.ActiveNumber = "";
        SipCoreHolder.CallStatus = "";

        Registered = false;
        Status = "Logged out";
        CurrentScreen = Screen.Login;
        Caller = "";
        ActiveNumber = "";
        CallDurationSeconds = 0;
        ConferenceParticipants.Clear();
        Conversations.Clear();
        Contacts.Clear();
        CallLogs.Clear();
    }

    // ---------------------------------------------------------------------
    // Call actions (port of answerIncomingCall()/rejectIncomingCall()/hangupCall()
    // and the onMute/onHold/onSpeaker/onSendDtmf/onAddConference/
    // onRemoveConferenceParticipant callback bodies)
    // ---------------------------------------------------------------------

    public async Task AnswerIncomingCallAsync()
    {
        NotificationHelper.CancelIncomingCall();

        var number = GetDisplayNameForExtension(string.IsNullOrEmpty(Caller) ? ActiveNumber : Caller);

        CurrentScreen = Screen.ActiveCall;
        ActiveNumber = number;
        Status = "Connecting...";

        SipCoreHolder.IsCallActive = true;
        SipCoreHolder.ActiveNumber = number;
        SipCoreHolder.CallStatus = "Connecting...";

        try
        {
            await SipManager.AnswerCallAsync();
        }
        catch
        {
            SipCoreHolder.IsCallActive = false;
            Status = "Answer failed";
        }
    }

    public void RejectIncomingCall()
    {
        SipManager.RejectCall();
        NotificationHelper.CancelIncomingCall();

        SipCoreHolder.IsCallActive = false;
        SipCoreHolder.ActiveNumber = "";
        SipCoreHolder.CallStatus = "";

        CurrentScreen = Screen.DialPad;
        Status = "Call rejected";
        ActiveNumber = "";
        Caller = "";
    }

    public void HangupCall()
    {
        SipManager.Hangup();
        StopCallTimer();
        NotificationHelper.CancelIncomingCall();

        SipCoreHolder.IsCallActive = false;
        SipCoreHolder.ActiveNumber = "";
        SipCoreHolder.CallStatus = "";

        CurrentScreen = Screen.DialPad;
        Status = "Call ended";
        ActiveNumber = "";
        Caller = "";
    }

    public void Mute(bool enabled) => SipManager.Mute(enabled);

    public async Task HoldAsync(bool enabled)
    {
        SipManager.Hold(enabled);

        if (!enabled)
        {
            await Task.Delay(1000);
            SipManager.ReconnectAudio();
        }
    }

    public void Speaker(bool enabled) => SipManager.Speaker(enabled);

    public void SendDtmf(string digit) => SipManager.SendDtmf(digit);

    public async Task AddConferenceParticipantAsync(string number)
    {
        var clean = number.Trim();
        if (string.IsNullOrEmpty(clean)) return;

        var displayName = GetDisplayNameForExtension(clean);
        if (!ConferenceParticipants.Contains(displayName))
        {
            ConferenceParticipants.Add(displayName);
        }

        await SipManager.AddConferenceParticipantAsync(clean);
    }

    public void RemoveConferenceParticipant(string number)
    {
        var clean = ExtractExtensionFromDisplay(number);

        SipManager.HangupConferenceParticipant(clean);

        var toRemove = ConferenceParticipants
            .Where(p => p == number || p == clean || p.Contains($"({clean})"))
            .ToList();
        foreach (var p in toRemove) ConferenceParticipants.Remove(p);

        var stillHasParticipants = ConferenceParticipants.Count > 0;
        SipCoreHolder.IsCallActive = stillHasParticipants || !string.IsNullOrEmpty(ActiveNumber);
        SipCoreHolder.CallStatus = SipCoreHolder.IsCallActive ? "Connected" : "";
    }

    // ---------------------------------------------------------------------
    // Navigation guard (port of navigateSafely())
    // ---------------------------------------------------------------------

    /// <summary>Prevents navigating away from an in-progress call by accident.</summary>
    public void NavigateSafely(Screen screen)
    {
        if (SipCoreHolder.IsCallActive && screen != Screen.ActiveCall && screen != Screen.IncomingCall)
        {
            CurrentScreen = Screen.ActiveCall;
            return;
        }

        CurrentScreen = screen;
    }

    // ---------------------------------------------------------------------
    // Outgoing calls (port of makeOutgoingCall())
    // ---------------------------------------------------------------------

    public async Task MakeOutgoingCallAsync(string number)
    {
        Controls.InAppNotifier.Show("Making call", $"From {number}", Controls.SnackKind.Call, durationMs: 8000);
        var cleanNumber = number.Trim();
        if (string.IsNullOrEmpty(cleanNumber)) return;

        var contact = Contacts.FirstOrDefault(c => c.Extension == cleanNumber);
     
        if (contact is not null && contact.CanViewPresence)
        {
            var callableStatuses = new[] { "Online", "Available", "Registered", "Active", "Reachable", "Ready", "Connected" };
            if (!callableStatuses.Any(s => string.Equals(contact.Status, s, StringComparison.OrdinalIgnoreCase)))
            {
                Status = "Extension is not active";
                return;
            }
        }

        _currentCallNumber = cleanNumber;
        _currentCallIncoming = false;
        _callStartedAt = NowMs;
        _callConnectedAt = 0;

        SipCoreHolder.IsCallActive = true;
        var destination = $"sip:{cleanNumber}@{Domain}";
        await SipManager.MakeCallAsync(cleanNumber);
    }

    // ---------------------------------------------------------------------
    // ISipEvents
    // ---------------------------------------------------------------------

    public void OnRegistered() =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            Controls.InAppNotifier.Show("Connected", $"Registered as extension {Extension}", Controls.SnackKind.Success);
            var wasAlreadyRegistered = Registered;
            var shouldNavigateHome = !wasAlreadyRegistered && CurrentScreen == Screen.Login;

            Registered = true;
            Status = "Registered";

            if (shouldNavigateHome)
            {
                NavigateSafely(Screen.Home);
            }

            _ = LoadConversationsAsync();
            _ = LoadContactsAsync();
            StartPresenceHeartbeat();
            _ = RegisterPushTokenAsync();
            IsLoggingIn = false;

            _ = Task.Run(async () =>
            {
                await Task.Delay(5000);
                StartPresenceRefresh();
            });
        });

    public void OnRegistrationFailed(string reason) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            IsLoggingIn = false;
            Registered = false;
            Status = reason;
            CurrentScreen = Screen.Login;
        });

    public void OnIncomingCall(string callerRaw) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            var cleanCaller = ExtractExtensionFromDisplay(callerRaw);
            var callerName = GetDisplayNameForExtension(cleanCaller);

            var alreadyOnActiveCall = SipCoreHolder.IsCallActive && CurrentScreen != Screen.IncomingCall;

            if (alreadyOnActiveCall || ConferenceParticipants.Count > 0)
            {
                SipManager.RejectIncomingCallBusy();
                NotificationHelper.CancelIncomingCall();
                return;
            }

            _currentCallNumber = ExtractExtensionFromDisplay(cleanCaller);
            _currentCallIncoming = true;
            _callStartedAt = NowMs;
            _callConnectedAt = 0;

            // Call is ringing, not active yet.
            SipCoreHolder.IsCallActive = false;
            SipCoreHolder.ActiveNumber = callerName;
            SipCoreHolder.CallStatus = "Incoming call";

            NotificationHelper.ShowIncomingCall(callerName);

            Caller = callerName;
            ActiveNumber = callerName;
            Status = "Incoming call";
            CurrentScreen = Screen.IncomingCall;
            CallDurationSeconds = 0;
        });

    public void OnCallStateChanged(string state) =>
        MainThread.BeginInvokeOnMainThread(() =>
        {
            switch (state)
            {
                case "Ended":
                case "Disconnected":
                    NotificationHelper.CancelIncomingCall();
                    RingbackPlayer.Stop();

                    if (ConferenceParticipants.Count > 0)
                    {
                        SipCoreHolder.IsCallActive = true;
                        SipCoreHolder.CallStatus = "Connected";
                        CurrentScreen = Screen.ActiveCall;
                        Status = "Participant disconnected";
                        return;
                    }

                    StopCallTimer();
                    _ = SaveCallLogAsync();

                    _callConnectedAt = 0;
                    ConferenceParticipants.Clear();

                    SipCoreHolder.IsCallActive = false;
                    SipCoreHolder.ActiveNumber = "";
                    SipCoreHolder.CallStatus = "Call ended";

                    CurrentScreen = Screen.DialPad;
                    ActiveNumber = "";
                    Caller = "";
                    Status = "Call ended";
                    break;

                case "Connected":
                case "CONFIRMED":
                    RingbackPlayer.Stop();

                    if (_callConnectedAt == 0)
                    {
                        _callConnectedAt = NowMs;
                        StartCallTimer();
                    }

                    var number = string.IsNullOrEmpty(ActiveNumber) ? Caller : ActiveNumber;

                    SipCoreHolder.IsCallActive = true;
                    SipCoreHolder.ActiveNumber = number;
                    SipCoreHolder.CallStatus = "Connected";

                    CurrentScreen = Screen.ActiveCall;
                    ActiveNumber = number;
                    Caller = "";
                    Status = "Connected";
                    break;

                default:
                    if (SipCoreHolder.IsCallActive && CurrentScreen != Screen.IncomingCall)
                    {
                        CurrentScreen = Screen.ActiveCall;
                        Status = state;
                    }
                    else
                    {
                        Status = state;
                    }
                    break;
            }
        });

    // ---------------------------------------------------------------------
    // Call timer (port of startCallTimer()/stopCallTimer())
    // ---------------------------------------------------------------------

    private void StartCallTimer()
    {
        _callTimerCts?.Cancel();
        _callTimerCts = new CancellationTokenSource();
        var token = _callTimerCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                while (!token.IsCancellationRequested)
                {
                    await Task.Delay(1000, token);
                    var secs = _callConnectedAt > 0 ? (int)((NowMs - _callConnectedAt) / 1000) : 0;
                    MainThread.BeginInvokeOnMainThread(() => CallDurationSeconds = secs);
                }
            }
            catch (OperationCanceledException)
            {
                // expected when StopCallTimer() cancels the token
            }
        }, token);
    }

    private void HandleCallStateChanged(string state)
    {
        switch (state)
        {
            case "Ended":
            case "Disconnected":
                NotificationHelper.CancelIncomingCall();
                RingbackPlayer.Stop();

                if (ConferenceParticipants.Count > 0)
                {
                    SipCoreHolder.IsCallActive = true;
                    SipCoreHolder.CallStatus = "Connected";
                    CurrentScreen = Screen.ActiveCall;
                    Status = "Participant disconnected";
                    return;
                }

                StopCallTimer();
                _ = SaveCallLogAsync();

                _callConnectedAt = 0;
                ConferenceParticipants.Clear();

                SipCoreHolder.IsCallActive = false;
                SipCoreHolder.ActiveNumber = "";
                SipCoreHolder.CallStatus = "Call ended";

                CurrentScreen = Screen.DialPad;
                ActiveNumber = "";
                Caller = "";
                Status = "Call ended";
                break;

            case "Connected":
            case "CONFIRMED":
                RingbackPlayer.Stop();

                if (_callConnectedAt == 0)
                {
                    _callConnectedAt = NowMs;
                    StartCallTimer();
                }

                var number = string.IsNullOrEmpty(ActiveNumber) ? Caller : ActiveNumber;

                SipCoreHolder.IsCallActive = true;
                SipCoreHolder.ActiveNumber = number;
                SipCoreHolder.CallStatus = "Connected";

                CurrentScreen = Screen.ActiveCall;
                ActiveNumber = number;
                Caller = "";
                Status = "Connected";
                break;

            default:
                if (SipCoreHolder.IsCallActive && CurrentScreen != Screen.IncomingCall)
                {
                    CurrentScreen = Screen.ActiveCall;
                    Status = state;
                }
                else
                {
                    Status = state;
                }
                break;
        }
    }



    private void HandleIncomingCall(string callerRaw)
    {
        var cleanCaller = ExtractExtensionFromDisplay(callerRaw);
        var callerName = GetDisplayNameForExtension(cleanCaller);

        var alreadyOnActiveCall = SipCoreHolder.IsCallActive && CurrentScreen != Screen.IncomingCall;

        if (alreadyOnActiveCall || ConferenceParticipants.Count > 0)
        {
            SipManager.RejectIncomingCallBusy();
            NotificationHelper.CancelIncomingCall();
            return;
        }

        _currentCallNumber = ExtractExtensionFromDisplay(cleanCaller);
        _currentCallIncoming = true;
        _callStartedAt = NowMs;
        _callConnectedAt = 0;

        // Call is ringing, not active yet.
        SipCoreHolder.IsCallActive = false;
        SipCoreHolder.ActiveNumber = callerName;
        SipCoreHolder.CallStatus = "Incoming call";

        NotificationHelper.ShowIncomingCall(callerName);

        Caller = callerName;
        ActiveNumber = callerName;
        Status = "Incoming call";
        CurrentScreen = Screen.IncomingCall;
        CallDurationSeconds = 0;
    }

 
    private void StopCallTimer()
    {
        _callTimerCts?.Cancel();
        _callTimerCts = null;
        CallDurationSeconds = 0;
    }

    private async Task SaveCallLogAsync()
    {
        var number = _currentCallNumber;
        if (string.IsNullOrEmpty(number)) return;

        var duration = _callConnectedAt > 0 ? (int)((NowMs - _callConnectedAt) / 1000) : 0;
        var missed = _currentCallIncoming && _callConnectedAt == 0;

        await _repository.InsertCallLogAsync(new CallLogEntity
        {
            Number = number,
            Time = NowMs,
            IsIncoming = _currentCallIncoming,
            IsMissed = missed,
            DurationSeconds = duration
        });

        await LoadCallLogsAsync();

        _currentCallNumber = "";
        _currentCallIncoming = false;
        _callStartedAt = 0;
        _callConnectedAt = 0;
    }

    // ---------------------------------------------------------------------
    // Presence (port of startPresenceHeartbeat()/startPresenceRefresh())
    // ---------------------------------------------------------------------

    private void StartPresenceHeartbeat()
    {
        _presenceHeartbeatCts?.Cancel();
        _presenceHeartbeatCts = new CancellationTokenSource();
        var token = _presenceHeartbeatCts.Token;
        Controls.InAppNotifier.Show("You Can Now Recieve Message From Sipcore", $"From Info@sipcore.org", Controls.SnackKind.Call, durationMs: 8000);
        _ = Task.Run(async () =>
        {
            try
            {
                while (Registered && !token.IsCancellationRequested)
                {
                    try
                    {
                        await _api.UpdatePresenceAsync(new PresenceRequest { Extension = Extension });
                    }
                    catch
                    {
                        // best-effort, mirrors original
                    }

                    await Task.Delay(30000, token);
                }
            }
            catch (OperationCanceledException)
            {
                // expected when StopPresenceRefresh() cancels the token
            }
        }, token);
    }

    private void StartPresenceRefresh()
    {
        _presenceRefreshCts?.Cancel();
        _presenceRefreshCts = new CancellationTokenSource();
        var token = _presenceRefreshCts.Token;

        _ = Task.Run(async () =>
        {
            try
            {
                await Task.Delay(3000, token);

                while (Registered && !token.IsCancellationRequested)
                {
                    try { await LoadContactsAsync(); } catch { /* best-effort */ }
                    await Task.Delay(30000, token);
                }
            }
            catch (OperationCanceledException)
            {
                // expected when StopPresenceRefresh() cancels the token
            }
        }, token);
    }

    private void StopPresenceRefresh()
    {
        _presenceRefreshCts?.Cancel();
        _presenceRefreshCts = null;
        _presenceHeartbeatCts?.Cancel();
        _presenceHeartbeatCts = null;
    }

    // ---------------------------------------------------------------------
    // Contacts / conversations / call log loading
    // ---------------------------------------------------------------------

    public async Task LoadConversationsAsync()
    {
        try
        {
            var rows = await _repository.GetConversationsAsync();

            var items = rows.Select(row =>
            {
                var contact = Contacts.FirstOrDefault(c => c.Extension == row.Extension);
                return new ConversationUi
                {
                    Extension = row.Extension,
                    LastMessage = row.LastMessage,
                    LastMessageAt = row.LastMessageAt,
                    UnreadCount = row.UnreadCount,
                    IsOnline = contact?.IsOnline ?? false,
                    Status = contact?.Status ?? "Unknown",
                    CompanyId = contact?.CompanyId ?? 0,
                    CompanyName = contact?.CompanyName ?? "",
                    IsExternal = contact?.IsExternal ?? false
                };
            });

            Conversations.Clear();
            foreach (var item in items) Conversations.Add(item);
        }
        catch
        {
            Status = "load conversations failed";
        }
    }

    /// <summary>Port of loadContactsOnce(): resolves my company/display name from the API
    /// extensions list, filters same-company + external contacts, and merges with local +
    /// fallback contacts.</summary>
    public async Task LoadContactsAsync()
    {
        try
        {
            var localRows = await _repository.GetLocalContactsAsync();
            var localContacts = localRows.Select(row => new ContactUi
            {
                Extension = row.Extension,
                DisplayName = CleanContactName(row.DisplayName, row.Extension),
                IsOnline = false,
                Status = "Unknown",
                CompanyId = 0,
                CompanyName = "",
                IsExternal = false,
                CanCall = true,
                CanMessage = true,
                CanViewPresence = true,
                CanUseWorkDesk = false
            }).ToList();

            var fallbackContacts = new List<ContactUi>
            {
                new()
                {
                    Extension = "7000",
                    DisplayName = "SIPCore Help Desk",
                    IsOnline = true,
                    Status = "Active",
                    CompanyId = 0,
                    CompanyName = "SIPCore",
                    IsExternal = false,
                    CanCall = true,
                    CanMessage = true,
                    CanViewPresence = true,
                    CanUseWorkDesk = false
                }
            };

            List<ContactUi> apiContacts = new();
            try
            {
                var apiExtensions = await _api.GetExtensionsAsync(Extension);

                var myExtension = apiExtensions.FirstOrDefault(e =>
                {
                    var clean = e.Extension.Trim();
                    if (string.IsNullOrEmpty(clean)) clean = (e.SipUsername ?? "").Trim();
                    if (string.IsNullOrEmpty(clean)) clean = (e.Number ?? "").Trim();
                    return string.Equals(clean, Extension.Trim(), StringComparison.OrdinalIgnoreCase);
                });

                var myCompanyId = myExtension?.CompanyId ?? 0;
                var myCompanyNameValue = (myExtension?.CompanyName ?? "").Trim();
                var myDisplayNameValue = !string.IsNullOrWhiteSpace(myExtension?.Name) ? myExtension!.Name!.Trim() : Extension;

                CompanyId = myCompanyId;
                CompanyName = myCompanyNameValue;
                DisplayName = myDisplayNameValue;

                if (myCompanyId != 0)
                {
                    apiContacts = apiExtensions
                        .Select(item =>
                        {
                            var cleanExtension = item.Extension.Trim();
                            if (string.IsNullOrEmpty(cleanExtension)) cleanExtension = (item.SipUsername ?? "").Trim();
                            if (string.IsNullOrEmpty(cleanExtension)) cleanExtension = (item.Number ?? "").Trim();

                            if (string.IsNullOrEmpty(cleanExtension) ||
                                string.Equals(cleanExtension, Extension.Trim(), StringComparison.OrdinalIgnoreCase))
                            {
                                return null;
                            }

                            var isExternal = item.IsExternal || item.CompanyId != myCompanyId;
                            var contactName = !string.IsNullOrWhiteSpace(item.Name) ? item.Name!.Trim() : $"Extension {cleanExtension}";

                            return new ContactUi
                            {
                                Extension = cleanExtension,
                                DisplayName = CleanContactName(contactName, cleanExtension),
                                IsOnline = item.CanViewPresence && item.IsOnline,
                                Status = item.CanViewPresence ? (item.Status ?? "Unknown") : "Hidden",
                                CompanyId = item.CompanyId,
                                CompanyName = (item.CompanyName ?? "").Trim(),
                                IsExternal = isExternal,
                                CanCall = item.CanCall,
                                CanMessage = item.CanMessage,
                                CanViewPresence = item.CanViewPresence,
                                CanUseWorkDesk = item.CanUseWorkDesk
                            };
                        })
                        .Where(c => c is not null)
                        .Select(c => c!)
                        .Where(c => c.CompanyId == myCompanyId || c.IsExternal)
                        .ToList();
                }
            }
            catch
            {
                // Original swallowed API failures here too and fell through to local+fallback only.
            }

            var merged = apiContacts.Concat(localContacts).Concat(fallbackContacts)
                .GroupBy(c => c.Extension)
                .Select(g => g.First())
                .OrderBy(c => c.IsExternal)
                .ThenBy(c => c.CompanyName)
                .ThenBy(c => c.DisplayName)
                .ToList();

            await MainThread.InvokeOnMainThreadAsync(() =>
            {
                Contacts.Clear();
                foreach (var c in merged) Contacts.Add(c);
            });
        }
        catch
        {
            Status = "load contacts failed";
        }
    }

    /// <summary>Port of createContact().</summary>
    public async Task CreateContactAsync(string extension, string displayName)
    {
        var cleanExtension = extension.Trim();
        var cleanName = displayName.Trim();
        if (string.IsNullOrEmpty(cleanExtension) || string.IsNullOrEmpty(cleanName)) return;

        try
        {
            await _repository.SaveLocalContactAsync(new LocalContactEntity
            {
                Extension = cleanExtension,
                DisplayName = cleanName
            });

            await LoadContactsAsync();
        }
        catch
        {
            Status = "create contact failed";
        }
    }

    /// <summary>
    /// Used by ProfileScreen's WorkDesk-access badge only, ahead of the full WorkDesk module
    /// batch. Failures are silent since Profile just falls back to "No WorkDesk Access".
    /// </summary>


    /// <summary>
    /// Port of the `creators` list ProfileScreen uses to compute WorkDesk access level.
    /// Only this one endpoint from the WorkDesk API surface is used here -- the rest of the
    /// WorkDesk module (task lists, approvals, etc.) is its own future batch.
    /// </summary>
    public async Task LoadWorkTaskCreatorsAsync()
    {
        try
        {
            var creators = await _api.GetWorkTaskCreatorsAsync(CompanyId);
            WorkTaskCreators.Clear();
            foreach (var c in creators) WorkTaskCreators.Add(c);
        }
        catch
        {
            // best-effort; ProfileScreen falls back to the companyId-only WorkDesk-access heuristic
        }
    }

    public async Task LoadCallLogsAsync()
    {
        try
        {
            var rows = await _repository.GetCallLogsAsync();
            var items = rows.Select(r => new CallLogUi
            {
                Number = r.Number,
                Time = r.Time,
                IsIncoming = r.IsIncoming,
                IsMissed = r.IsMissed,
                DurationSeconds = r.DurationSeconds
            });

            CallLogs.Clear();
            foreach (var item in items) CallLogs.Add(item);
        }
        catch
        {
            Status = "load call logs failed";
        }
    }

    /// <summary>Port of CallHistoryScreen's onClearHistory callback.</summary>
    public async Task ClearCallLogsAsync()
    {
        try
        {
            await _repository.ClearCallLogsAsync();
            CallLogs.Clear();
        }
        catch
        {
            Status = "clear call history failed";
        }
    }

    // ---------------------------------------------------------------------
    // Push token registration (FCM in the original -- see PushMessageDispatcher
    // remarks in the calling-infra batch re: Windows using WNS instead)
    // ---------------------------------------------------------------------

    public async Task RegisterPushTokenAsync()
    {
        try
        {
            var token = SipCoreTokenStore.GetToken();
            if (string.IsNullOrEmpty(token)) return; // no WNS channel URI registered yet

            await _api.RegisterFirebaseTokenAsync(new FirebaseTokenRequest
            {
                Extension = Extension,
                Token = token,
                TokenType = "wns",
                Platform = "windows"
            });
        }
        catch
        {
            Status = "Push token registration failed";
        }
    }

    // ---------------------------------------------------------------------
    // Helpers (ported verbatim in spirit from MainActivity.kt)
    // ---------------------------------------------------------------------

    private static string ExtractExtensionFromDisplay(string value)
    {
        try
        {
            if (value.Contains('(') && value.Contains(')'))
            {
                var start = value.IndexOf('(') + 1;
                var end = value.IndexOf(')');
                return value[start..end].Trim();
            }
            return value.Trim();
        }
        catch
        {
            return value;
        }
    }

    private string GetDisplayNameForExtension(string extensionValue)
    {
        var cleanExt = ExtractExtensionFromDisplay(extensionValue);
        var contact = Contacts.FirstOrDefault(c => c.Extension == cleanExt);
        return contact is not null && !string.IsNullOrEmpty(contact.DisplayName)
            ? $"{contact.DisplayName} ({cleanExt})"
            : cleanExt;
    }

    private static string CleanContactName(string? name, string extensionValue)
    {
        var cleanExtension = extensionValue.Trim();
        var cleanName = (name ?? "").Trim();

        if (string.IsNullOrEmpty(cleanName)) return $"Extension {cleanExtension}";

        cleanName = cleanName.Replace($"({cleanExtension})", "").Trim();
        return string.IsNullOrEmpty(cleanName) ? $"Extension {cleanExtension}" : cleanName;
    }
}
