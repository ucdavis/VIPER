import LeftNavMenus from "@/CMS/pages/LeftNavMenus.vue"
import { mountCms, flushPromises, flushRouter, createTestRouter } from "./test-utils"

/**
 * LeftNavMenus list: filter inputs drive system/search query params, returned rows bind to the
 * table, and the ?add=1 deep-link opens the add dialog (on mount and on in-app
 * navigation), consuming the flag.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockDel = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        del: (...args: unknown[]) => mockDel(...args),
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
    mockDel.mockReset()
    mockGet.mockResolvedValue({ success: true, result: rows })
}

function lastUrl(): string {
    return (mockGet.mock.calls.at(-1)?.[0] as string) ?? ""
}

async function mountPageWithRouter(query: Record<string, string> = {}) {
    const router = createTestRouter()
    await router.push({ path: "/CMS/ManageLeftNav", query })
    await router.isReady()
    const wrapper = mountCms(LeftNavMenus, {}, router)
    await flushPromises()
    await flushPromises()
    return { wrapper, router }
}

async function mountPage(query: Record<string, string> = {}) {
    return (await mountPageWithRouter(query)).wrapper
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

    it("opens the add dialog when ?add=1 is present on mount and strips the flag", async () => {
        const { wrapper, router } = await mountPageWithRouter({ add: "1" })
        await flushRouter()

        expect(wrapper.findComponent({ name: "LeftNavMenuDialog" }).props("modelValue")).toBeTruthy()
        expect(router.currentRoute.value.query.add).toBeUndefined()
    })

    it("re-opens the add dialog when navigation adds ?add=1, consuming the flag", async () => {
        const { wrapper, router } = await mountPageWithRouter()
        expect(wrapper.findComponent({ name: "LeftNavMenuDialog" }).props("modelValue")).toBeFalsy()

        await router.push({ path: "/CMS/ManageLeftNav", query: { add: "1" } })
        await flushRouter()

        expect(wrapper.findComponent({ name: "LeftNavMenuDialog" }).props("modelValue")).toBeTruthy()
        // The flag is stripped so re-clicking the nav link can re-open the dialog later.
        expect(router.currentRoute.value.query.add).toBeUndefined()
    })

    it("leaves the add dialog closed without ?add=1", async () => {
        const wrapper = await mountPage()
        expect(wrapper.findComponent({ name: "LeftNavMenuDialog" }).props("modelValue")).toBeFalsy()
    })

    it("clears the table and notifies when loading menus fails", async () => {
        mockGet.mockReset()
        mockGet.mockResolvedValue({ success: false, errors: ["nav service down"] })
        const wrapper = await mountPage()

        expect(wrapper.findComponent({ name: "QTable" }).props("rows")).toEqual([])
        expect(document.body.textContent).toContain("nav service down")
    })

    it("navigates to the new menu's item editor after the add dialog reports a create", async () => {
        const { wrapper, router } = await mountPageWithRouter()

        wrapper.findComponent({ name: "LeftNavMenuDialog" }).vm.$emit("created", 33)
        await flushRouter()

        expect(router.currentRoute.value.name).toBe("CmsLeftNavEdit")
        expect(router.currentRoute.value.params.id).toBe("33")
    })
})

describe("LeftNavMenus.vue - delete", () => {
    beforeEach(() => routeGet())

    it("permanently deletes the menu and reloads once the confirm dialog is accepted", async () => {
        mockDel.mockResolvedValue({ success: true, result: null })
        const wrapper = await mountPage()
        const before = mockGet.mock.calls.length

        await wrapper.find("[aria-label='Delete menu']").trigger("click")
        await flushPromises()
        // The confirmation spells out the item count so the loss is clear.
        expect(document.body.textContent).toContain('Permanently delete "Main" and its 1 item?')

        clickBodyButton("Delete Menu")
        await flushPromises()
        await flushPromises()

        expect(mockDel).toHaveBeenCalledOnce()
        expect(mockDel.mock.calls[0]![0]).toContain("cms/left-navs/1")
        expect(document.body.textContent).toContain("Menu deleted")
        expect(mockGet.mock.calls.length).toBeGreaterThan(before)
    })

    it("does not delete when the confirm dialog is cancelled", async () => {
        const wrapper = await mountPage()

        await wrapper.find("[aria-label='Delete menu']").trigger("click")
        await flushPromises()
        clickBodyButton("Cancel")
        await flushPromises()

        expect(mockDel).not.toHaveBeenCalled()
    })

    it("notifies when the delete fails", async () => {
        mockDel.mockResolvedValue({ success: false, errors: ["in use"] })
        const wrapper = await mountPage()

        await wrapper.find("[aria-label='Delete menu']").trigger("click")
        await flushPromises()
        clickBodyButton("Delete Menu")
        await flushPromises()
        await flushPromises()

        expect(document.body.textContent).toContain("Failed to delete menu")
    })
})
