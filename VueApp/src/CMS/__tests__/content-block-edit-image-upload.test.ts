import { mount } from "@vue/test-utils"
import { Quasar, Notify, Dialog } from "quasar"
import { createPinia, setActivePinia } from "pinia"
import ContentBlockEdit from "@/CMS/pages/ContentBlockEdit.vue"
import { useUserStore } from "@/store/UserStore"
import { mountCms, flushPromises, createTestRouter, clickBodyButton } from "./test-utils"

/**
 * ContentBlockEdit's image-insertion wiring for the editor's Insert Image dialog: imageOptions
 * (attached image-extension files, url made relative), uploadImage (uploads immediately through
 * the block-scoped or global CMS files API depending on edit/create mode), and rollback of
 * dialog uploads when the user discards their edits on route leave. RichTextEditor is stubbed
 * (its own image props are being added in parallel) so only the props ContentBlockEdit passes it
 * are under test. Mock ViperFetch; capture vue-router's onBeforeRouteLeave callback to drive it.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockPost = vi.fn<(...args: unknown[]) => unknown>()
const mockPut = vi.fn<(...args: unknown[]) => unknown>()
const mockPatch = vi.fn<(...args: unknown[]) => unknown>()
const mockDel = vi.fn<(...args: unknown[]) => unknown>()
const mockPostForm = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        post: (...args: unknown[]) => mockPost(...args),
        put: (...args: unknown[]) => mockPut(...args),
        patch: (...args: unknown[]) => mockPatch(...args),
        del: (...args: unknown[]) => mockDel(...args),
        postForm: (...args: unknown[]) => mockPostForm(...args),
        putForm: vi.fn<(...args: unknown[]) => unknown>(),
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

// The route-leave rollback guard is registered via onBeforeRouteLeave, not exposed by the page or
// invoked by our test router's programmatic navigation, so capture it to drive it directly.
let capturedLeaveGuard: (() => Promise<boolean>) | null = null
vi.mock("vue-router", async (importOriginal) => {
    const actual = await importOriginal<typeof import("vue-router")>()
    return {
        ...actual,
        onBeforeRouteLeave: (fn: () => Promise<boolean>) => {
            capturedLeaveGuard = fn
        },
    }
})

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

function routeGet(block: Record<string, unknown> = BLOCK) {
    mockGet.mockImplementation((...args: unknown[]) => {
        const url = args[0] as string
        if (url.endsWith("/folders")) {
            return Promise.resolve({ success: true, result: ["/apps", "/students"] })
        }
        if (url.includes("/history")) {
            return Promise.resolve({ success: true, result: [] })
        }
        return Promise.resolve({ success: true, result: structuredClone(block) })
    })
}

// RichTextEditor is being extended with these props in parallel; stub it so this page's own props
// are testable without depending on the real (not-yet-updated) component.
const richTextEditorStub = {
    name: "RichTextEditor",
    props: [
        "modelValue",
        "toolbar",
        "labelId",
        "imageOptions",
        "imageOptionsHint",
        "uploadImage",
        "uploadUnavailableHint",
    ],
    emits: ["update:modelValue"],
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

async function mountEdit(routeArgs: { params?: Record<string, string> } = { params: { id: "7" } }) {
    const router = createTestRouter()
    await router.push({ name: "CmsContentBlockEdit", params: routeArgs.params ?? {} })
    await router.isReady()
    const wrapper = mountCms(
        ContentBlockEdit,
        { global: { stubs: { RichTextEditor: richTextEditorStub, InlineFileUpload: inlineUploadStub } } },
        router,
    )
    await flushPromises()
    await flushPromises()
    return { wrapper, router }
}

// MountCms always seeds a full CMS admin (incl. AllFiles) before mounting, and canAccessFiles is
// captured once at setup time (not reactive), so a non-AllFiles create-mode user needs permissions
// seeded BEFORE mount - mirrors mountCms's own Quasar/Pinia/router/apiURL/stub wiring with a
// custom initial permission set instead.
async function mountEditWithPermissions(permissions: string[]) {
    const pinia = createPinia()
    setActivePinia(pinia)
    useUserStore().setPermissions(permissions)
    const router = createTestRouter()
    await router.push({ name: "CmsContentBlockEdit", params: {} })
    await router.isReady()
    const wrapper = mount(ContentBlockEdit, {
        global: {
            plugins: [[Quasar, { plugins: { Notify, Dialog } }], router, pinia],
            provide: { apiURL: import.meta.env.VITE_API_URL },
            stubs: { RichTextEditor: richTextEditorStub, InlineFileUpload: inlineUploadStub },
        },
    })
    await flushPromises()
    await flushPromises()
    return { wrapper, router }
}

async function submitForm(wrapper: Awaited<ReturnType<typeof mountEdit>>["wrapper"]): Promise<void> {
    await wrapper.findComponent({ name: "QForm" }).find("form").trigger("submit")
    await flushPromises()
    await flushPromises()
}

function getUploadImage(wrapper: Awaited<ReturnType<typeof mountEdit>>["wrapper"]) {
    return wrapper.findComponent(richTextEditorStub).props("uploadImage") as
        | ((file: File) => Promise<string>)
        | undefined
}

beforeEach(() => {
    mockGet.mockReset()
    mockPost.mockReset()
    mockPut.mockReset()
    mockPatch.mockReset()
    mockDel.mockReset()
    mockPostForm.mockReset()
    mockCommit.mockReset()
    mockCommit.mockResolvedValue({ attached: [], createdGuids: [] })
    capturedLeaveGuard = null
    routeGet()
})

describe("ContentBlockEdit.vue - uploadImage", () => {
    it("edit mode uploads through the block-scoped route with only the file field, attaches the result, and resolves to url", async () => {
        mockPostForm.mockResolvedValue({
            success: true,
            result: {
                fileGuid: "img1",
                friendlyName: "photo.png",
                url: "/2/CMS/Files?id=img1",
                friendlyUrl: "https://x/photo.png",
            },
        })
        const { wrapper } = await mountEdit()
        const uploadImage = getUploadImage(wrapper)!

        const result = await uploadImage(new File(["x"], "photo.png", { type: "image/png" }))

        expect(mockPostForm).toHaveBeenCalledOnce()
        const [url, data] = mockPostForm.mock.calls[0]!
        expect(url).toContain("CMS/content/7/files/")
        expect([...(data as FormData).keys()]).toStrictEqual(["file"])
        expect(result).toBe("/2/CMS/Files?id=img1")
        await flushPromises()
        expect(wrapper.text()).toContain("photo.png")
    })

    it("edit mode surfaces the server's conflict message verbatim and attaches nothing on failure", async () => {
        mockPostForm.mockResolvedValue({ success: false, errors: ["A file with the name a.png already exists."] })
        const { wrapper } = await mountEdit()
        const uploadImage = getUploadImage(wrapper)!

        await expect(uploadImage(new File(["x"], "a.png"))).rejects.toThrow(
            "A file with the name a.png already exists.",
        )
        await flushPromises()
        expect(wrapper.text()).not.toContain("a.png")
    })

    it("create mode with AllFiles and a chosen section path uploads through the global route with file, folder, allowPublicAccess, and permissions", async () => {
        mockPostForm.mockResolvedValue({
            success: true,
            result: {
                fileGuid: "img2",
                friendlyName: "hero.jpg",
                url: "/2/cms/files/img2",
                friendlyUrl: "https://x/hero.jpg",
            },
        })
        const { wrapper } = await mountEdit({ params: {} })
        wrapper
            .findAllComponents({ name: "QSelect" })
            .find((s) => s.props("label") === "VIPER section path")!
            .vm.$emit("update:modelValue", "/apps")
        wrapper
            .findAllComponents({ name: "QSelect" })
            .find((s) => s.props("label") === "Permissions")!
            .vm.$emit("update:modelValue", ["SVMSecure.CMS"])
        await flushPromises()

        const uploadImage = getUploadImage(wrapper)!
        await uploadImage(new File(["x"], "hero.jpg", { type: "image/jpeg" }))

        expect(mockPostForm).toHaveBeenCalledOnce()
        const [url, data] = mockPostForm.mock.calls[0]!
        const fd = data as FormData
        expect(url).toContain("cms/files/")
        expect(fd.has("file")).toBeTruthy()
        expect(fd.get("folder")).toBe("/apps")
        expect(fd.get("allowPublicAccess")).toBe("false")
        expect(fd.getAll("permissions")).toStrictEqual(["SVMSecure.CMS"])
    })

    it("create mode without a section path leaves uploadImage undefined with a path-first hint", async () => {
        const { wrapper } = await mountEdit({ params: {} })

        expect(getUploadImage(wrapper)).toBeUndefined()
        expect(wrapper.findComponent(richTextEditorStub).props("uploadUnavailableHint")).toBe(
            "Choose a VIPER section path first to upload images",
        )
    })

    it("create mode without AllFiles leaves uploadImage undefined with a save-first hint", async () => {
        const { wrapper } = await mountEditWithPermissions([
            "SVMSecure",
            "SVMSecure.CMS",
            "SVMSecure.CMS.CreateContentBlock",
        ])

        expect(getUploadImage(wrapper)).toBeUndefined()
        expect(wrapper.findComponent(richTextEditorStub).props("uploadUnavailableHint")).toBe(
            "Save the block first to upload images",
        )
    })
})

describe("ContentBlockEdit.vue - imageOptions", () => {
    it("keeps image-extension attached files with a url, relativizes a same-origin absolute url, and flags files skipped for an empty url", async () => {
        const { origin } = window.location
        routeGet({
            ...BLOCK,
            files: [
                { fileGuid: "a", friendlyName: "sec-photo.png", url: `${origin}/2/CMS/Files?id=a` },
                { fileGuid: "b", friendlyName: "sec-doc.pdf", url: "/2/CMS/Files?id=b" },
                { fileGuid: "c", friendlyName: "sec-new.jpg", url: "" },
            ],
        })
        const { wrapper } = await mountEdit()
        const editorStub = wrapper.findComponent(richTextEditorStub)

        expect(editorStub.props("imageOptions")).toStrictEqual([{ label: "sec-photo.png", value: "/2/CMS/Files?id=a" }])
        expect(editorStub.props("imageOptionsHint")).toBe("Save the block to use files you just attached")
    })
})

describe("ContentBlockEdit.vue - dialog upload rollback on route leave", () => {
    it("rolls back a dialog-uploaded file through the block-scoped route when the user confirms discard", async () => {
        mockPostForm.mockResolvedValue({
            success: true,
            result: {
                fileGuid: "img1",
                friendlyName: "photo.png",
                url: "/2/CMS/Files?id=img1",
                friendlyUrl: "https://x/photo.png",
            },
        })
        mockDel.mockResolvedValue({ success: true })
        const { wrapper } = await mountEdit()
        const uploadImage = getUploadImage(wrapper)!
        await uploadImage(new File(["x"], "photo.png"))

        expect(capturedLeaveGuard).toBeTypeOf("function")
        const guardPromise = capturedLeaveGuard!()
        await flushPromises()
        clickBodyButton("Discard Changes")
        const result = await guardPromise

        expect(result).toBeTruthy()
        expect(mockDel).toHaveBeenCalledOnce()
        expect(mockDel.mock.calls[0]![0]).toContain("CMS/content/7/files/img1")
    })

    it("deletes nothing on a later leave once a successful save has cleared the dialog uploads", async () => {
        mockPostForm.mockResolvedValue({
            success: true,
            result: {
                fileGuid: "img1",
                friendlyName: "photo.png",
                url: "/2/CMS/Files?id=img1",
                friendlyUrl: "https://x/photo.png",
            },
        })
        mockPut.mockResolvedValue({ success: true, result: { ...BLOCK } })
        const { wrapper } = await mountEdit()
        const uploadImage = getUploadImage(wrapper)!
        await uploadImage(new File(["x"], "photo.png"))

        await submitForm(wrapper)

        expect(capturedLeaveGuard).toBeTypeOf("function")
        const result = await capturedLeaveGuard!()

        expect(result).toBeTruthy()
        expect(mockDel).not.toHaveBeenCalled()
    })
})

describe("ContentBlockEdit.vue - dialog upload rollback on conflict reload", () => {
    it("keeps dialog uploads on Keep editing and rolls them back when the user reloads after a 409", async () => {
        mockPostForm.mockResolvedValue({
            success: true,
            result: {
                fileGuid: "img1",
                friendlyName: "photo.png",
                url: "/2/CMS/Files?id=img1",
                friendlyUrl: "https://x/photo.png",
            },
        })
        mockPut.mockResolvedValue({ success: false, status: 409, errors: ["Someone else saved this block."] })
        mockDel.mockResolvedValue({ success: true })
        const { wrapper } = await mountEdit()
        await getUploadImage(wrapper)!(new File(["x"], "photo.png"))

        await submitForm(wrapper)
        clickBodyButton("Keep editing")
        await flushPromises()
        expect(mockDel).not.toHaveBeenCalled()

        // Reload discards the unsaved edits, so the image uploaded for them goes too; otherwise it
        // would sit unattached in the block's folder with nothing left pointing at it.
        await submitForm(wrapper)
        clickBodyButton("Reload")
        await flushPromises()
        await flushPromises()
        expect(mockDel).toHaveBeenCalledOnce()
        expect(mockDel.mock.calls[0]![0]).toContain("CMS/content/7/files/img1")
    })
})
