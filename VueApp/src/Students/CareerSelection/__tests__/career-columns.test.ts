import { OVERVIEW_COLUMNS, REPORT_COLUMNS, previewStatement, STATEMENT_PREVIEW_LENGTH } from "../utils/career-columns"

/**
 * Tests for the grid columns the roster and report are built from, and the statement excerpt the
 * report shows in place of a 5000-character plan.
 */

function column(columns: typeof OVERVIEW_COLUMNS, name: string) {
    return columns.find((c) => c.name === name)
}

describe("statement excerpt", () => {
    it("shows a short statement in full", () => {
        expect.hasAssertions()
        expect(previewStatement("Internship")).toBe("Internship")
    })

    it("trims the surrounding whitespace", () => {
        expect.hasAssertions()
        expect(previewStatement("  Internship \n")).toBe("Internship")
    })

    it("reads an absent statement as empty", () => {
        expect.hasAssertions()
        expect(previewStatement(null)).toBe("")
    })

    it("cuts a long statement to the preview length, ellipsis included", () => {
        expect.hasAssertions()
        const excerpt = previewStatement("x".repeat(500))

        expect(excerpt).toHaveLength(STATEMENT_PREVIEW_LENGTH)
        expect(excerpt.endsWith("…")).toBeTruthy()
    })

    it("does not cut a statement that just fits", () => {
        expect.hasAssertions()
        const exact = "x".repeat(STATEMENT_PREVIEW_LENGTH)

        expect(previewStatement(exact)).toBe(exact)
    })

    it("does not leave a dangling space before the ellipsis", () => {
        expect.hasAssertions()
        const statement = `${"x".repeat(STATEMENT_PREVIEW_LENGTH - 2)} more words`

        expect(previewStatement(statement)).toBe(`${"x".repeat(STATEMENT_PREVIEW_LENGTH - 2)}…`)
    })
})

describe("overview columns", () => {
    it("opens with the student's identity", () => {
        expect.hasAssertions()
        expect(OVERVIEW_COLUMNS.slice(0, 3).map((c) => c.name)).toStrictEqual(["classLevel", "fullName", "email"])
    })

    it("sorts a flagged field on its completeness, not its value", () => {
        expect.hasAssertions()
        expect(column(OVERVIEW_COLUMNS, "direction")?.field).toBe("directionCompleted")
    })

    it("sorts the mentor on its value, having no completeness flag", () => {
        expect.hasAssertions()
        expect(column(OVERVIEW_COLUMNS, "mentor")?.field).toBe("mentorName")
    })

    it("centres the completeness icons and leaves the mentor ranged left", () => {
        expect.hasAssertions()
        expect(column(OVERVIEW_COLUMNS, "direction")?.align).toBe("center")
        expect(column(OVERVIEW_COLUMNS, "mentor")?.align).toBe("left")
    })

    it("ends with a formatted last-updated date", () => {
        expect.hasAssertions()
        const lastUpdated = OVERVIEW_COLUMNS.at(-1)

        expect(lastUpdated?.name).toBe("lastUpdated")
        expect(lastUpdated?.format?.("2026-04-17T10:00:00", {})).toBe(
            new Date("2026-04-17T10:00:00").toLocaleDateString(),
        )
        expect(lastUpdated?.format?.(null, {})).toBe("")
    })
})

describe("report columns", () => {
    it("reads each field's selected value", () => {
        expect.hasAssertions()
        expect(column(REPORT_COLUMNS, "direction")?.field).toBe("direction")
        expect(column(REPORT_COLUMNS, "postGrad")?.field).toBe("postGrad")
    })

    it("leaves statements unformatted, so search reaches past the excerpt", () => {
        expect.hasAssertions()
        // QTable's search matches the formatted value; the excerpt is the report's cell slot.
        expect(column(REPORT_COLUMNS, "shortTerm")?.format).toBeUndefined()
        expect(column(REPORT_COLUMNS, "longTerm")?.format).toBeUndefined()
    })

    it("does not offer to sort on a statement", () => {
        expect.hasAssertions()
        expect(column(REPORT_COLUMNS, "shortTerm")?.sortable).toBeFalsy()
        expect(column(REPORT_COLUMNS, "direction")?.sortable).toBeTruthy()
    })

    it("carries the same fields in the same order as the roster", () => {
        expect.hasAssertions()
        expect(REPORT_COLUMNS.map((c) => c.name)).toStrictEqual(OVERVIEW_COLUMNS.map((c) => c.name))
    })
})
