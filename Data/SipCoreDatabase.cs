using SQLite;
using SipCoreMobile.Data.Entities;

namespace SipCoreMobile.Data;

/// <summary>
/// Port of Room's SipCoreDatabase. Room auto-creates tables from the @Entity list and
/// manages migrations by "version"; sqlite-net-pcl has no built-in migration system, so
/// CreateTablesAsync just ensures each table exists (safe to call repeatedly, does not
/// touch existing data/columns). If you need real schema migrations later, add explicit
/// ALTER TABLE steps here keyed off a stored schema version, the way Room's version=2 would have.
/// </summary>
public class SipCoreDatabase
{
    private const string DbFileName = "sipcore.db";
    private static SipCoreDatabase? _instance;
    private static readonly SemaphoreSlim InitLock = new(1, 1);

    public SQLiteAsyncConnection Connection { get; }

    private SipCoreDatabase()
    {
        var dbPath = Path.Combine(FileSystem.AppDataDirectory, DbFileName);
        Connection = new SQLiteAsyncConnection(dbPath);
    }

    public static async Task<SipCoreDatabase> GetDatabaseAsync()
    {
        if (_instance is not null) return _instance;

        await InitLock.WaitAsync();
        try
        {
            if (_instance is not null) return _instance;

            var db = new SipCoreDatabase();
            await db.Connection.CreateTableAsync<SipCredentialEntity>();
            await db.Connection.CreateTableAsync<ChatMessageEntity>();
            await db.Connection.CreateTableAsync<ConversationEntity>();
            await db.Connection.CreateTableAsync<CallLogEntity>();
            await db.Connection.CreateTableAsync<ContactGroupEntity>();
            await db.Connection.CreateTableAsync<LocalContactEntity>();
            await db.Connection.CreateTableAsync<ContactGroupMemberEntity>();

            _instance = db;
            return db;
        }
        finally
        {
            InitLock.Release();
        }
    }
}
