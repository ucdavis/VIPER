import { mount, flushPromises } from "@vue/test-utils"
import { createRouter, createWebHistory } from "vue-router"
import { Quasar, Notify } from "quasar"
import { createPinia, setActivePinia } from "pinia"
import SVMAddRecordDialog from "../components/SVMAddRecordDialog.vue"
import { svmUnitService } from "../services/svm-unit-service"
import type { QSelectOption } from "quasar"
import type { SVMPhoneDisplayRecord, UnitAdminStaff, UnitFaxNumber, UnitOptions } from "../types/svm-phone-types"
import { apiResult } from "./test-utils"

/**
 * SVMAddRecordDialog covers add vs edit flows for a section-scoped SVM record. Unlike
 * PhoneListAddRecordDialog, the Unit field is a real Quasar q-select gated by its own :rules
 * (add mode only - edit mode swaps it for plain text), while the dean/director requirement is
 * enforced by useAddRecordDialog's custom validate(), same as the other dialogs. Editing lets
 * these tests reach that custom validation gate directly via editData, without needing to
 * simulate picking an option in the real q-select. These tests mount the real dialog +
 * RecordFormDialog + QForm and mock svmUnitService so no network call happens.
 */

vi.mock("../services/svm-unit-service", () => ({
    svmUnitService: {
        addUnitData: vi.fn<(...args: unknown[]) => unknown>(),
        updateUnitData: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))

const selectorStub = {
    props: ["modelValue", "label", "listCode"],
    emits: ["update:modelValue"],
    template: "<div class='selector-stub' />",
}

// Mirror the props the component actually declares, so a prop-shape change is a compile error
// here rather than a structural near-miss the mount silently accepts.
function mountDialog(props: {
    modelValue: boolean
    section: QSelectOption
    units: UnitOptions[]
    unitFaxNumbers: UnitFaxNumber[]
    unitAdminStaff?: UnitAdminStaff[]
    editData?: SVMPhoneDisplayRecord | null
}) {
    const { unitAdminStaff = [], ...rest } = props
    const pinia = createPinia()
    setActivePinia(pinia)
    const router = createRouter({
        history: createWebHistory(),
        routes: [{ path: "/", component: { template: "<div />" } }],
    })
    return mount(SVMAddRecordDialog, {
        props: { ...rest, unitAdminStaff },
        global: {
            plugins: [[Quasar, { plugins: { Notify } }], router, pinia],
            stubs: { PersonSelector: selectorStub },
        },
        attachTo: document.body,
    })
}

function editRecord(overrides: Partial<SVMPhoneDisplayRecord> = {}): SVMPhoneDisplayRecord {
    return {
        sectionName: "VMDO",
        unitName: "Dean's Office",
        unitId: 10,
        unitAbbrv: "DO",
        officeLocation: "Room 100",
        officeFax: "530-555-9999",
        deanDirectorFullName: "Dean Person",
        deanDirectorDisplayName: "Dean Person",
        deanDirectorInterim: null,
        deanDirectorIam: "dean01",
        deanDirectorUnitPersonId: 1,
        deanDirectorPhone: "530-555-1000",
        deanDirectorModifiedDate: null,
        deanDirectorModifiedBy: "jdoe",
        adminStaffFullName: "Staff Person",
        adminStaffDisplayName: "Staff Person",
        adminStaffInterim: null,
        adminStaffIam: "staff01",
        adminStaffUnitPersonId: 2,
        adminStaffPhone: "530-555-2000",
        adminStaffModifiedDate: null,
        adminStaffModifiedBy: "jdoe",
        entryId: 1,
        isOnlyRowForUnit: true,
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

function resetTestState(): void {
    vi.clearAllMocks()
    document.body.innerHTML = ""
}

describe("sVMAddRecordDialog.vue - add vs edit mode", () => {
    it("shows the Add Phone Record title, Add Record button, and the Unit q-select in add mode", async () => {
        expect.hasAssertions()
        resetTestState()
        const wrapper = mountDialog({
            modelValue: true,
            section: { label: "VMDO", value: "1" },
            units: [],
            unitFaxNumbers: [],
        })
        await flushPromises()

        expect(bodyText()).toContain("Add Phone Record")
        expect(bodyText()).toContain("Add Record")
        // The Unit q-select is add-mode-only; deanDirectorInterim/staffInterim selects are always
        // present, so 3 selects (vs. 2 in edit mode) confirms it rendered.
        expect(wrapper.findAllComponents({ name: "QSelect" })).toHaveLength(3)
    })

    it("hides the unit/modified-by summary text in add mode", async () => {
        expect.hasAssertions()
        resetTestState()
        mountDialog({
            modelValue: true,
            section: { label: "VMDO", value: "1" },
            units: [],
            unitFaxNumbers: [],
        })
        await flushPromises()

        expect(bodyText()).not.toContain("Unit: ")
        expect(bodyText()).not.toContain("Dean/Director Modified")
        expect(bodyText()).not.toContain("Admin Staff Modified")
    })

    it("autofills the fax from the picked unit and submits it under the selected unit in add mode", async () => {
        expect.hasAssertions()
        resetTestState()
        const wrapper = mountDialog({
            modelValue: true,
            section: { label: "VMDO", value: "1" },
            units: [{ section: 1, units: [{ label: "Dean's Office", value: "10" }] }],
            unitFaxNumbers: [{ unitId: 10, fax: "530-555-9999" }],
        })
        await flushPromises()

        // UnitId (number) vs. the q-select option's value (string "10"): a prior regression
        // compared these directly and always missed, silently skipping the fax autofill.
        await wrapper
            .findAllComponents({ name: "QSelect" })[0]!
            .vm.$emit("update:model-value", { label: "Dean's Office", value: "10" })
        await flushPromises()
        const [deanSelector] = wrapper.findAllComponents(selectorStub)
        await deanSelector!.vm.$emit("update:model-value", { iamId: "dean01", fullName: "Dean Person" })
        vi.mocked(svmUnitService.addUnitData).mockResolvedValue(apiResult({ result: true }))

        await submitDialogForm(wrapper)

        expect(svmUnitService.addUnitData).toHaveBeenCalledWith("10", expect.objectContaining({ fax: "530-555-9999" }))
    })
})

// Split from the describe above so neither function grows past the linter's line-count budget.
describe("sVMAddRecordDialog.vue - admin staff autofill in add mode", () => {
    it("autofills the admin staff from the picked unit's existing staff record in add mode", async () => {
        expect.hasAssertions()
        resetTestState()
        const wrapper = mountDialog({
            modelValue: true,
            section: { label: "VMDO", value: "1" },
            units: [{ section: 1, units: [{ label: "Dean's Office", value: "10" }] }],
            unitFaxNumbers: [],
            unitAdminStaff: [
                {
                    unitId: 10,
                    staffIam: "staff01",
                    staffFullName: "Staff Person",
                    staffPhone: "530-555-2000",
                    staffInterim: "Interim",
                    staffUnitPersonId: 2,
                },
            ],
        })
        await flushPromises()

        await wrapper
            .findAllComponents({ name: "QSelect" })[0]!
            .vm.$emit("update:model-value", { label: "Dean's Office", value: "10" })
        await flushPromises()

        // Adding another leader to a unit that already has staff should start the form with that
        // staff record - same as edit mode - rather than leaving it for the user to look up again.
        const [deanSelector, staffSelector] = wrapper.findAllComponents(selectorStub)
        expect(staffSelector!.props("modelValue")).toMatchObject({ iamId: "staff01", fullName: "Staff Person" })

        await deanSelector!.vm.$emit("update:model-value", { iamId: "dean01", fullName: "Dean Person" })
        vi.mocked(svmUnitService.addUnitData).mockResolvedValue(apiResult({ result: true }))
        await submitDialogForm(wrapper)

        expect(svmUnitService.addUnitData).toHaveBeenCalledWith(
            "10",
            expect.objectContaining({
                staffIam: "staff01",
                staffPhone: "530-555-2000",
                staffInterim: "Interim",
                staffUnitPerson: 2,
            }),
        )
    })

    it("clears the admin staff fields when switching to a unit with no existing staff record", async () => {
        expect.hasAssertions()
        resetTestState()
        const wrapper = mountDialog({
            modelValue: true,
            section: { label: "VMDO", value: "1" },
            units: [
                {
                    section: 1,
                    units: [
                        { label: "Dean's Office", value: "10" },
                        { label: "Anatomy", value: "20" },
                    ],
                },
            ],
            unitFaxNumbers: [],
            unitAdminStaff: [
                {
                    unitId: 10,
                    staffIam: "staff01",
                    staffFullName: "Staff Person",
                    staffPhone: "530-555-2000",
                    staffInterim: "",
                    staffUnitPersonId: 2,
                },
            ],
        })
        await flushPromises()
        const [unitSelect] = wrapper.findAllComponents({ name: "QSelect" })

        await unitSelect!.vm.$emit("update:model-value", { label: "Dean's Office", value: "10" })
        await flushPromises()
        await unitSelect!.vm.$emit("update:model-value", { label: "Anatomy", value: "20" })
        await flushPromises()

        // Otherwise unit 10's staff would silently be carried over and saved under unit 20.
        const [deanSelector, staffSelector] = wrapper.findAllComponents(selectorStub)
        expect(staffSelector!.props("modelValue")).toMatchObject({ iamId: "", fullName: "" })

        await deanSelector!.vm.$emit("update:model-value", { iamId: "dean02", fullName: "Other Dean" })
        vi.mocked(svmUnitService.addUnitData).mockResolvedValue(apiResult({ result: true }))
        await submitDialogForm(wrapper)

        expect(svmUnitService.addUnitData).toHaveBeenCalledWith(
            "20",
            expect.objectContaining({ staffIam: "", staffPhone: "", staffUnitPerson: -1 }),
        )
    })
})

describe("sVMAddRecordDialog.vue - edit mode", () => {
    it("shows the Edit Phone Record title, unit summary, and Save Changes button in edit mode", async () => {
        expect.hasAssertions()
        resetTestState()
        mountDialog({
            modelValue: true,
            section: { label: "VMDO", value: "1" },
            units: [],
            unitFaxNumbers: [],
            editData: editRecord(),
        })
        await flushPromises()

        expect(bodyText()).toContain("Edit Phone Record")
        expect(bodyText()).toContain("Unit: Dean's Office")
        expect(bodyText()).toContain("Save Changes")
    })

    it("shows the modified-by summary and hides the Unit q-select in edit mode", async () => {
        expect.hasAssertions()
        resetTestState()
        const wrapper = mountDialog({
            modelValue: true,
            section: { label: "VMDO", value: "1" },
            units: [],
            unitFaxNumbers: [],
            editData: editRecord(),
        })
        await flushPromises()

        expect(bodyText()).toContain("Dean/Director Modified")
        expect(bodyText()).toContain("Admin Staff Modified")
        expect(bodyText()).toContain("by jdoe")
        expect(wrapper.findAllComponents({ name: "QSelect" })).toHaveLength(2)
    })

    it("blocks submit with a validation message when the record has no leadership", async () => {
        expect.hasAssertions()
        resetTestState()
        const wrapper = mountDialog({
            modelValue: true,
            section: { label: "VMDO", value: "1" },
            units: [],
            unitFaxNumbers: [],
            editData: editRecord({ deanDirectorIam: "", deanDirectorFullName: "" }),
        })

        await submitDialogForm(wrapper)

        expect(svmUnitService.updateUnitData).not.toHaveBeenCalled()
        expect(bodyText()).toContain("Must specify leadership.")
    })

    it("submits the update payload and emits saved + close in edit mode", async () => {
        expect.hasAssertions()
        resetTestState()
        vi.mocked(svmUnitService.updateUnitData).mockResolvedValue(apiResult({ result: true }))
        const wrapper = mountDialog({
            modelValue: true,
            section: { label: "VMDO", value: "1" },
            units: [],
            unitFaxNumbers: [],
            editData: editRecord(),
        })

        await submitDialogForm(wrapper)

        expect(svmUnitService.updateUnitData).toHaveBeenCalledWith(
            10,
            expect.objectContaining({
                fax: "530-555-9999",
                location: "Room 100",
                deanIam: "dean01",
                deanPhone: "530-555-1000",
                deanUnitPerson: 1,
                staffIam: "staff01",
                staffPhone: "530-555-2000",
                staffUnitPerson: 2,
            }),
        )
        expect(wrapper.emitted("saved")).toBeTruthy()
        expect(wrapper.emitted("update:modelValue")).toContainEqual([false])
    })

    it("carries the interim wording through to the update payload when the record has one", async () => {
        expect.hasAssertions()
        resetTestState()
        vi.mocked(svmUnitService.updateUnitData).mockResolvedValue(apiResult({ result: true }))
        const wrapper = mountDialog({
            modelValue: true,
            section: { label: "VMDO", value: "1" },
            units: [],
            unitFaxNumbers: [],
            editData: editRecord({ deanDirectorInterim: "Interim", adminStaffInterim: "Vice" }),
        })

        await submitDialogForm(wrapper)

        expect(svmUnitService.updateUnitData).toHaveBeenCalledWith(
            10,
            expect.objectContaining({ deanInterim: "Interim", staffInterim: "Vice" }),
        )
    })

    it("submits blank interim values when the record carries none", async () => {
        expect.hasAssertions()
        resetTestState()
        vi.mocked(svmUnitService.updateUnitData).mockResolvedValue(apiResult({ result: true }))
        const wrapper = mountDialog({
            modelValue: true,
            section: { label: "VMDO", value: "1" },
            units: [],
            unitFaxNumbers: [],
            editData: editRecord(),
        })

        await submitDialogForm(wrapper)

        expect(svmUnitService.updateUnitData).toHaveBeenCalledWith(
            10,
            expect.objectContaining({ deanInterim: "", staffInterim: "" }),
        )
    })
})

describe("sVMAddRecordDialog.vue - form field wiring", () => {
    it("autofills each phone field from the person picked for that role", async () => {
        expect.hasAssertions()
        resetTestState()
        const wrapper = mountDialog({
            modelValue: true,
            section: { label: "VMDO", value: "1" },
            units: [],
            unitFaxNumbers: [],
        })
        await flushPromises()
        const [deanSelector, staffSelector] = wrapper.findAllComponents(selectorStub)

        await deanSelector!.vm.$emit("update:modelValue", { phoneData: { phone: "530-555-1111" } })
        await staffSelector!.vm.$emit("update:modelValue", { phoneData: { phone: "530-555-2222" } })
        await flushPromises()

        // Each picker fills only its own phone field; crossing them would quietly file one
        // person's number under the other. QInput order in add mode is location, fax,
        // dean/director phone, admin staff phone.
        const inputs = wrapper.findAllComponents({ name: "QInput" })
        expect(inputs[2]!.props("modelValue")).toBe("530-555-1111")
        expect(inputs[3]!.props("modelValue")).toBe("530-555-2222")
    })

    it("clears the phone field when the person for that role is unset", async () => {
        expect.hasAssertions()
        resetTestState()
        const wrapper = mountDialog({
            modelValue: true,
            section: { label: "VMDO", value: "1" },
            units: [],
            unitFaxNumbers: [],
        })
        await flushPromises()
        const [deanSelector] = wrapper.findAllComponents(selectorStub)
        await deanSelector!.vm.$emit("update:modelValue", { phoneData: { phone: "530-555-1111" } })
        await flushPromises()

        await deanSelector!.vm.$emit("update:modelValue", null)
        await flushPromises()

        expect(wrapper.findAllComponents({ name: "QInput" })[2]!.props("modelValue")).toBe("")
    })

    it("requires a unit before an add can be submitted", async () => {
        expect.hasAssertions()
        resetTestState()
        const wrapper = mountDialog({
            modelValue: true,
            section: { label: "VMDO", value: "1" },
            units: [{ section: 1, units: [{ label: "Dean's Office", value: "10" }] }],
            unitFaxNumbers: [],
        })

        await submitDialogForm(wrapper)

        expect(bodyText()).toContain("Please select a unit")
        expect(svmUnitService.addUnitData).not.toHaveBeenCalled()
    })

    it("empties the form when the record being edited is cleared", async () => {
        expect.hasAssertions()
        resetTestState()
        const wrapper = mountDialog({
            modelValue: true,
            section: { label: "VMDO", value: "1" },
            units: [],
            unitFaxNumbers: [],
            editData: editRecord(),
        })
        await flushPromises()

        // The dialog re-derives its form whenever editData changes, and closing an edit clears
        // it. Nothing to edit has to read as every field blank, not as the previous record
        // lingering in the inputs the next time the dialog opens to add.
        await wrapper.setProps({ editData: null })
        await flushPromises()

        expect(bodyText()).not.toContain("Room 100")
        expect(bodyText()).not.toContain("530-555-9999")
    })
})
