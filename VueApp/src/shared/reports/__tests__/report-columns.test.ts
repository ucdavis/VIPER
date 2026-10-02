import { NOTES_COLUMN, ROW_NUMBER_COLUMN, hasNotes, toTableColumns } from "../report-columns"
import type { ReportResult, ReportRowResult } from "../report-types"

const ada: ReportRowResult = {
    number: 1,
    values: { name: "Ada", salary: 120_000 },
    flags: [{ kind: "Highlight", tone: "Warning", label: "60+", columnKey: null }],
}
const bo: ReportRowResult = { number: 2, values: { name: "Bo" }, flags: [] }

function resultWith(rows: ReportRowResult[], rowNumbers = false): ReportResult {
    return {
        key: "test.sample",
        title: "Sample",
        generatedAt: "2026-09-30T14:00:00-07:00",
        rowCount: rows.length,
        rowNumbers,
        columns: [
            { key: "name", label: "Name", format: "Text", align: "Left", decimals: null },
            { key: "salary", label: "Salary", format: "Currency", align: "Right", decimals: 2 },
        ],
        summary: [],
        groups: [{ label: null, rows, subtotals: [], charts: [] }],
        totals: [],
        pivots: [],
        charts: [],
    }
}

describe("toTableColumns()", () => {
    it("maps each report column to a sortable table column", () => {
        const [name, salary] = toTableColumns(resultWith([bo]))

        expect(name).toMatchObject({ name: "name", label: "Name", align: "left", sortable: true })
        expect(salary).toMatchObject({ name: "salary", label: "Salary", align: "right" })
    })

    it("reads values by column key, treating missing values as empty", () => {
        const [name, salary] = toTableColumns(resultWith([bo]))
        const readName = name?.field as (row: ReportRowResult) => unknown
        const readSalary = salary?.field as (row: ReportRowResult) => unknown

        expect(readName(ada)).toBe("Ada")
        expect(readSalary(bo)).toBeNull()
    })

    it("formats values like the exports", () => {
        const [, salary] = toTableColumns(resultWith([bo]))

        expect(salary?.format?.(120_000, ada)).toBe("$120,000.00")
    })

    it("adds a row number column first when the report numbers its rows", () => {
        const columns = toTableColumns(resultWith([bo], true))
        const [numberColumn] = columns
        const readNumber = numberColumn?.field as (row: ReportRowResult) => unknown

        expect(numberColumn).toMatchObject({ name: ROW_NUMBER_COLUMN, label: "#", align: "right" })
        expect(readNumber(bo)).toBe(2)
    })

    it("adds a Notes column last when any row has a flag", () => {
        const columns = toTableColumns(resultWith([ada, bo]))
        const notes = columns.at(-1)
        const readNotes = notes?.field as (row: ReportRowResult) => unknown

        expect(notes).toMatchObject({ name: NOTES_COLUMN, label: "Notes" })
        expect(readNotes(ada)).toBe("60+")
    })

    it("leaves out the extra columns when they aren't needed", () => {
        expect(toTableColumns(resultWith([bo])).map((column) => column.name)).toStrictEqual(["name", "salary"])
    })
})

describe("hasNotes()", () => {
    it("is true only when a row has a flag", () => {
        expect(hasNotes(resultWith([bo, ada]))).toBeTruthy()
        expect(hasNotes(resultWith([bo]))).toBeFalsy()
    })
})
