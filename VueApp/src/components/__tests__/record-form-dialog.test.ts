import { mount, flushPromises } from "@vue/test-utils"
import { Quasar } from "quasar"
import RecordFormDialog from "../RecordFormDialog.vue"

/**
 * RecordFormDialog is the shared shell behind PhoneListAddRecordDialog, SVMAddRecordDialog, and
 * SVMAddFrequentNumberDialog. This file tests functionality involving
 * closing and canceling these dialogs.
 */

function mountDialog(props: Partial<InstanceType<typeof RecordFormDialog>["$props"]> = {}) {
    return mount(RecordFormDialog, {
        props: {
            modelValue: true,
            titleId: "test-dialog-title",
            title: "Test Dialog",
            isEdit: false,
            saving: false,
            formError: "",
            submitLabel: "Upload",
            ...props,
        },
        global: { plugins: [Quasar] },
        attachTo: document.body,
    })
}

function findButtonByLabel(wrapper: ReturnType<typeof mountDialog>, label: string) {
    return wrapper.findAllComponents({ name: "QBtn" }).find((btn) => btn.props("label") === label)
}

// QDialog teleports to a Quasar-managed portal outside the mounted wrapper, and its @keydown.escape
// listener sits on the teleported [role="dialog"] element - a plain VTU .trigger() on the wrapper
// doesn't reach it, so dispatch a real KeyboardEvent there instead.
async function pressEscape(): Promise<void> {
    const dialogEl = document.querySelector('[role="dialog"]')
    dialogEl?.dispatchEvent(new KeyboardEvent("keydown", { key: "Escape", bubbles: true, cancelable: true }))
    await flushPromises()
}

describe("recordFormDialog.vue - close behavior", () => {
    it("emits update:modelValue false when Cancel is clicked, with no confirmClose guard", async () => {
        expect.hasAssertions()
        document.body.innerHTML = ""
        const wrapper = mountDialog()
        await flushPromises()

        await findButtonByLabel(wrapper, "Cancel")!.trigger("click")

        expect(wrapper.emitted("update:modelValue")).toStrictEqual([[false]])
    })

    it("emits update:modelValue false when the header close button is clicked", async () => {
        expect.hasAssertions()
        document.body.innerHTML = ""
        const wrapper = mountDialog()
        await flushPromises()

        const closeButton = wrapper.findAllComponents({ name: "QBtn" }).find((btn) => btn.props("icon") === "close")
        expect(closeButton).toBeTruthy()
        await closeButton!.trigger("click")

        expect(wrapper.emitted("update:modelValue")).toStrictEqual([[false]])
    })

    it("stays open when confirmClose resolves false", async () => {
        expect.hasAssertions()
        document.body.innerHTML = ""
        const confirmClose = vi.fn<() => Promise<boolean>>().mockResolvedValue(false)
        const wrapper = mountDialog({ confirmClose })
        await flushPromises()

        await findButtonByLabel(wrapper, "Cancel")!.trigger("click")

        expect(confirmClose).toHaveBeenCalledOnce()
        expect(wrapper.emitted("update:modelValue")).toBeFalsy()
    })

    it("closes when confirmClose resolves true", async () => {
        expect.hasAssertions()
        document.body.innerHTML = ""
        const confirmClose = vi.fn<() => Promise<boolean>>().mockResolvedValue(true)
        const wrapper = mountDialog({ confirmClose })
        await flushPromises()

        await findButtonByLabel(wrapper, "Cancel")!.trigger("click")

        expect(wrapper.emitted("update:modelValue")).toStrictEqual([[false]])
    })

    it("closes on Escape without a confirmClose guard", async () => {
        expect.hasAssertions()
        document.body.innerHTML = ""
        const wrapper = mountDialog()
        await flushPromises()

        await pressEscape()

        expect(wrapper.emitted("update:modelValue")).toStrictEqual([[false]])
    })

    it("stays open on Escape when confirmClose resolves false", async () => {
        expect.hasAssertions()
        document.body.innerHTML = ""
        const confirmClose = vi.fn<() => Promise<boolean>>().mockResolvedValue(false)
        const wrapper = mountDialog({ confirmClose })
        await flushPromises()

        await pressEscape()

        expect(confirmClose).toHaveBeenCalledOnce()
        expect(wrapper.emitted("update:modelValue")).toBeFalsy()
    })

    it("resets form validation and emits hide when the dialog reports hide", async () => {
        expect.hasAssertions()
        document.body.innerHTML = ""
        const wrapper = mountDialog()
        await flushPromises()
        const resetValidation = vi.fn<() => void>()
        // FormRef is the mounted QForm instance; stub its resetValidation to confirm onHide calls it,
        // without needing to actually fail validation first.
        wrapper.findComponent({ name: "QForm" }).vm.resetValidation = resetValidation

        await wrapper.findComponent({ name: "QDialog" }).vm.$emit("hide")

        expect(resetValidation).toHaveBeenCalledOnce()
        expect(wrapper.emitted("hide")).toBeTruthy()
    })
})
