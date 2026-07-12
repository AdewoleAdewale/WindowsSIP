namespace SipCoreMobile.Models;

public enum MessageStatus
{
    Sending,
    Sent,
    Delivered,
    Read,
    Failed
}

public record ChatMessage
{
    public string Id { get; init; } = Guid.NewGuid().ToString();
    public required string Extension { get; init; }
    public required string Body { get; init; }
    public bool IsMine { get; init; }
    public long Time { get; init; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public MessageStatus Status { get; init; } = MessageStatus.Sent;

    public bool IsDeleted { get; init; }
    public string? ReplyToId { get; init; }
    public string? ReplyPreview { get; init; }
    public string? Reaction { get; init; }
}
