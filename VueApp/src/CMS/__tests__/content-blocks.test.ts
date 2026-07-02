import ContentBlocks from "@/CMS/pages/ContentBlocks.vue"
import { mountCms, flushPromises, flushRouter, createTestRouter } from "./test-utils"

/**
 * Representative page filter-state test: filter inputs must drive the right query params on the
 * list request, and returned rows must bind to the table. Mock ViperFetch; section-paths and
 * block list both go through get().
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        del: vi.fn<(...args: unknown[]) => unknown>(),
        post: vi.fn<(...args: unknown[]) => unknown>(),
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

// The list endpoint is the one without "/section-paths"; route by URL.
function routeGet(blockRows: unknown[] = [BLOCK_ROW]) {
    mockGet.mockReset()
    mockGet.mockImplementation((...args: unknown[]) => {
        const url = args[0] as string
        if (url.includes("/section-paths")) {
            return Promise.resolve({ success: true, result: ["/a", "/b"] })
        }
        return Promise.resolve({ success: true, result: blockRows })
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
    beforeEach(() => routeGet())

    it("requests with the default active status on mount and binds returned rows", async () => {
        const { wrapper } = await mountPage()
        expect(lastListUrl()).toContain("status=active")
        // Returned row binds to the table.
        expect(wrapper.findComponent({ name: "QTable" }).props("rows")).toHaveLength(1)
        expect(wrapper.text()).toContain("Welcome")
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
})
