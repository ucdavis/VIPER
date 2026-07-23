import Files from "@/CMS/pages/Files.vue"
import { mountCms, flushPromises, flushRouter, createTestRouter } from "./test-utils"

/**
 * Files is a server-paged list. Its filters initialize from the URL (deep-link), onRequest builds
 * the page/sort/filter query and binds rowsNumber from the response pagination. Mock ViperFetch
 * and route requests by URL (folders / folder-counts / the paged list).
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockDel = vi.fn<(...args: unknown[]) => unknown>()
const mockPost = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        del: (...args: unknown[]) => mockDel(...args),
        post: (...args: unknown[]) => mockPost(...args),
        createUrlSearchParams: (obj: Record<string, string | number | null | undefined>) => {
            const params = new URLSearchParams()
            for (const [k, v] of Object.entries(obj)) {
                if (v !== null && v !== undefined) {
                    params.append(k, v.toString())
                }
            }
            return params
        },
    }),
}))

const FILE_ROW = {
    fileGuid: "g1",
    fileName: "a.pdf",
    folder: "Apps",
    friendlyName: "a.pdf",
    encrypted: false,
    description: "",
    allowPublicAccess: false,
    oldUrl: null,
    modifiedOn: "2024-01-01T00:00:00",
    modifiedBy: "u",
    deletedOn: null,
    purgeOn: null,
    permissions: [],
    people: [],
    url: "/files/g1",
    friendlyUrl: "/Apps/a.pdf",
}

// Returns the list endpoint URL (the bare "cms/files/?..." call, not folders/folder-counts).
function lastListUrl(): string {
    const listCalls = mockGet.mock.calls.map((c) => c[0] as string).filter((u) => u.includes("cms/files/?"))
    return listCalls.at(-1) ?? ""
}

function routeGet(opts: { rows?: unknown[]; total?: number } = {}) {
    const rows = opts.rows ?? [FILE_ROW]
    mockGet.mockReset()
    mockDel.mockReset()
    mockPost.mockReset()
    mockGet.mockImplementation((...args: unknown[]) => {
        const url = args[0] as string
        if (url.includes("folder-counts")) {
            return Promise.resolve({ success: true, result: [{ folder: "Apps", count: 3 }] })
        }
        if (url.includes("folders")) {
            return Promise.resolve({ success: true, result: ["Apps", "Reports"] })
        }
        // The paged list request.
        return Promise.resolve({
            success: true,
            result: rows,
            pagination: opts.total === undefined ? undefined : { totalRecords: opts.total },
        })
    })
}

async function mountPageWithRouter(query: Record<string, string> = {}) {
    const router = createTestRouter()
    await router.push({ path: "/CMS/ManageFiles", query })
    await router.isReady()
    const wrapper = mountCms(Files, {}, router)
    await flushPromises()
    await flushPromises()
    return { wrapper, router }
}

async function mountPage(query: Record<string, string> = {}) {
    return (await mountPageWithRouter(query)).wrapper
}

function listCallCount(): number {
    return mockGet.mock.calls.map((c) => c[0] as string).filter((u) => u.includes("cms/files/?")).length
}

// Quasar plugin dialogs ($q.dialog) and notifications teleport to document.body. Click the LAST
// match: a dismissed dialog's portal can linger mid-transition, so the newest dialog is the live
// one. The body is never wiped between tests (Quasar caches its notification container there),
// so toast assertions use per-test-unique messages.
function clickBodyButton(label: string) {
    const btn = [...document.body.querySelectorAll("button")].filter((b) => b.textContent?.includes(label)).at(-1)
    expect(btn, `expected a "${label}" button in the dialog`).toBeTruthy()
    btn!.click()
}

describe("Files.vue - query param construction", () => {
    beforeEach(() => routeGet({ total: 42 }))

    it("requests page 1 with default sort and status on mount, binding rowsNumber from pagination", async () => {
        const wrapper = await mountPage()
        const url = lastListUrl()
        expect(url).toContain("page=1")
        expect(url).toContain("perPage=50")
        expect(url).toContain("sortBy=friendlyName")
        expect(url).toContain("status=active")
        expect(wrapper.findComponent({ name: "QTable" }).props("rows")).toHaveLength(1)
        // RowsNumber is read off the QTable's bound pagination prop.
        const pag = wrapper.findComponent({ name: "QTable" }).props("pagination") as { rowsNumber: number }
        expect(pag.rowsNumber).toBe(42)
    })

    it("initializes filters from the URL query (deep-link) and reflects them in the request", async () => {
        await mountPage({ search: "report", status: "deleted", encrypted: "1", public: "1", folder: "Reports" })
        const url = lastListUrl()
        expect(url).toContain("search=report")
        expect(url).toContain("status=deleted")
        expect(url).toContain("encrypted=true")
        expect(url).toContain("isPublic=true")
        expect(url).toContain("folder=Reports")
    })

    it("omits the folder param when folder is the All sentinel", async () => {
        await mountPage()
        expect(lastListUrl()).not.toContain("folder=")
    })

    it("falls back to result length for rowsNumber when no pagination is returned", async () => {
        routeGet({ rows: [FILE_ROW, { ...FILE_ROW, fileGuid: "g2" }] })
        const wrapper = await mountPage()
        const pag = wrapper.findComponent({ name: "QTable" }).props("pagination") as { rowsNumber: number }
        expect(pag.rowsNumber).toBe(2)
    })
})

describe("Files.vue - onRequest pagination passthrough", () => {
    beforeEach(() => routeGet({ total: 200 }))

    it("uses the sort/descending/page from a table request", async () => {
        const wrapper = await mountPage()
        await wrapper.findComponent({ name: "QTable" }).vm.$emit("request", {
            pagination: { page: 3, rowsPerPage: 25, sortBy: "modifiedOn", descending: true },
        })
        await flushPromises()
        const url = lastListUrl()
        expect(url).toContain("page=3")
        expect(url).toContain("perPage=25")
        expect(url).toContain("sortBy=modifiedOn")
        expect(url).toContain("descending=true")
    })

    it("maps a Purges column sort to the deletedOn sort the API supports", async () => {
        const wrapper = await mountPage({ status: "deleted" })
        await wrapper.findComponent({ name: "QTable" }).vm.$emit("request", {
            pagination: { page: 1, rowsPerPage: 25, sortBy: "purgeOn", descending: false },
        })
        await flushPromises()
        expect(lastListUrl()).toContain("sortBy=deletedOn")
    })
})

describe("Files.vue - delete and restore actions", () => {
    beforeEach(() => routeGet())

    it("soft-deletes the file and reloads the list once the confirm dialog is accepted", async () => {
        mockDel.mockResolvedValue({ success: true, result: null })
        const wrapper = await mountPage()
        const before = listCallCount()

        wrapper.findComponent({ name: "DeleteRestoreButtons" }).vm.$emit("delete")
        await flushPromises()
        expect(document.body.textContent).toContain('Mark "a.pdf" as deleted?')

        clickBodyButton("Delete")
        await flushPromises()
        await flushPromises()

        expect(mockDel).toHaveBeenCalledOnce()
        expect(mockDel.mock.calls[0]![0]).toContain("cms/files/g1")
        expect(document.body.textContent).toContain("File marked as deleted")
        expect(listCallCount()).toBeGreaterThan(before)
    })

    it("does not delete when the confirm dialog is cancelled", async () => {
        const wrapper = await mountPage()

        wrapper.findComponent({ name: "DeleteRestoreButtons" }).vm.$emit("delete")
        await flushPromises()
        clickBodyButton("Cancel")
        await flushPromises()

        expect(mockDel).not.toHaveBeenCalled()
    })

    it("notifies and keeps the list when the delete fails", async () => {
        mockDel.mockResolvedValue({ success: false, errors: ["nope"] })
        const wrapper = await mountPage()
        const before = listCallCount()

        wrapper.findComponent({ name: "DeleteRestoreButtons" }).vm.$emit("delete")
        await flushPromises()
        clickBodyButton("Delete")
        await flushPromises()
        await flushPromises()

        expect(document.body.textContent).toContain("Failed to delete file")
        expect(listCallCount()).toBe(before)
    })

    it("restores a deleted file and reloads the list", async () => {
        routeGet({ rows: [{ ...FILE_ROW, deletedOn: "2024-02-01T00:00:00" }] })
        mockPost.mockResolvedValue({ success: true, result: null })
        const wrapper = await mountPage()
        const before = listCallCount()

        wrapper.findComponent({ name: "DeleteRestoreButtons" }).vm.$emit("restore")
        await flushPromises()
        await flushPromises()

        expect(mockPost).toHaveBeenCalledOnce()
        expect(mockPost.mock.calls[0]![0]).toContain("cms/files/g1/restore")
        expect(document.body.textContent).toContain("File restored")
        expect(listCallCount()).toBeGreaterThan(before)
    })

    it("notifies when the restore fails", async () => {
        routeGet({ rows: [{ ...FILE_ROW, deletedOn: "2024-02-01T00:00:00" }] })
        mockPost.mockResolvedValue({ success: false, errors: ["nope"] })
        const wrapper = await mountPage()

        wrapper.findComponent({ name: "DeleteRestoreButtons" }).vm.$emit("restore")
        await flushPromises()
        await flushPromises()

        expect(document.body.textContent).toContain("Failed to restore file")
    })
})

describe("Files.vue - row presentation", () => {
    beforeEach(() => routeGet())
    afterEach(() => vi.unstubAllEnvs())

    it("labels a trashed file with its deletion and auto-purge dates", async () => {
        routeGet({ rows: [{ ...FILE_ROW, deletedOn: "2024-02-01T00:00:00", purgeOn: "2024-03-02T00:00:00" }] })
        const wrapper = await mountPage()

        const deletedIcon = wrapper
            .findAllComponents({ name: "StatusIcon" })
            .find((c) => (c.props("label") as string).startsWith("Deleted"))!
        expect(deletedIcon.props("label")).toBe("Deleted 02/01/24 · purges 03/02/24")
    })

    it("shows the Purges column in the deleted view, with a warning badge when purge is imminent", async () => {
        const soon = new Date(Date.now() + 3 * 86_400_000).toISOString()
        routeGet({ rows: [{ ...FILE_ROW, deletedOn: "2024-02-01T00:00:00", purgeOn: soon }] })
        const wrapper = await mountPage({ status: "deleted" })

        expect(wrapper.text()).toContain("Purges")
        const badge = wrapper.findAllComponents({ name: "StatusBadge" }).find((b) => b.props("label") === "soon")
        expect(badge).toBeTruthy()
    })

    it("shows the purge date without the badge when the purge is not imminent", async () => {
        const far = new Date(Date.now() + 20 * 86_400_000).toISOString()
        routeGet({ rows: [{ ...FILE_ROW, deletedOn: "2024-02-01T00:00:00", purgeOn: far }] })
        const wrapper = await mountPage({ status: "deleted" })

        expect(wrapper.text()).toContain("Purges")
        const badge = wrapper.findAllComponents({ name: "StatusBadge" }).find((b) => b.props("label") === "soon")
        expect(badge).toBeFalsy()
    })

    it("omits the Purges column in the active view", async () => {
        const wrapper = await mountPage()

        expect(wrapper.text()).not.toContain("Purges")
    })

    it("links the old URL on the VIPER 1 host so legacy links are exercised end to end", async () => {
        vi.stubEnv("VITE_VIPER_1_HOME", "http://v1.local/")
        routeGet({ rows: [{ ...FILE_ROW, oldUrl: "/old/report.pdf" }] })
        const wrapper = await mountPage()

        expect(wrapper.find('a[href="http://v1.local/old/report.pdf"]').exists()).toBeTruthy()
    })

    it("builds the folder filter options with per-folder counts and an All total", async () => {
        const wrapper = await mountPage()

        const options = wrapper.findAllComponents({ name: "QSelect" })[0]!.props("options") as { label: string }[]
        expect(options.map((o) => o.label)).toEqual(["All (3)", "Apps (3)", "Reports (0)"])
    })
})

describe("Files.vue - URL and query watcher", () => {
    beforeEach(() => routeGet())

    it("opens the upload dialog when mounted with the ?upload=1 deep-link and strips the flag", async () => {
        const { wrapper, router } = await mountPageWithRouter({ upload: "1" })
        await flushRouter()

        expect(wrapper.findComponent({ name: "FileFormDialog" }).props("modelValue")).toBeTruthy()
        // The on-mount filter sync races the upload-watcher's strip; the flag must not
        // survive either write, so re-clicking the nav link can re-open the dialog later.
        expect(router.currentRoute.value.query.upload).toBeUndefined()
    })

    it("re-opens the upload dialog when navigation adds ?upload=1, consuming the flag", async () => {
        const { wrapper, router } = await mountPageWithRouter()
        expect(wrapper.findComponent({ name: "FileFormDialog" }).props("modelValue")).toBeFalsy()

        await router.push({ path: "/CMS/ManageFiles", query: { upload: "1" } })
        await flushRouter()

        expect(wrapper.findComponent({ name: "FileFormDialog" }).props("modelValue")).toBeTruthy()
        // The flag is stripped so re-clicking the nav link can re-open the dialog later.
        expect(router.currentRoute.value.query.upload).toBeUndefined()
    })

    it("re-syncs filters and refetches when in-app navigation changes the query", async () => {
        const { router } = await mountPageWithRouter()
        const before = listCallCount()

        await router.push({ path: "/CMS/ManageFiles", query: { folder: "Reports", status: "deleted" } })
        await flushRouter()

        expect(listCallCount()).toBeGreaterThan(before)
        const url = lastListUrl()
        expect(url).toContain("folder=Reports")
        expect(url).toContain("status=deleted")
    })

    it("does not refetch when the watcher sees our own URL sync (equality guard)", async () => {
        const { wrapper } = await mountPageWithRouter()

        wrapper.findComponent({ name: "QInput" }).vm.$emit("update:modelValue", "report")
        await flushPromises()
        const afterFilterChange = listCallCount()

        // The filter change itself fetched once; the router.replace it triggered must not re-fetch.
        await flushRouter()
        expect(listCallCount()).toBe(afterFilterChange)
    })
})
