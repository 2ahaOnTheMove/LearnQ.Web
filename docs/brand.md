# LearnQ Brand Assets

## Asset List

| File | Location | Description | Use When |
|---|---|---|---|
| `learnq-logo.svg` | `wwwroot/brand/` | Horizontal lockup: emerald mark + dark wordmark | Light/white backgrounds (default) |
| `learnq-logo-white.svg` | `wwwroot/brand/` | All-white lockup | Emerald or dark backgrounds |
| `learnq-mark.svg` | `wwwroot/brand/` | Monogram only, emerald | Compact spaces, favicons, tiles |
| `learnq-icon.svg` | `wwwroot/brand/` | App icon tile: white mark on rounded emerald square | PWA icon, app launcher |
| `favicon.svg` | `wwwroot/` | Same as icon, for browser tab | Browser favicon |

## Colors

| Token | Value | Usage |
|---|---|---|
| Emerald | `#047857` | Primary brand color, buttons, links, mark |
| Ink | `#0F172A` | Wordmark text, headings |

CSS variables in `wwwroot/css/app.css` override Bootstrap's `--bs-primary` and related tokens.

## Minimum Sizes

- **Full lockup:** Never below 20px tall. Below that, use the Mark.
- **Mark:** Never below 16px tall.

## Clear Space

Maintain clear space around the logo equal to approximately half the mark's height. Do not crowd the logo with other elements.

## Do and Don't

**Do:**
- Use the `BrandLogo` component everywhere (`Components/Shared/BrandLogo.razor`)
- Use `Variant="Full"` for light backgrounds, `Variant="FullWhite"` for dark/emerald backgrounds
- Use `Variant="Mark"` for compact spaces (footer, favicons)
- Set `Link="true"` when the logo should navigate to the home page
- Set `Decorative="true"` when the logo sits next to visible "LearnQ" text (e.g., footer)

**Don't:**
- Recolor, rotate, stretch, shadow, or outline the logo
- Place the emerald logo on emerald or dark backgrounds (use the white variant)
- Use raw `<img>` tags for brand assets — always use `BrandLogo`
- Reduce the logo below the minimum sizes
- Add CSS filters or transforms to the logo

## BrandLogo Component Usage

```razor
@* Navbar (linked, 28px) *@
<BrandLogo Variant="BrandLogoVariant.Full" Height="28" Link="true" />

@* Auth pages (linked, 32px) *@
<BrandLogo Variant="BrandLogoVariant.Full" Height="32" Link="true" />

@* Footer (decorative mark, 20px) *@
<BrandLogo Variant="BrandLogoVariant.Mark" Height="20" Decorative="true" />

@* Dark background (white variant) *@
<BrandLogo Variant="BrandLogoVariant.FullWhite" Height="28" Link="true" />
```

## Raster PNG Exports

The following PNG files are **not yet generated** (no SVG-to-PNG tooling is installed). To regenerate:

| File | Size | Source | Notes |
|---|---|---|---|
| `apple-touch-icon.png` | 180×180 | `learnq-icon.svg` | Opaque emerald background, no transparency |
| `icon-192.png` | 192×192 | `learnq-icon.svg` | For web manifest |
| `icon-512.png` | 512×512 | `learnq-icon.svg` | For web manifest |
| `icon-512-maskable.png` | 512×512 | `learnq-icon.svg` | Mark inside central 80% safe zone |
| `og-image.png` | 1200×630 | `learnq-logo.svg` | White background, centered lockup |
| `favicon.ico` | 16/32/48px | `learnq-icon.svg` | Multi-size ICO |

Once generated, place them in `wwwroot/` and update `site.webmanifest` and `App.razor` head metadata to reference them.
