# Navbar Manual QA Checklist

## Anonymous User (Logged Out)

- [ ] At 360px: Logo left, "Get started" button right, single consistent bar, no wrapping, no horizontal scroll
- [ ] At 768px: Same bar, no changes
- [ ] At 1280px: Same bar, no changes
- [ ] At 1440px: Same bar, no changes
- [ ] "Get started" button navigates to `/auth/login`
- [ ] No hamburger toggler rendered (MainItems is empty)
- [ ] Bar height is constant across widths
- [ ] Logo aspect ratio is preserved, no layout shift

## Authenticated User (Signed In)

- [ ] At all widths: Logo left, "Dashboard" button right
- [ ] "Dashboard" button routes to the correct destination for the user's state:
  - [ ] Unverified → `/auth/verify-email`
  - [ ] No role → `/onboarding/role`
  - [ ] Not onboarded → correct onboarding step
  - [ ] Teacher awaiting review → `/onboarding/status`
  - [ ] Fully onboarded → `/dashboard`
- [ ] After login, the button changes from "Get started" to "Dashboard" without hard refresh
- [ ] After logout, the button changes back to "Get started"

## Enhanced Navigation

- [ ] Navigate between pages (Home → Dashboard → Home): bar stays correct
- [ ] No console errors
- [ ] No requests to removed files (no 404s for old assets)

## Keyboard & Accessibility

- [ ] Tab order: Skip link → Logo → Action button
- [ ] Enter/Space activates the action button
- [ ] Focus rings are visible on all focusable elements
- [ ] After navigating, focus moves to the page h1
- [ ] Skip link is visually hidden until focused, then appears at the top
- [ ] `<nav aria-label="Main">` is present
- [ ] One `<main>` element with `id="main"`

## Sticky Bar & Modals

- [ ] Bar is sticky at the top when scrolling
- [ ] Content is never hidden under the sticky bar (scroll-padding-top works)
- [ ] Notice modals appear above the navbar (z-index)
- [ ] Printing hides the navbar (`d-print-none`)

## Logout Coverage

- [ ] Footer: Authenticated users see "Log out" link next to Terms/Privacy
- [ ] OnboardingLayout: Authenticated users see "Log out" below the logo
- [ ] VerifyEmailForm: "Log out and verify later" button works

## Extension Path (Temporarily Add Items)

Temporarily add three items to `NavigationCatalog.MainItems` (one External, one AuthenticatedOnly) and verify:

- [ ] At lg+: Items render inline between logo and action button
- [ ] Below lg: Hamburger opens offcanvas with stacked items
- [ ] Backdrop click closes offcanvas
- [ ] Esc closes offcanvas
- [ ] Close button closes offcanvas
- [ ] Clicking a link closes offcanvas AND navigates
- [ ] Browser Back/Forward leaves offcanvas closed
- [ ] No stuck backdrop or locked page scroll after closing
- [ ] Resizing from mobile to desktop while open hides offcanvas and restores scrolling
- [ ] Focus is trapped while offcanvas is open
- [ ] Focus returns to the hamburger on close
- [ ] `aria-current="page"` is correct on the active link
- [ ] External links open in a new tab with `rel="noopener noreferrer"`

**Then remove the items** and confirm the bar returns to logo + single action button with no hamburger.

## Console

- [ ] No JavaScript errors
- [ ] `window.bootstrap` exists
- [ ] `window.Blazor.addEventListener` exists
