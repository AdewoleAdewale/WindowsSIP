using SQLite;

namespace SipCoreMobile.Data.Entities;

[Table("call_logs")]
public class CallLogEntity
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }

    public  string Number { get; set; }    =string.Empty;
    public long Time { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
    public bool IsIncoming { get; set; }
    public bool IsMissed { get; set; }
    public int DurationSeconds { get; set; }
}
