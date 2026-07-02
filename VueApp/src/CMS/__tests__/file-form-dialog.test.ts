import FileFormDialog from "@/CMS/components/FileFormDialog.vue"
import type { CmsFile } from "@/CMS/types"
import { mountCms, flushPromises } from "./test-utils"

/**
 * FileFormDialog covers add vs edit upload flows. Edit mode has no required file (replacing
 * is optional) and no folder field; add mode requires both a file and a folder. New uploads
 * first hit a check-name endpoint and prompt on conflict. These tests mock ViperFetch and
 * stub the async selectors, then assert validation gating and the submitted FormData payload.
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

// QDialog teleports content to document.body, so query there.
function bodyText(): string {
    return document.body.textContent ?? ""
}

// QDialog teleports its DOM, but the component tree (and so findComponent) still includes the
// teleported children. Trigger submit on the QForm's <form> via VTU so a real event is dispatched
// (calling QForm.submit() directly passes an undefined event and trips .prevent).
async function submitDialogForm(wrapper: ReturnType<typeof mountDialog>): Promise<void> {
    await wrapper.findComponent({ name: "QForm" }).find("form").trigger("submit")
}

describe("FileFormDialog.vue - add vs edit mode", () => {
    beforeEach(() => {
        mockGet.mockReset()
        mockPostForm.mockReset()
        mockPutForm.mockReset()
        document.body.innerHTML = ""
    })

    it("shows the Add File title and an Upload button in add mode", async () => {
        mountDialog({ modelValue: true, folders: ["Apps"], file: null })
        await flushPromises()
        expect(bodyText()).toContain("Add File")
        expect(bodyText()).toContain("Upload")
    })

    it("shows the Edit File title and the existing file link in edit mode", async () => {
        mountDialog({ modelValue: true, folders: ["Apps"], file: existingFile() })
        await flushPromises()
        expect(bodyText()).toContain("Edit File")
        expect(bodyText()).toContain("Save Changes")
        const link = [...document.querySelectorAll("a")].find((a) => a.getAttribute("href") === "/Apps/report.pdf")
        expect(link).toBeTruthy()
    })

    it("renders the folder select only in add mode (not edit)", async () => {
        const add = mountDialog({ modelValue: true, folders: ["Apps", "Reports"], file: null })
        await flushPromises()
        expect(bodyText()).toContain("VIPER app (folder)")
        add.unmount()
        document.body.innerHTML = ""

        mountDialog({ modelValue: true, folders: ["Apps"], file: existingFile() })
        await flushPromises()
        expect(bodyText()).not.toContain("VIPER app (folder)")
    })

    it("applies the accepted-extensions allow-list to the file input", async () => {
        mountDialog({ modelValue: true, folders: ["Apps"], file: null })
        await flushPromises()
        const fileInput = document.querySelector("input[type='file']") as HTMLInputElement | null
        expect(fileInput).toBeTruthy()
        const accept = fileInput!.getAttribute("accept") ?? ""
        expect(accept).toContain(".pdf")
        expect(accept).toContain(".docx")
        expect(accept).toContain(".zip")
        // A dangerous extension that is NOT allowed should be absent.
        expect(accept).not.toContain(".bat")
    })
})

describe("FileFormDialog.vue - required-field enforcement (add mode)", () => {
    beforeEach(() => {
        mockGet.mockReset()
        mockPostForm.mockReset()
        mockPutForm.mockReset()
        document.body.innerHTML = ""
    })

    it("does not call check-name or postForm when add-mode submit has no file/folder", async () => {
        const wrapper = mountDialog({ modelValue: true, folders: ["Apps"], file: null })
        await flushPromises()
        // Submit the empty form: greedy validation blocks save(), and save() also early-returns
        // because there is no upload/folder, so no network calls are made.
        await submitDialogForm(wrapper)
        await flushPromises()
        expect(mockGet).not.toHaveBeenCalled()
        expect(mockPostForm).not.toHaveBeenCalled()
        // The validation-error handler surfaces a banner message.
        expect(bodyText()).toContain("Please choose a file")
    })
})

describe("FileFormDialog.vue - save payload", () => {
    beforeEach(() => {
        mockGet.mockReset()
        mockPostForm.mockReset()
        mockPutForm.mockReset()
        document.body.innerHTML = ""
    })

    it("edit-mode save PUTs FormData to the file's guid URL and emits saved", async () => {
        const file = existingFile({ description: "Updated desc" })
        mockPutForm.mockResolvedValue({ success: true, result: file })
        // The form is populated by the open watcher (modelValue false -> true), so open via a
        // prop toggle rather than mounting already-open, which would leave the form empty.
        const wrapper = mountDialog({ modelValue: false, folders: ["Apps"], file })
        await wrapper.setProps({ modelValue: true })
        await flushPromises()

        await submitDialogForm(wrapper)
        await flushPromises()

        expect(mockPutForm).toHaveBeenCalledOnce()
        const [url, body] = mockPutForm.mock.calls[0]!
        expect(url).toContain("cms/files/guid-123")
        expect(body).toBeInstanceOf(FormData)
        // Edit mode carries description/flags but not folder.
        expect((body as FormData).get("description")).toBe("Updated desc")
        expect((body as FormData).has("folder")).toBeFalsy()
        expect((body as FormData).get("allowPublicAccess")).toBe("false")
        expect((body as FormData).get("encrypt")).toBe("false")
        expect(wrapper.emitted("saved")).toBeTruthy()
        expect(wrapper.emitted("update:modelValue")?.at(-1)).toEqual([false])
    })

    it("edit-mode save surfaces the server error on the form banner and does not emit saved", async () => {
        mockPutForm.mockResolvedValue({ success: false, errors: ["Name already taken"] })
        const wrapper = mountDialog({ modelValue: true, folders: ["Apps"], file: existingFile() })
        await flushPromises()

        await submitDialogForm(wrapper)
        await flushPromises()

        expect(bodyText()).toContain("Name already taken")
        expect(wrapper.emitted("saved")).toBeFalsy()
    })
})
