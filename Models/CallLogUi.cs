namespace SipCoreMobile.Models;

public class CallLogUi
{
    public required string Number { get; init; }
    public long Time { get; init; }
    public bool IsIncoming { get; init; }
    public bool IsMissed { get; init; }
    public int DurationSeconds { get; init; }
}
