import FileFormDialog from "@/CMS/components/FileFormDialog.vue"
import type { CmsFile } from "@/CMS/types"
import { mountCms, flushPromises } from "./test-utils"

/**
 * FileFormDialog upload outcomes: a clean add POSTs the form with folder/flags, a taken name
 * prompts for rename vs overwrite (PUT to the existing record's guid, or POST with the overwrite
 * flag when only an orphaned disk file exists), a failed conflict upload arrives as a toast with
 * the conflict dialog kept open, and copy-link notifies on success/failure.
 *
 * Separate from file-form-dialog.test.ts: these tests assert Quasar toasts, and that spec wipes
 * document.body between tests, which detaches Quasar's cached notification container. Here the
 * body is never wiped and every asserted message is unique per test.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockPostForm = vi.fn<(...args: unknown[]) => unknown>()
const mockPutForm = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        postForm: (...args: unknown[]) => mockPostForm(...args),
        putForm: (...args: unknown[]) => mockPutForm(...args),
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

// Stub the async selectors so they don't make their own fetches; their v-model still flows.
const selectorStub = {
    props: ["modelValue", "label"],
    emits: ["update:modelValue"],
    template: "<div class='selector-stub' />",
}

function existingFile(overrides: Partial<CmsFile> = {}): CmsFile {
    return {
        fileGuid: "guid-123",
        fileName: "report.pdf",
        folder: "Apps",
        friendlyName: "report.pdf",
        encrypted: false,
        description: "A report",
        allowPublicAccess: false,
        oldUrl: "/old/report.pdf",
        modifiedOn: "2024-01-01T00:00:00",
        modifiedBy: "u",
        deletedOn: null,
        purgeOn: null,
        permissions: ["SVMSecure.CMS"],
        people: [{ iamId: "iam1", name: "Person One" }],
        url: "/files/guid-123",
        friendlyUrl: "/Apps/report.pdf",
        ...overrides,
    }
}

function mountDialog(props: { modelValue: boolean; folders: string[]; file: CmsFile | null }) {
    return mountCms(FileFormDialog, {
        props,
        global: {
            stubs: { PermissionSelector: selectorStub, PersonSelector: selectorStub },
        },
    })
}

// QDialog and notifications teleport to document.body.
function bodyText(): string {
    return document.body.textContent ?? ""
}

async function submitDialogForm(wrapper: ReturnType<typeof mountDialog>): Promise<void> {
    await wrapper.findComponent({ name: "QForm" }).find("form").trigger("submit")
}

// Fills the add form (file + folder are the required fields) so a submit passes validation.
async function fillAddForm(wrapper: ReturnType<typeof mountDialog>, fileName = "report.pdf") {
    wrapper.findComponent({ name: "QFile" }).vm.$emit("update:modelValue", new File(["data"], fileName))
    wrapper.findComponent({ name: "QSelect" }).vm.$emit("update:modelValue", "Apps")
    await flushPromises()
}

// Click the LAST matching button: a dismissed dialog's portal can linger mid-transition, so the
// newest dialog is always the live one.
function clickBodyButton(label: string) {
    const btn = [...document.body.querySelectorAll("button")].filter((b) => b.textContent?.includes(label)).at(-1)
    expect(btn, `expected a "${label}" button`).toBeTruthy()
    btn!.click()
}

const NAME_FREE = { success: true, result: { inUse: false } }
const NAME_TAKEN = {
    success: true,
    result: {
        inUse: true,
        suggestedName: "report_1.pdf",
        existingFileGuid: "eg9",
        existingFriendlyName: "report.pdf",
        existingDeleted: false,
    },
}

beforeEach(() => {
    mockGet.mockReset()
    mockPostForm.mockReset()
    mockPutForm.mockReset()
})

describe("FileFormDialog.vue - add-mode upload", () => {
    it("checks the destination name, then POSTs the upload with folder, flags, and toggles", async () => {
        mockGet.mockResolvedValue(NAME_FREE)
        mockPostForm.mockResolvedValue({ success: true, result: existingFile() })
        const wrapper = mountDialog({ modelValue: true, folders: ["Apps"], file: null })
        await flushPromises()
        await fillAddForm(wrapper)
        const [publicToggle, encryptToggle] = wrapper.findAllComponents({ name: "QToggle" })
        publicToggle!.vm.$emit("update:modelValue", true)
        encryptToggle!.vm.$emit("update:modelValue", true)
        await flushPromises()

        await submitDialogForm(wrapper)
        await flushPromises()

        const checkUrl = mockGet.mock.calls[0]![0] as string
        expect(checkUrl).toContain("check-name")
        expect(checkUrl).toContain("folder=Apps")
        expect(checkUrl).toContain("fileName=report.pdf")

        const [, body] = mockPostForm.mock.calls[0]! as [string, FormData]
        expect((body.get("file") as File).name).toBe("report.pdf")
        expect(body.get("folder")).toBe("Apps")
        expect(body.get("allowPublicAccess")).toBe("true")
        expect(body.get("encrypt")).toBe("true")
        expect(body.has("fileName")).toBeFalsy()
        expect(body.has("overwrite")).toBeFalsy()

        expect(bodyText()).toContain("File uploaded")
        expect(wrapper.emitted("saved")).toBeTruthy()
        expect(wrapper.emitted("update:modelValue")?.at(-1)).toEqual([false])
    })
})

describe("FileFormDialog.vue - name conflict resolution", () => {
    async function openConflict() {
        mockGet.mockResolvedValue(NAME_TAKEN)
        const wrapper = mountDialog({ modelValue: true, folders: ["Apps"], file: null })
        await flushPromises()
        await fillAddForm(wrapper)
        await submitDialogForm(wrapper)
        await flushPromises()
        expect(bodyText()).toContain("File name already exists")
        return wrapper
    }

    it("prompts on a taken name instead of uploading, prefilling the suggested rename", async () => {
        const wrapper = await openConflict()
        expect(mockPostForm).not.toHaveBeenCalled()
        // Rename is the default choice and the input is prefilled with the server's suggestion.
        const renameInput = wrapper
            .findAllComponents({ name: "QInput" })
            .find((i) => i.props("label") === "New file name")!
        expect(renameInput.props("modelValue")).toBe("report_1.pdf")
    })

    it("uploads under the new name when rename is confirmed", async () => {
        const wrapper = await openConflict()
        mockPostForm.mockResolvedValue({ success: true, result: existingFile() })

        clickBodyButton("Upload with new name")
        await flushPromises()
        await flushPromises()

        const [, body] = mockPostForm.mock.calls[0]! as [string, FormData]
        expect(body.get("fileName")).toBe("report_1.pdf")
        expect(body.has("overwrite")).toBeFalsy()
        expect(bodyText()).toContain("File uploaded")
        expect(wrapper.emitted("saved")).toBeTruthy()
    })

    it("PUTs to the existing record's guid when overwrite is chosen for a managed file", async () => {
        const wrapper = await openConflict()
        mockPutForm.mockResolvedValue({ success: true, result: existingFile() })

        wrapper.findComponent({ name: "QOptionGroup" }).vm.$emit("update:modelValue", "overwrite")
        await flushPromises()
        clickBodyButton("Overwrite")
        await flushPromises()
        await flushPromises()

        expect(mockPostForm).not.toHaveBeenCalled()
        const [url, body] = mockPutForm.mock.calls[0]! as [string, FormData]
        expect(url).toContain("cms/files/eg9")
        // Overwriting a managed record replaces content in place; no folder/new-name fields.
        expect(body.has("folder")).toBeFalsy()
        expect(bodyText()).toContain("File overwritten")
    })

    it("POSTs with the overwrite flag when the conflicting name has no managed record", async () => {
        mockGet.mockResolvedValue({
            ...NAME_TAKEN,
            result: { ...NAME_TAKEN.result, existingFileGuid: null, existingFriendlyName: null },
        })
        const wrapper = mountDialog({ modelValue: true, folders: ["Apps"], file: null })
        await flushPromises()
        await fillAddForm(wrapper)
        await submitDialogForm(wrapper)
        await flushPromises()
        mockPostForm.mockResolvedValue({ success: true, result: existingFile() })

        wrapper.findComponent({ name: "QOptionGroup" }).vm.$emit("update:modelValue", "overwrite")
        await flushPromises()
        clickBodyButton("Overwrite")
        await flushPromises()
        await flushPromises()

        expect(mockPutForm).not.toHaveBeenCalled()
        const [, body] = mockPostForm.mock.calls[0]! as [string, FormData]
        expect(body.get("overwrite")).toBe("true")
    })

    it("reports a failed upload as a toast and keeps the conflict dialog open for a retry", async () => {
        const wrapper = await openConflict()
        mockPostForm.mockResolvedValue({ success: false, errors: ["disk full"] })

        clickBodyButton("Upload with new name")
        await flushPromises()
        await flushPromises()

        // The user is in the conflict sub-dialog, so the error arrives as a notification...
        expect(bodyText()).toContain("disk full")
        // ...and the conflict dialog stays open rather than stranding them on the form.
        expect(bodyText()).toContain("File name already exists")
        expect(wrapper.emitted("saved")).toBeFalsy()
    })
})

describe("FileFormDialog.vue - copy link", () => {
    function stubClipboard(writeText: (text: string) => Promise<void>) {
        Object.defineProperty(navigator, "clipboard", { value: { writeText }, configurable: true })
    }

    it("copies the friendly URL and confirms", async () => {
        const writeText = vi.fn<(text: string) => Promise<void>>().mockResolvedValue(undefined)
        stubClipboard(writeText)
        const wrapper = mountDialog({ modelValue: true, folders: ["Apps"], file: existingFile() })
        await flushPromises()

        await wrapper
            .findAllComponents({ name: "QBtn" })
            .find((b) => b.attributes("aria-label") === "Copy link")!
            .trigger("click")
        await flushPromises()

        expect(writeText).toHaveBeenCalledWith("/Apps/report.pdf")
        expect(bodyText()).toContain("Link copied")
    })

    it("notifies when the clipboard write fails", async () => {
        stubClipboard(vi.fn<(text: string) => Promise<void>>().mockRejectedValue(new Error("denied")))
        const wrapper = mountDialog({ modelValue: true, folders: ["Apps"], file: existingFile() })
        await flushPromises()

        await wrapper
            .findAllComponents({ name: "QBtn" })
            .find((b) => b.attributes("aria-label") === "Copy link")!
            .trigger("click")
        await flushPromises()

        expect(bodyText()).toContain("Failed to copy link")
    })
})
