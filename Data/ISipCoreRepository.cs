using SipCoreMobile.Data.Entities;

namespace SipCoreMobile.Data;

/// <summary>Port of the Room @Dao interface SipCoreDao.kt.</summary>
public interface ISipCoreRepository
{
    Task SaveCredentialsAsync(SipCredentialEntity credentials);
    Task<SipCredentialEntity?> GetCredentialsAsync();
    Task ClearCredentialsAsync();

    Task<List<ChatMessageEntity>> GetMessagesAsync(string conversationId);
    Task SaveConversationAsync(ConversationEntity conversation);
    Task<List<ConversationEntity>> GetConversationsAsync();
    Task<ConversationEntity?> GetConversationByExtensionAsync(string extension);
    Task MarkConversationReadAsync(string extension);

    Task InsertCallLogAsync(CallLogEntity log);
    Task<List<CallLogEntity>> GetCallLogsAsync();
    Task ClearCallLogsAsync();

    Task<long> CreateGroupAsync(ContactGroupEntity group);
    Task SaveLocalContactAsync(LocalContactEntity contact);
    Task AddContactToGroupAsync(ContactGroupMemberEntity member);
    Task RemoveContactFromGroupAsync(long groupId, string extension);
    Task<List<ContactGroupEntity>> GetGroupsAsync();
    Task<List<LocalContactEntity>> GetLocalContactsAsync();
    Task<List<LocalContactEntity>> GetContactsInGroupAsync(long groupId);

    Task MarkMessageDeletedAsync(string messageId);
    Task UpdateMessageReactionAsync(string messageId, string? reaction);
    Task UpdateMessageStatusAsync(string messageId, string status);
    Task<ChatMessageEntity?> GetMessageByMessageIdAsync(string messageId);
    Task UpdateMessageFromMiddlewareAsync(
        string messageId, string body, string status, bool isDeleted,
        string? reaction, string? replyToId, string? replyPreview);

    Task<ChatMessageEntity?> GetRecentDuplicateMessageAsync(string sender, string body, long afterTime);
    Task InsertMessageAsync(ChatMessageEntity message);
    Task UpsertMessageAsync(ChatMessageEntity message);
}
