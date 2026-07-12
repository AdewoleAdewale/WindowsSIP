namespace SipCoreMobile.Models;

public class ExtensionDto
{
    public int Id { get; init; }
    public string? OutboundNumber { get; init; }
    public string? Number { get; init; }
    public string? UserId { get; init; }
    public int CompanyId { get; init; }
    public string? CompanyName { get; init; }
    public string? SipUsername { get; init; }
    public string? CreatedAt { get; init; }

    public string? Email { get; init; }
    public string? LastSeenUtc { get; init; }

    public required string Extension { get; init; }
    public string? Name { get; init; }
    public bool IsOnline { get; init; }
    public string? Status { get; init; }

    public bool IsExternal { get; init; }
    public bool CanCall { get; init; } = true;
    public bool CanMessage { get; init; } = true;
    public bool CanViewPresence { get; init; } = true;
    public bool CanUseWorkDesk { get; init; } = true;
}
