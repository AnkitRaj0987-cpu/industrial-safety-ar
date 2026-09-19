// UITheme.cs
// Namespace : IndustrialSafetyAR.UI
//
// Centralized, reusable mobile design system and theme for the Industrial Safety AR application.
// Enforces a clean mobile-first UI:
// - Light/off-white card surfaces (#FFFFFF / #F8FAFC)
// - Crisp dark typography (#0F172A / #475569) for high contrast and immediate readability
// - Safety-action orange for primary CTAs (#F97316 / #EA580C)
// - Semantic green (#16A34A), red (#DC2626), and amber (#D97706) for safety statuses
// - Typography calibrated for 1080x1920 mobile reference resolution
// - Touch targets sized >= 48dp equivalent for reliable one-handed industrial glove / finger taps

using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace IndustrialSafetyAR.UI
{
    public static class UITheme
    {
        // =========================================================================
        // PALETTE TOKENS
        // =========================================================================

        // Surface / Backgrounds
        public static readonly Color ScreenBackground = new Color(0.965f, 0.973f, 0.984f, 1f); // #F6F8FB Light warm slate
        public static readonly Color CardBackground = new Color(1f, 1f, 1f, 1f);              // #FFFFFF Pure white card
        public static readonly Color CardSecondaryBg = new Color(0.957f, 0.965f, 0.976f, 1f); // #F4F6F9 Light surface
        public static readonly Color CardBorder = new Color(0.886f, 0.910f, 0.941f, 1f);      // #E2E8F0 Subtle card border
        public static readonly Color CardBorderHighlight = new Color(0.75f, 0.80f, 0.88f, 1f); // Highlighted card outline
        public static readonly Color ArHudBackground = new Color(1f, 1f, 1f, 0.96f);           // Translucent white for AR HUD cards

        // Typography (Dark High-Contrast Text on Light Cards)
        public static readonly Color TextPrimary = new Color(0.059f, 0.090f, 0.165f, 1f);      // #0F172A Deep slate dark text
        public static readonly Color TextSecondary = new Color(0.278f, 0.333f, 0.412f, 1f);    // #475569 Mid slate secondary
        public static readonly Color TextMuted = new Color(0.450f, 0.520f, 0.620f, 1f);        // #73859E Muted / caption text
        public static readonly Color TextLightOnDark = new Color(1f, 1f, 1f, 1f);              // White text for buttons/badges

        // Primary Safety Action CTA (Safety Orange)
        public static readonly Color PrimaryOrange = new Color(0.976f, 0.451f, 0.086f, 1f);    // #F97316 Safety Orange
        public static readonly Color PrimaryOrangePressed = new Color(0.761f, 0.255f, 0.047f, 1f); // #C2410C Deep orange
        public static readonly Color PrimaryDisabled = new Color(0.820f, 0.850f, 0.890f, 1f);  // #D1D9E3 Disabled button
        public static readonly Color PrimaryDisabledText = new Color(0.55f, 0.60f, 0.68f, 1f);

        // Semantic Status Colors
        public static readonly Color SuccessGreen = new Color(0.086f, 0.639f, 0.290f, 1f);     // #16A34A Success Green
        public static readonly Color SuccessSurface = new Color(0.863f, 0.988f, 0.906f, 1f);   // #DCFCE7 Light green badge
        public static readonly Color SuccessText = new Color(0.082f, 0.502f, 0.231f, 1f);      // #15803D Dark green text

        public static readonly Color DangerRed = new Color(0.863f, 0.149f, 0.149f, 1f);       // #DC2626 Danger Red
        public static readonly Color DangerSurface = new Color(0.996f, 0.886f, 0.886f, 1f);     // #FEE2E2 Light red badge
        public static readonly Color DangerText = new Color(0.722f, 0.106f, 0.106f, 1f);       // #B91C1C Dark red text

        public static readonly Color WarningAmber = new Color(0.851f, 0.467f, 0.024f, 1f);     // #D97706 Warning Amber
        public static readonly Color WarningSurface = new Color(0.996f, 0.953f, 0.780f, 1f);   // #FEF3C7 Light amber badge
        public static readonly Color WarningText = new Color(0.706f, 0.325f, 0.016f, 1f);      // #B45309 Dark amber text

        public static readonly Color InfoBlue = new Color(0.012f, 0.522f, 0.784f, 1f);         // #0284C7 Info Blue
        public static readonly Color InfoSurface = new Color(0.878f, 0.949f, 0.996f, 1f);      // #E0F2FE Light blue badge
        public static readonly Color InfoText = new Color(0.012f, 0.412f, 0.627f, 1f);         // #0369A1 Dark blue text

        // Unified Aliases across controllers
        public static readonly Color PrimaryAction = PrimaryOrange;
        public static readonly Color CardSecondary = CardSecondaryBg;
        public static readonly Color BorderSubtle = CardBorder;
        public static readonly Color Success = SuccessGreen;
        public static readonly Color Danger = DangerRed;
        public static readonly Color PrimaryOrangeSurface = new Color(1f, 0.94f, 0.90f, 1f);

        // =========================================================================
        // TYPOGRAPHY SIZES (SCALED FOR 1080x1920 REFERENCE CANVAS)
        // =========================================================================
        public const float ScreenTitleSize = 46f;
        public const float SectionHeadingSize = 36f;
        public const float CardTitleSize = 32f;
        public const float InstructionSize = 28f;
        public const float BodySize = 26f;
        public const float ButtonSize = 28f;
        public const float StatusBadgeSize = 22f;
        public const float SmallMetadataSize = 20f;

        private static TMP_FontAsset s_CachedFont;

        /// <summary>
        /// Retrieves the shared application font asset (LiberationSans SDF with Devanagari &amp; Ol Chiki fallbacks).
        /// </summary>
        public static TMP_FontAsset GetFont()
        {
            if (s_CachedFont == null)
            {
                s_CachedFont = Resources.Load<TMP_FontAsset>("Fonts & Materials/LiberationSans SDF")
                    ?? TMP_Settings.defaultFontAsset;
            }
            return s_CachedFont;
        }

        // =========================================================================
        // COMPONENT STYLING HELPERS
        // =========================================================================

        /// <summary>
        /// Applies standard light card styling to an Image.
        /// </summary>
        public static void ApplyCard(Image image, bool isSecondary = false)
        {
            if (image == null) return;
            image.color = isSecondary ? CardSecondaryBg : CardBackground;
        }

        /// <summary>
        /// Applies AR HUD translucent light card styling to an Image.
        /// </summary>
        public static void ApplyArHudCard(Image image)
        {
            if (image == null) return;
            image.color = ArHudBackground;
        }

        /// <summary>
        /// Formats and colors a primary touch button (Safety Orange CTA).
        /// </summary>
        public static void ApplyPrimaryButton(Button button, Image background, TextMeshProUGUI label, bool isEnabled = true)
        {
            if (button != null) button.interactable = isEnabled;
            if (background != null)
            {
                background.color = isEnabled ? PrimaryOrange : PrimaryDisabled;
            }
            if (label != null)
            {
                var font = GetFont();
                if (font != null) label.font = font;
                label.fontSize = ButtonSize;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.color = isEnabled ? TextLightOnDark : PrimaryDisabledText;
            }
        }

        /// <summary>
        /// Formats and colors a secondary action button (clean neutral light surface with dark text).
        /// </summary>
        public static void ApplySecondaryButton(Button button, Image background, TextMeshProUGUI label, bool isSelected = false)
        {
            if (background != null)
            {
                background.color = isSelected ? CardBorderHighlight : CardSecondaryBg;
            }
            if (label != null)
            {
                var font = GetFont();
                if (font != null) label.font = font;
                label.fontSize = ButtonSize;
                label.fontStyle = isSelected ? FontStyles.Bold : FontStyles.Normal;
                label.alignment = TextAlignmentOptions.Center;
                label.color = isSelected ? PrimaryOrange : TextPrimary;
            }
        }

        /// <summary>
        /// Formats standard dark body or title text with proper wrapping, margin, and font.
        /// </summary>
        public static void ApplyText(TextMeshProUGUI tmp, float fontSize, Color color, FontStyles style = FontStyles.Normal, TextAlignmentOptions align = TextAlignmentOptions.Left)
        {
            if (tmp == null) return;
            var font = GetFont();
            if (font != null) tmp.font = font;
            tmp.fontSize = fontSize;
            tmp.color = color;
            tmp.fontStyle = style;
            tmp.alignment = align;
            tmp.textWrappingMode = TextWrappingModes.Normal;
        }

        public enum BadgeType
        {
            Success,
            Danger,
            Warning,
            Info,
            Neutral
        }

        /// <summary>
        /// Formats a status badge / pill element with semantic colors and dark text.
        /// </summary>
        public static void ApplyBadge(Image background, TextMeshProUGUI label, BadgeType type)
        {
            Color surfaceColor;
            Color textColor;

            switch (type)
            {
                case BadgeType.Success:
                    surfaceColor = SuccessSurface;
                    textColor = SuccessText;
                    break;
                case BadgeType.Danger:
                    surfaceColor = DangerSurface;
                    textColor = DangerText;
                    break;
                case BadgeType.Warning:
                    surfaceColor = WarningSurface;
                    textColor = WarningText;
                    break;
                case BadgeType.Info:
                    surfaceColor = InfoSurface;
                    textColor = InfoText;
                    break;
                default:
                    surfaceColor = CardSecondaryBg;
                    textColor = TextSecondary;
                    break;
            }

            if (background != null) background.color = surfaceColor;
            if (label != null)
            {
                var font = GetFont();
                if (font != null) label.font = font;
                label.fontSize = StatusBadgeSize;
                label.fontStyle = FontStyles.Bold;
                label.alignment = TextAlignmentOptions.Center;
                label.color = textColor;
            }
        }
    }
}
