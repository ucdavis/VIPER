import { mount, flushPromises } from "@vue/test-utils"
import { createRouter, createWebHistory } from "vue-router"
import { Quasar, Notify } from "quasar"
import { createPinia, setActivePinia } from "pinia"
import PhoneListAddRecordDialog from "../components/PhoneListAddRecordDialog.vue"
import { phoneListUnitService } from "../services/phone-list-unit-service"
import type { PhoneListDisplayRecord } from "../types/phone-list-phone-types"
import { apiResult } from "./test-utils"

/**
 * PhoneListAddRecordDialog covers add vs edit flows for a unit-scoped phone record. Add mode
 * requires an employee to be picked via PersonSelector (stubbed here, mirroring CMS's
 * FileFormDialog tests) before it will submit; edit mode has the employee pre-filled from
 * editData and no PersonSelector at all. These tests mount the real dialog + RecordFormDialog +
 * QForm so submission goes through the real validation gate, and mock the unit service so no
 * network call happens.
 */

vi.mock("../services/phone-list-unit-service", () => ({
    phoneListUnitService: {
        addUnitPersonData: vi.fn<(...args: unknown[]) => unknown>(),
        updateUnitPersonData: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))

const selectorStub = {
    props: ["modelValue", "label", "listCode"],
    emits: ["update:modelValue"],
    template: "<div class='selector-stub' />",
}

function mountDialog(props: {
    modelValue: boolean
    unit: { name: string; id: number }
    listCode: string
    editData?: PhoneListDisplayRecord | null
}) {
    const pinia = createPinia()
    setActivePinia(pinia)
    const router = createRouter({
        history: createWebHistory(),
        routes: [{ path: "/", component: { template: "<div />" } }],
    })
    return mount(PhoneListAddRecordDialog, {
        props,
        global: {
            plugins: [[Quasar, { plugins: { Notify } }], router, pinia],
            stubs: { PersonSelector: selectorStub },
        },
        attachTo: document.body,
    })
}

function editRecord(overrides: Partial<PhoneListDisplayRecord> = {}): PhoneListDisplayRecord {
    return {
        fullName: "Amy Smith",
        name: "Smith, Amy",
        employeeIam: "asmith",
        employeeMailId: "asmith",
        phone: "530-555-1000",
        directPhone: "530-555-2000",
        office: "Room 100",
        listFirst: false,
        unitPersonId: 7,
        unitId: 10,
        unitName: "Dean's Office",
        modifiedBy: "jdoe",
        modifiedDate: null,
        ...overrides,
    }
}

async function submitDialogForm(wrapper: ReturnType<typeof mountDialog>): Promise<void> {
    await flushPromises()
    await wrapper.findComponent({ name: "QForm" }).find("form").trigger("submit")
    await flushPromises()
}

function bodyText(): string {
    return document.body.textContent ?? ""
}

// QDialog teleports its content to document.body, so a wrapper-scoped find() misses it -
// query the document directly, mirroring the bodyText() helper above.
function personSelectorExists(): boolean {
    return document.querySelector(".selector-stub") !== null
}

function resetTestState(): void {
    vi.clearAllMocks()
    document.body.innerHTML = ""
}

describe("phoneListAddRecordDialog.vue - add vs edit mode", () => {
    it("shows the PersonSelector picker in add mode, and hides the employee/modified-by summary", async () => {
        expect.hasAssertions()
        resetTestState()
        mountDialog({ modelValue: true, unit: { name: "Dean's Office", id: 10 }, listCode: "VMDO" })
        await flushPromises()

        expect(bodyText()).toContain("Add Phone Record")
        expect(bodyText()).toContain("Upload")
        expect(personSelectorExists()).toBeTruthy()
        expect(bodyText()).not.toContain("Employee:")
        expect(bodyText()).not.toContain("Modified By:")
    })

    it("shows the employee/modified-by summary in edit mode, and hides the PersonSelector picker", async () => {
        expect.hasAssertions()
        resetTestState()
        mountDialog({
            modelValue: true,
            unit: { name: "Dean's Office", id: 10 },
            listCode: "VMDO",
            editData: editRecord(),
        })
        await flushPromises()

        expect(bodyText()).toContain("Edit Phone Record")
        expect(bodyText()).toContain("Employee: Smith, Amy")
        expect(bodyText()).toContain("Modified By: jdoe")
        expect(bodyText()).toContain("Save Changes")
        expect(personSelectorExists()).toBeFalsy()
    })

    it("blocks submit with a validation message when no employee is selected in add mode", async () => {
        expect.hasAssertions()
        resetTestState()
        const wrapper = mountDialog({ modelValue: true, unit: { name: "Dean's Office", id: 10 }, listCode: "VMDO" })

        await submitDialogForm(wrapper)

        expect(phoneListUnitService.addUnitPersonData).not.toHaveBeenCalled()
        expect(bodyText()).toContain("Please select an employee.")
    })

    it("submits the update payload and emits saved + close in edit mode", async () => {
        expect.hasAssertions()
        resetTestState()
        vi.mocked(phoneListUnitService.updateUnitPersonData).mockResolvedValue(apiResult({ result: true }))
        const wrapper = mountDialog({
            modelValue: true,
            unit: { name: "Dean's Office", id: 10 },
            listCode: "VMDO",
            editData: editRecord(),
        })

        await submitDialogForm(wrapper)

        expect(phoneListUnitService.updateUnitPersonData).toHaveBeenCalledWith(
            "VMDO",
            7,
            expect.objectContaining({ unitId: 10, employeeIam: "asmith", phone: "530-555-1000" }),
        )
        expect(wrapper.emitted("saved")).toBeTruthy()
        expect(wrapper.emitted("update:modelValue")).toContainEqual([false])
    })
})
