/*
 * UC Davis Brand Colors - Single Source of Truth
 *
 * This file provides UC Davis brand colors for both Vue SPAs and server-rendered pages.
 * Colors are based on the UC Davis Communications Guide.
 * Source: https://communicationsguide.ucdavis.edu/brand-guide/colors
 *
 * Usage:
 * - Server-rendered pages: Access via window.UCDavisBrandColors
 * - Vue SPAs: Import from colors.ts which references these same values
 */

globalThis.UCDavisBrandColors = {
    // Primary brand colors
    primary: "#022851", // UC Davis Blue 100 (Primary Aggie Blue)
    secondary: "#4b6983", // UC Davis Blue 70
    accent: "#ffc519", // UC Davis Gold 90

    // Status colors (UC Davis secondary palette)
    positive: "#266041", // Redwood - green for success states
    negative: "#79242F", // Merlot - red for error states
    info: "#00B2E3", // Tahoe - blue for informational states
    warning: "#ffc519", // Gold for warning states (same as accent)

    // Dark theme colors
    dark: "#1d1d1d", // Dark background
    "dark-page": "#121212", // Darker page background
}
