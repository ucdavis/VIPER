import ContentBlockEdit from "@/CMS/pages/ContentBlockEdit.vue"
import { mountCms, flushPromises, createTestRouter } from "./test-utils"

/**
 * Diff additions to ContentBlockEdit: selecting a version only sets selectedHistory (it no longer
 * auto-loads), the "Diff vs current" / "Load into editor" buttons are disabled until a version is
 * selected, and diffAgainstCurrent POSTs the editor's CURRENT content to the diff endpoint, opens
 * ContentDiffDialog with the returned HTML, and notifies + closes on failure. Mock ViperFetch.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockPost = vi.fn<(...args: unknown[]) => unknown>()
const mockPut = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        post: (...args: unknown[]) => mockPost(...args),
        put: (...args: unknown[]) => mockPut(...args),
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

const BLOCK = {
    contentBlockId: 7,
    content: "<p>current editor content</p>",
    title: "Welcome",
    system: "Viper",
    application: null,
    page: null,
    viperSectionPath: null,
    blockOrder: 1,
    friendlyName: "welcome",
    allowPublicAccess: false,
    modifiedOn: "2024-03-01T12:00:00",
    modifiedBy: "editor",
    deletedOn: null,
    permissions: [],
    files: [],
}

const HISTORY = [
    { contentHistoryId: 91, modifiedOn: "2024-02-01T10:00:00", modifiedBy: "bob" },
    { contentHistoryId: 92, modifiedOn: "2024-01-01T09:00:00", modifiedBy: "amy" },
]

function routeGet() {
    mockGet.mockReset()
    mockPost.mockReset()
    mockGet.mockImplementation((...args: unknown[]) => {
        const url = args[0] as string
        if (url.includes("/section-paths")) {
            return Promise.resolve({ success: true, result: [] })
        }
        if (url.includes("/history")) {
            return Promise.resolve({ success: true, result: HISTORY })
        }
        // The single-block load (.../content/7).
        return Promise.resolve({ success: true, result: { ...BLOCK } })
    })
}

// QEditor relies on document.execCommand and rich-text DOM that happy-dom lacks; stub it with a
// minimal v-model textarea so block.content still binds without exercising the real editor.
const qEditorStub = {
    name: "QEditor",
    props: ["modelValue"],
    emits: ["update:modelValue"],
    methods: {
        getContentEl() {
            return null
        },
    },
    template: `<textarea :value="modelValue" @input="$emit('update:modelValue', $event.target.value)" />`,
}

async function mountEdit() {
    const router = createTestRouter()
    await router.push({ name: "CmsContentBlockEdit", params: { id: "7" } })
    await router.isReady()
    const wrapper = mountCms(ContentBlockEdit, { global: { stubs: { QEditor: qEditorStub } } }, router)
    await flushPromises()
    await flushPromises()
    return wrapper
}

function diffButton(wrapper: Awaited<ReturnType<typeof mountEdit>>) {
    return wrapper.findAllComponents({ name: "QBtn" }).find((b) => b.text().includes("Diff vs current"))!
}
function loadButton(wrapper: Awaited<ReturnType<typeof mountEdit>>) {
    return wrapper.findAllComponents({ name: "QBtn" }).find((b) => b.text().includes("Load into editor"))!
}
function historySelect(wrapper: Awaited<ReturnType<typeof mountEdit>>) {
    // The version-history select is the one labeled "Select a previous version".
    return wrapper.findAllComponents({ name: "QSelect" }).find((s) => s.props("label") === "Select a previous version")!
}

describe("ContentBlockEdit.vue - history selection gating", () => {
    beforeEach(() => routeGet())

    it("renders the diff/load buttons disabled until a version is selected", async () => {
        const wrapper = await mountEdit()
        expect(diffButton(wrapper).props("disable")).toBeTruthy()
        expect(loadButton(wrapper).props("disable")).toBeTruthy()
    })

    it("selecting a version only sets selectedHistory and does not fetch that version", async () => {
        const wrapper = await mountEdit()
        const getCallsBefore = mockGet.mock.calls.length
        historySelect(wrapper).vm.$emit("update:modelValue", HISTORY[0])
        await flushPromises()
        // No extra GET for the version content; the buttons just enable.
        expect(mockGet.mock.calls.length).toBe(getCallsBefore)
        expect(diffButton(wrapper).props("disable")).toBeFalsy()
        expect(loadButton(wrapper).props("disable")).toBeFalsy()
    })
})

describe("ContentBlockEdit.vue - diffAgainstCurrent", () => {
    beforeEach(() => routeGet())

    it("POSTs the current editor content to the diff endpoint and opens the dialog with the result", async () => {
        mockPost.mockResolvedValue({
            success: true,
            result: { content: "<ins>new</ins>", hasComparison: true, hasChanges: true },
        })
        const wrapper = await mountEdit()
        historySelect(wrapper).vm.$emit("update:modelValue", HISTORY[0])
        await flushPromises()

        await diffButton(wrapper).trigger("click")
        await flushPromises()

        expect(mockPost).toHaveBeenCalledOnce()
        const [url, payload] = mockPost.mock.calls[0]!
        expect(url).toContain("CMS/content/7/history/91/diff")
        // The CURRENT editor content is posted (so unsaved edits are diffed).
        expect(payload).toEqual({ content: "<p>current editor content</p>" })

        const dialog = wrapper.findComponent({ name: "ContentDiffDialog" })
        expect(dialog.props("modelValue")).toBeTruthy()
        expect(dialog.props("diffHtml")).toContain("new")
        expect(dialog.props("subtitle")).toContain("to your current editor content")
    })

    it("notifies and closes the diff dialog when the diff POST fails", async () => {
        mockPost.mockResolvedValue({ success: false, errors: ["diff failed"] })
        const wrapper = await mountEdit()
        historySelect(wrapper).vm.$emit("update:modelValue", HISTORY[0])
        await flushPromises()

        await diffButton(wrapper).trigger("click")
        await flushPromises()

        const dialog = wrapper.findComponent({ name: "ContentDiffDialog" })
        expect(dialog.props("modelValue")).toBeFalsy()
        expect(document.body.textContent).toContain("diff failed")
    })
})
