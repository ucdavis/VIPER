import ContentBlockEdit from "@/CMS/pages/ContentBlockEdit.vue"
import { useUserStore } from "@/store/UserStore"
import { mountCms, flushPromises, createTestRouter } from "./test-utils"

/**
 * Delegated editing: a user WITHOUT ManageContentBlocks/CreateContentBlock who reaches an existing
 * block edits only its content and files. The settings sidebar collapses to a read-only summary
 * (no inputs, no permission selectors, no public-access toggle), the content editor and attached
 * files stay functional, and Save issues the content-only PATCH (with the same lastModifiedOn
 * concurrency stamp + 409 conflict dialog as the full save). Mock ViperFetch; the inline uploader
 * is stubbed with a controllable commit().
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockPost = vi.fn<(...args: unknown[]) => unknown>()
const mockPut = vi.fn<(...args: unknown[]) => unknown>()
const mockPatch = vi.fn<(...args: unknown[]) => unknown>()
const mockDel = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        post: (...args: unknown[]) => mockPost(...args),
        put: (...args: unknown[]) => mockPut(...args),
        patch: (...args: unknown[]) => mockPatch(...args),
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

function routeGet() {
    mockGet.mockImplementation((...args: unknown[]) => {
        const url = args[0] as string
        if (url.includes("/history")) {
            return Promise.resolve({ success: true, result: [] })
        }
        return Promise.resolve({ success: true, result: structuredClone(BLOCK) })
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

const mockCommit = vi.fn<() => Promise<{ attached: unknown[]; createdGuids: string[] }>>()
const inlineUploadStub = {
    name: "InlineFileUpload",
    props: ["folder", "permissions", "allowPublicAccess", "contentBlockId"],
    emits: ["staged-count"],
    template: "<div class='inline-upload-stub' />",
    methods: {
        commit() {
            return mockCommit()
        },
    },
}

// mountCms seeds a full CMS admin; drop to a delegated editor (holds a block edit permission but
// none of the CMS management permissions) so the page switches into content-only mode. canManage is
// a reactive computed, so the template re-renders after the permission change.
async function mountDelegated(permissions = ["SVMSecure", "SVMSecure.CMS", "SVMSecure.CMS.Delegate"]) {
    const router = createTestRouter()
    await router.push({ name: "CmsContentBlockEdit", params: { id: "7" } })
    await router.isReady()
    const wrapper = mountCms(
        ContentBlockEdit,
        { global: { stubs: { QEditor: qEditorStub, InlineFileUpload: inlineUploadStub } } },
        router,
    )
    useUserStore().setPermissions(permissions)
    await flushPromises()
    await flushPromises()
    return { wrapper, router }
}

async function submitForm(wrapper: Awaited<ReturnType<typeof mountDelegated>>["wrapper"]): Promise<void> {
    await wrapper.findComponent({ name: "QForm" }).find("form").trigger("submit")
    await flushPromises()
    await flushPromises()
}

beforeEach(() => {
    mockGet.mockReset()
    mockPost.mockReset()
    mockPut.mockReset()
    mockPatch.mockReset()
    mockDel.mockReset()
    mockCommit.mockReset()
    mockCommit.mockResolvedValue({ attached: [], createdGuids: [] })
    routeGet()
})

describe("ContentBlockEdit.vue - delegated (content-only) mode", () => {
    it("renders a read-only settings summary and hides the manager-only controls", async () => {
        const { wrapper } = await mountDelegated()

        // No editable Title/Page inputs, no System select, no permission selectors, no public toggle.
        const inputLabels = wrapper.findAllComponents({ name: "QInput" }).map((i) => i.props("label"))
        expect(inputLabels).not.toContain("Title")
        expect(inputLabels).not.toContain("Page")
        const selectLabels = wrapper.findAllComponents({ name: "QSelect" }).map((s) => s.props("label"))
        expect(selectLabels).not.toContain("System")
        expect(selectLabels).not.toContain("Permissions")
        expect(selectLabels).not.toContain("Edit access")
        expect(wrapper.findComponent({ name: "QToggle" }).exists()).toBeFalsy()

        // The identifying fields appear as plain text, and the content editor is still available.
        const summary = wrapper.find(".settings-summary")
        expect(summary.exists()).toBeTruthy()
        expect(summary.text()).toContain("/apps")
        expect(summary.text()).toContain("home")
        expect(wrapper.text()).toContain("Welcome")
        expect(wrapper.find("textarea").exists()).toBeTruthy()
    })

    it("treats a CreateContentBlock holder as delegated on an EXISTING block", async () => {
        // CreateContentBlock owns only the create flow; the update endpoint is manager-only,
        // so showing this user the manager editor would end in a 403 on save.
        const { wrapper } = await mountDelegated([
            "SVMSecure",
            "SVMSecure.CMS",
            "SVMSecure.CMS.CreateContentBlock",
            "SVMSecure.CMS.Delegate",
        ])
        expect(wrapper.find(".settings-summary").exists()).toBeTruthy()
        const selectLabels = wrapper.findAllComponents({ name: "QSelect" }).map((s) => s.props("label"))
        expect(selectLabels).not.toContain("Permissions")
        expect(selectLabels).not.toContain("Edit access")
    })

    it("keeps the attached-files controls functional for a delegated editor", async () => {
        const { wrapper } = await mountDelegated()

        // The existing attachment, the attach-by-search select, and the inline uploader all render.
        expect(wrapper.text()).toContain("a.pdf")
        const attach = wrapper.findAllComponents({ name: "QSelect" }).find((s) => s.props("label") === "Attach a file")
        expect(attach).toBeTruthy()
        expect(wrapper.findComponent({ name: "InlineFileUpload" }).exists()).toBeTruthy()
    })

    it("saves via the content PATCH endpoint with content, lastModifiedOn, and the attachment set", async () => {
        mockPatch.mockResolvedValue({ success: true, result: { ...BLOCK } })
        const { wrapper } = await mountDelegated()

        await submitForm(wrapper)

        expect(mockPatch).toHaveBeenCalledOnce()
        const [url, payload] = mockPatch.mock.calls[0]!
        expect(url).toContain("CMS/content/7/content")
        expect(payload).toEqual({
            content: "<p>hello</p>",
            lastModifiedOn: "2024-03-01T12:00:00",
            fileGuids: ["f1"],
        })
        // The full-save verbs are never used in delegated mode.
        expect(mockPut).not.toHaveBeenCalled()
        expect(mockPost).not.toHaveBeenCalled()
        expect(document.body.textContent).toContain('Saved "Welcome"')
    })

    it("opens the edit-conflict dialog when the content PATCH returns a 409", async () => {
        mockPatch.mockResolvedValue({ success: false, status: 409, errors: ["Someone else saved this block."] })
        const { wrapper } = await mountDelegated()

        await submitForm(wrapper)

        expect(document.body.textContent).toContain("Edit Conflict")
        expect(document.body.textContent).toContain("Someone else saved this block.")
    })
})
