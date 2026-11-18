import { flushPromises } from "@vue/test-utils"
import { ref } from "vue"
import { useReportBuilder } from "../use-report-builder"
import { reportBuilderService } from "../report-builder-service"
import type { ReportViewerDefinition } from "../report-builder-types"
import { resultOf, roster, textParameter } from "./report-builder-fixtures"

const { route, mockReplace } = vi.hoisted(() => ({
    route: { query: {} as Record<string, unknown> },
    mockReplace: vi.fn<(...args: unknown[]) => Promise<void>>(),
}))
vi.mock("vue-router", () => ({
    useRoute: () => route,
    useRouter: () => ({ replace: mockReplace }),
}))
vi.mock("../report-builder-service", () => ({
    reportBuilderService: {
        getDefinition: vi.fn<(...args: unknown[]) => unknown>(),
        run: vi.fn<(...args: unknown[]) => unknown>(),
        exportReport: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))

const rows = resultOf([{ Name: "Ada", Salary: 1 }])
const optionalOnly: ReportViewerDefinition = { ...roster, parameters: [textParameter] }

function reset(definition: ReportViewerDefinition | null, query: Record<string, unknown> = {}): void {
    route.query = query
    mockReplace.mockResolvedValue()
    vi.mocked(reportBuilderService.getDefinition).mockResolvedValue(definition)
    vi.mocked(reportBuilderService.run).mockResolvedValue({ result: rows, errors: [] })
    vi.mocked(reportBuilderService.exportReport).mockResolvedValue("downloaded")
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

function lastRequest() {
    return vi.mocked(reportBuilderService.run).mock.lastCall?.[1]
}

describe("report builder state - loading", () => {
    it("loads the report and fills the form from the query and defaults", async () => {
        expect.hasAssertions()
        reset(roster, { Name: "Ada" })

        const report = useReportBuilder(roster.slug)
        await flushPromises()

        expect(report.loading.value).toBeFalsy()
        expect(report.values.value).toStrictEqual({ Name: "Ada", Dept: [], Hired: "2026-01-01" })
        expect(report.pagination.value).toStrictEqual({
            page: 1,
            rowsPerPage: 25,
            sortBy: "Name",
            descending: false,
            rowsNumber: 1,
        })
    })

    it("runs straight away when every required field has a value", async () => {
        expect.hasAssertions()
        reset(roster)

        const report = useReportBuilder(roster.slug)
        await flushPromises()

        expect(report.result.value).toStrictEqual(rows)
        expect(lastRequest()).toStrictEqual({
            parameters: { Hired: ["2026-01-01"] },
            page: 1,
            perPage: 25,
            sortBy: "Name",
            sortDescending: false,
            search: null,
        })
    })

    it("waits for the user when a required field is empty", async () => {
        expect.hasAssertions()
        reset({ ...roster, parameters: [{ ...roster.parameters[2]!, defaultValue: null }] })

        useReportBuilder(roster.slug)
        await flushPromises()

        expect(reportBuilderService.run).not.toHaveBeenCalled()
    })

    it("marks a report that isn't available", async () => {
        expect.hasAssertions()
        reset(null)

        const report = useReportBuilder("missing")
        await flushPromises()

        expect(report.notFound.value).toBeTruthy()
        expect(report.values.value).toStrictEqual({})
    })

    it("uses the embedding page's values and leaves the URL alone", async () => {
        expect.hasAssertions()
        reset(optionalOnly, { Name: "From the URL" })

        const report = useReportBuilder(roster.slug, { params: { Name: "Given" }, syncQuery: false })
        await flushPromises()
        await report.run()

        expect(report.values.value).toStrictEqual({ Name: "Given" })
        expect(mockReplace).not.toHaveBeenCalled()
    })

    it("shows all rows of an unpaginated report", async () => {
        expect.hasAssertions()
        reset({ ...optionalOnly, paginate: false })

        const report = useReportBuilder(roster.slug)
        await flushPromises()

        expect(report.pagination.value.rowsPerPage).toBe(0)
        expect(lastRequest()?.perPage).toBeNull()
    })

    it("ignores a definition that arrives after the slug changed", async () => {
        expect.hasAssertions()
        reset(roster)
        const first = deferred<ReportViewerDefinition | null>()
        vi.mocked(reportBuilderService.getDefinition).mockReturnValueOnce(first.promise)
        const slug = ref("first")

        const report = useReportBuilder(slug)
        slug.value = "second"
        await flushPromises()
        first.resolve({ ...roster, title: "Stale" })
        await flushPromises()

        expect(report.definition.value?.title).toBe("Faculty roster")
    })
})

describe("report builder state - running", () => {
    it("runs from the first page and records the values in the URL", async () => {
        expect.hasAssertions()
        reset(optionalOnly)
        const report = useReportBuilder(roster.slug)
        await flushPromises()
        report.pagination.value = { ...report.pagination.value, page: 3 }
        report.values.value = { Name: "Ada" }

        await report.run()

        expect(mockReplace).toHaveBeenCalledWith({ query: { Name: "Ada" } })
        expect(lastRequest()).toMatchObject({ parameters: { Name: ["Ada"] }, page: 1 })
    })

    it("lists the server's messages instead of rows", async () => {
        expect.hasAssertions()
        reset(optionalOnly)
        vi.mocked(reportBuilderService.run).mockResolvedValue({ result: null, errors: ["Department is required."] })

        const report = useReportBuilder(roster.slug)
        await flushPromises()

        expect(report.result.value).toBeNull()
        expect(report.errors.value).toStrictEqual(["Department is required."])
    })

    it("asks for the page and sort the table picked", async () => {
        expect.hasAssertions()
        reset(optionalOnly)
        const report = useReportBuilder(roster.slug)
        await flushPromises()

        await report.changePage({ page: 2, rowsPerPage: 50, sortBy: "Salary", descending: true })

        expect(lastRequest()).toMatchObject({ page: 2, perPage: 50, sortBy: "Salary", sortDescending: true })
    })

    it("searches from the first page", async () => {
        expect.hasAssertions()
        reset(optionalOnly)
        const report = useReportBuilder(roster.slug)
        await flushPromises()
        await report.changePage({ page: 4, rowsPerPage: 25, sortBy: null, descending: false })

        await report.changeSearch("  ada ")

        expect(lastRequest()).toMatchObject({ page: 1, search: "ada" })
    })

    it("clears the search", async () => {
        expect.hasAssertions()
        reset(optionalOnly)
        const report = useReportBuilder(roster.slug)
        await flushPromises()

        await report.changeSearch(null)

        expect(lastRequest()?.search).toBeNull()
    })

    it("counts a further page when a source that can't count fills this one", async () => {
        expect.hasAssertions()
        reset(optionalOnly)
        const full = resultOf(
            Array.from({ length: 25 }, (_, index) => ({ Name: `P${index}` })),
            {
                totalRows: null,
                page: 2,
            },
        )
        vi.mocked(reportBuilderService.run).mockResolvedValue({ result: full, errors: [] })

        const report = useReportBuilder(roster.slug)
        await flushPromises()

        expect(report.pagination.value.rowsNumber).toBe(51)
    })

    it("keeps only the latest run's rows", async () => {
        expect.hasAssertions()
        reset(optionalOnly)
        const report = useReportBuilder(roster.slug)
        await flushPromises()
        const slow = deferred<{ result: typeof rows; errors: [] }>()
        vi.mocked(reportBuilderService.run).mockReturnValueOnce(slow.promise)

        const stale = report.changePage({ page: 2, rowsPerPage: 25, sortBy: null, descending: false })
        await report.changePage({ page: 3, rowsPerPage: 25, sortBy: null, descending: false })
        slow.resolve({ result: resultOf([{ Name: "Stale" }]), errors: [] })
        await stale

        expect(report.result.value).toStrictEqual(rows)
    })

    it("does nothing before the report has loaded", async () => {
        expect.hasAssertions()
        reset(null)
        const report = useReportBuilder("missing")
        await flushPromises()

        await report.run()
        await report.changePage({ page: 2, rowsPerPage: 25, sortBy: null, descending: false })
        await report.exportAs("xlsx")

        expect(reportBuilderService.run).not.toHaveBeenCalled()
        expect(reportBuilderService.exportReport).not.toHaveBeenCalled()
    })
})

/** Fakes only the timers the auto-run waits on; flushPromises keeps its own scheduler. */
function fakeDebounce(): void {
    vi.useFakeTimers({ toFake: ["setTimeout", "clearTimeout"] })
}

async function waitForAutoRun(): Promise<void> {
    await vi.runOnlyPendingTimersAsync()
    await flushPromises()
}

describe("report builder state - running when the form changes", () => {
    it("runs again once the form settles, from the first page and with the last values", async () => {
        expect.hasAssertions()
        fakeDebounce()
        reset(optionalOnly)
        const report = useReportBuilder(roster.slug)
        await flushPromises()
        report.pagination.value = { ...report.pagination.value, page: 3 }

        report.values.value = { Name: "A" }
        await flushPromises()
        report.values.value = { Name: "Ada" }
        await waitForAutoRun()
        vi.useRealTimers()

        expect(reportBuilderService.run).toHaveBeenCalledTimes(2)
        expect(lastRequest()).toMatchObject({ parameters: { Name: ["Ada"] }, page: 1 })
        expect(mockReplace).toHaveBeenLastCalledWith({ query: { Name: "Ada" } })
    })

    it("doesn't run again for the values it loaded with or just ran", async () => {
        expect.hasAssertions()
        fakeDebounce()
        reset(optionalOnly)
        const report = useReportBuilder(roster.slug)
        await waitForAutoRun()
        report.values.value = { Name: "Ada" }
        await report.run()
        await waitForAutoRun()
        vi.useRealTimers()

        expect(reportBuilderService.run).toHaveBeenCalledTimes(2)
    })

    it("clears the rows, and drops a run on its way, when a required field is emptied", async () => {
        expect.hasAssertions()
        fakeDebounce()
        reset(roster)
        const report = useReportBuilder(roster.slug)
        await flushPromises()
        const slow = deferred<{ result: typeof rows; errors: [] }>()
        vi.mocked(reportBuilderService.run).mockReturnValueOnce(slow.promise)
        const inFlight = report.changePage({ page: 2, rowsPerPage: 25, sortBy: "Name", descending: false })

        report.values.value = { ...report.values.value, Hired: null }
        await waitForAutoRun()
        slow.resolve({ result: resultOf([{ Name: "Stale" }]), errors: [] })
        await inFlight
        vi.useRealTimers()

        expect([report.ready.value, report.result.value, report.running.value]).toStrictEqual([false, null, false])
        expect(reportBuilderService.run).toHaveBeenCalledTimes(2)
    })
})

describe("report builder state - downloads", () => {
    it("downloads the rows on screen, not the form's newer values", async () => {
        expect.hasAssertions()
        reset(optionalOnly, { Name: "Ada" })
        const report = useReportBuilder(roster.slug)
        await flushPromises()
        report.values.value = { Name: "Edited" }

        await report.exportAs("xlsx")

        expect(reportBuilderService.exportReport).toHaveBeenCalledWith(
            expect.objectContaining({ slug: roster.slug }),
            "xlsx",
            expect.objectContaining({ parameters: { Name: ["Ada"] } }),
        )
        expect(report.exporting.value).toBeNull()
    })

    it("says when there was nothing to download", async () => {
        expect.hasAssertions()
        reset(optionalOnly)
        vi.mocked(reportBuilderService.exportReport).mockResolvedValue("empty")
        const report = useReportBuilder(roster.slug)
        await flushPromises()

        await report.exportAs("pdf")

        expect(report.nothingToExport.value).toBeTruthy()
    })

    it("can download only when there are rows", async () => {
        expect.hasAssertions()
        reset(optionalOnly)
        vi.mocked(reportBuilderService.run).mockResolvedValue({ result: resultOf([]), errors: [] })

        const report = useReportBuilder(roster.slug)
        await flushPromises()

        expect(report.canExport.value).toBeFalsy()
    })

    it("has nothing to download after a failed run", async () => {
        expect.hasAssertions()
        reset(optionalOnly)
        vi.mocked(reportBuilderService.run).mockResolvedValue({ result: null, errors: ["No."] })
        const report = useReportBuilder(roster.slug)
        await flushPromises()

        await report.exportAs("pdf")

        expect(reportBuilderService.exportReport).not.toHaveBeenCalled()
    })
})
