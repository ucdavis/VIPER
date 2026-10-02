import {
    formatGeneratedAt,
    formatPlainNumber,
    formatReportDate,
    formatReportTotal,
    formatReportValue,
    isNumericFormat,
} from "../report-format"
import type { ReportColumnFormat, ReportColumnMetadata } from "../report-types"

/**
 * These cases mirror ReportValueFormatterTests and ReportExportTextTests on the server, so the
 * screen shows values the same way the CSV and PDF exports do.
 */

function column(format: ReportColumnFormat, decimals: number | null = null, key = "value"): ReportColumnMetadata {
    return { key, label: "Value", format, align: "Right", decimals }
}

describe("formatReportValue()", () => {
    it.each([
        [1234.5, column("Number"), "1,235"],
        [1234.5, column("Number", 2), "1,234.50"],
        [120_000, column("Currency", 2), "$120,000.00"],
        [0.125, column("Percent", 1), "12.5%"],
        [0.5, column("Percent"), "50%"],
        [61, column("Text"), "61"],
    ])("formats the number %s for a %o column as %s", (value, format, expected) => {
        expect(formatReportValue(value, format)).toBe(expected)
    })

    it.each([
        [null, ""],
        [undefined, ""],
        [["Asian", "White"], "Asian, White"],
        [true, "Yes"],
        [false, "No"],
        ["pending", "pending"],
    ])("formats %o as %o", (value, expected) => {
        expect(formatReportValue(value, column("Text"))).toBe(expected)
    })

    it("formats dates only in date columns", () => {
        expect(formatReportValue("2026-07-01", column("Date"))).toBe("07/01/2026")
        expect(formatReportValue("2026-07-01", column("Text"))).toBe("2026-07-01")
    })
})

describe("formatReportDate()", () => {
    it.each([
        ["2026-07-01", "07/01/2026"],
        ["1995-07-01T00:00:00", "07/01/1995"],
        ["not a date", "not a date"],
    ])("formats %s as %s", (value, expected) => {
        expect(formatReportDate(value)).toBe(expected)
    })
})

describe("formatReportTotal()", () => {
    const columns = [column("Currency", 2, "salary"), column("Text", null, "name")]

    it("formats a total like the column it sits under", () => {
        expect(formatReportTotal({ label: "Salaries", columnKey: "salary", value: 430_000 }, columns)).toBe(
            "$430,000.00",
        )
        expect(formatReportTotal({ label: "People", columnKey: "name", value: 5 }, columns)).toBe("5")
    })

    it("formats a total without a column as a plain number", () => {
        expect(formatReportTotal({ label: "Average age", columnKey: null, value: 48.256 }, columns)).toBe("48.26")
        expect(formatReportTotal({ label: "Hidden", columnKey: "ssn", value: 1234 }, columns)).toBe("1,234")
    })

    it("leaves a missing total blank", () => {
        expect(formatReportTotal({ label: "Unknown", columnKey: "salary", value: null }, columns)).toBe("")
    })
})

describe("formatPlainNumber()", () => {
    it("uses separators and up to two decimals, and blanks null", () => {
        expect(formatPlainNumber(1234.5)).toBe("1,234.5")
        expect(formatPlainNumber(null)).toBe("")
    })
})

describe("isNumericFormat()", () => {
    it.each([
        ["Number", true],
        ["Currency", true],
        ["Percent", true],
        ["Text", false],
        ["Date", false],
        ["List", false],
    ] as const)("treats %s as numeric: %s", (format, expected) => {
        expect(isNumericFormat(format)).toBe(expected)
    })
})

describe("formatGeneratedAt()", () => {
    it("matches the export header wording", () => {
        const local = new Date(2026, 8, 30, 14, 5)

        expect(formatGeneratedAt(local.toISOString())).toBe("Generated 09/30/2026 2:05 PM")
    })
})
