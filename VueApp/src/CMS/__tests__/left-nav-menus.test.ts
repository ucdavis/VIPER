import LeftNavMenus from "@/CMS/pages/LeftNavMenus.vue"
import { mountCms, flushPromises, createTestRouter } from "./test-utils"

/**
 * LeftNavMenus list: filter inputs drive system/search query params, returned rows bind to the
 * table, and the ?add=1 deep-link opens the add dialog on mount.
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

const MENU_ROW = {
    leftNavMenuId: 1,
    menuHeaderText: "Main",
    system: "Viper",
    viperSectionPath: null,
    page: null,
    friendlyName: null,
    items: [{ leftNavItemId: 1 }],
    modifiedOn: "2024-01-01T00:00:00",
    modifiedBy: "u",
}

function routeGet(rows: unknown[] = [MENU_ROW]) {
    mockGet.mockReset()
    mockGet.mockResolvedValue({ success: true, result: rows })
}

function lastUrl(): string {
    return (mockGet.mock.calls.at(-1)?.[0] as string) ?? ""
}

async function mountPage(query: Record<string, string> = {}) {
    const router = createTestRouter()
    await router.push({ path: "/CMS/ManageLeftNav", query })
    await router.isReady()
    const wrapper = mountCms(LeftNavMenus, {}, router)
    await flushPromises()
    await flushPromises()
    return wrapper
}

describe("LeftNavMenus.vue", () => {
    beforeEach(() => routeGet())

    it("loads menus on mount and binds the returned rows to the table", async () => {
        const wrapper = await mountPage()
        expect(mockGet).toHaveBeenCalled()
        expect(wrapper.findComponent({ name: "QTable" }).props("rows")).toHaveLength(1)
        expect(wrapper.text()).toContain("Main")
    })

    it("includes the search term in the query when the search field changes", async () => {
        const wrapper = await mountPage()
        const searchInput = wrapper.findAllComponents({ name: "QInput" })[0]!
        searchInput.vm.$emit("update:modelValue", "main")
        await flushPromises()
        expect(lastUrl()).toContain("search=main")
    })

    it("includes the chosen system in the query", async () => {
        const wrapper = await mountPage()
        const systemSelect = wrapper.findAllComponents({ name: "QSelect" })[0]!
        systemSelect.vm.$emit("update:modelValue", "Public")
        await flushPromises()
        expect(lastUrl()).toContain("system=Public")
    })

    it("opens the add dialog when ?add=1 is present on mount", async () => {
        const wrapper = await mountPage({ add: "1" })
        expect(wrapper.findComponent({ name: "LeftNavMenuDialog" }).props("modelValue")).toBeTruthy()
    })

    it("leaves the add dialog closed without ?add=1", async () => {
        const wrapper = await mountPage()
        expect(wrapper.findComponent({ name: "LeftNavMenuDialog" }).props("modelValue")).toBeFalsy()
    })
})
