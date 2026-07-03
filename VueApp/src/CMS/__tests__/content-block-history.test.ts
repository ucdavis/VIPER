import ContentBlockHistory from "@/CMS/pages/ContentBlockHistory.vue"
import { mountCms, flushPromises, flushRouter, createTestRouter } from "./test-utils"

/**
 * ContentBlockHistory is a logic-dense page: filters deep-link from route.query, onRequest builds
 * the list query (contentBlockId/modifiedBy/from/to/search/page/perPage) and binds rowsNumber,
 * syncFiltersToUrl reflects active filters back (omitting empties), clearBlockFilter drops the
 * block chip and reloads, viewDiff fetches the GET diff endpoint and drives ContentDiffDialog
 * with a comparison/original subtitle, and the route.query watcher's equality guard avoids a
 * double-fetch on self-writes. DateRangeFilter is mounted real to exercise the binding end-to-end.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
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

const HISTORY_ROW = {
    contentHistoryId: 55,
    contentBlockId: 7,
    title: "Welcome",
    friendlyName: "welcome",
    page: "home",
    modifiedOn: "2024-03-01T12:00:00",
    modifiedBy: "editor",
    blockDeleted: false,
}

type DiffResult = {
    content: string
    hasComparison: boolean
    hasChanges: boolean
    oldModifiedOn: string | null
    oldModifiedBy: string | null
    newModifiedOn: string | null
    newModifiedBy: string | null
}

function routeGet(opts: { rows?: unknown[]; total?: number; diff?: DiffResult | { fail: true } } = {}) {
    const rows = opts.rows ?? [HISTORY_ROW]
    mockGet.mockReset()
    mockGet.mockImplementation((...args: unknown[]) => {
        const url = args[0] as string
        if (url.includes("/diff")) {
            if (opts.diff && "fail" in opts.diff) {
                return Promise.resolve({ success: false, errors: ["boom"] })
            }
            return Promise.resolve({ success: true, result: opts.diff })
        }
        return Promise.resolve({
            success: true,
            result: rows,
            pagination: opts.total === undefined ? undefined : { totalRecords: opts.total },
        })
    })
}

function lastListUrl(): string {
    return mockGet.mock.calls.map((c) => c[0] as string).findLast((u) => u.includes("CMS/content/history?")) ?? ""
}

async function mountPage(query: Record<string, string> = {}) {
    const router = createTestRouter()
    await router.push({ path: "/CMS/ManageContentBlocks/History", query })
    await router.isReady()
    const wrapper = mountCms(ContentBlockHistory, {}, router)
    await flushPromises()
    await flushPromises()
    return { wrapper, router }
}

describe("ContentBlockHistory.vue - list request", () => {
    beforeEach(() => routeGet({ total: 17 }))

    it("requests page 1 with default perPage on mount and binds rowsNumber", async () => {
        const { wrapper } = await mountPage()
        const url = lastListUrl()
        expect(url).toContain("page=1")
        expect(url).toContain("perPage=50")
        const pag = wrapper.findComponent({ name: "QTable" }).props("pagination") as { rowsNumber: number }
        expect(pag.rowsNumber).toBe(17)
        expect(wrapper.findComponent({ name: "QTable" }).props("rows")).toHaveLength(1)
    })

    it("initializes filters from route.query (deep-link) and reflects them in the request", async () => {
        await mountPage({
            contentBlockId: "7",
            modifiedBy: "editor",
            from: "2024-01-01",
            to: "2024-02-01",
            search: "welcome",
        })
        const url = lastListUrl()
        expect(url).toContain("contentBlockId=7")
        expect(url).toContain("modifiedBy=editor")
        expect(url).toContain("from=2024-01-01")
        expect(url).toContain("to=2024-02-01")
        expect(url).toContain("search=welcome")
    })

    it("shows the single-block filter chip when contentBlockId is present", async () => {
        const { wrapper } = await mountPage({ contentBlockId: "7" })
        expect(wrapper.text()).toContain("Filtered to one block: 7")
    })
})

describe("ContentBlockHistory.vue - filter to URL sync", () => {
    beforeEach(() => routeGet({ total: 5 }))

    it("syncFiltersToUrl writes only the active filters to the URL (empties omitted)", async () => {
        const { wrapper, router } = await mountPage({ contentBlockId: "7" })
        const modifiedByInput = wrapper.findAllComponents({ name: "QInput" })[0]!
        modifiedByInput.vm.$emit("update:modelValue", "alice")
        await flushRouter()
        // The new filter reaches both the request and the URL.
        expect(lastListUrl()).toContain("modifiedBy=alice")
        expect(router.currentRoute.value.query.modifiedBy).toBe("alice")
        expect(router.currentRoute.value.query.contentBlockId).toBe("7")
        // Untouched empty filters are not serialized.
        expect(router.currentRoute.value.query.search).toBeUndefined()
        expect(router.currentRoute.value.query.from).toBeUndefined()
    })

    it("clearBlockFilter drops the chip and the contentBlockId param, then reloads", async () => {
        const { wrapper, router } = await mountPage({ contentBlockId: "7" })
        const before = mockGet.mock.calls.length
        wrapper.findComponent({ name: "QChip" }).vm.$emit("remove")
        await flushRouter()
        expect(wrapper.text()).not.toContain("Filtered to one block")
        expect(router.currentRoute.value.query.contentBlockId).toBeUndefined()
        // A reload (new list request) happened.
        expect(mockGet.mock.calls.length).toBeGreaterThan(before)
        expect(lastListUrl()).not.toContain("contentBlockId=")
    })

    it("the route.query watcher's equality guard does not re-fetch on a self-write", async () => {
        const { wrapper } = await mountPage()
        const searchInput = wrapper.findAllComponents({ name: "QInput" }).at(-1)!
        searchInput.vm.$emit("update:modelValue", "abc")
        await flushPromises()
        const callsAfterSearch = mockGet.mock.calls.length
        // SyncFiltersToUrl (router.replace) updates the query; the watcher sees state already
        // equal to filters and returns without a second fetch.
        await flushPromises()
        await flushPromises()
        expect(mockGet.mock.calls.length).toBe(callsAfterSearch)
    })
})

describe("ContentBlockHistory.vue - viewDiff", () => {
    it("fetches the GET diff endpoint and shows the comparison subtitle for a non-original version", async () => {
        routeGet({
            diff: {
                content: "<ins>added</ins>",
                hasComparison: true,
                hasChanges: true,
                oldModifiedOn: "2024-02-01T10:00:00",
                oldModifiedBy: "bob",
                newModifiedOn: "2024-03-01T12:00:00",
                newModifiedBy: "editor",
            },
        })
        const { wrapper } = await mountPage()
        await wrapper
            .findAllComponents({ name: "QBtn" })
            .find((b) => b.attributes("aria-label") === "Diff with previous version")!
            .trigger("click")
        await flushPromises()

        // The diff endpoint is .../content/<blockId>/history/<historyId>/diff.
        const diffCall = mockGet.mock.calls.map((c) => c[0] as string).find((u) => u.includes("/diff"))
        expect(diffCall).toContain("CMS/content/7/history/55/diff")

        const dialog = wrapper.findComponent({ name: "ContentDiffDialog" })
        expect(dialog.props("hasComparison")).toBeTruthy()
        expect(dialog.props("diffHtml")).toContain("added")
        // DiffStamp builds "Changes from <old> to <new>".
        expect(dialog.props("subtitle")).toContain("Changes from")
        expect(dialog.props("subtitle")).toContain("by bob")
        expect(dialog.props("subtitle")).toContain("by editor")
    })

    it("shows the original-version subtitle when the version has no comparison", async () => {
        routeGet({
            diff: {
                content: "",
                hasComparison: false,
                hasChanges: false,
                oldModifiedOn: null,
                oldModifiedBy: null,
                newModifiedOn: "2024-03-01T12:00:00",
                newModifiedBy: "editor",
            },
        })
        const { wrapper } = await mountPage()
        await wrapper
            .findAllComponents({ name: "QBtn" })
            .find((b) => b.attributes("aria-label") === "Diff with previous version")!
            .trigger("click")
        await flushPromises()
        const dialog = wrapper.findComponent({ name: "ContentDiffDialog" })
        expect(dialog.props("hasComparison")).toBeFalsy()
        expect(dialog.props("subtitle")).toContain("Original version")
    })

    it("notifies and closes the viewer when the diff request fails", async () => {
        routeGet({ diff: { fail: true } })
        const { wrapper } = await mountPage()
        await wrapper
            .findAllComponents({ name: "QBtn" })
            .find((b) => b.attributes("aria-label") === "Diff with previous version")!
            .trigger("click")
        await flushPromises()
        const dialog = wrapper.findComponent({ name: "ContentDiffDialog" })
        expect(dialog.props("modelValue")).toBeFalsy()
        expect(document.body.textContent).toContain("boom")
    })
})
