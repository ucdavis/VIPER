import type { ReportCellValue, ReportColumnFormat } from "./report-builder-types"

/**
 * Formats values for the screen the way the server formats them for downloads
 * (ReportCellFormatter), so the table and the files agree: US English separators, dates as
 * MM/dd/yyyy, and percent columns holding fractions (0.25 shows as 25%).
 */

const LOCALE = "en-US"
const MAX_DECIMALS = 2
const CURRENCY_DECIMALS = 2
const ISO_DATE_TIME = /^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})(?:T(?<hour>\d{2}):(?<minute>\d{2}))?/u
const NUMERIC_FORMATS: ReadonlySet<ReportColumnFormat> = new Set(["Number", "Currency", "Percent"])
const YES_WORDS = new Set(["y", "yes", "true", "1"])
const NO_WORDS = new Set(["n", "no", "false", "0"])
const NOON = 12

const NUMBER_FORMATS: Record<"Number" | "Currency" | "Percent", Intl.NumberFormat> = {
    Number: new Intl.NumberFormat(LOCALE, { maximumFractionDigits: MAX_DECIMALS }),
    Currency: new Intl.NumberFormat(LOCALE, {
        style: "currency",
        currency: "USD",
        minimumFractionDigits: CURRENCY_DECIMALS,
        maximumFractionDigits: CURRENCY_DECIMALS,
    }),
    Percent: new Intl.NumberFormat(LOCALE, { style: "percent", maximumFractionDigits: MAX_DECIMALS }),
}

/** Whether the format lines its values up on the right. */
function isNumericFormat(format: ReportColumnFormat): boolean {
    return NUMERIC_FORMATS.has(format)
}

function formatNumber(value: number, format: ReportColumnFormat): string {
    const style = format === "Currency" || format === "Percent" ? format : "Number"
    return NUMBER_FORMATS[style].format(value)
}

function formatTime(hour: number, minute: string): string {
    const twelveHour = hour % NOON === 0 ? NOON : hour % NOON
    return `${twelveHour}:${minute} ${hour < NOON ? "AM" : "PM"}`
}

/**
 * An ISO date or date-time as MM/dd/yyyy, with the time when the column shows times or, for a
 * text column, when there is one. Null when the text isn't an ISO date the column should reformat:
 * a text column only reformats full date-times, which only real date values arrive as.
 */
function formatIsoDate(value: string, format: ReportColumnFormat): string | null {
    const parts = ISO_DATE_TIME.exec(value)?.groups
    const isDateFormat = format === "Date" || format === "DateTime"
    if (!parts || (!isDateFormat && parts.hour === undefined)) {
        return null
    }
    const date = `${parts.month}/${parts.day}/${parts.year}`
    const hour = Number(parts.hour ?? "0")
    const minute = parts.minute ?? "00"
    const hasTime = hour !== 0 || minute !== "00"
    const showTime = format === "DateTime" || (format === "Text" && hasTime)
    return showTime ? `${date} ${formatTime(hour, minute)}` : date
}

function formatYesNo(value: string | number | boolean): string {
    if (typeof value === "boolean") {
        return value ? "Yes" : "No"
    }
    if (typeof value === "number") {
        return value === 0 ? "No" : "Yes"
    }
    const word = value.trim().toLowerCase()
    if (YES_WORDS.has(word)) {
        return "Yes"
    }
    return NO_WORDS.has(word) ? "No" : value
}

function formatCell(value: ReportCellValue | undefined, format: ReportColumnFormat): string {
    if (value === null || value === undefined) {
        return ""
    }
    if (format === "YesNo") {
        return formatYesNo(value)
    }
    if (typeof value === "boolean") {
        return value ? "Yes" : "No"
    }
    if (typeof value === "number") {
        return formatNumber(value, format)
    }
    return formatIsoDate(value, format) ?? value
}

export { formatCell, isNumericFormat }
