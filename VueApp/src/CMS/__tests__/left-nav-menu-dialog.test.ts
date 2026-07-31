import LeftNavMenuDialog from "@/CMS/components/LeftNavMenuDialog.vue"
import { mountCms, flushPromises } from "./test-utils"

/**
 * LeftNavMenuDialog adds a left-nav menu. The behaviors worth locking down: the menu-header
 * field is required (greedy validation blocks save), opening the dialog resets to an empty
 * form and captures the unsaved-changes baseline, and a successful POST emits created + closes.
 */

const mockPost = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        post: (...args: unknown[]) => mockPost(...args),
    }),
}))

function bodyText(): string {
    return document.body.textContent ?? ""
}

// QDialog teleports its DOM, but the component tree (and so findComponent) still includes the
// teleported children. Trigger submit on the QForm's <form> element via VTU so a real event is
// dispatched (calling QForm.submit() directly passes an undefined event and trips .prevent).
async function submitDialogForm(wrapper: ReturnType<typeof mountDialog>): Promise<void> {
    await wrapper.findComponent({ name: "QForm" }).find("form").trigger("submit")
}

function mountDialog(modelValue = true) {
    return mountCms(LeftNavMenuDialog, { props: { modelValue } })
}

describe("LeftNavMenuDialog.vue", () => {
    beforeEach(() => {
        mockPost.mockReset()
        document.body.innerHTML = ""
    })

    it("renders the Add Left-Nav Menu title and Create button when open", async () => {
        mountDialog(true)
        await flushPromises()
        expect(bodyText()).toContain("Add Left-Nav Menu")
        expect(bodyText()).toContain("Create Menu")
    })

    it("blocks save and shows the validation banner when the menu header is blank", async () => {
        const wrapper = mountDialog(true)
        await flushPromises()
        await submitDialogForm(wrapper)
        await flushPromises()
        expect(mockPost).not.toHaveBeenCalled()
        expect(bodyText()).toContain("Please complete the required fields")
    })

    it("POSTs the menu and emits created + close on success", async () => {
        mockPost.mockResolvedValue({ success: true, result: { leftNavMenuId: 42 } })
        const wrapper = mountDialog(true)
        await flushPromises()

        // The menu-header field is the first QInput the settings fields render.
        await wrapper.findComponent({ name: "QInput" }).setValue("Main Menu")
        await flushPromises()

        await submitDialogForm(wrapper)
        await flushPromises()

        expect(mockPost).toHaveBeenCalledOnce()
        const [url, payload] = mockPost.mock.calls[0]!
        expect(url).toContain("cms/left-navs")
        expect(payload).toMatchObject({ menuHeaderText: "Main Menu", system: "Viper" })
        // Blank optional fields are normalized to null.
        expect((payload as Record<string, unknown>).page).toBeNull()
        expect(wrapper.emitted("created")?.at(-1)).toEqual([42])
        expect(wrapper.emitted("update:modelValue")?.at(-1)).toEqual([false])
    })

    it("surfaces the server error and keeps the dialog open when POST fails", async () => {
        mockPost.mockResolvedValue({ success: false, errors: ["A menu with that name exists"] })
        const wrapper = mountDialog(true)
        await flushPromises()

        await wrapper.findComponent({ name: "QInput" }).setValue("Dupe")
        await flushPromises()

        await submitDialogForm(wrapper)
        await flushPromises()

        expect(bodyText()).toContain("A menu with that name exists")
        expect(wrapper.emitted("created")).toBeFalsy()
    })

    it("closes immediately (no guard prompt) when the form is unchanged", async () => {
        const wrapper = mountDialog(true)
        await flushPromises()
        // Close with a pristine, empty form: confirmClose resolves true with no dialog.
        await wrapper.findComponent({ name: "QBtn" }).trigger("click")
        await flushPromises()
        expect(wrapper.emitted("update:modelValue")?.at(-1)).toEqual([false])
        expect(bodyText()).not.toContain("Unsaved Changes")
    })
})
