namespace LearnQ.Presentation.Notices;

public static class NoticeCatalog
{
    public static class NoticeKeys
    {
        public const string AccessDenied = "access-denied";
        public const string SessionExpired = "session-expired";
        public const string Registered = "registered";
        public const string TooManyRequests = "too-many-requests";
    }

    public sealed record NoticeEntry(
        string Key,
        string Title,
        string Message,
        string CloseText,
        string Severity)
    {
        public static readonly NoticeEntry AccessDenied = new(
            NoticeKeys.AccessDenied,
            "Access Denied",
            "You don't have access to that page.",
            "OK",
            "danger");

        public static readonly NoticeEntry SessionExpired = new(
            NoticeKeys.SessionExpired,
            "Session Expired",
            "Your session has expired. Please sign in again.",
            "Sign In",
            "danger");

        public static readonly NoticeEntry Registered = new(
            NoticeKeys.Registered,
            "Account Created",
            "Your account was created. Please sign in.",
            "Sign In",
            "info");

        public static readonly NoticeEntry TooManyRequests = new(
            NoticeKeys.TooManyRequests,
            "Too Many Attempts",
            "Too many attempts. Please wait a moment and try again.",
            "OK",
            "warning");

        public static readonly NoticeEntry NotFound = new(
            "",
            "Page Not Found",
            "Sorry, the page you are looking for does not exist or may have been moved.",
            "Go to Home",
            "info");
    }

    static readonly IReadOnlyDictionary<string, NoticeEntry> _entries = new Dictionary<string, NoticeEntry>(StringComparer.OrdinalIgnoreCase)
    {
        [NoticeKeys.AccessDenied] = NoticeEntry.AccessDenied,
        [NoticeKeys.SessionExpired] = NoticeEntry.SessionExpired,
        [NoticeKeys.Registered] = NoticeEntry.Registered,
        [NoticeKeys.TooManyRequests] = NoticeEntry.TooManyRequests,
    };

    public static bool TryGetNotice(string? key, out NoticeEntry? entry)
    {
        if (!string.IsNullOrEmpty(key) && _entries.TryGetValue(key, out entry))
        {
            return true;
        }

        entry = null;
        return false;
    }
}
