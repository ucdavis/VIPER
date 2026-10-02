import type { ReportCellValue, ReportColumnFormat, ReportColumnMetadata, ReportTotal } from "./report-types"

/**
 * Formats report values for display the way the server formats them for CSV and PDF exports
 * (ReportValueFormatter and ReportExportText), so the screen and the files agree.
 */

const LOCALE = "en-US"
const ISO_DATE = /^(?<year>\d{4})-(?<month>\d{2})-(?<day>\d{2})/u
const TOTAL_MAX_DECIMALS = 2
const NUMERIC_FORMATS: ReadonlySet<ReportColumnFormat> = new Set(["Number", "Currency", "Percent"])

const STYLE_OPTIONS: Partial<Record<ReportColumnFormat, Intl.NumberFormatOptions>> = {
    Currency: { style: "currency", currency: "USD" },
    Percent: { style: "percent" },
}

type ColumnFormat = Pick<ReportColumnMetadata, "format" | "decimals">

function isNumericFormat(format: ReportColumnFormat): boolean {
    return NUMERIC_FORMATS.has(format)
}

/** "2026-07-01" or "2026-07-01T00:00:00" becomes "07/01/2026"; anything else is left alone. */
function formatReportDate(value: string): string {
    const parts = ISO_DATE.exec(value)?.groups
    return parts ? `${parts.month}/${parts.day}/${parts.year}` : value
}

function formatColumnNumber(value: number, column: ColumnFormat): string {
    const digits = column.decimals ?? 0
    return new Intl.NumberFormat(LOCALE, {
        ...STYLE_OPTIONS[column.format],
        minimumFractionDigits: digits,
        maximumFractionDigits: digits,
    }).format(value)
}

/** A number with no column format: thousands separators and up to two decimals. */
function formatPlainNumber(value: number | null): string {
    return value === null
        ? ""
        : new Intl.NumberFormat(LOCALE, { maximumFractionDigits: TOTAL_MAX_DECIMALS }).format(value)
}

function formatReportValue(value: ReportCellValue | undefined, column: ColumnFormat): string {
    if (value === null || value === undefined) {
        return ""
    }
    if (Array.isArray(value)) {
        return value.join(", ")
    }
    if (typeof value === "number") {
        return isNumericFormat(column.format) ? formatColumnNumber(value, column) : String(value)
    }
    if (typeof value === "boolean") {
        return value ? "Yes" : "No"
    }
    return column.format === "Date" ? formatReportDate(value) : value
}

/** A total formatted like the column it sits under, or as a plain number when it has none. */
function formatReportTotal(total: ReportTotal, columns: ReportColumnMetadata[]): string {
    const column = columns.find((candidate) => candidate.key === total.columnKey)
    if (total.value === null) {
        return ""
    }
    return column ? formatReportValue(total.value, column) : formatPlainNumber(total.value)
}

/** "Generated 09/30/2026 2:15 PM", matching the export headers. */
function formatGeneratedAt(generatedAt: string): string {
    const date = new Date(generatedAt)
    const day = date.toLocaleDateString(LOCALE, { month: "2-digit", day: "2-digit", year: "numeric" })
    const time = date.toLocaleTimeString(LOCALE, { hour: "numeric", minute: "2-digit" })
    return `Generated ${day} ${time}`
}

export { formatGeneratedAt, formatPlainNumber, formatReportDate, formatReportTotal, formatReportValue, isNumericFormat }
