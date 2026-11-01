namespace LearnQ.Presentation.Authorization;

/// <summary>
/// Helper extensions for HTTP request checks.
/// </summary>
internal static class HttpRequestExtensions
{
    /// <summary>
    /// Returns true if the request Accept header includes text/html.
    /// </summary>
    public static bool AcceptsHtml(this HttpRequest request)
    {
        return request.Headers.Accept.Any(accept =>
            accept?.Contains("text/html", StringComparison.OrdinalIgnoreCase) == true);
    }
}
