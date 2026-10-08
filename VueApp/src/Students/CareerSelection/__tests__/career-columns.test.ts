import {
    OVERVIEW_COLUMNS,
    REPORT_COLUMNS,
    previewStatement,
    searchRows,
    STATEMENT_PREVIEW_LENGTH,
} from "../utils/career-columns"

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
        // The app's shared date format: the date part, in the browser's locale.
        expect(lastUpdated?.format?.("2026-04-17T10:00:00", {})).toBe(
            new Date("2026-04-17T00:00:00").toLocaleDateString(),
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

describe("grid search", () => {
    // Report-shaped rows: every career field holds the text the cell shows.
    const reportRows = [
        {
            rowKey: "1",
            fullName: "Beta, Ann",
            classLevel: "V2",
            email: "abeta@ucdavis.edu",
            direction: "Private Practice",
            mentorName: "Vet, Ann",
            shortTermPlans: `${"x".repeat(STATEMENT_PREVIEW_LENGTH)} then a rural internship`,
            lastUpdated: "2026-04-17T10:00:00",
        },
        {
            rowKey: "2",
            fullName: "Alpha, Bo",
            classLevel: "V3",
            email: "balpha@ucdavis.edu",
            direction: "Academia",
            mentorName: "Doc, Cy",
            shortTermPlans: "Residency",
            lastUpdated: null,
        },
    ]

    function keys(rows: { rowKey: string }[]): string[] {
        return rows.map((r) => r.rowKey)
    }

    it("returns every row for a blank search", () => {
        expect.hasAssertions()
        expect(keys(searchRows(reportRows, "", REPORT_COLUMNS))).toStrictEqual(["1", "2"])
        expect(keys(searchRows(reportRows, "   ", REPORT_COLUMNS))).toStrictEqual(["1", "2"])
    })

    it("matches any column, ignoring case", () => {
        expect.hasAssertions()
        expect(keys(searchRows(reportRows, "ACADEMIA", REPORT_COLUMNS))).toStrictEqual(["2"])
        expect(keys(searchRows(reportRows, "doc, cy", REPORT_COLUMNS))).toStrictEqual(["2"])
    })

    it("ignores spaces around the search text", () => {
        expect.hasAssertions()
        // A stray space typed or pasted around a word would otherwise hide every row.
        expect(keys(searchRows(reportRows, "  academia ", REPORT_COLUMNS))).toStrictEqual(["2"])
    })

    it("matches the text as one phrase within a cell", () => {
        expect.hasAssertions()
        // Inner spaces are kept: the words must appear together, not anywhere in the row.
        expect(keys(searchRows(reportRows, "private practice", REPORT_COLUMNS))).toStrictEqual(["1"])
        expect(keys(searchRows(reportRows, "beta academia", REPORT_COLUMNS))).toStrictEqual([])
    })

    it("searches a statement's whole text, past the excerpt the report shows", () => {
        expect.hasAssertions()
        expect(keys(searchRows(reportRows, "rural internship", REPORT_COLUMNS))).toStrictEqual(["1"])
    })

    it("searches every column it is given, whether or not the grid shows it", () => {
        expect.hasAssertions()
        // The search takes its columns from the page, not from what is visible, so hiding the
        // Mentor column does not stop a row matching on it.
        expect(keys(searchRows(reportRows, "vet, ann", REPORT_COLUMNS))).toStrictEqual(["1"])
    })

    it("matches the last-updated date as the grid shows it", () => {
        expect.hasAssertions()
        const shown = new Date("2026-04-17T00:00:00").toLocaleDateString()

        expect(keys(searchRows(reportRows, shown, REPORT_COLUMNS))).toStrictEqual(["1"])
    })

    it("does not search the overview's completeness columns", () => {
        expect.hasAssertions()
        // They hold true/false behind an icon, which the legacy app never searched.
        const overviewRows = [{ rowKey: "1", fullName: "Beta, Ann", directionCompleted: true, mentorName: "" }]

        expect(keys(searchRows(overviewRows, "true", OVERVIEW_COLUMNS))).toStrictEqual([])
    })

    it("still searches the overview's mentor, which has no completeness flag", () => {
        expect.hasAssertions()
        const overviewRows = [{ rowKey: "1", fullName: "Beta, Ann", directionCompleted: true, mentorName: "Vet, Ann" }]

        expect(keys(searchRows(overviewRows, "vet", OVERVIEW_COLUMNS))).toStrictEqual(["1"])
    })

    it("marks exactly the overview's flagged fields as unsearchable", () => {
        expect.hasAssertions()
        const unsearchable = OVERVIEW_COLUMNS.filter((c) => c.searchable === false).map((c) => c.name)

        expect(unsearchable).toStrictEqual([
            "direction",
            "primarySpecies",
            "secondarySpecies",
            "postGrad",
            "shortTerm",
            "longTerm",
        ])
        expect(REPORT_COLUMNS.filter((c) => c.searchable === false)).toStrictEqual([])
    })
})
