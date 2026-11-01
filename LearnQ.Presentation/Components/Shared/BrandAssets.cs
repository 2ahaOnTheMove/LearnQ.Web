namespace LearnQ.Presentation.Components.Shared;

/// <summary>
/// Single source of truth for all LearnQ brand asset paths, aspect ratios, and colors.
/// </summary>
public static class BrandAssets
{
    public const string AppName = "LearnQ";
    public const string Emerald = "#047857";
    public const string Ink = "#0F172A";

    // Asset paths (relative to wwwroot)
    public const string LogoPath = "brand/learnq-logo.svg";
    public const string LogoWhitePath = "brand/learnq-logo-white.svg";
    public const string MarkPath = "brand/learnq-mark.svg";
    public const string IconPath = "brand/learnq-icon.svg";
    public const string FaviconPath = "favicon.svg";

    // Aspect ratios (width / height) from viewBox
    public const double LogoAspectRatio = 217.44 / 56.64; // ~3.84
    public const double MarkAspectRatio = 84.0 / 84.0;    // 1.0
}

/// <summary>
/// Brand logo display variants.
/// </summary>
public enum BrandLogoVariant
{
    Full,
    FullWhite,
    Mark
}
