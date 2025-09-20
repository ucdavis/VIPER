/**
 * UC Davis Brand Colors - TypeScript Interface
 * Colors are defined in src/styles/colors.css (single source of truth)
 * This file provides TypeScript access to CSS custom properties
 * Source: https://communicationsguide.ucdavis.edu/brand-guide/colors
 */

// Cache for computed style values to avoid repeated DOM access
const cssVariableCache = new Map<string, string>()

// Fallback colors for when CSS variables aren't loaded yet
const fallbackColors = new Map<string, string>([
    ["ucdavis-blue-100", "#022851"],
    ["ucdavis-blue-90", "#023868"],
    ["ucdavis-blue-80", "#335379"],
    ["ucdavis-blue-70", "#4b6983"],
    ["ucdavis-blue-60", "#73849a"],
    ["ucdavis-blue-50", "#9aa5b5"],
    ["ucdavis-blue-40", "#bcc3ce"],
    ["ucdavis-blue-30", "#d4d9e0"],
    ["ucdavis-blue-20", "#e6e9ed"],
    ["ucdavis-blue-10", "#f2f4f6"],
])

// Helper function to get CSS custom property value with caching
function getCSSVariable(name: string): string {
    // Check if we're in a browser environment
    if (typeof document === "undefined") {
        // Return fallback for server-side rendering or build time
        return fallbackColors.get(name) || `var(--${name})`
    }

    // Return cached value if it exists and is not empty
    const cached = cssVariableCache.get(name)
    if (cached) {
        return cached
    }

    // Get value from CSS
    const value = getComputedStyle(document.documentElement).getPropertyValue(`--${name}`).trim()

    // If value is empty, use fallback and don't cache
    if (!value) {
        return fallbackColors.get(name) || `var(--${name})`
    }

    // Cache non-empty value and return
    cssVariableCache.set(name, value)
    return value
}

// UC Davis Blue Shades (reads from CSS variables)
const ucdavisBlue = {
    get 100() {
        return getCSSVariable("ucdavis-blue-100")
    }, // Primary Aggie Blue
    get 90() {
        return getCSSVariable("ucdavis-blue-90")
    },
    get 80() {
        return getCSSVariable("ucdavis-blue-80")
    },
    get 70() {
        return getCSSVariable("ucdavis-blue-70")
    },
    get 60() {
        return getCSSVariable("ucdavis-blue-60")
    },
    get 50() {
        return getCSSVariable("ucdavis-blue-50")
    },
    get 40() {
        return getCSSVariable("ucdavis-blue-40")
    },
    get 30() {
        return getCSSVariable("ucdavis-blue-30")
    },
    get 20() {
        return getCSSVariable("ucdavis-blue-20")
    },
    get 10() {
        return getCSSVariable("ucdavis-blue-10")
    },
} as const

// UC Davis Gold Shades (reads from CSS variables)
const ucdavisGold = {
    get 100() {
        return getCSSVariable("ucdavis-gold-100")
    }, // Primary Aggie Gold
    get 90() {
        return getCSSVariable("ucdavis-gold-90")
    },
    get 80() {
        return getCSSVariable("ucdavis-gold-80")
    },
    get 70() {
        return getCSSVariable("ucdavis-gold-70")
    },
    get 60() {
        return getCSSVariable("ucdavis-gold-60")
    },
    get 50() {
        return getCSSVariable("ucdavis-gold-50")
    },
    get 40() {
        return getCSSVariable("ucdavis-gold-40")
    },
    get 30() {
        return getCSSVariable("ucdavis-gold-30")
    },
    get 20() {
        return getCSSVariable("ucdavis-gold-20")
    },
    get 10() {
        return getCSSVariable("ucdavis-gold-10")
    },
} as const

// UC Davis Black/Grey Shades (reads from CSS variables)
const ucdavisBlack = {
    get 100() {
        return getCSSVariable("ucdavis-black-100")
    },
    get 90() {
        return getCSSVariable("ucdavis-black-90")
    },
    get 80() {
        return getCSSVariable("ucdavis-black-80")
    },
    get 70() {
        return getCSSVariable("ucdavis-black-70")
    },
    get 60() {
        return getCSSVariable("ucdavis-black-60")
    },
    get 50() {
        return getCSSVariable("ucdavis-black-50")
    },
    get 40() {
        return getCSSVariable("ucdavis-black-40")
    },
    get 30() {
        return getCSSVariable("ucdavis-black-30")
    },
    get 20() {
        return getCSSVariable("ucdavis-black-20")
    },
    get 10() {
        return getCSSVariable("ucdavis-black-10")
    },
} as const

// Define colors as constants (single source of truth)
const BRAND_COLORS = {
    // UC Davis Blue shades
    "ucdavis-blue-100": "#022851", // Primary Aggie Blue
    "ucdavis-blue-90": "#023868",
    "ucdavis-blue-80": "#335379",
    "ucdavis-blue-70": "#4b6983",
    "ucdavis-blue-60": "#73849a",
    "ucdavis-blue-50": "#9aa5b5",
    "ucdavis-blue-40": "#bcc3ce",
    "ucdavis-blue-30": "#d4d9e0",
    "ucdavis-blue-20": "#e6e9ed",
    "ucdavis-blue-10": "#f2f4f6",

    // UC Davis Gold shades
    "ucdavis-gold-100": "#FFBF00", // Primary Aggie Gold
    "ucdavis-gold-90": "#FFC519",

    // Status colors
    positive: "#226e34",
    negative: "#6e2222",
    info: "#289094",

    // Dark theme
    dark: "#1d1d1d",
    "dark-page": "#121212",
} as const

// Semantic color mappings for application use (single source of truth)
const semanticColors = {
    // Primary brand colors
    primary: BRAND_COLORS["ucdavis-blue-100"],
    secondary: BRAND_COLORS["ucdavis-blue-70"],
    accent: BRAND_COLORS["ucdavis-gold-90"],

    // Status colors
    positive: BRAND_COLORS.positive,
    negative: BRAND_COLORS.negative,
    info: BRAND_COLORS.info,
    warning: BRAND_COLORS["ucdavis-gold-90"],

    // Dark theme colors
    dark: BRAND_COLORS.dark,
    "dark-page": BRAND_COLORS["dark-page"],
} as const

// Complete color palette for easy access
const colors = {
    blue: ucdavisBlue,
    gold: ucdavisGold,
    black: ucdavisBlack,
    ...semanticColors,
} as const

// CSS variable names for direct usage (no values since they're in CSS)
const cssVariableNames = [
    // Blue shades
    "ucdavis-blue-100",
    "ucdavis-blue-90",
    "ucdavis-blue-80",
    "ucdavis-blue-70",
    "ucdavis-blue-60",
    "ucdavis-blue-50",
    "ucdavis-blue-40",
    "ucdavis-blue-30",
    "ucdavis-blue-20",
    "ucdavis-blue-10",

    // Gold shades
    "ucdavis-gold-100",
    "ucdavis-gold-90",
    "ucdavis-gold-80",
    "ucdavis-gold-70",
    "ucdavis-gold-60",
    "ucdavis-gold-50",
    "ucdavis-gold-40",
    "ucdavis-gold-30",
    "ucdavis-gold-20",
    "ucdavis-gold-10",

    // Black/Grey shades
    "ucdavis-black-100",
    "ucdavis-black-90",
    "ucdavis-black-80",
    "ucdavis-black-70",
    "ucdavis-black-60",
    "ucdavis-black-50",
    "ucdavis-black-40",
    "ucdavis-black-30",
    "ucdavis-black-20",
    "ucdavis-black-10",
] as const

// Type definitions for better TypeScript support
type UcdavisBlueShade = keyof typeof ucdavisBlue
type UcdavisGoldShade = keyof typeof ucdavisGold
type UcdavisBlackShade = keyof typeof ucdavisBlack
type SemanticColorName = keyof typeof semanticColors
type CssVariableName = (typeof cssVariableNames)[number]

export { ucdavisBlue, ucdavisGold, ucdavisBlack, BRAND_COLORS, semanticColors, colors, cssVariableNames }
export type { UcdavisBlueShade, UcdavisGoldShade, UcdavisBlackShade, SemanticColorName, CssVariableName }
