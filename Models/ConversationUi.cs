namespace SipCoreMobile.Models;

public class ConversationUi
{
    public required string Extension { get; init; }
    public required string LastMessage { get; init; }
    public long LastMessageAt { get; init; }
    public int UnreadCount { get; init; }
    public bool IsOnline { get; init; }
    public string Status { get; init; } = "Unknown";
    public int CompanyId { get; init; }
    public string CompanyName { get; init; } = "";
    public bool IsExternal { get; init; }
    public long LastMessageTime { get; init; }
}
