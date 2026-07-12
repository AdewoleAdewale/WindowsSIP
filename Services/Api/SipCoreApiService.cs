using System.Net.Http.Json;
using System.Text.Json;
using SipCoreMobile.Models;
using SipCoreMobile.Models.Api;

namespace SipCoreMobile.Services.Api;

/// <summary>
/// Port of ApiClient.kt's OkHttp/Retrofit/Gson setup. Register HttpClient via
/// MauiProgram: builder.Services.AddHttpClient&lt;ISipCoreApiService, SipCoreApiService&gt;(c =>
///     c.BaseAddress = new Uri("https://softworks-us.org/"));
/// The original OkHttpClient allowed TLS 1.0-1.3 plus cleartext as a fallback and set
/// 30s connect/read/write timeouts with retry-on-connection-failure; on Windows/.NET,
/// TLS version negotiation is handled by the OS (Schannel), so there's no equivalent
/// knob to set here -- just the 30s timeout, applied below.
/// </summary>
public class SipCoreApiService : ISipCoreApiService
{
    private readonly HttpClient _http;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };

    public SipCoreApiService(HttpClient httpClient)
    {
        _http = httpClient;
        _http.Timeout = TimeSpan.FromSeconds(30);
    }

    public async Task RegisterFirebaseTokenAsync(FirebaseTokenRequest request)
    {
        var resp = await _http.PostAsJsonAsync("api/firebase/register-token", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
    }

    public Task<MediaUploadResponse> UploadVoiceNoteAsync(Stream fileStream, string fileName, string contentType) =>
        UploadFileAsync("api/media/upload-voice", fileStream, fileName, contentType);

    public Task<MediaUploadResponse> UploadAttachmentAsync(Stream fileStream, string fileName, string contentType) =>
        UploadFileAsync("api/media/upload-attachment", fileStream, fileName, contentType);

    private async Task<MediaUploadResponse> UploadFileAsync(string path, Stream fileStream, string fileName, string contentType)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);

        var resp = await _http.PostAsync(path, content);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<MediaUploadResponse>(JsonOptions))!;
    }

    public async Task SaveChatMessageAsync(SaveChatMessageRequest request)
    {
        var resp = await _http.PostAsJsonAsync("api/chat/save", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<List<RemoteChatMessage>> GetConversationAsync(string userA, string userB)
    {
        var resp = await _http.GetAsync($"api/chat/conversation?userA={Uri.EscapeDataString(userA)}&userB={Uri.EscapeDataString(userB)}");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<List<RemoteChatMessage>>(JsonOptions))!;
    }

    public async Task DeleteMessageAsync(MessageActionRequest request)
    {
        var resp = await _http.PostAsJsonAsync("api/chat/delete", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
    }

    public async Task ReactToMessageAsync(ReactionRequest request)
    {
        var resp = await _http.PostAsJsonAsync("api/chat/reaction", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
    }

    public async Task MarkMessageReadAsync(MessageStatusActionRequest request)
    {
        var resp = await _http.PostAsJsonAsync("api/chat/read", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
    }

    public async Task MarkMessageDeliveredAsync(MessageStatusActionRequest request)
    {
        var resp = await _http.PostAsJsonAsync("api/chat/delivered", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<List<ExtensionDto>> GetExtensionsAsync(string extension)
    {
        var resp = await _http.GetAsync($"api/extensions?extension={Uri.EscapeDataString(extension)}");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<List<ExtensionDto>>(JsonOptions))!;
    }

    public async Task UpdatePresenceAsync(PresenceRequest request)
    {
        var resp = await _http.PostAsJsonAsync("api/extensions/presence", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<ChangePasswordResponse> ChangePasswordAsync(ChangePasswordRequest request)
    {
        var resp = await _http.PostAsJsonAsync("api/extensions/change-password", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<ChangePasswordResponse>(JsonOptions))!;
    }

    public async Task<WorkDeskDashboardDto> GetWorkDeskDashboardAsync(int companyId, string extension)
    {
        var resp = await _http.GetAsync($"api/companies/{companyId}/worktasks/dashboard/{extension}");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<WorkDeskDashboardDto>(JsonOptions))!;
    }

    public async Task<List<WorkTaskDto>> GetMyWorkTasksAsync(int companyId, string extension)
    {
        var resp = await _http.GetAsync($"api/companies/{companyId}/worktasks/my-tasks/{extension}");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<List<WorkTaskDto>>(JsonOptions))!;
    }

    public async Task<WorkTaskDetailResponse> GetWorkTaskAsync(int companyId, string taskId)
    {
        var resp = await _http.GetAsync($"api/companies/{companyId}/worktasks/{taskId}");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<WorkTaskDetailResponse>(JsonOptions))!;
    }

    public async Task AddWorkTaskCommentAsync(int companyId, string taskId, AddWorkTaskCommentRequest request)
    {
        var resp = await _http.PostAsJsonAsync($"api/companies/{companyId}/worktasks/{taskId}/comment", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
    }

    public async Task UpdateWorkTaskStatusAsync(int companyId, string taskId, UpdateWorkTaskStatusRequest request)
    {
        var resp = await _http.PostAsJsonAsync($"api/companies/{companyId}/worktasks/{taskId}/status", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
    }

    public async Task ApproveWorkTaskAsync(int companyId, string taskId, ApproveTaskRequest request)
    {
        var resp = await _http.PostAsJsonAsync($"api/companies/{companyId}/worktasks/{taskId}/approval", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<WorkTaskDto> CreateWorkTaskAsync(int companyId, CreateWorkTaskRequest request)
    {
        var resp = await _http.PostAsJsonAsync($"api/companies/{companyId}/worktasks/create", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<WorkTaskDto>(JsonOptions))!;
    }

    public async Task CompleteChecklistItemAsync(int companyId, string taskId, string checklistItemId, CompleteChecklistItemRequest request)
    {
        var resp = await _http.PostAsJsonAsync(
            $"api/companies/{companyId}/worktasks/{taskId}/checklist/{checklistItemId}/complete", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
    }

    public async Task<WorkTaskAttachmentUploadResponse> UploadWorkTaskAttachmentAsync(
        int companyId, string taskId, Stream fileStream, string fileName, string contentType, string uploadedByExtension)
    {
        using var content = new MultipartFormDataContent();
        using var fileContent = new StreamContent(fileStream);
        fileContent.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue(contentType);
        content.Add(fileContent, "file", fileName);
        content.Add(new StringContent(uploadedByExtension), "uploadedByExtension");

        var resp = await _http.PostAsync($"api/companies/{companyId}/worktasks/{taskId}/upload", content);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<WorkTaskAttachmentUploadResponse>(JsonOptions))!;
    }

    public async Task<List<WorkTaskTemplateDto>> GetWorkTaskTemplatesAsync(int companyId)
    {
        var resp = await _http.GetAsync($"api/companies/{companyId}/worktasks/templates");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<List<WorkTaskTemplateDto>>(JsonOptions))!;
    }

    public async Task<List<WorkTaskCreatorDto>> GetWorkTaskCreatorsAsync(int companyId)
    {
        var resp = await _http.GetAsync($"api/companies/{companyId}/worktasks/creators");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<List<WorkTaskCreatorDto>>(JsonOptions))!;
    }

    public async Task<Stream> DownloadSingleTaskReportPdfAsync(int companyId, string taskId, string extension)
    {
        var resp = await _http.GetAsync(
            $"api/companies/{companyId}/worktasks/{taskId}/reports/ai-summary/pdf?extension={Uri.EscapeDataString(extension)}");
        resp.EnsureSuccessStatusCode();
        return await resp.Content.ReadAsStreamAsync();
    }

    public async Task<WorkTaskDto> UpdateWorkTaskAsync(int companyId, string taskId, UpdateWorkTaskRequest request)
    {
        var resp = await _http.PostAsJsonAsync($"api/companies/{companyId}/worktasks/{taskId}/update", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<WorkTaskDto>(JsonOptions))!;
    }

    public async Task<CreateMeetingResponse> CreateMeetingAsync(int companyId, CreateMeetingRequest request)
    {
        var resp = await _http.PostAsJsonAsync($"api/companies/{companyId}/meetings/create", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<CreateMeetingResponse>(JsonOptions))!;
    }

    public async Task<MeetingListResponse> GetMeetingsAsync(int companyId, string extension)
    {
        var resp = await _http.GetAsync($"api/companies/{companyId}/meetings?extension={Uri.EscapeDataString(extension)}");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<MeetingListResponse>(JsonOptions))!;
    }

    public async Task<MeetingSingleResponse> GetMeetingAsync(int companyId, string meetingId, string extension)
    {
        var resp = await _http.GetAsync($"api/companies/{companyId}/meetings/{meetingId}?extension={Uri.EscapeDataString(extension)}");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<MeetingSingleResponse>(JsonOptions))!;
    }

    public async Task<MeetingControlResponse> StartMeetingAsync(int companyId, string meetingId, MeetingActionRequest request)
    {
        var resp = await _http.PostAsJsonAsync($"api/companies/{companyId}/meetings/{meetingId}/start", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<MeetingControlResponse>(JsonOptions))!;
    }

    public async Task<MeetingControlResponse> EndMeetingAsync(int companyId, string meetingId, MeetingActionRequest request)
    {
        var resp = await _http.PostAsJsonAsync($"api/companies/{companyId}/meetings/{meetingId}/end", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<MeetingControlResponse>(JsonOptions))!;
    }

    public async Task<MeetingControlResponse> LockMeetingAsync(int companyId, string meetingId, MeetingActionRequest request)
    {
        var resp = await _http.PostAsJsonAsync($"api/companies/{companyId}/meetings/{meetingId}/lock", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<MeetingControlResponse>(JsonOptions))!;
    }

    public async Task<MeetingControlResponse> UnlockMeetingAsync(int companyId, string meetingId, MeetingActionRequest request)
    {
        var resp = await _http.PostAsJsonAsync($"api/companies/{companyId}/meetings/{meetingId}/unlock", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<MeetingControlResponse>(JsonOptions))!;
    }

    public async Task<MeetingLiveResponse> GetLiveMeetingMembersAsync(int companyId, string meetingId, string extension)
    {
        var resp = await _http.GetAsync($"api/companies/{companyId}/meetings/{meetingId}/live?extension={Uri.EscapeDataString(extension)}");
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<MeetingLiveResponse>(JsonOptions))!;
    }

    public async Task<SimpleSuccessResponse> MuteMeetingMemberAsync(int companyId, string meetingId, MeetingMemberActionRequest request)
    {
        var resp = await _http.PostAsJsonAsync($"api/companies/{companyId}/meetings/{meetingId}/mute", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<SimpleSuccessResponse>(JsonOptions))!;
    }

    public async Task<SimpleSuccessResponse> UnmuteMeetingMemberAsync(int companyId, string meetingId, MeetingMemberActionRequest request)
    {
        var resp = await _http.PostAsJsonAsync($"api/companies/{companyId}/meetings/{meetingId}/unmute", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<SimpleSuccessResponse>(JsonOptions))!;
    }

    public async Task<SimpleSuccessResponse> KickMeetingMemberAsync(int companyId, string meetingId, MeetingMemberActionRequest request)
    {
        var resp = await _http.PostAsJsonAsync($"api/companies/{companyId}/meetings/{meetingId}/kick", request, JsonOptions);
        resp.EnsureSuccessStatusCode();
        return (await resp.Content.ReadFromJsonAsync<SimpleSuccessResponse>(JsonOptions))!;
    }
}
