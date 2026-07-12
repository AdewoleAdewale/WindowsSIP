namespace SipCoreMobile.Models;

public class GroupUi
{
    public long Id { get; init; }
    public required string Name { get; init; }
    public int MemberCount { get; init; }
}

public class GroupMemberUi
{
    public required string Extension { get; init; }
    public required string DisplayName { get; init; }
}
