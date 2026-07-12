namespace SipCoreMobile.Models;

public enum Screen
{
    Login,
    Home,
    DialPad,
    ActiveCall,
    IncomingCall,
    Conversations,
    Chat,
    CallHistory,
    Contacts,
    Profile,
    Groups,
    GroupDetails,
    GroupChat,
    Settings,
    Email,

    WorkDesk,
    WorkDeskTask,
    WorkDeskCreateTask,
    WorkDeskApprovals,
    WorkDeskEditTask,

    EmailDetail,
    EmailCompose,
    EmailThread,
    EmailSearch,
    EmailAttachmentPreview,

    MeetingHome,
    MeetingCreate,
    MeetingDetail
}

/// <summary>
/// Snapshot of app UI state. In the original Kotlin app this backed a Compose
/// single-activity UI (immutable data class + StateFlow). In MAUI this is intended
/// to be held by a central ObservableObject (e.g. AppShellViewModel) and mutated
/// via property setters that raise PropertyChanged, rather than copy()'d wholesale.
/// </summary>
public class SipUiState
{
    public bool Registered { get; set; }
    public string Status { get; set; } = "Not registered";
    public Screen CurrentScreen { get; set; } = Screen.Login;
    public string Extension { get; set; } = "";
    public string Domain { get; set; } = "";

    public int CompanyId { get; set; }
    public string SelectedTaskId { get; set; } = "";
    public string SelectedTaskTitle { get; set; } = "";

    public string Caller { get; set; } = "";
    public string ActiveNumber { get; set; } = "";
    public string SelectedConversation { get; set; } = "";
    public int CallDurationSeconds { get; set; }
    public string TypingExtension { get; set; } = "";
    public List<string> ConferenceParticipants { get; set; } = new();
    public string OutboundNumber { get; set; } = "";
    public bool HasOutboundNumber { get; set; }
    public bool VoicemailEnabled { get; set; }
    public bool CallRecordingEnabled { get; set; }
    public bool IsPremiumUser { get; set; }
    public string SettingsStatus { get; set; } = "";
    public bool IsChatLoading { get; set; }
    public string CompanyName { get; set; } = "";
    public string DisplayName { get; set; } = "";
    public bool IsWorkDeskCreator { get; set; }
    public string PrefilledChatText { get; set; } = "";
    public string SelectedMeetingId { get; set; } = "";
    public string SelectedMeetingTitle { get; set; } = "";
    public string SelectedMeetingConferenceNumber { get; set; } = "";
}
