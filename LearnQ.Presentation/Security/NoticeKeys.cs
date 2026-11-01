namespace LearnQ.Presentation.Security;

public static class NoticeKeys
{
    public const string AccessDenied = "access-denied";
    public const string SessionExpired = "session-expired";
    public const string Registered = "registered";
    public const string TooManyRequests = "too-many-requests";

    public static readonly IReadOnlyDictionary<string, (string Title, string Message)> Notices =
        new Dictionary<string, (string Title, string Message)>(StringComparer.OrdinalIgnoreCase)
        {
            [AccessDenied] = ("Access Denied", "You do not have permission to access that area."),
            [SessionExpired] = ("Session Expired", "Your session has expired. Please sign in again to continue."),
            [Registered] = ("Account Created", "Your account was created successfully. Please sign in to verify your email."),
            [TooManyRequests] = ("Too Many Requests", "Too many attempts were made in a short period. Please wait a moment before trying again.")
        };

    public static bool TryGetNotice(string? key, out (string Title, string Message) notice)
    {
        if (!string.IsNullOrEmpty(key) && Notices.TryGetValue(key, out notice))
        {
            return true;
        }

        notice = default;
        return false;
    }
}
