/**
 * UC Davis brand colors — semantic palette for Quasar configuration.
 * Source: https://communicationsguide.ucdavis.edu/brand-guide/colors
 * CSS custom properties live in src/styles/colors.css.
 */

const BRAND_COLORS = {
    "ucdavis-blue-100": "#022851",
    "ucdavis-blue-70": "#4b6983",
    "ucdavis-gold-90": "#FFC519",
    positive: "#266041",
    negative: "#79242F",
    info: "#00B2E3",
    dark: "#1d1d1d",
    "dark-page": "#121212",
} as const

const semanticColors = {
    primary: BRAND_COLORS["ucdavis-blue-100"],
    secondary: BRAND_COLORS["ucdavis-blue-70"],
    accent: BRAND_COLORS["ucdavis-gold-90"],
    positive: BRAND_COLORS.positive,
    negative: BRAND_COLORS.negative,
    info: BRAND_COLORS.info,
    warning: BRAND_COLORS["ucdavis-gold-90"],
    dark: BRAND_COLORS.dark,
    "dark-page": BRAND_COLORS["dark-page"],
} as const

// Quasar/brand colors whose backgrounds are light enough that white foreground
// text fails WCAG contrast. Pair these with text-color="dark".
const LIGHT_BACKGROUND_COLORS = new Set(["warning", "info", "accent"])

function getAccessibleTextColor(color: string | null | undefined): "dark" | "white" {
    return color && LIGHT_BACKGROUND_COLORS.has(color) ? "dark" : "white"
}

// Quasar's grey (alias for grey-5, #9e9e9e) and grey-6 (#757575) sit in the
// contrast dead zone where neither white nor dark foreground reaches 4.5:1.
// Remap to the nearest shade that clears AA with white text.
const CONTRAST_SAFE_SWAPS = new Map<string, string>([
    ["grey", "grey-7"],
    ["grey-5", "grey-7"],
    ["grey-6", "grey-7"],
])

function toContrastSafeColor(color: string | null | undefined): string {
    if (!color) {
        return ""
    }
    return CONTRAST_SAFE_SWAPS.get(color) ?? color
}

export { semanticColors, getAccessibleTextColor, toContrastSafeColor }
