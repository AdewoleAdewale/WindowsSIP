using SQLite;

namespace SipCoreMobile.Data.Entities;

[Table("conversations")]
public class ConversationEntity
{
    [PrimaryKey]
    public  string Extension { get; set; } = string.Empty;

    public  string LastMessage { get; set; } = string.Empty;
    public long LastMessageAt { get; set; }
    public int UnreadCount { get; set; }
}
