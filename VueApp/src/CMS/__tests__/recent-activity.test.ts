import RecentActivity from "@/CMS/components/RecentActivity.vue"
import { createTestRouter, mountCms, flushPromises, flushRouter } from "./test-utils"

/**
 * RecentActivity merges up to three sources (content blocks, files, left-nav menus), each
 * gated by a prop, then sorts the merged items by modifiedOn (desc) and caps at MAX_ITEMS (8).
 * It uses Promise.allSettled, so a single source failing must NOT mark the panel failed;
 * only an all-reject set does. typeLabel is shown per item.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockPost = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
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
// The trash source defaults to an empty success so pre-existing tests keep their exact
// call semantics; tests exercising deletions or all-source failure pass it explicitly.
// The latest-change diff is a POST (current content vs newest history row) and answers
// from historyDiff.
function routeGet(handlers: {
    blocks?: unknown
    blockDetail?: unknown
    files?: unknown
    deletedFiles?: unknown
    historyList?: unknown
    historyDiff?: unknown
    leftNavs?: unknown
}) {
    mockPost.mockReset()
    mockPost.mockResolvedValue(handlers.historyDiff)
    mockGet.mockReset()
    mockGet.mockImplementation((...args: unknown[]) => {
        const url = args[0] as string
        if (url.includes("CMS/content/history")) {
            return Promise.resolve(handlers.historyList)
        }
        if (/CMS\/content\/\d+$/u.test(url)) {
            return Promise.resolve(handlers.blockDetail)
        }
        if (url.includes("CMS/content")) {
            return Promise.resolve(handlers.blocks)
        }
        if (url.includes("cms/files") && url.includes("status=deleted")) {
            return Promise.resolve(handlers.deletedFiles ?? { success: true, result: [] })
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

describe("recentActivity.vue - source gating", () => {
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
        const url = mockGet.mock.calls[0]![0] as string
        expect(url).toContain("CMS/content")
        // Recency must be resolved server-side: a client-side sort of the default page can
        // only ever see the first alphabetical page of blocks.
        expect(url).toContain("sortBy=modifiedOn")
        expect(url).toContain("descending=true")
        expect(url).toContain("perPage=5")
    })

    it("loads all three sources when every flag is set", async () => {
        routeGet({
            blocks: { success: true, result: [block(1, "2024-01-03T00:00:00")] },
            files: { success: true, result: [file("g1", "2024-01-02T00:00:00")] },
            leftNavs: { success: true, result: [leftNav(1, "2024-01-01T00:00:00")] },
        })
        await mountActivity({ showBlocks: true, showFiles: true, showLeftNavs: true })
        // ShowFiles drives two fetches: active files and recently deleted (trash) files.
        expect(mockGet).toHaveBeenCalledTimes(4)
    })
})

describe("recentActivity.vue - merge, sort, cap", () => {
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

describe("recentActivity.vue - partial failure semantics", () => {
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
            deletedFiles: { success: false, result: [] },
        })
        const wrapper = await mountActivity({ showBlocks: true, showFiles: true })
        expect(wrapper.text()).toContain("Couldn't load recent activity.")
    })
})

describe("recentActivity.vue - deleted files and item actions", () => {
    it("shows recently deleted files with a deleted verb and a trash deep-link", async () => {
        routeGet({
            files: { success: true, result: [] },
            deletedFiles: {
                success: true,
                result: [{ ...file("g9", "2024-05-01T00:00:00", "Gone file"), deletedOn: "2024-05-02T00:00:00" }],
            },
        })
        const wrapper = await mountActivity({ showFiles: true })

        expect(wrapper.text()).toContain("Gone file")
        expect(wrapper.text()).toContain("deleted")
        const item = wrapper.findAllComponents({ name: "QItem" }).find((i) => i.text().includes("Gone file"))!
        expect(item.props("to")).toStrictEqual({
            name: "CmsFiles",
            query: { status: "deleted", search: "Gone file" },
        })
        const deletedFetch = mockGet.mock.calls.map((c) => c[0] as string).find((u) => u.includes("status=deleted"))!
        expect(deletedFetch).toContain("sortBy=deletedOn")
        expect(deletedFetch).toContain("descending=true")
    })

    it("links block items to their edit history and files to their audit trail", async () => {
        routeGet({
            blocks: { success: true, result: [block(7, "2024-01-02T00:00:00")] },
            files: { success: true, result: [file("g1", "2024-01-01T00:00:00")] },
        })
        const wrapper = await mountActivity({ showBlocks: true, showFiles: true })

        const buttons = wrapper.findAllComponents({ name: "QBtn" })
        const history = buttons.find((b) => b.props("icon") === "history")!
        expect(history.props("to")).toStrictEqual({
            name: "CmsContentBlockHistory",
            query: { contentBlockId: "7" },
        })
        const audit = buttons.find((b) => b.props("icon") === "fact_check")!
        expect(audit.props("to")).toStrictEqual({ name: "CmsFileAudit", query: { fileGuid: "g1" } })
    })

    // Guards the click handling: a preventDefault on to-actions would make QBtn cancel its own
    // router navigation, and a missing .stop would let the row's edit link hijack the click.
    it("navigates to the block's edit history when its history action is clicked", async () => {
        routeGet({ blocks: { success: true, result: [block(7, "2024-01-02T00:00:00")] } })
        const router = createTestRouter()
        const wrapper = mountCms(RecentActivity, { props: { showBlocks: true } }, router)
        await flushPromises()
        await flushPromises()

        await wrapper
            .findAllComponents({ name: "QBtn" })
            .find((b) => b.props("icon") === "history")!
            .trigger("click")
        await flushRouter()

        expect(router.currentRoute.value.name).toBe("CmsContentBlockHistory")
        expect(router.currentRoute.value.query).toStrictEqual({ contentBlockId: "7" })
    })

    it("opens the latest-change diff inline from a block item", async () => {
        routeGet({
            blocks: { success: true, result: [block(7, "2024-01-02T00:00:00", "Diffable block")] },
            blockDetail: {
                success: true,
                result: { ...block(7, "2024-01-02T00:00:00", "Diffable block"), content: "<p>now</p>" },
            },
            historyList: { success: true, result: [{ contentHistoryId: 42, contentBlockId: 7 }] },
            historyDiff: {
                success: true,
                result: {
                    content: "<p>diff <ins>added</ins></p>",
                    hasComparison: true,
                    hasChanges: true,
                    oldModifiedOn: "2024-01-01T00:00:00",
                    oldModifiedBy: "before",
                    newModifiedOn: null,
                    newModifiedBy: null,
                },
            },
        })
        const wrapper = await mountActivity({ showBlocks: true })

        await wrapper
            .findAllComponents({ name: "QBtn" })
            .find((b) => b.props("icon") === "difference")!
            .trigger("click")
        await flushPromises()

        const dialog = wrapper.findComponent({ name: "ContentDiffDialog" })
        expect(dialog.props("modelValue")).toBeTruthy()
        expect(dialog.props("diffHtml")).toContain("added")
        // The latest change is live content vs the newest history row, so it must go through
        // the POST diff (history rows only hold superseded versions) with the current content.
        expect(mockPost).toHaveBeenCalledOnce()
        const [diffUrl, diffBody] = mockPost.mock.calls[0] as [string, unknown]
        expect(diffUrl).toContain("CMS/content/7/history/42/diff")
        expect(diffBody).toStrictEqual({ content: "<p>now</p>" })
    })

    it("closes the diff viewer and notifies when the block has no history", async () => {
        routeGet({
            blocks: { success: true, result: [block(7, "2024-01-02T00:00:00")] },
            blockDetail: { success: true, result: { ...block(7, "2024-01-02T00:00:00"), content: "<p>now</p>" } },
            historyList: { success: true, result: [] },
        })
        const wrapper = await mountActivity({ showBlocks: true })

        await wrapper
            .findAllComponents({ name: "QBtn" })
            .find((b) => b.props("icon") === "difference")!
            .trigger("click")
        await flushPromises()

        expect(wrapper.findComponent({ name: "ContentDiffDialog" }).props("modelValue")).toBeFalsy()
        expect(mockPost).not.toHaveBeenCalled()
    })
})
