namespace SipCoreMobile.Models;

public class ContactUi
{
    public required string Extension { get; init; }
    public required string DisplayName { get; init; }
    public bool IsOnline { get; init; }
    public string Status { get; init; } = "Unknown";
    public int CompanyId { get; init; }
    public string CompanyName { get; init; } = "";
    public bool IsExternal { get; init; }
    public bool CanCall { get; init; } = true;
    public bool CanMessage { get; init; } = true;
    public bool CanViewPresence { get; init; } = true;
    public bool CanUseWorkDesk { get; init; } = true;
    public string? Email { get; init; }
    public string? LastSeenUtc { get; init; }
}
