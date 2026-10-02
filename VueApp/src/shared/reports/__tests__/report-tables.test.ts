import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import ReportGroupTable from "../ReportGroupTable.vue"
import ReportNumberTable from "../ReportNumberTable.vue"
import ReportTotals from "../ReportTotals.vue"
import { ada, bo, sampleResult } from "./report-fixtures"

/**
 * The building blocks of a report on screen. Highlights must carry their label as text (the
 * Notes column or a badge), never color alone.
 */

const global = { plugins: [[Quasar, {}]] as [typeof Quasar, object][] }

// Sortable headers carry Quasar's sort icon, whose ligature name shows up in the text.
function headerText(wrapper: ReturnType<typeof mount>): string[] {
    return wrapper.findAll("th").map((th) => th.text().replace("arrow_upward", ""))
}

function rowCells(wrapper: ReturnType<typeof mount>, index: number) {
    return wrapper.findAll("tbody tr")[index]?.findAll("td") ?? []
}

describe("ReportGroupTable", () => {
    it("shows row numbers, formatted values and a Notes column", () => {
        expect.hasAssertions()
        const wrapper = mount(ReportGroupTable, { props: { result: sampleResult(), rows: [ada, bo] }, global })

        expect(headerText(wrapper)).toStrictEqual(["#", "Name", "Salary", "Hired", "Notes"])
        expect(rowCells(wrapper, 0).map((td) => td.text())).toStrictEqual([
            "1",
            "Ada Emeritus",
            "$120,000.00",
            "07/01/1995",
            "60+; Emeritus",
        ])
    })

    it("tints highlighted cells by tone", () => {
        expect.hasAssertions()
        const wrapper = mount(ReportGroupTable, { props: { result: sampleResult(), rows: [ada, bo] }, global })
        const [, name] = rowCells(wrapper, 0)
        const [, boName, boSalary] = rowCells(wrapper, 1)

        expect(name?.classes()).toContain("report-tone-warning")
        expect(boSalary?.classes()).toContain("report-tone-negative")
        expect(boName?.classes()).not.toContain("report-tone-negative")
    })

    it("puts badges beside the first column only", () => {
        expect.hasAssertions()
        const wrapper = mount(ReportGroupTable, { props: { result: sampleResult(), rows: [ada] }, global })

        expect(wrapper.findAll(".q-badge").map((badge) => badge.text())).toStrictEqual(["Emeritus"])
    })
})

describe("ReportTotals", () => {
    it("lists each total with its column's format", () => {
        expect.hasAssertions()
        const result = sampleResult()
        const wrapper = mount(ReportTotals, { props: { totals: result.totals, columns: result.columns } })

        expect(wrapper.get("dt").text()).toBe("All salaries")
        expect(wrapper.get("dd").text()).toBe("$360,000.00")
    })

    it("renders nothing when there are no totals", () => {
        expect.hasAssertions()
        const wrapper = mount(ReportTotals, { props: { totals: [], columns: [] } })

        expect(wrapper.find("dl").exists()).toBeFalsy()
    })
})

describe("ReportNumberTable", () => {
    it("shows each row and a bold totals row", () => {
        expect.hasAssertions()
        const table = {
            headers: ["Category", "APC", "Total"],
            rows: [{ label: "60+", values: [1234.5, null] }],
            total: { label: "Total", values: [1234.5, 7] },
        }
        const wrapper = mount(ReportNumberTable, { props: { table }, global })

        expect(headerText(wrapper)).toStrictEqual(["Category", "APC", "Total"])
        expect(rowCells(wrapper, 0).map((td) => td.text())).toStrictEqual(["60+", "1,234.5", ""])
        expect(wrapper.get("tr.text-weight-bold").text()).toBe("Total1,234.57")
    })

    it("leaves out the totals row when there is none", () => {
        expect.hasAssertions()
        const table = { headers: ["Category", "Value"], rows: [], total: null }
        const wrapper = mount(ReportNumberTable, { props: { table }, global })

        expect(wrapper.find("tr.text-weight-bold").exists()).toBeFalsy()
    })
})
