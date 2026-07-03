import ContentBlockEdit from "@/CMS/pages/ContentBlockEdit.vue"
import { mountCms, flushPromises, flushRouter, createTestRouter } from "./test-utils"

/**
 * ContentBlockEdit save flow: staged inline uploads commit BEFORE the block save (so their GUIDs
 * ride along in fileGuids), the save payload carries the full block shape with lastModifiedOn as
 * the concurrency stamp (null on create), a 409 rolls freshly-created files back and offers a
 * reload, other failures roll back and show a banner, and success notifies + refreshes history.
 * Mock ViperFetch; the inline uploader is stubbed with a controllable commit().
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockPost = vi.fn<(...args: unknown[]) => unknown>()
const mockPut = vi.fn<(...args: unknown[]) => unknown>()
const mockDel = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        post: (...args: unknown[]) => mockPost(...args),
        put: (...args: unknown[]) => mockPut(...args),
        del: (...args: unknown[]) => mockDel(...args),
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
    content: "<p>hello</p>",
    title: "Welcome",
    system: "Viper",
    application: null,
    page: "home",
    viperSectionPath: "/apps",
    blockOrder: 2,
    friendlyName: "welcome",
    allowPublicAccess: false,
    modifiedOn: "2024-03-01T12:00:00",
    modifiedBy: "editor",
    deletedOn: null,
    permissions: ["SVMSecure.CMS"],
    editPermissions: ["SVMSecure.CMS.Delegate"],
    files: [{ fileGuid: "f1", friendlyName: "a.pdf", url: "/files/a.pdf" }],
}

// The payload buildSavePayload derives from BLOCK (files flattened to fileGuids, modifiedOn
// becoming the lastModifiedOn concurrency stamp, editPermissions carried through).
const EXPECTED_PUT_PAYLOAD = {
    contentBlockId: 7,
    content: "<p>hello</p>",
    title: "Welcome",
    system: "Viper",
    application: null,
    page: "home",
    viperSectionPath: "/apps",
    blockOrder: 2,
    friendlyName: "welcome",
    allowPublicAccess: false,
    permissions: ["SVMSecure.CMS"],
    editPermissions: ["SVMSecure.CMS.Delegate"],
    fileGuids: ["f1"],
    lastModifiedOn: "2024-03-01T12:00:00",
}

const UPLOADED_FILE = { fileGuid: "f2", friendlyName: "b.pdf", friendlyUrl: "/files/b.pdf" }

function routeGet(block: Record<string, unknown> = BLOCK) {
    mockGet.mockImplementation((...args: unknown[]) => {
        const url = args[0] as string
        if (url.endsWith("/folders")) {
            return Promise.resolve({ success: true, result: ["/apps", "/students"] })
        }
        if (url.includes("/history")) {
            return Promise.resolve({ success: true, result: [] })
        }
        if (url.includes("cms/files/")) {
            return Promise.resolve({ success: true, result: [] })
        }
        // The single-block load (.../content/7); deep-copied so tests can't cross-mutate BLOCK.
        return Promise.resolve({ success: true, result: structuredClone(block) })
    })
}

function blockLoadCount(): number {
    return mockGet.mock.calls.map((c) => c[0] as string).filter((u) => u.endsWith("CMS/content/7")).length
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

// The inline uploader stages files client-side; the page only calls its exposed commit() during
// save. Stub it with a controllable commit so the tests drive each staged-upload outcome.
const mockCommit = vi.fn<() => Promise<{ attached: unknown[]; createdGuids: string[] }>>()
const inlineUploadStub = {
    name: "InlineFileUpload",
    props: ["folder", "permissions", "allowPublicAccess"],
    emits: ["staged-count"],
    template: "<div class='inline-upload-stub' />",
    methods: {
        commit() {
            return mockCommit()
        },
    },
}

async function mountEdit(routeArgs: { params?: Record<string, string> } = { params: { id: "7" } }) {
    const router = createTestRouter()
    await router.push({ name: "CmsContentBlockEdit", params: routeArgs.params ?? {} })
    await router.isReady()
    const wrapper = mountCms(
        ContentBlockEdit,
        { global: { stubs: { QEditor: qEditorStub, InlineFileUpload: inlineUploadStub } } },
        router,
    )
    await flushPromises()
    await flushPromises()
    return { wrapper, router }
}

async function submitForm(wrapper: Awaited<ReturnType<typeof mountEdit>>["wrapper"]): Promise<void> {
    await wrapper.findComponent({ name: "QForm" }).find("form").trigger("submit")
    await flushPromises()
    await flushPromises()
}

// Quasar plugin dialogs ($q.dialog) and notifications teleport to document.body, outside the
// wrapper. Click the LAST match: a dismissed dialog's portal can linger mid-transition, so the
// newest dialog is always the live one. The body is never wiped between tests (Quasar caches its
// notification container there), so assertions use per-test-unique messages.
function clickBodyButton(label: string) {
    const btn = [...document.body.querySelectorAll("button")].filter((b) => b.textContent?.includes(label)).at(-1)
    expect(btn, `expected a "${label}" button in the dialog`).toBeTruthy()
    btn!.click()
}

beforeEach(() => {
    mockGet.mockReset()
    mockPost.mockReset()
    mockPut.mockReset()
    mockDel.mockReset()
    mockCommit.mockReset()
    mockCommit.mockResolvedValue({ attached: [], createdGuids: [] })
    routeGet()
})

describe("ContentBlockEdit.vue - save payload", () => {
    it("edit-mode save PUTs the full block payload with lastModifiedOn as the concurrency stamp", async () => {
        mockPut.mockResolvedValue({ success: true, result: { ...BLOCK } })
        const { wrapper } = await mountEdit()

        await submitForm(wrapper)

        expect(mockPut).toHaveBeenCalledOnce()
        const [url, payload] = mockPut.mock.calls[0]!
        expect(url).toContain("CMS/content/7")
        expect(payload).toEqual(EXPECTED_PUT_PAYLOAD)
        expect(document.body.textContent).toContain("Content block saved")
    })

    it("renders the Edit access permission selector for a manager", async () => {
        const { wrapper } = await mountEdit()
        const editAccess = wrapper
            .findAllComponents({ name: "QSelect" })
            .find((s) => s.props("label") === "Edit access")
        expect(editAccess).toBeTruthy()
    })

    it("create-mode save POSTs with a null lastModifiedOn and empty friendlyName collapsed to null", async () => {
        mockPost.mockResolvedValue({ success: true, result: { ...BLOCK, contentBlockId: 42, files: [] } })
        const { wrapper, router } = await mountEdit({ params: {} })

        wrapper.findAllComponents({ name: "QInput" })[0]!.vm.$emit("update:modelValue", "New block")
        wrapper
            .findAllComponents({ name: "QSelect" })
            .find((s) => s.props("label") === "VIPER section path")!
            .vm.$emit("update:modelValue", "/apps")
        // Let the v-models propagate back into the inputs' props before submit-time validation.
        await flushPromises()
        await submitForm(wrapper)

        expect(mockPost).toHaveBeenCalledOnce()
        const [, payload] = mockPost.mock.calls[0]!
        expect(payload).toMatchObject({
            contentBlockId: 0,
            title: "New block",
            viperSectionPath: "/apps",
            friendlyName: null,
            lastModifiedOn: null,
            fileGuids: [],
        })
        expect(document.body.textContent).toContain("Content block created")

        // Success navigates from the create form to the new block's edit route.
        await flushRouter()
        expect(router.currentRoute.value.params.id).toBe("42")
    })

    it("blocks submit and shows the required-fields banner when validation fails", async () => {
        const { wrapper } = await mountEdit({ params: {} })

        await submitForm(wrapper)

        expect(mockPost).not.toHaveBeenCalled()
        expect(wrapper.text()).toContain("Please complete the required fields before creating this content block.")
    })
})

describe("ContentBlockEdit.vue - staged upload commit", () => {
    it("commits staged uploads before the save and includes the new GUIDs in fileGuids", async () => {
        mockCommit.mockResolvedValue({ attached: [UPLOADED_FILE], createdGuids: ["f2"] })
        mockPut.mockResolvedValue({ success: true, result: { ...BLOCK } })
        const { wrapper } = await mountEdit()

        await submitForm(wrapper)

        expect(mockCommit.mock.invocationCallOrder[0]!).toBeLessThan(mockPut.mock.invocationCallOrder[0]!)
        const [, payload] = mockPut.mock.calls[0]!
        expect((payload as { fileGuids: string[] }).fileGuids).toEqual(["f1", "f2"])
    })

    it("aborts the save with a banner when a staged upload fails, leaving the block unsaved", async () => {
        mockCommit.mockRejectedValue(new Error("upload boom"))
        const { wrapper } = await mountEdit()

        await submitForm(wrapper)

        expect(mockPut).not.toHaveBeenCalled()
        expect(wrapper.text()).toContain("upload boom")
    })

    it("rolls back freshly-created files and shows the error when the save fails", async () => {
        mockCommit.mockResolvedValue({ attached: [UPLOADED_FILE], createdGuids: ["f2"] })
        mockPut.mockResolvedValue({ success: false, errors: ["Friendly name already in use"] })
        const { wrapper } = await mountEdit()

        await submitForm(wrapper)

        // The new upload is soft-deleted and detached again; the pre-existing file stays.
        expect(mockDel).toHaveBeenCalledOnce()
        expect(mockDel.mock.calls[0]![0]).toContain("CMS/content/7/files/f2")
        expect(wrapper.text()).toContain("Friendly name already in use")
        expect(wrapper.text()).toContain("a.pdf")
        expect(wrapper.text()).not.toContain("b.pdf")
        expect(document.body.textContent).not.toContain("Edit Conflict")
    })
})

describe("ContentBlockEdit.vue - 409 conflict handling", () => {
    it("rolls back new files and offers to reload the latest version on a 409", async () => {
        mockCommit.mockResolvedValue({ attached: [UPLOADED_FILE], createdGuids: ["f2"] })
        mockPut.mockResolvedValue({ success: false, status: 409, errors: ["Someone else saved this block."] })
        const { wrapper } = await mountEdit()
        const loadsBefore = blockLoadCount()

        await submitForm(wrapper)

        expect(mockDel.mock.calls[0]![0]).toContain("CMS/content/7/files/f2")
        expect(document.body.textContent).toContain("Edit Conflict")
        expect(document.body.textContent).toContain("Someone else saved this block.")

        clickBodyButton("Reload")
        await flushPromises()
        await flushPromises()
        expect(blockLoadCount()).toBe(loadsBefore + 1)
    })

    it("keeps the user's edits when they dismiss the conflict dialog", async () => {
        mockPut.mockResolvedValue({ success: false, status: 409, errors: ["Someone else saved this block."] })
        const { wrapper } = await mountEdit()
        const loadsBefore = blockLoadCount()

        await submitForm(wrapper)
        clickBodyButton("Keep editing")
        await flushPromises()

        expect(blockLoadCount()).toBe(loadsBefore)
    })
})

// Like routeGet, but the attachable-files endpoint returns candidates including one already-attached
// file (f1) and one new candidate (f9). The endpoint's slim shape omits the URL.
function routeGetWithFileSearch() {
    mockGet.mockImplementation((...args: unknown[]) => {
        const url = args[0] as string
        if (url.includes("attachable-files")) {
            return Promise.resolve({
                success: true,
                result: [
                    { fileGuid: "f1", friendlyName: "a.pdf" },
                    { fileGuid: "f9", friendlyName: "new.pdf" },
                ],
            })
        }
        if (url.includes("/history")) {
            return Promise.resolve({ success: true, result: [] })
        }
        return Promise.resolve({ success: true, result: structuredClone(BLOCK) })
    })
}

describe("ContentBlockEdit.vue - attached files", () => {
    it("searches attachable files (excluding already-attached ones) and attaches the selection", async () => {
        routeGetWithFileSearch()
        const { wrapper } = await mountEdit()

        const attachSelect = wrapper
            .findAllComponents({ name: "QSelect" })
            .find((s) => s.props("label") === "Attach a file")!
        attachSelect.vm.$emit("filter", "pdf", (fn: () => void) => fn())
        await flushPromises()
        await flushPromises()

        // The block-scoped attachable-files endpoint (not the global file catalog) backs the search.
        const fileSearch = mockGet.mock.calls.map((c) => c[0] as string).find((u) => u.includes("attachable-files"))
        expect(fileSearch).toContain("search=pdf")
        expect(fileSearch).toContain("contentBlockId=7")
        // The already-attached f1 is excluded from the options.
        const options = attachSelect.props("options") as { fileGuid: string }[]
        expect(options.map((o) => o.fileGuid)).toEqual(["f9"])

        attachSelect.vm.$emit("update:modelValue", options[0])
        await flushPromises()
        expect(wrapper.text()).toContain("new.pdf")
    })

    it("detaching a file removes it from the attachment list and the next save payload", async () => {
        mockPut.mockResolvedValue({ success: true, result: { ...BLOCK } })
        const { wrapper } = await mountEdit()

        await wrapper.find("[aria-label='Detach file']").trigger("click")
        expect(wrapper.text()).not.toContain("a.pdf")

        await submitForm(wrapper)
        const [, payload] = mockPut.mock.calls[0]!
        expect((payload as { fileGuids: string[] }).fileGuids).toEqual([])
    })
})

describe("ContentBlockEdit.vue - system/public-access coupling and restore", () => {
    it("switching the system to Public auto-enables public access and explains why", async () => {
        const { wrapper } = await mountEdit()

        wrapper
            .findAllComponents({ name: "QSelect" })
            .find((s) => s.props("label") === "System")!
            .vm.$emit("update:modelValue", "Public")
        await flushPromises()

        expect(wrapper.findComponent({ name: "QToggle" }).props("modelValue")).toBeTruthy()
        expect(wrapper.text()).toContain("we turned on Public access")
    })

    it("restores a deleted block via the banner action and reloads it", async () => {
        routeGet({ ...BLOCK, deletedOn: "2024-04-01T00:00:00" })
        mockPost.mockResolvedValue({ success: true, result: null })
        const { wrapper } = await mountEdit()
        const loadsBefore = blockLoadCount()
        expect(wrapper.text()).toContain("marked as deleted")

        await wrapper
            .findAllComponents({ name: "QBtn" })
            .find((b) => b.text().includes("Restore"))!
            .trigger("click")
        await flushPromises()
        await flushPromises()

        expect(mockPost.mock.calls[0]![0]).toContain("CMS/content/7/restore")
        expect(document.body.textContent).toContain("Content block restored")
        expect(blockLoadCount()).toBe(loadsBefore + 1)
    })
})
