import { chartTable, pivotTable } from "../report-number-tables"
import { sampleChart, samplePivot } from "./report-fixtures"

describe("pivotTable()", () => {
    it("adds a total to every row and a totals row, as the PDF export does", () => {
        expect.hasAssertions()
        expect(pivotTable(samplePivot)).toStrictEqual({
            headers: ["Category", "APC", "VME", "Total"],
            rows: [{ label: "60+", values: [0, 1, 1] }],
            total: { label: "Total", values: [1, 2, 3] },
        })
    })
})

describe("chartTable()", () => {
    it("lists each chart point as a category and value", () => {
        expect.hasAssertions()
        expect(chartTable(sampleChart)).toStrictEqual({
            headers: ["Category", "Value"],
            rows: [{ label: "Under 30", values: [2] }],
            total: null,
        })
    })
})
