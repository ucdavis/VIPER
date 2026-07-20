import InlineFileUpload from "@/CMS/components/InlineFileUpload.vue"
import type { CmsFile } from "@/CMS/types"
import { mountCms, flushPromises } from "./test-utils"

/**
 * InlineFileUpload stages files client-side (validating the extension and checking the name for
 * conflicts) and only creates anything server-side when the parent calls the exposed commit().
 * Each staged file commits per its chosen conflict action: new upload / rename (POST, rolled back
 * on later failure), overwrite-in-place (PUT, never rolled back), or attach-existing (GET only).
 * Mock ViperFetch; the file picker (VueUse useFileDialog) is mocked at the browser boundary so
 * tests can hand the component a File directly.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockPostForm = vi.fn<(...args: unknown[]) => unknown>()
const mockPutForm = vi.fn<(...args: unknown[]) => unknown>()
const mockDel = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        postForm: (...args: unknown[]) => mockPostForm(...args),
        putForm: (...args: unknown[]) => mockPutForm(...args),
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

// Mock the file dialog at the browser boundary: capture the onChange callback so tests can
// "pick" a file. The drop zone needs real pointer events, so it is inert here (staging via
// drag and via the picker share stageFile).
const fileDialogState = vi.hoisted(() => ({
    onChange: undefined as ((files: File[] | null) => void) | undefined,
    open: (() => undefined) as () => void,
}))
vi.mock("@vueuse/core", async (importOriginal) => {
    const actual = await importOriginal<typeof import("@vueuse/core")>()
    const { ref } = await import("vue")
    return {
        ...actual,
        useFileDialog: () => ({
            open: fileDialogState.open,
            onChange: (cb: (files: File[] | null) => void) => {
                fileDialogState.onChange = cb
            },
        }),
        useDropZone: () => ({ isOverDropZone: ref(false) }),
    }
})

type NameCheck = {
    inUse: boolean
    suggestedName: string
    existingFileGuid: string | null
    existingFriendlyName: string | null
    existingDeleted: boolean
}

const NO_CONFLICT: NameCheck = {
    inUse: false,
    suggestedName: "",
    existingFileGuid: null,
    existingFriendlyName: null,
    existingDeleted: false,
}

const CONFLICT: NameCheck = {
    inUse: true,
    suggestedName: "report_1.pdf",
    existingFileGuid: "eg1",
    existingFriendlyName: "report.pdf",
    existingDeleted: false,
}

function cmsFile(fileGuid: string, friendlyName: string): CmsFile {
    return {
        fileGuid,
        fileName: friendlyName,
        folder: "Apps",
        friendlyName,
        encrypted: false,
        description: "",
        allowPublicAccess: false,
        oldUrl: null,
        modifiedOn: "2024-01-01T00:00:00",
        modifiedBy: "u",
        deletedOn: null,
        purgeOn: null,
        permissions: [],
        people: [],
        url: `/files/${fileGuid}`,
        friendlyUrl: `/Apps/${friendlyName}`,
    }
}

// Routes the check-name probe; other get() calls (attach-existing) are configured per test.
function routeCheckName(check: NameCheck) {
    mockGet.mockImplementation((...args: unknown[]) => {
        const url = args[0] as string
        if (url.includes("check-name")) {
            return Promise.resolve({ success: true, result: check })
        }
        return Promise.resolve({ success: true, result: cmsFile("eg1", "report.pdf") })
    })
}

function mountUpload(
    props: Partial<{ folder: string | null; allowPublicAccess: boolean; contentBlockId: number | null }> = {},
) {
    return mountCms(InlineFileUpload, {
        props: {
            folder: "Apps",
            permissions: ["SVMSecure.CMS", "SVMSecure.CMS.AllFiles"],
            allowPublicAccess: false,
            ...props,
        },
    })
}

async function stage(fileName: string) {
    fileDialogState.onChange!([new File(["data"], fileName)])
    await flushPromises()
    await flushPromises()
}

function commitOf(wrapper: ReturnType<typeof mountUpload>) {
    return (wrapper.vm as unknown as { commit: () => Promise<{ attached: CmsFile[]; createdGuids: string[] }> }).commit
}

beforeEach(() => {
    mockGet.mockReset()
    mockPostForm.mockReset()
    mockPutForm.mockReset()
    mockDel.mockReset()
    routeCheckName(NO_CONFLICT)
})

describe("inlineFileUpload.vue - staging", () => {
    it("rejects a file type outside the allow-list without checking its name", async () => {
        const wrapper = mountUpload()
        await stage("script.bat")

        expect(wrapper.text()).toContain("Files of type .bat aren't allowed.")
        expect(wrapper.find(".staged-file").exists()).toBeFalsy()
        expect(mockGet).not.toHaveBeenCalled()
    })

    it("checks the name in the block's folder and stages an allowed file, reporting the count", async () => {
        const wrapper = mountUpload()
        await stage("report.pdf")

        const checkUrl = mockGet.mock.calls[0]![0] as string
        expect(checkUrl).toContain("check-name")
        expect(checkUrl).toContain("folder=Apps")
        expect(checkUrl).toContain("fileName=report.pdf")
        expect(wrapper.find(".staged-file").text()).toContain("report.pdf")
        expect(wrapper.emitted("staged-count")?.at(-1)).toStrictEqual([1])
    })

    it("offers rename, overwrite, and use-existing choices when the name belongs to an existing record", async () => {
        routeCheckName(CONFLICT)
        const wrapper = mountUpload()
        await stage("report.pdf")

        const options = wrapper.findComponent({ name: "QOptionGroup" }).props("options") as { label: string }[]
        expect(options).toHaveLength(3)
        expect(options[0]!.label).toContain("report_1.pdf")
        expect(options.map((o) => o.label).join(",")).toContain("Use the existing file instead")
    })

    it("omits the use-existing choice when the conflicting name has no file record", async () => {
        routeCheckName({ ...CONFLICT, existingFileGuid: null })
        const wrapper = mountUpload()
        await stage("report.pdf")

        const options = wrapper.findComponent({ name: "QOptionGroup" }).props("options") as { label: string }[]
        expect(options.map((o) => o.label)).toStrictEqual([
            "Upload under a new name (report_1.pdf)",
            "Overwrite the existing file",
        ])
    })

    it("removes a staged file and reports the new count", async () => {
        const wrapper = mountUpload()
        await stage("report.pdf")

        await wrapper.find("[aria-label='Remove report.pdf']").trigger("click")

        expect(wrapper.find(".staged-file").exists()).toBeFalsy()
        expect(wrapper.emitted("staged-count")?.at(-1)).toStrictEqual([0])
    })

    it("ignores picked files while no folder is set (staging is disabled)", async () => {
        const wrapper = mountUpload({ folder: null })
        await stage("report.pdf")

        expect(wrapper.find(".staged-file").exists()).toBeFalsy()
        expect(mockGet).not.toHaveBeenCalled()
        expect(wrapper.text()).toContain("Set a VIPER section path")
    })
})

describe("inlineFileUpload.vue - commit", () => {
    it("resolves empty without any requests when nothing is staged", async () => {
        const wrapper = mountUpload()
        await expect(commitOf(wrapper)()).resolves.toStrictEqual({ attached: [], createdGuids: [] })
        expect(mockPostForm).not.toHaveBeenCalled()
    })

    it("uploads a new file with the block's folder, permissions, and public flag, marking it created", async () => {
        mockPostForm.mockResolvedValue({ success: true, result: cmsFile("n1", "report.pdf") })
        const wrapper = mountUpload({ allowPublicAccess: true })
        await stage("report.pdf")

        const result = await commitOf(wrapper)()

        expect(mockPostForm).toHaveBeenCalledOnce()
        const [url, data] = mockPostForm.mock.calls[0]! as [string, FormData]
        expect(url).toContain("cms/files/")
        expect((data.get("file") as File).name).toBe("report.pdf")
        expect(data.get("folder")).toBe("Apps")
        expect(data.get("allowPublicAccess")).toBe("true")
        expect(data.getAll("permissions")).toStrictEqual(["SVMSecure.CMS", "SVMSecure.CMS.AllFiles"])
        expect(data.has("fileName")).toBeFalsy()
        expect(data.has("overwrite")).toBeFalsy()

        expect(result.attached.map((f) => f.fileGuid)).toStrictEqual(["n1"])
        expect(result.createdGuids).toStrictEqual(["n1"])
        // The staged list is cleared once everything committed.
        expect(wrapper.emitted("staged-count")?.at(-1)).toStrictEqual([0])
    })

    it("uploads under the suggested name when rename is chosen for a conflict", async () => {
        routeCheckName(CONFLICT)
        mockPostForm.mockResolvedValue({ success: true, result: cmsFile("n2", "report_1.pdf") })
        const wrapper = mountUpload()
        await stage("report.pdf")

        // Rename is the default conflict action.
        const result = await commitOf(wrapper)()

        const [, data] = mockPostForm.mock.calls[0]! as [string, FormData]
        expect(data.get("fileName")).toBe("report_1.pdf")
        expect(data.has("overwrite")).toBeFalsy()
        expect(result.createdGuids).toStrictEqual(["n2"])
    })

    it("overwrites the existing record in place via PUT and does not mark it for rollback", async () => {
        routeCheckName(CONFLICT)
        mockPutForm.mockResolvedValue({ success: true, result: cmsFile("eg1", "report.pdf") })
        const wrapper = mountUpload()
        await stage("report.pdf")

        wrapper.findComponent({ name: "QOptionGroup" }).vm.$emit("update:modelValue", "overwrite")
        const result = await commitOf(wrapper)()

        expect(mockPostForm).not.toHaveBeenCalled()
        const [url, data] = mockPutForm.mock.calls[0]! as [string, FormData]
        expect(url).toContain("cms/files/eg1")
        // Overwrite-in-place reuses the record; no folder/new-name fields.
        expect(data.has("folder")).toBeFalsy()
        expect(result.attached.map((f) => f.fileGuid)).toStrictEqual(["eg1"])
        expect(result.createdGuids).toStrictEqual([])
    })

    it("pOSTs with the overwrite flag when overwriting an on-disk file that has no record", async () => {
        routeCheckName({ ...CONFLICT, existingFileGuid: null })
        mockPostForm.mockResolvedValue({ success: true, result: cmsFile("n3", "report.pdf") })
        const wrapper = mountUpload()
        await stage("report.pdf")

        wrapper.findComponent({ name: "QOptionGroup" }).vm.$emit("update:modelValue", "overwrite")
        const result = await commitOf(wrapper)()

        expect(mockPutForm).not.toHaveBeenCalled()
        const [, data] = mockPostForm.mock.calls[0]! as [string, FormData]
        expect(data.get("overwrite")).toBe("true")
        // A NEW record was created for the orphan, so it is rollback-safe.
        expect(result.createdGuids).toStrictEqual(["n3"])
    })

    it("attaches the existing file without uploading when use-existing is chosen", async () => {
        routeCheckName(CONFLICT)
        const wrapper = mountUpload()
        await stage("report.pdf")

        wrapper.findComponent({ name: "QOptionGroup" }).vm.$emit("update:modelValue", "existing")
        const result = await commitOf(wrapper)()

        expect(mockPostForm).not.toHaveBeenCalled()
        expect(mockPutForm).not.toHaveBeenCalled()
        const fileFetch = mockGet.mock.calls.map((c) => c[0] as string).find((u) => !u.includes("check-name"))
        expect(fileFetch).toContain("cms/files/eg1")
        expect(result.attached.map((f) => f.fileGuid)).toStrictEqual(["eg1"])
        expect(result.createdGuids).toStrictEqual([])
    })

    it("aborts the commit with the load error when the existing file cannot be fetched", async () => {
        mockGet.mockImplementation((...args: unknown[]) => {
            const url = args[0] as string
            if (url.includes("check-name")) {
                return Promise.resolve({ success: true, result: CONFLICT })
            }
            return Promise.resolve({ success: false, errors: ["gone"] })
        })
        const wrapper = mountUpload()
        await stage("report.pdf")

        wrapper.findComponent({ name: "QOptionGroup" }).vm.$emit("update:modelValue", "existing")
        await expect(commitOf(wrapper)()).rejects.toThrow("gone")
    })

    it("rolls back files created earlier in the batch when a later upload fails, keeping them staged", async () => {
        mockPostForm
            .mockResolvedValueOnce({ success: true, result: cmsFile("n1", "a.pdf") })
            .mockResolvedValueOnce({ success: false, errors: ["disk full"] })
        mockDel.mockResolvedValue({ success: true, result: null })
        const wrapper = mountUpload()
        await stage("a.pdf")
        await stage("b.pdf")

        await expect(commitOf(wrapper)()).rejects.toThrow("disk full")

        // The first (created) upload is soft-deleted so nothing new is stranded.
        expect(mockDel).toHaveBeenCalledOnce()
        expect(mockDel.mock.calls[0]![0]).toContain("cms/files/n1")
        // The staged list is NOT cleared, so the user can retry the save.
        expect(wrapper.findAll(".staged-file")).toHaveLength(2)
    })

    it("keeps rolling back and rethrows the original error when a rollback delete fails", async () => {
        mockPostForm
            .mockResolvedValueOnce({ success: true, result: cmsFile("n1", "a.pdf") })
            .mockResolvedValueOnce({ success: true, result: cmsFile("n2", "b.pdf") })
            .mockResolvedValueOnce({ success: false, errors: ["disk full"] })
        // The first rollback delete rejects; it must not abort the remaining rollbacks nor mask
        // the upload error the caller sees.
        mockDel.mockRejectedValueOnce(new Error("delete blew up")).mockResolvedValue({ success: true, result: null })
        const wrapper = mountUpload()
        await stage("a.pdf")
        await stage("b.pdf")
        await stage("c.pdf")

        // The ORIGINAL upload error propagates, not the rollback delete error.
        await expect(commitOf(wrapper)()).rejects.toThrow("disk full")

        // Both created files were still attempted for rollback despite the first delete failing.
        expect(mockDel).toHaveBeenCalledTimes(2)
        const deleted = mockDel.mock.calls.map((c) => c[0] as string)
        expect(deleted.some((u) => u.includes("cms/files/n1"))).toBe(true)
        expect(deleted.some((u) => u.includes("cms/files/n2"))).toBe(true)
    })
})

// With a contentBlockId, the uploader targets the block-scoped files API a delegated editor can
// reach: check-name/upload/rollback all live under CMS/content/{id}/files, the folder + permissions
// come from the block (not the client), and the per-file overwrite-in-place / use-existing paths
// (which that API lacks) are unavailable.
describe("inlineFileUpload.vue - block-scoped (delegated) mode", () => {
    it("checks names and uploads through the block-scoped files API, sending only the file", async () => {
        mockPostForm.mockResolvedValue({ success: true, result: cmsFile("c1", "report.pdf") })
        const wrapper = mountUpload({ contentBlockId: 7 })
        await stage("report.pdf")

        const checkUrl = mockGet.mock.calls[0]![0] as string
        expect(checkUrl).toContain("CMS/content/7/files/check-name")
        // The block implies the folder, so it is not sent.
        expect(checkUrl).not.toContain("folder=")

        const result = await commitOf(wrapper)()

        const [url, data] = mockPostForm.mock.calls[0]! as [string, FormData]
        expect(url).toContain("CMS/content/7/files/")
        expect(data.has("folder")).toBeFalsy()
        expect(data.has("permissions")).toBeFalsy()
        expect(data.has("allowPublicAccess")).toBeFalsy()
        expect(result.createdGuids).toStrictEqual(["c1"])
    })

    it("rolls back through the block-scoped delete when a later upload fails", async () => {
        mockPostForm
            .mockResolvedValueOnce({ success: true, result: cmsFile("c1", "a.pdf") })
            .mockResolvedValueOnce({ success: false, errors: ["disk full"] })
        mockDel.mockResolvedValue({ success: true, result: null })
        const wrapper = mountUpload({ contentBlockId: 7 })
        await stage("a.pdf")
        await stage("b.pdf")

        await expect(commitOf(wrapper)()).rejects.toThrow("disk full")

        expect(mockDel.mock.calls[0]![0]).toContain("CMS/content/7/files/c1")
    })

    it("offers only rename and overwrite on a conflict (no use-existing) in block-scoped mode", async () => {
        routeCheckName(CONFLICT)
        const wrapper = mountUpload({ contentBlockId: 7 })
        await stage("report.pdf")

        const options = wrapper.findComponent({ name: "QOptionGroup" }).props("options") as { label: string }[]
        expect(options.map((o) => o.label)).toStrictEqual([
            "Upload under a new name (report_1.pdf)",
            "Overwrite the existing file",
        ])
    })

    it("uploads with the overwrite flag (POST, not PUT) when overwrite is chosen in block-scoped mode", async () => {
        routeCheckName(CONFLICT)
        mockPostForm.mockResolvedValue({ success: true, result: cmsFile("c2", "report.pdf") })
        const wrapper = mountUpload({ contentBlockId: 7 })
        await stage("report.pdf")

        wrapper.findComponent({ name: "QOptionGroup" }).vm.$emit("update:modelValue", "overwrite")
        const result = await commitOf(wrapper)()

        expect(mockPutForm).not.toHaveBeenCalled()
        const [url, data] = mockPostForm.mock.calls[0]! as [string, FormData]
        expect(url).toContain("CMS/content/7/files/")
        expect(data.get("overwrite")).toBe("true")
        expect(result.createdGuids).toStrictEqual(["c2"])
    })
})
