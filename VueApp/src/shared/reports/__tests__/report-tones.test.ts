import { badgeColor, cellBadges, flagText, highlightClass } from "../report-tones"
import type { ReportFlag, ReportRowResult } from "../report-types"

/**
 * Highlights follow the export rules (ReportExportText.CellFill), so a row is emphasized the
 * same way on screen and in Excel and PDF.
 */

const visible = new Set(["name", "age", "salary"])

const rowWarning: ReportFlag = { kind: "Highlight", tone: "Warning", label: "60+", columnKey: null }
const ageNegative: ReportFlag = { kind: "Highlight", tone: "Negative", label: "Retirement age", columnKey: "age" }
const hiddenMuted: ReportFlag = { kind: "Highlight", tone: "Muted", label: "Retired", columnKey: "ssn" }
const neutral: ReportFlag = { kind: "Highlight", tone: "Neutral", label: "Reviewed", columnKey: null }
const emeritusBadge: ReportFlag = { kind: "Badge", tone: "Info", label: "Emeritus", columnKey: null }
const salaryBadge: ReportFlag = { kind: "Badge", tone: "Positive", label: "Raised", columnKey: "salary" }

describe("highlightClass()", () => {
    it("applies a whole-row highlight to every cell, including the row number", () => {
        expect(highlightClass([rowWarning], "name", visible)).toBe("report-tone-warning")
        expect(highlightClass([rowWarning], null, visible)).toBe("report-tone-warning")
    })

    it("applies a column highlight only to its column", () => {
        expect(highlightClass([ageNegative], "age", visible)).toBe("report-tone-negative")
        expect(highlightClass([ageNegative], "name", visible)).toBeNull()
    })

    it("lets the last highlight covering a cell win", () => {
        expect(highlightClass([rowWarning, ageNegative], "age", visible)).toBe("report-tone-negative")
        expect(highlightClass([ageNegative, rowWarning], "age", visible)).toBe("report-tone-warning")
    })

    it("spreads a highlight on a hidden column across the row", () => {
        expect(highlightClass([hiddenMuted], "name", visible)).toBe("report-tone-muted")
    })

    it("adds no class for neutral highlights or badges", () => {
        expect(highlightClass([rowWarning, neutral], "name", visible)).toBe("report-tone-warning")
        expect(highlightClass([neutral, emeritusBadge], "name", visible)).toBeNull()
    })
})

describe("cellBadges()", () => {
    it("puts column badges beside their column and row badges beside the first column", () => {
        const flags = [rowWarning, emeritusBadge, salaryBadge]

        expect(cellBadges(flags, "name", true)).toStrictEqual([emeritusBadge])
        expect(cellBadges(flags, "salary", false)).toStrictEqual([salaryBadge])
        expect(cellBadges(flags, "age", false)).toStrictEqual([])
    })
})

describe("badgeColor()", () => {
    it.each([
        ["Neutral", "grey-7"],
        ["Info", "info"],
        ["Positive", "positive"],
        ["Warning", "warning"],
        ["Negative", "negative"],
        ["Muted", "grey-7"],
    ] as const)("uses %s -> %s", (tone, color) => {
        expect(badgeColor(tone)).toBe(color)
    })
})

describe("flagText()", () => {
    it("lists each label once, in order", () => {
        const row: ReportRowResult = { number: 1, values: {}, flags: [ageNegative, rowWarning, { ...rowWarning }] }

        expect(flagText(row)).toBe("Retirement age; 60+")
    })
})
