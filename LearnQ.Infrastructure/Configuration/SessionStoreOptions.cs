namespace LearnQ.Infrastructure.Configuration;

public sealed class SessionStoreOptions
{
    public const string SectionName = "Session";

    // Redis key prefixes
    public const string SessionTokenKeyPrefix = "learnq-session-tokens:";
    public const string AuthTicketKeyPrefix = "learnq-auth-ticket:";

    // Data Protection purposes
    public const string SessionTokensPurpose = "LearnQ.SessionTokens.v1";
    public const string AuthTicketsPurpose = "LearnQ.AuthTickets.v1";

    // Redis Data Protection key collection name
    public const string DataProtectionKeyCollection = "DataProtection-Keys";

    // Redis cache instance name
    public const string CacheInstanceName = "learnq:";

    // Session durations
    public TimeSpan SlidingExpiration { get; set; } = TimeSpan.FromHours(8);
    public TimeSpan PersistentExpiration { get; set; } = TimeSpan.FromDays(30);
}
