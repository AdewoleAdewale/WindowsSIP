namespace SipCoreMobile.Models.Api;

public class SaveChatMessageRequest
{
    public required string MessageId { get; set; }
    public required string ConversationId { get; set; }
    public required string SenderExtension { get; set; }
    public required string ReceiverExtension { get; set; }
    public required string Body { get; set; }
    public string? Status { get; set; } = "Sent";
    public bool IsDeleted { get; set; }
    public string? Reaction { get; set; }
    public string? ReplyToId { get; set; }
    public string? ReplyPreview { get; set; }
}

public class RemoteChatMessage
{
    public required string MessageId { get; set; }
    public required string ConversationId { get; set; }
    public required string SenderExtension { get; set; }
    public required string ReceiverExtension { get; set; }
    public required string Body { get; set; }
    public required string Status { get; set; }
    public bool IsDeleted { get; set; }
    public string? Reaction { get; set; }
    public string? ReplyToId { get; set; }
    public string? ReplyPreview { get; set; }
    public required string SentAt { get; set; }
}

public class MessageActionRequest
{
    public required string MessageId { get; set; }
}

public class MessageStatusActionRequest
{
    public required string MessageId { get; set; }
    public required string ActorExtension { get; set; }
}

public class ReactionRequest
{
    public required string MessageId { get; set; }
    public required string Reaction { get; set; }
}
