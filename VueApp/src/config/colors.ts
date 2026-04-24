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

export { semanticColors }
