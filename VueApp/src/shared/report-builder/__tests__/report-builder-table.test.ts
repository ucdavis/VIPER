import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import ReportBuilderTable from "../ReportBuilderTable.vue"
import type { ReportPagination } from "../use-report-builder"
import { roster } from "./report-builder-fixtures"

const pagination: ReportPagination = { page: 1, rowsPerPage: 25, sortBy: null, descending: false, rowsNumber: 2 }

function mountTable(overrides: Partial<ReportPagination> = {}, paginate = true) {
    return mount(ReportBuilderTable, {
        props: {
            columns: roster.columns,
            rows: [
                { Name: "Ada", Salary: 1234.5 },
                { Name: "Bo", Salary: null },
            ],
            pagination: { ...pagination, ...overrides },
            paginate,
            title: "Faculty roster",
        },
        global: { plugins: [[Quasar, {}]] },
    })
}

function table(wrapper: ReturnType<typeof mountTable>) {
    return wrapper.findComponent({ name: "QTable" })
}

describe("report table", () => {
    it("shows each row's values formatted by column", () => {
        expect.hasAssertions()

        const cells = mountTable()
            .findAll("tbody tr")
            .map((row) => row.findAll("td").map((cell) => cell.text()))

        expect(cells).toStrictEqual([
            ["Ada", "$1,234.50"],
            ["Bo", ""],
        ])
    })

    it("lines numbers up on the right and sorts only sortable columns", () => {
        expect.hasAssertions()

        const columns = table(mountTable()).props("columns") as { align: string; sortable: boolean }[]

        expect(columns.map((column) => [column.align, column.sortable])).toStrictEqual([
            ["left", true],
            ["right", false],
        ])
    })

    it("asks the report for the page and sort the user picks", () => {
        expect.hasAssertions()
        const wrapper = mountTable()

        table(wrapper).vm.$emit("request", {
            pagination: { page: 2, rowsPerPage: 50, sortBy: "Name", descending: true },
        })

        expect(wrapper.emitted("request")).toStrictEqual([
            [{ page: 2, rowsPerPage: 50, sortBy: "Name", descending: true }],
        ])
    })

    it.each([
        { from: "unsorted", sortBy: null, descending: false, next: { sortBy: "Name", descending: false } },
        { from: "ascending", sortBy: "Name", descending: false, next: { sortBy: "Name", descending: true } },
        { from: "descending", sortBy: "Name", descending: true, next: { sortBy: "Name", descending: false } },
    ])("flips the sort when the $from column's heading is clicked", async ({ sortBy, descending, next }) => {
        expect.hasAssertions()
        const wrapper = mountTable({ sortBy, descending })

        await wrapper.get("thead th").trigger("click")

        expect(wrapper.emitted("request")).toStrictEqual([[{ page: 1, rowsPerPage: 25, ...next }]])
    })

    it("follows the report's sort after a request, so the next click flips it", async () => {
        expect.hasAssertions()
        const wrapper = mountTable()
        await wrapper.get("thead th").trigger("click")

        await wrapper.setProps({ pagination: { ...pagination, sortBy: "Name", descending: false } })
        await wrapper.get("thead th").trigger("click")

        expect(wrapper.emitted<[ReportPagination]>("request")?.at(-1)?.[0]).toMatchObject({
            sortBy: "Name",
            descending: true,
        })
    })

    it("shows the page the report is on", async () => {
        expect.hasAssertions()
        const wrapper = mountTable({ rowsNumber: 60 })

        await wrapper.setProps({ pagination: { ...pagination, page: 2, rowsNumber: 60 } })

        expect(wrapper.text()).toContain("26–50 of 60")
    })

    it("fills in what the table leaves out of a request", () => {
        expect.hasAssertions()
        const wrapper = mountTable()

        table(wrapper).vm.$emit("request", { pagination: {} })

        expect(wrapper.emitted("request")).toStrictEqual([
            [{ page: 1, rowsPerPage: 25, sortBy: null, descending: false }],
        ])
    })

    it.each([
        [25, [10, 25, 50, 100, 250, 500]],
        [30, [10, 25, 30, 50, 100, 250, 500]],
        [0, [10, 25, 50, 100, 250, 500]],
    ])("offers the usual page sizes plus the report's own (%i)", (rowsPerPage, expected) => {
        expect.hasAssertions()

        expect(table(mountTable({ rowsPerPage })).props("rowsPerPageOptions")).toStrictEqual(expected)
    })

    it("hides the pager and scrolls instead for an unpaginated report", () => {
        expect.hasAssertions()

        const props = table(mountTable({ rowsPerPage: 0 }, false)).props()

        expect([props.hidePagination, props.virtualScroll]).toStrictEqual([true, true])
    })

    it("names its scrolling region after the report", () => {
        expect.hasAssertions()

        const region = mountTable().find(".q-table__middle.scroll")

        expect([region.attributes("role"), region.attributes("aria-label")]).toStrictEqual(["region", "Faculty roster"])
    })
})
