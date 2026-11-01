using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;

namespace LearnQ.Presentation.Navigation;

/// <summary>
/// Single source of truth for navigation items and labels.
/// All user-facing nav strings live here so localization can be added later.
/// </summary>
public static class NavigationCatalog
{
    /// <summary>
    /// Main navigation items. Empty by default; add items here to extend the navbar.
    /// When non-empty, the navbar renders an offcanvas menu below the lg breakpoint.
    /// </summary>
    public static IReadOnlyList<NavigationItem> MainItems { get; } = Array.Empty<NavigationItem>();

    /// <summary>
    /// Href for the primary action button when the user is anonymous.
    /// Currently points to the login page; change to <c>AppRoutes.Register</c> if desired.
    /// </summary>
    public const string GetStartedHref = Routing.AppRoutes.Login;

    /// <summary>
    /// Label for the primary action button when the user is anonymous.
    /// </summary>
    public const string GetStartedLabel = "Get started";

    /// <summary>
    /// Label for the primary action button when the user is authenticated.
    /// </summary>
    public const string DashboardLabel = "Dashboard";

    /// <summary>
    /// Returns true when at least one item in <see cref="MainItems"/> exists.
    /// Used to decide whether to render the offcanvas toggler.
    /// </summary>
    public static bool HasItems() => MainItems.Count > 0;

    /// <summary>
    /// Filters navigation items by visibility and optional authorization policy.
    /// </summary>
    /// <param name="items">Items to filter.</param>
    /// <param name="isAuthenticated">Whether the user is signed in.</param>
    /// <param name="authorizationService">Optional service for policy checks; pass null to skip policy filtering.</param>
    /// <returns>Items visible to the current user.</returns>
    public static async Task<IReadOnlyList<NavigationItem>> FilterVisibleItemsAsync(
        IEnumerable<NavigationItem> items,
        bool isAuthenticated,
        IAuthorizationService? authorizationService)
    {
        var visible = new List<NavigationItem>();
        ClaimsPrincipal? nullPrincipal = null;

        foreach (var item in items)
        {
            if (!IsVisibleByState(item, isAuthenticated))
                continue;

            if (item.Policy != null && authorizationService != null)
            {
#pragma warning disable CS8604
                var result = await authorizationService.AuthorizeAsync(nullPrincipal, item.Policy);
#pragma warning restore CS8604
                if (!result.Succeeded)
                    continue;
            }

            visible.Add(item);
        }

        return visible;
    }

    private static bool IsVisibleByState(NavigationItem item, bool isAuthenticated) => item.Visibility switch
    {
        NavVisibility.Always => true,
        NavVisibility.AnonymousOnly => !isAuthenticated,
        NavVisibility.AuthenticatedOnly => isAuthenticated,
        _ => false
    };
}
