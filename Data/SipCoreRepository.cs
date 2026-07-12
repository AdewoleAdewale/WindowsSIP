using SipCoreMobile.Data.Entities;

namespace SipCoreMobile.Data;

public class SipCoreRepository : ISipCoreRepository
{
    private readonly SipCoreDatabase _db;

    public SipCoreRepository(SipCoreDatabase db) => _db = db;

    public Task SaveCredentialsAsync(SipCredentialEntity credentials) =>
        _db.Connection.InsertOrReplaceAsync(credentials);

    public async Task<SipCredentialEntity?> GetCredentialsAsync() =>
        await _db.Connection.Table<SipCredentialEntity>().Where(c => c.Id == 1).FirstOrDefaultAsync();

    public Task ClearCredentialsAsync() =>
        _db.Connection.DeleteAllAsync<SipCredentialEntity>();

    public async Task<List<ChatMessageEntity>> GetMessagesAsync(string conversationId) =>
        await _db.Connection.Table<ChatMessageEntity>()
            .Where(m => m.ConversationId == conversationId)
            .OrderBy(m => m.SentAt)
            .ToListAsync();

    public Task SaveConversationAsync(ConversationEntity conversation) =>
        _db.Connection.InsertOrReplaceAsync(conversation);

    public async Task<List<ConversationEntity>> GetConversationsAsync() =>
        await _db.Connection.Table<ConversationEntity>()
            .OrderByDescending(c => c.LastMessageAt)
            .ToListAsync();

    public async Task<ConversationEntity?> GetConversationByExtensionAsync(string extension) =>
        await _db.Connection.Table<ConversationEntity>()
            .Where(c => c.Extension == extension)
            .FirstOrDefaultAsync();

    public Task MarkConversationReadAsync(string extension) =>
        _db.Connection.ExecuteAsync(
            "UPDATE conversations SET unreadCount = 0 WHERE extension = ?", extension);

    public Task InsertCallLogAsync(CallLogEntity log) =>
        _db.Connection.InsertAsync(log);

    public async Task<List<CallLogEntity>> GetCallLogsAsync() =>
        await _db.Connection.Table<CallLogEntity>()
            .OrderByDescending(l => l.Time)
            .ToListAsync();

    public Task ClearCallLogsAsync() =>
        _db.Connection.DeleteAllAsync<CallLogEntity>();

    public async Task<long> CreateGroupAsync(ContactGroupEntity group)
    {
        await _db.Connection.InsertAsync(group);
        return group.Id;
    }

    public Task SaveLocalContactAsync(LocalContactEntity contact) =>
        _db.Connection.InsertOrReplaceAsync(contact);

    public Task AddContactToGroupAsync(ContactGroupMemberEntity member) =>
        _db.Connection.ExecuteAsync(
            "INSERT OR REPLACE INTO contact_group_members (GroupId, Extension) VALUES (?, ?)",
            member.GroupId, member.Extension);

    public Task RemoveContactFromGroupAsync(long groupId, string extension) =>
        _db.Connection.ExecuteAsync(
            "DELETE FROM contact_group_members WHERE GroupId = ? AND Extension = ?", groupId, extension);

    public async Task<List<ContactGroupEntity>> GetGroupsAsync() =>
        await _db.Connection.Table<ContactGroupEntity>()
            .OrderBy(g => g.Name)
            .ToListAsync();

    public async Task<List<LocalContactEntity>> GetLocalContactsAsync() =>
        await _db.Connection.Table<LocalContactEntity>()
            .OrderBy(c => c.DisplayName)
            .ToListAsync();

    public Task<List<LocalContactEntity>> GetContactsInGroupAsync(long groupId) =>
        _db.Connection.QueryAsync<LocalContactEntity>(@"
            SELECT c.* FROM local_contacts c
            INNER JOIN contact_group_members m ON c.Extension = m.Extension
            WHERE m.GroupId = ?
            ORDER BY c.DisplayName ASC", groupId);

    public Task MarkMessageDeletedAsync(string messageId) =>
        _db.Connection.ExecuteAsync(
            "UPDATE chat_messages SET isDeleted = 1 WHERE MessageId = ?", messageId);

    public Task UpdateMessageReactionAsync(string messageId, string? reaction) =>
        _db.Connection.ExecuteAsync(
            "UPDATE chat_messages SET reaction = ? WHERE MessageId = ?", reaction, messageId);

    public Task UpdateMessageStatusAsync(string messageId, string status) =>
        _db.Connection.ExecuteAsync(
            "UPDATE chat_messages SET status = ? WHERE MessageId = ?", status, messageId);

    public Task<ChatMessageEntity?> GetMessageByMessageIdAsync(string messageId) =>
        _db.Connection.Table<ChatMessageEntity>()
            .Where(m => m.MessageId == messageId)
            .FirstOrDefaultAsync();

    public Task UpdateMessageFromMiddlewareAsync(
        string messageId, string body, string status, bool isDeleted,
        string? reaction, string? replyToId, string? replyPreview) =>
        _db.Connection.ExecuteAsync(@"
            UPDATE chat_messages
            SET body = ?, status = ?, isDeleted = ?, reaction = ?, replyToId = ?, replyPreview = ?
            WHERE MessageId = ?",
            body, status, isDeleted, reaction, replyToId, replyPreview, messageId);

    public Task<ChatMessageEntity?> GetRecentDuplicateMessageAsync(string sender, string body, long afterTime) =>
        _db.Connection.Table<ChatMessageEntity>()
            .Where(m => m.Sender == sender && m.Body == body && m.SentAt >= afterTime)
            .FirstOrDefaultAsync();

    // FIX CS1061: Replaced non-existent native async call with standard insertion wrapper
    public async Task InsertMessageAsync(ChatMessageEntity message)
    {
        try
        {
            await _db.Connection.InsertAsync(message);
        }
        catch (SQLite.SQLiteException ex) when (ex.Result == SQLite.SQLite3.Result.Constraint)
        {
            // Already inside database, safely ignore duplicate key constraint violations
        }
    }

    public Task UpsertMessageAsync(ChatMessageEntity message) =>
        _db.Connection.InsertOrReplaceAsync(message);
}