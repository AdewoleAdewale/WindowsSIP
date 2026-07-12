using SipCoreMobile.Models;
using SipCoreMobile.Models.Api;

namespace SipCoreMobile.Services.Api;

/// <summary>
/// 1:1 port of the Kotlin Retrofit "SipCoreApiService" interface (from ApiClient.kt)
/// and the smaller "SipCoreApi" interface. Base URL: https://softworks-us.org/
///
/// NOTE: the email/mail endpoints from the original file are intentionally omitted here.
/// Their request/response DTOs (MailAccountDto, MailFolderDto, MailMessagePageDto,
/// MailMessageDetailDto, MailOAuthStartRequest/Response, ConnectMailAccountRequest,
/// SendMailRequest, ReplyMailRequest, ForwardMailRequest, SaveDraftRequest, MoveMailRequest,
/// EmailAiAnalyzeRequest/Response, MailAttachmentDto) are referenced in ApiClient.kt but
/// their class bodies live in a source file that wasn't included in the exported repo.
/// Port those endpoints once that file is available.
/// </summary>
public interface ISipCoreApiService
{
    Task RegisterFirebaseTokenAsync(FirebaseTokenRequest request);

    Task<MediaUploadResponse> UploadVoiceNoteAsync(Stream fileStream, string fileName, string contentType);
    Task<MediaUploadResponse> UploadAttachmentAsync(Stream fileStream, string fileName, string contentType);

    Task SaveChatMessageAsync(SaveChatMessageRequest request);
    Task<List<RemoteChatMessage>> GetConversationAsync(string userA, string userB);
    Task DeleteMessageAsync(MessageActionRequest request);
    Task ReactToMessageAsync(ReactionRequest request);
    Task MarkMessageReadAsync(MessageStatusActionRequest request);
    Task MarkMessageDeliveredAsync(MessageStatusActionRequest request);

    Task<List<ExtensionDto>> GetExtensionsAsync(string extension);
    Task UpdatePresenceAsync(PresenceRequest request);
    Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request);

    Task<WorkDeskDashboardDto> GetWorkDeskDashboardAsync(int companyId, string extension);
    Task<List<WorkTaskDto>> GetMyWorkTasksAsync(int companyId, string extension);
    Task<WorkTaskDetailResponse> GetWorkTaskAsync(int companyId, string taskId);
    Task AddWorkTaskCommentAsync(int companyId, string taskId, AddWorkTaskCommentRequest request);
    Task UpdateWorkTaskStatusAsync(int companyId, string taskId, UpdateWorkTaskStatusRequest request);
    Task ApproveWorkTaskAsync(int companyId, string taskId, ApproveTaskRequest request);
    Task<WorkTaskDto> CreateWorkTaskAsync(int companyId, CreateWorkTaskRequest request);
    Task CompleteChecklistItemAsync(int companyId, string taskId, string checklistItemId, CompleteChecklistItemRequest request);
    Task<WorkTaskAttachmentUploadResponse> UploadWorkTaskAttachmentAsync(int companyId, string taskId, Stream fileStream, string fileName, string contentType, string uploadedByExtension);
    Task<List<WorkTaskTemplateDto>> GetWorkTaskTemplatesAsync(int companyId);
    Task<List<WorkTaskCreatorDto>> GetWorkTaskCreatorsAsync(int companyId);
    Task<Stream> DownloadSingleTaskReportPdfAsync(int companyId, string taskId, string extension);
    Task<WorkTaskDto> UpdateWorkTaskAsync(int companyId, string taskId, UpdateWorkTaskRequest request);

    Task<CreateMeetingResponse> CreateMeetingAsync(int companyId, CreateMeetingRequest request);
    Task<MeetingListResponse> GetMeetingsAsync(int companyId, string extension);
    Task<MeetingSingleResponse> GetMeetingAsync(int companyId, string meetingId, string extension);
    Task<MeetingControlResponse> StartMeetingAsync(int companyId, string meetingId, MeetingActionRequest request);
    Task<MeetingControlResponse> EndMeetingAsync(int companyId, string meetingId, MeetingActionRequest request);
    Task<MeetingControlResponse> LockMeetingAsync(int companyId, string meetingId, MeetingActionRequest request);
    Task<MeetingControlResponse> UnlockMeetingAsync(int companyId, string meetingId, MeetingActionRequest request);
    Task<MeetingLiveResponse> GetLiveMeetingMembersAsync(int companyId, string meetingId, string extension);
    Task<SimpleSuccessResponse> MuteMeetingMemberAsync(int companyId, string meetingId, MeetingMemberActionRequest request);
    Task<SimpleSuccessResponse> UnmuteMeetingMemberAsync(int companyId, string meetingId, MeetingMemberActionRequest request);
    Task<SimpleSuccessResponse> KickMeetingMemberAsync(int companyId, string meetingId, MeetingMemberActionRequest request);
}
