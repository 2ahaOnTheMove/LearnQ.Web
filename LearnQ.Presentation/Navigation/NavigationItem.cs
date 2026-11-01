using Microsoft.AspNetCore.Components.Routing;

namespace LearnQ.Presentation.Navigation;

/// <summary>
/// Represents a single navigation item in the catalog.
/// </summary>
/// <param name="Key">Unique identifier for the item.</param>
/// <param name="Label">Display text.</param>
/// <param name="Href">Route path, typically from <see cref="Routing.AppRoutes"/>.</param>
/// <param name="Match">How the active state is matched.</param>
/// <param name="Visibility">When the item is visible.</param>
/// <param name="Policy">Optional authorization policy name; null means no policy check.</param>
/// <param name="External">True if the link opens in a new tab.</param>
public sealed record NavigationItem(
    string Key,
    string Label,
    string Href,
    NavLinkMatch Match,
    NavVisibility Visibility,
    string? Policy,
    bool External
);

/// <summary>
/// Controls when a navigation item is visible.
/// </summary>
public enum NavVisibility
{
    /// <summary>Always visible regardless of authentication state.</summary>
    Always,

    /// <summary>Visible only to anonymous (not signed-in) users.</summary>
    AnonymousOnly,

    /// <summary>Visible only to authenticated users.</summary>
    AuthenticatedOnly
}

/// <summary>
/// Where the navigation links are rendered.
/// </summary>
public enum NavLinksPlacement
{
    /// <summary>Inline desktop navbar (lg+).</summary>
    Desktop,

    /// <summary>Stacked offcanvas menu (below lg).</summary>
    Offcanvas
}
