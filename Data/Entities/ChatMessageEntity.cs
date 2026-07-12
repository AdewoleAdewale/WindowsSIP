using SQLite;

namespace SipCoreMobile.Data.Entities;

[Table("chat_messages")]
public class ChatMessageEntity
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }

    public  string ConversationId { get; set; } = string.Empty;
    public  string Sender { get; set; } = string.Empty;
    public  string Body { get; set; } = string.Empty;
    public bool IsMine { get; set; }
    public long SentAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public string Status { get; set; } = "Sent";

    [Indexed(Unique = true)]
    public string MessageId { get; set; } = Guid.NewGuid().ToString();

    public bool IsDeleted { get; set; }
    public string? Reaction { get; set; }
    public string? ReplyToId { get; set; }
    public string? ReplyPreview { get; set; }
}
