namespace SipCoreMobile.Models.Api;

public class CreateMeetingRequest
{
    public required string Title { get; set; }
    public string Description { get; set; } = "";
    public required string CreatedByExtension { get; set; }
    public List<string> ParticipantExtensions { get; set; } = new();
    public string? StartDateTime { get; set; }
    public string? EndDateTime { get; set; }
    public bool IsInstant { get; set; } = true;
    public bool IsRecorded { get; set; }
    public bool AutoDeleteAfterEnd { get; set; } = true;
    public int MaxMembers { get; set; }
}

public class MeetingActionRequest
{
    public required string ActorExtension { get; set; }
}

public class MeetingMemberActionRequest
{
    public required string ActorExtension { get; set; }
    public required string MemberId { get; set; }
}

public class SipCoreMeetingDto
{
    public string MeetingId { get; set; } = "";
    public int CompanyId { get; set; }
    public string CompanyName { get; set; } = "";
    public string Title { get; set; } = "";
    public string Description { get; set; } = "";
    public string ConferenceNumber { get; set; } = "";
    public string ModeratorPin { get; set; } = "";
    public string ParticipantPin { get; set; } = "";
    public string CreatedByExtension { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsInstant { get; set; } = true;
    public bool IsLocked { get; set; }
    public bool IsRecorded { get; set; }
    public string? StartDateTime { get; set; }
    public string? EndDateTime { get; set; }
    public string CreatedAt { get; set; } = "";
}

public class CreateMeetingResponse
{
    public bool Success { get; set; }
    public string MeetingId { get; set; } = "";
    public string Title { get; set; } = "";
    public string ConferenceNumber { get; set; } = "";
    public string ModeratorPin { get; set; } = "";
    public string ParticipantPin { get; set; } = "";
    public string Status { get; set; } = "";
    public bool IsInstant { get; set; } = true;
    public bool IsRecorded { get; set; }
    public string? StartDateTime { get; set; }
    public string? EndDateTime { get; set; }
    public string JoinInstruction { get; set; } = "";
    public string? Message { get; set; }
}

public class MeetingListResponse
{
    public bool Success { get; set; }
    public List<SipCoreMeetingDto> Meetings { get; set; } = new();
    public string? Message { get; set; }
}

public class MeetingSingleResponse
{
    public bool Success { get; set; }
    public SipCoreMeetingDto? Meeting { get; set; }
    public string? Message { get; set; }
}

public class MeetingControlResponse
{
    public bool Success { get; set; }
    public SipCoreMeetingDto? Meeting { get; set; }
    public string? Message { get; set; }
}

public class MeetingLiveResponse
{
    public bool Success { get; set; }
    public string MeetingId { get; set; } = "";
    public string Title { get; set; } = "";
    public string ConferenceNumber { get; set; } = "";
    public string Live { get; set; } = "";
    public string? Message { get; set; }
}
