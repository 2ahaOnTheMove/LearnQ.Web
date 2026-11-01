namespace LearnQ.Presentation.Routing;

public static class ReturnUrlValidator
{
    public static bool IsSafeLocalUrl(string? url)
    {
        if (string.IsNullOrWhiteSpace(url))
        {
            return false;
        }

        // Must start with exactly one '/' and not '//' or '/\'
        if (url[0] != '/' || (url.Length > 1 && (url[1] == '/' || url[1] == '\\')))
        {
            return false;
        }

        // Must not be an absolute URI with scheme or host
        if (Uri.TryCreate(url, UriKind.RelativeOrAbsolute, out var uri) && uri.IsAbsoluteUri)
        {
            return false;
        }

        return true;
    }

    public static string? GetSafeReturnUrl(string? url) => IsSafeLocalUrl(url) ? url : null;
}
