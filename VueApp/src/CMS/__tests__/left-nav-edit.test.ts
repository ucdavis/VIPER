import LeftNavEdit from "@/CMS/pages/LeftNavEdit.vue"
import { mountCms, flushPromises, createTestRouter } from "./test-utils"

/**
 * LeftNavEdit menu-settings save flow: the settings save carries the menu's modifiedOn as a
 * lastModifiedOn concurrency stamp, and a 409 (someone saved first) opens an "Edit Conflict"
 * dialog offering to reload. Mock ViperFetch; the settings form renders its real inputs.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockPost = vi.fn<(...args: unknown[]) => unknown>()
const mockPut = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        post: (...args: unknown[]) => mockPost(...args),
        put: (...args: unknown[]) => mockPut(...args),
    }),
}))

const MENU = {
    leftNavMenuId: 7,
    menuHeaderText: "Main Menu",
    system: "Viper",
    viperSectionPath: "cats",
    page: null,
    friendlyName: "main-menu",
    modifiedOn: "2024-03-01T12:00:00",
    modifiedBy: "editor",
    items: [],
}

async function mountEdit(routeArgs: { params?: Record<string, string> } = { params: { id: "7" } }) {
    const router = createTestRouter()
    await router.push({ name: "CmsLeftNavEdit", params: routeArgs.params ?? {} })
    await router.isReady()
    const wrapper = mountCms(LeftNavEdit, {}, router)
    await flushPromises()
    await flushPromises()
    return { wrapper, router }
}

async function submitMenuForm(wrapper: Awaited<ReturnType<typeof mountEdit>>["wrapper"]): Promise<void> {
    await wrapper.findComponent({ name: "QForm" }).find("form").trigger("submit")
    await flushPromises()
    await flushPromises()
}

beforeEach(() => {
    mockGet.mockReset()
    mockPost.mockReset()
    mockPut.mockReset()
    // The single menu load; deep-copied so tests can't cross-mutate MENU.
    mockGet.mockImplementation(() => Promise.resolve({ success: true, result: structuredClone(MENU) }))
})

describe("LeftNavEdit.vue - menu settings save", () => {
    it("PUTs the menu's modifiedOn as the lastModifiedOn concurrency stamp", async () => {
        mockPut.mockResolvedValue({ success: true, result: { ...MENU, modifiedOn: "2024-03-02T09:00:00" } })
        const { wrapper } = await mountEdit()

        await submitMenuForm(wrapper)

        expect(mockPut).toHaveBeenCalledOnce()
        const [url, payload] = mockPut.mock.calls[0]!
        expect(url).toContain("cms/left-navs/7")
        expect(payload).toMatchObject({
            menuHeaderText: "Main Menu",
            lastModifiedOn: "2024-03-01T12:00:00",
        })
        expect(document.body.textContent).toContain("Menu settings saved")
    })

    it("opens an Edit Conflict dialog when the settings save returns a 409", async () => {
        mockPut.mockResolvedValue({ success: false, status: 409, errors: ["Someone else saved this menu."] })
        const { wrapper } = await mountEdit()

        await submitMenuForm(wrapper)

        expect(document.body.textContent).toContain("Edit Conflict")
        expect(document.body.textContent).toContain("Someone else saved this menu.")
    })
})

describe("LeftNavEdit.vue - items save", () => {
    type ItemsVm = { saveItems: () => Promise<void> }

    it("PUTs the items with the menu's stamp, then advances the stamp from the response", async () => {
        mockPut.mockResolvedValue({ success: true, result: { ...MENU, modifiedOn: "2024-03-02T09:00:00" } })
        const { wrapper } = await mountEdit()
        const vm = wrapper.vm as unknown as ItemsVm

        await vm.saveItems()
        await flushPromises()

        const [url, payload] = mockPut.mock.calls[0]!
        expect(url).toContain("cms/left-navs/7/items")
        expect(payload).toMatchObject({ lastModifiedOn: "2024-03-01T12:00:00", items: [] })

        // Saving items bumps the menu's ModifiedOn server-side; the next save (items or
        // settings) must carry the advanced stamp or it would 409 against the user's own save.
        await vm.saveItems()
        await flushPromises()
        expect((mockPut.mock.calls[1]![1] as { lastModifiedOn: string }).lastModifiedOn).toBe("2024-03-02T09:00:00")
    })

    it("opens the Edit Conflict dialog when the items save returns a 409", async () => {
        mockPut.mockResolvedValue({ success: false, status: 409, errors: ["This menu was modified by rex."] })
        const { wrapper } = await mountEdit()
        const vm = wrapper.vm as unknown as ItemsVm

        await vm.saveItems()
        await flushPromises()

        expect(document.body.textContent).toContain("Edit Conflict")
        expect(document.body.textContent).toContain("This menu was modified by rex.")
    })
})
