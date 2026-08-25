import { mount, flushPromises } from "@vue/test-utils"
import { Quasar, Notify } from "quasar"
import SVMAddFrequentNumberDialog from "../components/SVMAddFrequentNumberDialog.vue"
import { svmFrequentNumberService } from "../services/svm-frequent-number-service"
import type { SVMFrequentNumberRecord } from "../types/svm-phone-types"
import { apiResult } from "./test-utils"

/**
 * SVMAddFrequentNumberDialog is the one add/edit dialog with no custom validate() -
 * useAddRecordDialog's validate always returns null, so both required fields (label, phone) are
 * enforced entirely by QForm's own :rules. That's a different validation path than
 * PhoneListAddRecordDialog/SVMAddRecordDialog exercise, where a custom validate() blocks submit.
 */

vi.mock("../services/svm-frequent-number-service", () => ({
    svmFrequentNumberService: {
        addFrequentNumber: vi.fn<(...args: unknown[]) => unknown>(),
        updateFrequentNumber: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))

function mountDialog(props: { modelValue: boolean; editFrequentData?: SVMFrequentNumberRecord | null }) {
    return mount(SVMAddFrequentNumberDialog, {
        props,
        global: { plugins: [[Quasar, { plugins: { Notify } }]] },
        attachTo: document.body,
    })
}

async function submitDialogForm(wrapper: ReturnType<typeof mountDialog>): Promise<void> {
    await flushPromises()
    await wrapper.findComponent({ name: "QForm" }).find("form").trigger("submit")
    await flushPromises()
}

function bodyText(): string {
    return document.body.textContent ?? ""
}

function resetTestState(): void {
    vi.clearAllMocks()
    document.body.innerHTML = ""
}

describe("sVMAddFrequentNumberDialog.vue - add vs edit mode", () => {
    it("shows the Add Frequent Number title and an Upload button in add mode", async () => {
        expect.hasAssertions()
        resetTestState()
        mountDialog({ modelValue: true })
        await flushPromises()

        expect(bodyText()).toContain("Add Frequent Number")
        expect(bodyText()).toContain("Upload")
    })

    it("shows the Edit Frequent Number title, pre-filled fields, and a Save Changes button in edit mode", async () => {
        expect.hasAssertions()
        resetTestState()
        const wrapper = mountDialog({
            modelValue: true,
            editFrequentData: { label: "Front Desk", phone: "530-555-1000", entryId: 5 },
        })
        await flushPromises()

        expect(bodyText()).toContain("Edit Frequent Number")
        expect(bodyText()).toContain("Save Changes")
        const inputs = wrapper.findAllComponents({ name: "QInput" })
        expect(inputs[0]!.props("modelValue")).toBe("Front Desk")
        expect(inputs[1]!.props("modelValue")).toBe("530-555-1000")
    })

    it("blocks submit via QForm's own field rules when required fields are empty", async () => {
        expect.hasAssertions()
        resetTestState()
        // No custom validate() exists on this dialog (unlike the other two), so this exercises
        // QForm's native :rules gate instead of useAddRecordDialog's validate-then-block path.
        const wrapper = mountDialog({ modelValue: true })

        await submitDialogForm(wrapper)

        expect(svmFrequentNumberService.addFrequentNumber).not.toHaveBeenCalled()
        expect(bodyText()).toContain("Please complete the required fields before saving.")
    })

    it("submits the update payload and emits saved + close in edit mode", async () => {
        expect.hasAssertions()
        resetTestState()
        vi.mocked(svmFrequentNumberService.updateFrequentNumber).mockResolvedValue(apiResult({ result: true }))
        const wrapper = mountDialog({
            modelValue: true,
            editFrequentData: { label: "Front Desk", phone: "530-555-1000", entryId: 5 },
        })

        await submitDialogForm(wrapper)

        expect(svmFrequentNumberService.updateFrequentNumber).toHaveBeenCalledWith(5, {
            label: "Front Desk",
            phone: "530-555-1000",
            entryId: 5,
        })
        expect(wrapper.emitted("saved")).toBeTruthy()
        expect(wrapper.emitted("update:modelValue")).toContainEqual([false])
    })
})
