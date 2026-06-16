import RecentActivity from "@/CMS/components/RecentActivity.vue"
import { mountCms, flushPromises } from "./test-utils"

/**
 * RecentActivity merges up to three sources (content blocks, files, left-nav menus), each
 * gated by a prop, then sorts the merged items by modifiedOn (desc) and caps at MAX_ITEMS (8).
 * It uses Promise.allSettled, so a single source failing must NOT mark the panel failed;
 * only an all-reject set does. typeLabel is shown per item.
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

function block(id: number, modifiedOn: string, title = `Block ${id}`) {
    return { contentBlockId: id, title, friendlyName: null, modifiedOn, modifiedBy: "editor" }
}
function file(guid: string, modifiedOn: string, friendlyName = `File ${guid}`) {
    return { fileGuid: guid, friendlyName, modifiedOn, modifiedBy: "uploader" }
}
function leftNav(id: number, modifiedOn: string, menuHeaderText = `Menu ${id}`) {
    return { leftNavMenuId: id, menuHeaderText, friendlyName: null, modifiedOn, modifiedBy: "navadmin" }
}

// Routes requests to the right canned response based on the URL the component builds.
function routeGet(handlers: { blocks?: unknown; files?: unknown; leftNavs?: unknown }) {
    mockGet.mockReset()
    mockGet.mockImplementation((...args: unknown[]) => {
        const url = args[0] as string
        if (url.includes("CMS/content")) {
            return Promise.resolve(handlers.blocks)
        }
        if (url.includes("cms/files")) {
            return Promise.resolve(handlers.files)
        }
        if (url.includes("cms/left-navs")) {
            return Promise.resolve(handlers.leftNavs)
        }
        return Promise.resolve({ success: false, result: [] })
    })
}

async function mountActivity(props: Record<string, boolean>) {
    const wrapper = mountCms(RecentActivity, { props })
    await flushPromises()
    await flushPromises()
    return wrapper
}

describe("RecentActivity.vue - source gating", () => {
    it("loads no sources and shows the empty message when all flags are off", async () => {
        routeGet({})
        const wrapper = await mountActivity({})
        expect(mockGet).not.toHaveBeenCalled()
        expect(wrapper.text()).toContain("Nothing edited recently.")
    })

    it("loads only blocks when showBlocks is set", async () => {
        routeGet({ blocks: { success: true, result: [block(1, "2024-01-01T00:00:00")] } })
        await mountActivity({ showBlocks: true })
        expect(mockGet).toHaveBeenCalledOnce()
        expect(mockGet.mock.calls[0]![0]).toContain("CMS/content")
    })

    it("loads all three sources when every flag is set", async () => {
        routeGet({
            blocks: { success: true, result: [block(1, "2024-01-03T00:00:00")] },
            files: { success: true, result: [file("g1", "2024-01-02T00:00:00")] },
            leftNavs: { success: true, result: [leftNav(1, "2024-01-01T00:00:00")] },
        })
        await mountActivity({ showBlocks: true, showFiles: true, showLeftNavs: true })
        expect(mockGet).toHaveBeenCalledTimes(3)
    })
})

describe("RecentActivity.vue - merge, sort, cap", () => {
    it("merges sources and sorts by modifiedOn descending", async () => {
        routeGet({
            blocks: { success: true, result: [block(1, "2024-01-01T00:00:00", "Old block")] },
            files: { success: true, result: [file("g1", "2024-03-01T00:00:00", "New file")] },
            leftNavs: { success: true, result: [leftNav(1, "2024-02-01T00:00:00", "Mid menu")] },
        })
        const wrapper = await mountActivity({ showBlocks: true, showFiles: true, showLeftNavs: true })
        const labels = wrapper.findAll(".q-item__label").map((n) => n.text())
        const newFileIdx = labels.findIndex((t) => t.includes("New file"))
        const midMenuIdx = labels.findIndex((t) => t.includes("Mid menu"))
        const oldBlockIdx = labels.findIndex((t) => t.includes("Old block"))
        expect(newFileIdx).toBeLessThan(midMenuIdx)
        expect(midMenuIdx).toBeLessThan(oldBlockIdx)
    })

    it("caps the merged list at MAX_ITEMS (8)", async () => {
        // Five blocks + five files = 10 candidates after each source's own per-source cap.
        const fiveBlocks = Array.from({ length: 5 }, (_, i) => block(i + 1, `2024-01-0${i + 1}T00:00:00`))
        const fiveFiles = Array.from({ length: 5 }, (_, i) => file(`g${i}`, `2024-02-0${i + 1}T00:00:00`))
        routeGet({
            blocks: { success: true, result: fiveBlocks },
            files: { success: true, result: fiveFiles },
        })
        const wrapper = await mountActivity({ showBlocks: true, showFiles: true })
        expect(wrapper.findAllComponents({ name: "QItem" })).toHaveLength(8)
    })

    it("renders the typeLabel for each item", async () => {
        routeGet({
            blocks: { success: true, result: [block(1, "2024-01-01T00:00:00")] },
            files: { success: true, result: [file("g1", "2024-01-02T00:00:00")] },
        })
        const wrapper = await mountActivity({ showBlocks: true, showFiles: true })
        expect(wrapper.text()).toContain("Content block")
        expect(wrapper.text()).toContain("File")
    })
})

describe("RecentActivity.vue - partial failure semantics", () => {
    it("does NOT mark failed when one source rejects but another succeeds", async () => {
        routeGet({
            blocks: { success: false, result: [] }, // LoadBlocks throws on !success
            files: { success: true, result: [file("g1", "2024-01-02T00:00:00")] },
        })
        const wrapper = await mountActivity({ showBlocks: true, showFiles: true })
        expect(wrapper.text()).not.toContain("Couldn't load recent activity.")
        expect(wrapper.findAllComponents({ name: "QItem" })).toHaveLength(1)
    })

    it("marks failed only when ALL active sources reject", async () => {
        routeGet({
            blocks: { success: false, result: [] },
            files: { success: false, result: [] },
        })
        const wrapper = await mountActivity({ showBlocks: true, showFiles: true })
        expect(wrapper.text()).toContain("Couldn't load recent activity.")
    })
})
