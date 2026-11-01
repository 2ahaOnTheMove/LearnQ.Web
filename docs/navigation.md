# Navigation System

## Overview

The LearnQ navbar is a minimal, data-driven component. By default it shows only the **logo** and **one action button**. The action button label and target change based on authentication state:

- **Anonymous users**: "Get started" → Login page
- **Authenticated users**: "Dashboard" → Post-auth destination (verify email, onboarding, or dashboard)

## Architecture

```
Navigation/
  NavigationItem.cs        Sealed record + NavVisibility enum + NavLinksPlacement enum
  NavigationCatalog.cs     Static catalog: MainItems, labels, filtering

Components/Layout/
  NavMenu.razor            Header shell (logo + action button)
  NavPrimaryAction.razor   Single action button (AuthorizeView)
  NavLinks.razor           Renders MainItems (desktop inline or offcanvas stacked)
```

## NavigationCatalog

All user-facing nav strings and constants live in one place:

| Property | Value | Purpose |
|---|---|---|
| `MainItems` | Empty list | Extension point for nav items |
| `GetStartedHref` | `AppRoutes.Login` | Anonymous action button target |
| `GetStartedLabel` | "Get started" | Anonymous action button text |
| `DashboardLabel` | "Dashboard" | Authenticated action button text |

## Adding a Navigation Item

Add a record to `NavigationCatalog.MainItems`:

```csharp
public static IReadOnlyList<NavigationItem> MainItems { get; } = new[]
{
    new NavigationItem(
        Key: "home",
        Label: "Home",
        Href: AppRoutes.Home,
        Match: NavLinkMatch.All,
        Visibility: NavVisibility.Always,
        Policy: null,
        External: false
    ),
    new NavigationItem(
        Key: "docs",
        Label: "Documentation",
        Href: "https://docs.learnq.example",
        Match: NavLinkMatch.None,
        Visibility: NavVisibility.Always,
        Policy: null,
        External: true
    ),
};
```

When `MainItems` is non-empty, the navbar automatically renders:
- **lg and wider**: Items inline between the logo and the action button
- **Below lg**: Items in a Bootstrap offcanvas menu opened by a hamburger toggler

## Visibility and Policy Rules

| `NavVisibility` | Shown to |
|---|---|
| `Always` | Everyone |
| `AnonymousOnly` | Not signed in |
| `AuthenticatedOnly` | Signed in |

If `Policy` is set, the item is additionally filtered by `IAuthorizationService`. Hiding a link is **never** treated as security — the page guards remain the authority.

## Primary Action Target

The authenticated "Dashboard" button uses `PostAuthRoute.GetPostAuthDestination(ClaimsPrincipal)` so:
- Unverified users → `/auth/verify-email`
- No role → `/onboarding/role`
- Not onboarded → `/onboarding/student` or `/onboarding/teacher`
- Teacher awaiting review → `/onboarding/status`
- Otherwise → `/dashboard`

## Offcanvas Behavior

When MainItems is non-empty:
- The offcanvas closes on link click (via `navbar.js`)
- Back/Forward navigation closes the offcanvas
- Backdrop click, Esc, and the close button close it
- Focus is trapped while open, returns to the toggler on close

`wwwroot/js/navbar.js` handles all of this via document-level event delegation.

## Active Link Styling

Active links use `text-primary fw-medium` only. The `NavLink` component emits `aria-current="page"` automatically.
