namespace server.Models;

public enum VaultStatus
{
    Active = 0,
    Deleted = 1
}

public enum Privilege
{
    Owner = 0,
    Admin = 1,
    Member = 2
}

public enum InviteType
{
    Immediate = 0,
    Delayed = 1
}

public enum InviteStatus
{
    Pending = 0,
    Sent = 1,
    Accepted = 2,
    Cancelled = 3,
    Expired = 4
}

public enum MemberStatus
{
    Active = 0,
    Left = 1,
    Removed = 2
}

