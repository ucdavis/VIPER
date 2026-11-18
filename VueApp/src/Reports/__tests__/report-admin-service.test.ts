import { reportAdminService } from "../admin/report-admin-service"
import { rosterReport, rosterView, saved } from "./report-admin-fixtures"

const { mockGet, mockPost, mockPut, mockDel } = vi.hoisted(() => ({
    mockGet: vi.fn<(...args: unknown[]) => unknown>(),
    mockPost: vi.fn<(...args: unknown[]) => unknown>(),
    mockPut: vi.fn<(...args: unknown[]) => unknown>(),
    mockDel: vi.fn<(...args: unknown[]) => unknown>(),
}))
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        post: (...args: unknown[]) => mockPost(...args),
        put: (...args: unknown[]) => mockPut(...args),
        del: (...args: unknown[]) => mockDel(...args),
    }),
}))

const run = { parameters: {}, page: 1, perPage: null, sortBy: null, sortDescending: false, search: null }

describe("admin reads", () => {
    it.each([
        { name: "sources", read: () => reportAdminService.getSources(), url: /admin\/sources$/u, result: [rosterView] },
        {
            name: "one source",
            read: () => reportAdminService.getSource("Faculty Roster"),
            url: /admin\/sources\/Faculty%20Roster$/u,
            result: rosterView,
        },
        {
            name: "the list",
            read: () => reportAdminService.list(true),
            url: /definitions\?includeDeleted=true$/u,
            result: [],
        },
        { name: "a report", read: () => reportAdminService.get(4), url: /definitions\/4$/u, result: saved },
        {
            name: "the history",
            read: () => reportAdminService.getHistory(4),
            url: /definitions\/4\/history$/u,
            result: [],
        },
        {
            name: "a version",
            read: () => reportAdminService.getVersion(4, 9),
            url: /definitions\/4\/history\/9$/u,
            result: { version: { id: 9 } },
        },
        {
            name: "the failed runs",
            read: () => reportAdminService.getFailures(7),
            url: /admin\/failures\?days=7$/u,
            result: [],
        },
        {
            name: "the check",
            read: () => reportAdminService.check(),
            url: /admin\/check$/u,
            result: { checked: 0, failing: [] },
        },
    ])("gets $name", async ({ read, url, result }) => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: true, result })

        await expect(read()).resolves.toStrictEqual(result)
        expect(mockGet).toHaveBeenCalledWith(expect.stringMatching(url))
    })

    it.each([
        () => reportAdminService.getSources(),
        () => reportAdminService.getSource("x"),
        () => reportAdminService.list(false),
        () => reportAdminService.get(4),
        () => reportAdminService.getHistory(4),
        () => reportAdminService.getVersion(4, 9),
        () => reportAdminService.getFailures(30),
        () => reportAdminService.check(),
    ])("returns null when a read fails (%#)", async (read) => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: false, result: null })

        await expect(read()).resolves.toBeNull()
    })

    it("searches RAPS permissions, finding none when the request fails", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValueOnce({
            success: true,
            result: [{ permission: "SVMSecure.Effort", description: null }],
        })
        mockGet.mockResolvedValueOnce({ success: false, result: null })

        await expect(reportAdminService.searchPermissions("a&b")).resolves.toHaveLength(1)
        await expect(reportAdminService.searchPermissions("x")).resolves.toStrictEqual([])
        expect(mockGet).toHaveBeenCalledWith(expect.stringMatching(/permissions\?search=a%26b$/u))
    })
})

describe("admin saves", () => {
    it("creates a new report and saves over an existing one", async () => {
        expect.hasAssertions()
        mockPost.mockResolvedValue({ success: true, result: saved })
        mockPut.mockResolvedValue({ success: true, result: saved })

        await expect(reportAdminService.save(null, rosterReport)).resolves.toStrictEqual({
            definition: saved,
            errors: [],
            conflict: false,
        })
        await reportAdminService.save(4, rosterReport)

        expect(mockPost).toHaveBeenCalledWith(expect.stringMatching(/admin\/definitions$/u), rosterReport)
        expect(mockPut).toHaveBeenCalledWith(expect.stringMatching(/definitions\/4$/u), rosterReport)
    })

    it("returns the server's messages and whether someone else saved first", async () => {
        expect.hasAssertions()
        mockPut.mockResolvedValue({ success: false, result: null, errors: ["Someone else saved it."], status: 409 })

        await expect(reportAdminService.save(4, rosterReport)).resolves.toStrictEqual({
            definition: null,
            errors: ["Someone else saved it."],
            conflict: true,
        })
    })

    it("explains a failure that came without messages", async () => {
        expect.hasAssertions()
        mockPost.mockResolvedValue({ success: false, result: null, errors: null, status: null })

        const outcome = await reportAdminService.save(null, rosterReport)

        expect([outcome.errors, outcome.conflict]).toStrictEqual([
            ["The report couldn't be saved. Please try again later."],
            false,
        ])
    })

    it("deletes and restores", async () => {
        expect.hasAssertions()
        mockDel.mockResolvedValue({ success: true, result: saved })
        mockPost.mockResolvedValue({ success: false, result: null })

        await expect(reportAdminService.remove(4)).resolves.toStrictEqual(saved)
        await expect(reportAdminService.restore(4)).resolves.toBeNull()
        expect(mockPost).toHaveBeenCalledWith(expect.stringMatching(/definitions\/4\/restore$/u))
    })
})

describe("admin preview", () => {
    it("posts the unsaved report with the page to show", async () => {
        expect.hasAssertions()
        const result = { columns: [], rows: [], page: 1, perPage: 50, totalRows: 0, truncated: false }
        mockPost.mockResolvedValue({ success: true, result })

        await expect(reportAdminService.preview(rosterReport, run)).resolves.toStrictEqual({ result, errors: [] })
        expect(mockPost).toHaveBeenCalledWith(expect.stringMatching(/admin\/preview$/u), { report: rosterReport, run })
    })

    it("returns the server's messages", async () => {
        expect.hasAssertions()
        mockPost.mockResolvedValue({ success: false, result: null, errors: ["Show at least one column.", 3] })

        await expect(reportAdminService.preview(rosterReport, run)).resolves.toStrictEqual({
            result: null,
            errors: ["Show at least one column."],
        })
    })
})
