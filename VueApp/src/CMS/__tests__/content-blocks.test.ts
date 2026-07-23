import ContentBlocks from "@/CMS/pages/ContentBlocks.vue"
import { mountCms, flushPromises, flushRouter, createTestRouter, clickBodyButton } from "./test-utils"

/**
 * ContentBlocks is a server-paged list (like Files): filter inputs must drive the right query
 * params on the paged list request, page/sort ride through onRequest, and rowsNumber binds from
 * the response pagination envelope. Mock ViperFetch; section-paths and the paged list both go
 * through get(), routed by URL.
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

const BLOCK_ROW = {
    contentBlockId: 1,
    title: "Welcome",
    friendlyName: "welcome",
    system: "Viper",
    viperSectionPath: null,
    page: null,
    blockOrder: 1,
    allowPublicAccess: false,
    deletedOn: null,
    permissions: [],
    modifiedOn: "2024-01-01T00:00:00",
    modifiedBy: "u",
}

// The list endpoint is the one without "/section-paths"; route by URL. Returns a paged
// envelope carrying totalRecords when a total is given (else falls back to row count).
function routeGet(opts: { rows?: unknown[]; total?: number } = {}) {
    const rows = opts.rows ?? [BLOCK_ROW]
    mockGet.mockReset()
    mockDel.mockReset()
    mockPost.mockReset()
    mockGet.mockImplementation((...args: unknown[]) => {
        const url = args[0] as string
        if (url.includes("/section-paths")) {
            return Promise.resolve({ success: true, result: ["/a", "/b"] })
        }
        return Promise.resolve({
            success: true,
            result: rows,
            pagination: opts.total === undefined ? undefined : { totalRecords: opts.total },
        })
    })
}

function lastListUrl(): string {
    const listCalls = mockGet.mock.calls.map((c) => c[0] as string).filter((u) => !u.includes("/section-paths"))
    return listCalls.at(-1) ?? ""
}

async function mountPage(query: Record<string, string> = {}) {
    const router = createTestRouter()
    await router.push({ path: "/CMS/ManageContentBlocks", query })
    await router.isReady()
    const wrapper = mountCms(ContentBlocks, {}, router)
    await flushPromises()
    await flushPromises()
    return { wrapper, router }
}

describe("ContentBlocks.vue - filter-driven query params", () => {
    beforeEach(() => routeGet({ total: 42 }))

    it("requests page 1 with default sort and active status on mount, binding rows and rowsNumber", async () => {
        const { wrapper } = await mountPage()
        const url = lastListUrl()
        expect(url).toContain("status=active")
        expect(url).toContain("page=1")
        expect(url).toContain("perPage=50")
        expect(url).toContain("sortBy=title")
        // Returned row binds to the table, and rowsNumber comes from the pagination envelope.
        expect(wrapper.findComponent({ name: "QTable" }).props("rows")).toHaveLength(1)
        expect(wrapper.text()).toContain("Welcome")
        const pag = wrapper.findComponent({ name: "QTable" }).props("pagination") as { rowsNumber: number }
        expect(pag.rowsNumber).toBe(42)
    })

    it("falls back to result length for rowsNumber when no pagination envelope is returned", async () => {
        routeGet({ rows: [BLOCK_ROW, { ...BLOCK_ROW, contentBlockId: 2 }] })
        const { wrapper } = await mountPage()
        const pag = wrapper.findComponent({ name: "QTable" }).props("pagination") as { rowsNumber: number }
        expect(pag.rowsNumber).toBe(2)
    })

    it("includes the search term in the query when the search field changes", async () => {
        const { wrapper } = await mountPage()
        const searchInput = wrapper.findAllComponents({ name: "QInput" })[0]!
        searchInput.vm.$emit("update:modelValue", "welcome")
        await flushPromises()
        expect(lastListUrl()).toContain("search=welcome")
    })

    it("adds isPublic=true only when the Public-only toggle is on", async () => {
        const { wrapper } = await mountPage()
        expect(lastListUrl()).not.toContain("isPublic")
        const toggle = wrapper.findComponent({ name: "QToggle" })
        toggle.vm.$emit("update:modelValue", true)
        await flushPromises()
        expect(lastListUrl()).toContain("isPublic=true")
    })

    it("sends the chosen status when the status select changes", async () => {
        const { wrapper } = await mountPage()
        const statusSelect = wrapper.findAllComponents({ name: "QSelect" })[0]!
        statusSelect.vm.$emit("update:modelValue", "deleted")
        await flushPromises()
        expect(lastListUrl()).toContain("status=deleted")
    })

    it("binds an empty table when the list request fails", async () => {
        mockGet.mockReset()
        mockGet.mockImplementation((...args: unknown[]) => {
            const url = args[0] as string
            if (url.includes("/section-paths")) {
                return Promise.resolve({ success: true, result: [] })
            }
            return Promise.resolve({ success: false, result: null })
        })
        const { wrapper } = await mountPage()
        expect(wrapper.findComponent({ name: "QTable" }).props("rows")).toEqual([])
    })
})

describe("ContentBlocks.vue - URL filter sync", () => {
    beforeEach(() => routeGet())

    it("initializes filters from the URL query (deep-link) and reflects them in the request", async () => {
        await mountPage({ status: "deleted", section: "courses", search: "welcome", public: "1" })
        const url = lastListUrl()
        expect(url).toContain("status=deleted")
        expect(url).toContain("viperSectionPath=courses")
        expect(url).toContain("search=welcome")
        expect(url).toContain("isPublic=true")
    })

    it("writes only the active filters to the URL when a filter changes (defaults omitted)", async () => {
        const { wrapper, router } = await mountPage()
        const searchInput = wrapper.findAllComponents({ name: "QInput" })[0]!
        searchInput.vm.$emit("update:modelValue", "welcome")
        await flushRouter()
        // The changed filter reaches the URL; the default active status and the off
        // public toggle are omitted rather than serialized as defaults.
        expect(router.currentRoute.value.query.search).toBe("welcome")
        expect(router.currentRoute.value.query.status).toBeUndefined()
        expect(router.currentRoute.value.query.public).toBeUndefined()
    })

    it("re-syncs filters and refetches when in-app navigation changes the query", async () => {
        const { router } = await mountPage()
        const before = mockGet.mock.calls.length

        await router.push({ path: "/CMS/ManageContentBlocks", query: { status: "deleted", search: "old" } })
        await flushRouter()

        expect(mockGet.mock.calls.length).toBeGreaterThan(before)
        const url = lastListUrl()
        expect(url).toContain("status=deleted")
        expect(url).toContain("search=old")
    })
})

describe("ContentBlocks.vue - onRequest pagination passthrough", () => {
    beforeEach(() => routeGet({ total: 200 }))

    it("uses the sort/descending/page from a table request", async () => {
        const { wrapper } = await mountPage()
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
})

// The body is never wiped between tests (Quasar caches its notification container there), so toast
// assertions use per-test-unique messages; clickBodyButton (shared, from test-utils) reaches the
// dialog/notification buttons Quasar teleports to document.body.
function listCallCount(): number {
    return mockGet.mock.calls.map((c) => c[0] as string).filter((u) => !u.includes("/section-paths")).length
}

describe("ContentBlocks.vue - delete and restore actions", () => {
    beforeEach(() => routeGet())

    it("soft-deletes the block and reloads the list once the confirm dialog is accepted", async () => {
        mockDel.mockResolvedValue({ success: true, result: null })
        const { wrapper } = await mountPage()
        const before = listCallCount()

        wrapper.findComponent({ name: "DeleteRestoreButtons" }).vm.$emit("delete")
        await flushPromises()
        expect(document.body.textContent).toContain('Mark "Welcome" as deleted?')

        clickBodyButton("Delete")
        await flushPromises()
        await flushPromises()

        expect(mockDel).toHaveBeenCalledOnce()
        expect(mockDel.mock.calls[0]![0]).toContain("CMS/content/1")
        expect(document.body.textContent).toContain("Content block marked as deleted")
        expect(listCallCount()).toBeGreaterThan(before)
    })

    it("does not delete when the confirm dialog is cancelled", async () => {
        const { wrapper } = await mountPage()

        wrapper.findComponent({ name: "DeleteRestoreButtons" }).vm.$emit("delete")
        await flushPromises()
        clickBodyButton("Cancel")
        await flushPromises()

        expect(mockDel).not.toHaveBeenCalled()
    })

    it("notifies when the delete fails", async () => {
        mockDel.mockResolvedValue({ success: false, errors: ["nope"] })
        const { wrapper } = await mountPage()

        wrapper.findComponent({ name: "DeleteRestoreButtons" }).vm.$emit("delete")
        await flushPromises()
        clickBodyButton("Delete")
        await flushPromises()
        await flushPromises()

        expect(document.body.textContent).toContain("Failed to delete content block")
    })

    it("restores a deleted block and reloads the list", async () => {
        routeGet({ rows: [{ ...BLOCK_ROW, deletedOn: "2024-02-01T00:00:00" }] })
        mockPost.mockResolvedValue({ success: true, result: null })
        const { wrapper } = await mountPage()
        const before = listCallCount()

        wrapper.findComponent({ name: "DeleteRestoreButtons" }).vm.$emit("restore")
        await flushPromises()
        await flushPromises()

        expect(mockPost.mock.calls[0]![0]).toContain("CMS/content/1/restore")
        expect(document.body.textContent).toContain("Content block restored")
        expect(listCallCount()).toBeGreaterThan(before)
    })

    it("notifies when the restore fails", async () => {
        routeGet({ rows: [{ ...BLOCK_ROW, deletedOn: "2024-02-01T00:00:00" }] })
        mockPost.mockResolvedValue({ success: false, errors: ["nope"] })
        const { wrapper } = await mountPage()

        wrapper.findComponent({ name: "DeleteRestoreButtons" }).vm.$emit("restore")
        await flushPromises()
        await flushPromises()

        expect(document.body.textContent).toContain("Failed to restore content block")
    })
})
