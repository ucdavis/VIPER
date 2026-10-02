import { flushPromises } from "@vue/test-utils"
import { ref } from "vue"
import { useReport } from "../use-report"
import { reportService } from "../report-service"
import type { ReportDefinitionMetadata, ReportResult } from "../report-types"

const { route, mockReplace } = vi.hoisted(() => ({
    route: { query: {} as Record<string, unknown> },
    mockReplace: vi.fn<(...args: unknown[]) => Promise<void>>(),
}))
vi.mock("vue-router", () => ({
    useRoute: () => route,
    useRouter: () => ({ replace: mockReplace }),
}))
vi.mock("../report-service", () => ({
    reportService: {
        getDefinition: vi.fn<(...args: unknown[]) => unknown>(),
        run: vi.fn<(...args: unknown[]) => unknown>(),
        exportReport: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))

const onLeave: ReportDefinitionMetadata = {
    key: "personnel.employees-on-leave",
    title: "Employees on Leave",
    area: "Personnel",
    description: "Faculty and staff on leave.",
    parameters: [
        {
            name: "startDate",
            label: "Start date",
            type: "Date",
            required: true,
            defaultValue: "2026-07-01",
            options: [],
        },
        { name: "search", label: "Search", type: "Text", required: false, defaultValue: null, options: [] },
    ],
    columns: [],
}
const noParameters: ReportDefinitionMetadata = { ...onLeave, key: "personnel.vmdo-birthdays", parameters: [] }
const result = { key: onLeave.key, rowCount: 3 } as ReportResult

/** Fresh mocks and query, with the given definition coming back from the API. */
function reset(definition: ReportDefinitionMetadata | null, query: Record<string, unknown> = {}): void {
    vi.clearAllMocks()
    route.query = query
    mockReplace.mockResolvedValue()
    vi.mocked(reportService.getDefinition).mockResolvedValue(definition)
    vi.mocked(reportService.run).mockResolvedValue({ result, errors: [] })
}

/** A promise the test settles by hand, to hold a request in flight. */
function deferred<T>() {
    let settle: ((value: T) => void) | null = null
    // eslint-disable-next-line avoid-new -- a promise the test settles by hand
    const promise = new Promise<T>((resolve) => {
        settle = resolve
    })
    return { promise, resolve: (value: T) => settle?.(value) }
}

async function loadedReport() {
    reset(onLeave)
    const report = useReport(onLeave.key)
    await flushPromises()
    return report
}

describe("useReport() - loading", () => {
    it("is loading until the definition arrives", async () => {
        expect.hasAssertions()
        reset(onLeave)

        const report = useReport(onLeave.key)
        expect(report.loading.value).toBeTruthy()
        await flushPromises()

        expect(report.loading.value).toBeFalsy()
        expect(reportService.getDefinition).toHaveBeenCalledWith(onLeave.key)
    })

    it("fills the form with the report's defaults and waits to be run", async () => {
        expect.hasAssertions()

        const report = await loadedReport()

        expect(report.definition.value).toStrictEqual(onLeave)
        expect(report.values.value).toStrictEqual({ startDate: "2026-07-01", search: null })
        expect(reportService.run).not.toHaveBeenCalled()
    })

    it("fills the form from the query and runs straight away for a bookmarked URL", async () => {
        expect.hasAssertions()
        reset(onLeave, { search: "smith" })

        const report = useReport(onLeave.key)
        await flushPromises()

        expect(report.values.value.search).toBe("smith")
        expect(reportService.run).toHaveBeenCalledWith(onLeave.key, { startDate: "2026-07-01", search: "smith" })
        expect(report.result.value).toStrictEqual(result)
    })

    it("runs a report that has no parameters straight away", async () => {
        expect.hasAssertions()
        reset(noParameters)

        useReport(noParameters.key)
        await flushPromises()

        expect(reportService.run).toHaveBeenCalledWith(noParameters.key, {})
    })

    it("reports a missing report", async () => {
        expect.hasAssertions()
        reset(null)

        const report = useReport("personnel.visa-audit")
        await flushPromises()

        expect(report.notFound.value).toBeTruthy()
        expect(report.values.value).toStrictEqual({})
    })

    it("reloads when the key changes and ignores an earlier load that finishes late", async () => {
        expect.hasAssertions()
        reset(noParameters)
        const first = deferred<ReportDefinitionMetadata>()
        vi.mocked(reportService.getDefinition).mockReturnValueOnce(first.promise)
        const key = ref(onLeave.key)

        const report = useReport(key)
        key.value = noParameters.key
        await flushPromises()
        first.resolve(onLeave)
        await flushPromises()

        expect(reportService.getDefinition).toHaveBeenLastCalledWith(noParameters.key)
        expect(report.definition.value).toStrictEqual(noParameters)
    })
})

describe("useReport() - running and exporting", () => {
    it("writes the parameters to the query before running", async () => {
        expect.hasAssertions()
        const report = await loadedReport()
        report.values.value = { startDate: "2025-07-01", search: "" }

        await report.run()

        expect(mockReplace).toHaveBeenCalledWith({ query: { startDate: "2025-07-01" } })
        expect(reportService.run).toHaveBeenCalledWith(onLeave.key, { startDate: "2025-07-01" })
        expect(report.running.value).toBeFalsy()
    })

    it("shows the server's messages when the run is refused", async () => {
        expect.hasAssertions()
        const report = await loadedReport()
        vi.mocked(reportService.run).mockResolvedValue({ result: null, errors: ["Start date is required."] })

        await report.run()

        expect(report.result.value).toBeNull()
        expect(report.errors.value).toStrictEqual(["Start date is required."])
    })

    it("marks the format being exported until the download finishes", async () => {
        expect.hasAssertions()
        const report = await loadedReport()
        await report.run()
        const download = deferred<boolean>()
        vi.mocked(reportService.exportReport).mockReturnValue(download.promise)

        const exported = report.exportAs("xlsx")
        expect(report.exporting.value).toBe("xlsx")
        download.resolve(true)
        await exported

        expect(reportService.exportReport).toHaveBeenCalledWith(onLeave.key, "xlsx", { startDate: "2026-07-01" })
        expect(report.exporting.value).toBeNull()
    })

    it("exports with the parameters of the run on screen, not later form edits", async () => {
        expect.hasAssertions()
        const report = await loadedReport()
        report.values.value = { startDate: "2025-07-01", search: "smith" }
        await report.run()
        report.values.value = { startDate: "2020-01-01", search: "jones" }

        await report.exportAs("csv")

        expect(reportService.exportReport).toHaveBeenCalledWith(onLeave.key, "csv", {
            startDate: "2025-07-01",
            search: "smith",
        })
    })

    it("does not export before the report has run", async () => {
        expect.hasAssertions()
        const report = await loadedReport()

        await report.exportAs("pdf")

        expect(reportService.exportReport).not.toHaveBeenCalled()
    })

    it("does nothing before a report has loaded", async () => {
        expect.hasAssertions()
        reset(null)
        const report = useReport("personnel.visa-audit")
        await flushPromises()

        await report.run()
        await report.exportAs("pdf")

        expect(reportService.run).not.toHaveBeenCalled()
        expect(reportService.exportReport).not.toHaveBeenCalled()
    })
})
