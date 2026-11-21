import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import ReportBuilderResults from "../ReportBuilderResults.vue"
import ReportBuilderTable from "../ReportBuilderTable.vue"
import ExportToolbar from "@/components/ExportToolbar.vue"
import type { ReportExportFormat, ReportRunResult } from "../report-builder-types"
import { resultOf } from "./report-builder-fixtures"

const exportAs = vi.fn<(format: ReportExportFormat) => Promise<void>>()

function mountResults(result: ReportRunResult = resultOf([{ Name: "Ada" }]), overrides: Record<string, unknown> = {}) {
    return mount(ReportBuilderResults, {
        props: {
            result,
            pagination: { page: 1, rowsPerPage: 25, sortBy: null, descending: false, rowsNumber: 1 },
            paginate: true,
            title: "Faculty roster",
            search: "",
            allowExcel: true,
            allowPdf: false,
            canExport: true,
            running: false,
            exporting: null,
            nothingToExport: false,
            exportAs,
            ...overrides,
        },
        global: { plugins: [[Quasar, {}]] },
    })
}

function toolbar(wrapper: ReturnType<typeof mountResults>) {
    return wrapper.findComponent(ExportToolbar)
}

describe("report results", () => {
    it("offers only the downloads the report allows", async () => {
        expect.hasAssertions()
        const bar = toolbar(mountResults())

        await bar.props("excelExport")?.()

        expect(bar.props("pdfExport")).toBeUndefined()
        expect(exportAs).toHaveBeenCalledWith("xlsx")
    })

    it("offers a PDF when the report allows it", async () => {
        expect.hasAssertions()
        const bar = toolbar(mountResults(undefined, { allowExcel: false, allowPdf: true }))

        await bar.props("pdfExport")?.()

        expect([bar.props("excelExport"), exportAs.mock.lastCall]).toStrictEqual([undefined, ["pdf"]])
    })

    it("offers no downloads when there's nothing to download", () => {
        expect.hasAssertions()

        const bar = toolbar(mountResults(undefined, { canExport: false, allowPdf: true }))

        expect([bar.props("excelExport"), bar.props("pdfExport")]).toStrictEqual([undefined, undefined])
    })

    it("passes the search on", () => {
        expect.hasAssertions()
        const wrapper = mountResults()

        toolbar(wrapper).vm.$emit("update:filter", "ada")

        expect(wrapper.emitted("search")).toStrictEqual([["ada"]])
    })

    it("passes the table's page requests on", () => {
        expect.hasAssertions()
        const wrapper = mountResults()
        const request = { page: 2, rowsPerPage: 25, sortBy: null, descending: false }

        wrapper.findComponent(ReportBuilderTable).vm.$emit("request", request)

        expect(wrapper.emitted("request")).toStrictEqual([[request]])
    })

    it("says when the row cap cut the result short", () => {
        expect.hasAssertions()

        expect(mountResults(resultOf([{ Name: "Ada" }], { truncated: true })).text()).toContain(
            "Only the first 50,000 rows are shown.",
        )
    })

    it("says when there are no rows instead of showing an empty table", () => {
        expect.hasAssertions()
        const wrapper = mountResults(resultOf([]))

        expect(wrapper.text()).toContain("No rows match these filters.")
        expect(wrapper.findComponent(ReportBuilderTable).exists()).toBeFalsy()
    })

    it("keeps the table while a new page loads", () => {
        expect.hasAssertions()

        expect(mountResults(resultOf([]), { running: true }).findComponent(ReportBuilderTable).exists()).toBeTruthy()
    })

    it("says when a download had nothing in it", () => {
        expect.hasAssertions()

        expect(mountResults(undefined, { nothingToExport: true }).text()).toContain("There's nothing to download")
    })
})
