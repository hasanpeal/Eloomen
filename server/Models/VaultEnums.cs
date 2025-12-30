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

public enum ItemType
{
    Document = 0,
    Password = 1,
    Note = 2,
    Link = 3,
    CryptoWallet = 4
}

public enum ItemStatus
{
    Active = 0,
    Deleted = 1
}

public enum ItemPermission
{
    View = 0,
    Edit = 1
}

public enum WalletType
{
    SeedPhrase = 0,
    PrivateKey = 1,
    ExchangeLogin = 2
}

public enum ContentFormat
{
    PlainText = 0
}

