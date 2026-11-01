---
sessionId: session-261101-204745-mpy7
---

# Solution Refactor — Final Cleanup

## Completed (Steps 1-4)

| Step | Status | Summary |
|---|---|---|
| 1. NoticeCatalog + modal components | ✓ | `Notices/NoticeCatalog.cs` created, `NoticeModal` and `NoticeHost` updated with severity and retry support |
| 2. Rate limiter OnRejected + NotFound | ✓ | OnRejected handler fixed (303 redirect, Retry-After header, GET vs POST handling), `NotFound.razor` uses catalog |
| 3. Application abstractions + Infrastructure | ✓ | `ISessionTokenStore`, `ISessionAccessor` created; `BearerTokenHandler` and `RedisSessionTokenStore` moved to Infrastructure; options pattern added |
| 4. Presentation reorganization + Program.cs | ✓ | Security/ files moved to Authentication/, Authorization/, Middleware/, Notices/, Endpoints/; Startup extensions created; Program.cs ~17 lines |

## Remaining Work (Step 5 — Final Cleanup)

### Issues Found

| Issue | Severity | Location |
|---|---|---|
| **Security/ folder still exists** with 10 dead files | High | `Security/*.cs` — duplicates of files now in Authentication/, Authorization/, etc. |
| **Duplicate claim types**: `ClaimTypesConstants` (Application) vs `AppClaims` (Presentation) | High | 5 constants duplicated; `AppClaims` has 9 total, `ClaimTypesConstants` has 5 |
| **PresentationExtensions.cs has no namespace** | Medium | Sits at Presentation root in global namespace |
| **Magic strings** in PostAuthRoute | Medium | `"REVIEW"`, `"PENDING_REVIEW"` hardcoded |
| **Magic strings** in SecurityExtensions | Medium | Rate limit policy names `"login"`, `"verify-email"`, `"resend-verification"`; config keys |
| **Magic strings** in AuthenticationExtensions | Medium | Cookie names, `"ASPNETCORE_ENVIRONMENT"` check |
| **No Directory.Build.props** | Low | Shared properties scattered across .csproj files |
| **No .editorconfig** | Low | No IDE rules for namespace matching, unused usings, etc. |
| **No RootNamespace** in .csproj | Low | Relies on implicit naming |
| **_Imports.razor** has redundant entry | Low | `LearnQ.Presentation.Components` is redundant since subfolders are imported |
| **.gitignore** missing patterns | Low | No user secrets, no local config exclusions |

### Plan

### ✓ Step 1: Delete dead Security/ folder and merge duplicate claim types

- Delete all 10 files in `Security/` folder (dead duplicates)
- Merge `ClaimTypesConstants` + `AppClaims` into single `AuthClaimTypes` in `Application/Common/Constants/`
- Update all 16 references to use the merged `AuthClaimTypes`
- Delete `ClaimTypesConstants.cs` and `Authentication/AuthClaimTypes.cs`
- Run `dotnet build` — 0 errors, 0 warnings

### ✓ Step 2: Fix PresentationExtensions namespace and move to Startup/

- Moved `PresentationExtensions.cs` to `Startup/PresentationExtensions.cs`
- Added `namespace LearnQ.Presentation.Startup`
- Run `dotnet build` — 0 errors, 0 warnings

### ✓ Step 3: Replace magic strings with constants

- Added `Review` and `PendingReview` constants to `OnboardingConstants`
- Replaced magic strings in `PostAuthRoute.cs`
- Created `RateLimitPolicies` static class with policy name constants
- Replaced magic strings in `SecurityExtensions.cs`
- Added cookie name constants to `AuthenticationExtensions.cs`
- Replaced `"ASPNETCORE_ENVIRONMENT"` string with `IHostEnvironment`
- Updated `Login.razor` and `VerifyEmail.razor` to use `RateLimitPolicies` constants
- Run `dotnet build` — 0 errors, 0 warnings

### ✓ Step 4: Add project infrastructure files

- Created `Directory.Build.props` with shared properties (TargetFramework, Nullable, ImplicitUsings, LangVersion)
- Created `.editorconfig` with namespace matching, unused usings warnings, file-scoped namespaces
- Removed duplicate properties from all `.csproj` files (now inherited from Directory.Build.props)
- Updated `.gitignore` with user secrets, local config, and OS file patterns
- Cleaned up `_Imports.razor` — removed redundant `LearnQ.Presentation.Components` entry
- Run `dotnet build` — 0 errors, 0 warnings

### ✓ Step 5: Final verification

- `dotnet clean` then `dotnet build` (Debug) — 0 errors, 0 warnings
- `dotnet build -c Release` — 0 errors, 0 warnings
- `dotnet format` fixed unused usings; `dotnet format --verify-no-changes` — clean
- Runtime verification (Redis startup, DI validation, modal rendering) requires backend endpoints

## Navbar Minimal Rebuild (In Progress)

### ✓ Step 1: Create Navigation catalog types and labels

- Created `Navigation/NavigationItem.cs` with sealed record and `NavVisibility` enum
- Created `Navigation/NavigationCatalog.cs` with empty `MainItems`, labels, and filtering method

### ✓ Step 2: Rebuild NavMenu and create NavPrimaryAction

- Rebuilt `NavMenu.razor` as minimal 9-line header: logo + NavPrimaryAction
- Created `Components/Layout/NavPrimaryAction.razor` with AuthorizeView
- Deleted `Authentication/ClaimsPrincipalExtensions.cs`
- Fixed nullable warning in `NavigationCatalog.cs`

### ✓ Step 3: Create NavLinks component and keep navbar.js

- Created `Components/Layout/NavLinks.razor` with desktop and offcanvas placements
- Moved `NavLinksPlacement` enum to `Navigation/NavigationItem.cs`
- `wwwroot/js/navbar.js` kept as-is for extension path

### ✓ Step 4: Add accessibility, FocusOnNavigate, and logout coverage

- Added `--app-navbar-height` CSS variable and `scroll-padding-top` to `app.css`
- Removed dead CSS: `.nav-user-name`, `.avatar-initials`
- Added `<FocusOnNavigate>` to `Routes.razor`
- Added logout button to `OnboardingLayout` header
- Added authenticated logout to `MainLayout` footer

### ✓ Step 5: Cleanup and documentation

- Created `docs/navigation.md`: catalog usage, how to add items, visibility/policy rules
- Created `docs/navbar-qa.md`: manual QA checklist covering all verification points
- `dotnet build` (Debug) — 0 errors, 0 warnings
- `dotnet build -c Release` — 0 errors, 0 warnings
- `dotnet format --verify-no-changes` — clean