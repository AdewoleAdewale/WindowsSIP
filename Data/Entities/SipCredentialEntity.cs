using SQLite;

namespace SipCoreMobile.Data.Entities;

[Table("sip_credentials")]
public class SipCredentialEntity
{
    [PrimaryKey]
    public int Id { get; set; } = 1;

    public  string Extension { get; set; } = string.Empty;
    public  string Password { get; set; } = string.Empty;
    public  string Domain { get; set; } = string.Empty;
}

[Table("contact_groups")]
public class ContactGroupEntity
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }

    public  string Name { get; set; } = string.Empty;
    public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

[Table("local_contacts")]
public class LocalContactEntity
{
    [PrimaryKey]
    public  string Extension { get; set; } = string.Empty;

    public  string DisplayName { get; set; } = string.Empty;
    public long CreatedAt { get; set; } = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
}

// Composite primary key (groupId, extension) isn't natively supported by sqlite-net-pcl;
// enforce uniqueness via a composite unique index instead and treat Id as the surrogate key.
[Table("contact_group_members")]
public class ContactGroupMemberEntity
{
    [PrimaryKey, AutoIncrement]
    public long Id { get; set; }

    [Indexed(Name = "IX_GroupMember", Order = 1, Unique = true)]
    public long GroupId { get; set; }

    [Indexed(Name = "IX_GroupMember", Order = 2, Unique = true)]
    public  string Extension { get; set; } = string.Empty;
}
