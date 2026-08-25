import { mount, flushPromises } from "@vue/test-utils"
import { Quasar, Notify } from "quasar"
import PhoneListMaintain from "../pages/PhoneListMaintain.vue"
import PhoneListAddRecordDialog from "../components/PhoneListAddRecordDialog.vue"
import { getPhoneListData } from "../composables/phone-list-data-fetch.ts"
import { phoneListService } from "../services/phone-list-service.ts"
import { phoneListUnitService } from "../services/phone-list-unit-service.ts"
import type { PhoneListUnit } from "../types/phone-list-phone-types"
import { apiError } from "./test-utils"

/**
 * PhoneListMaintain shows a StatusBanner only when a delete/save action reports an error
 * (v-if="errorMessage"). Reaching that state means driving the real child chain (PhoneListUnitTable
 * -> RecordActionButton "delete" -> the confirm dialog -> phoneListUnitService), since errorMessage
 * is page-local state with no prop/route to set it directly.
 */

vi.mock("../composables/phone-list-data-fetch.ts", () => ({
    getPhoneListData: vi.fn<(...args: unknown[]) => unknown>(),
}))
vi.mock("../services/phone-list-service.ts", () => ({
    phoneListService: { getPhoneListInfo: vi.fn<(...args: unknown[]) => unknown>() },
}))
vi.mock("../services/phone-list-unit-service.ts", () => ({
    phoneListUnitService: {
        addUnitPersonData: vi.fn<(...args: unknown[]) => unknown>(),
        updateUnitPersonData: vi.fn<(...args: unknown[]) => unknown>(),
        deleteUnitPersonData: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))
const mockReplace = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("vue-router", () => ({
    useRoute: () => ({ params: { code: "VMDO" } }),
    useRouter: () => ({ replace: (...args: unknown[]) => mockReplace(...args) }),
}))
// Stub only the public useQuasar export, so the toast the page raises can be asserted directly.
// Quasar components resolve $q through their own internals, so QTable and friends still render.
const { mockNotify } = vi.hoisted(() => ({ mockNotify: vi.fn<(...args: unknown[]) => unknown>() }))
vi.mock("quasar", async (importOriginal) => {
    const actual = await importOriginal<typeof import("quasar")>()
    return { ...actual, useQuasar: () => ({ notify: mockNotify }) }
})
vi.mock("@/composables/use-confirm-dialog", () => ({
    useConfirmDialog: () => ({ confirmAction: vi.fn<(...args: unknown[]) => unknown>().mockResolvedValue(true) }),
}))

/** Stubs a list the caller may maintain, which is the precondition for the editor to render. */
function stubListInfo(canMaintain = true): void {
    vi.mocked(phoneListService.getPhoneListInfo).mockResolvedValue({
        phoneListId: 1,
        code: "VMDO",
        name: "Dean's Office Phone List",
        canMaintain,
        canViewDirectPhone: true,
    })
}

function unitWithDeletableRow(): PhoneListUnit {
    return {
        name: "Dean's Office",
        id: 10,
        cols: [
            { name: "name", label: "Name", field: "name", align: "left" },
            { name: "edit", label: "Edit", field: "edit", align: "left" },
            { name: "delete", label: "Delete", field: "delete", align: "left" },
        ],
        rows: [
            {
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
                modifiedBy: null,
                modifiedDate: null,
            },
        ],
    }
}

const personSelectorStub = {
    props: ["modelValue", "label", "listCode"],
    emits: ["update:modelValue"],
    template: "<div class='selector-stub' />",
}

function mountPage() {
    return mount(PhoneListMaintain, {
        global: {
            plugins: [[Quasar, { plugins: { Notify } }]],
            stubs: { PersonSelector: personSelectorStub },
        },
    })
}

function findAddButton(wrapper: ReturnType<typeof mountPage>) {
    return wrapper.findAllComponents({ name: "QBtn" }).find((btn) => btn.props("icon") === "add")
}

describe("phoneListMaintain.vue - error banner", () => {
    it("hides the error banner on a normal load", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        stubListInfo()
        // RecordFormDialog's QDialog teleports to document.body, so clear any leftover content
        // from a previous test's dialog before asserting on document.body.textContent.
        document.body.innerHTML = ""
        vi.mocked(getPhoneListData).mockResolvedValue([])

        const wrapper = mountPage()
        await flushPromises()

        expect(wrapper.findComponent({ name: "StatusBanner" }).exists()).toBeFalsy()
    })

    it("raises a toast carrying the server message when a delete fails", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        stubListInfo()
        // RecordFormDialog's QDialog teleports to document.body, so clear any leftover content
        // from a previous test's dialog before asserting on document.body.textContent.
        document.body.innerHTML = ""
        vi.mocked(getPhoneListData).mockResolvedValue([unitWithDeletableRow()])
        vi.mocked(phoneListUnitService.deleteUnitPersonData).mockResolvedValue(apiError(["Failed to delete record"]))
        const wrapper = mountPage()
        await flushPromises()

        const deleteButton = wrapper
            .findAllComponents({ name: "RecordActionButton" })
            .find((btn) => btn.props("action") === "delete")
        expect(deleteButton).toBeTruthy()
        await deleteButton!.vm.$emit("action")
        await flushPromises()

        // A failed delete is transient, so it is reported as a toast rather than the banner,
        // which would otherwise persist past the reload that follows.
        expect(wrapper.findComponent({ name: "StatusBanner" }).exists()).toBeFalsy()
        expect(mockNotify).toHaveBeenCalledWith(
            expect.objectContaining({ type: "negative", message: "Failed to delete record" }),
        )
    })

    it("opens the add dialog scoped to the clicked unit", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        stubListInfo()
        // RecordFormDialog's QDialog teleports to document.body, so clear any leftover content
        // from a previous test's dialog before asserting on document.body.textContent.
        document.body.innerHTML = ""
        vi.mocked(getPhoneListData).mockResolvedValue([unitWithDeletableRow()])
        const wrapper = mountPage()
        await flushPromises()

        await findAddButton(wrapper)!.trigger("click")
        await flushPromises()

        const dialog = wrapper.findComponent(PhoneListAddRecordDialog)
        expect(dialog.props("modelValue")).toBeTruthy()
        expect(dialog.props("unit")).toMatchObject({ name: "Dean's Office", id: 10 })
    })

    it("opens the edit dialog pre-filled when edit is clicked on a row", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        stubListInfo()
        // RecordFormDialog's QDialog teleports to document.body, so clear any leftover content
        // from a previous test's dialog before asserting on document.body.textContent.
        document.body.innerHTML = ""
        vi.mocked(getPhoneListData).mockResolvedValue([unitWithDeletableRow()])
        const wrapper = mountPage()
        await flushPromises()

        const editButton = wrapper
            .findAllComponents({ name: "RecordActionButton" })
            .find((btn) => btn.props("action") === "edit")
        expect(editButton).toBeTruthy()
        await editButton!.vm.$emit("action")
        await flushPromises()

        const dialog = wrapper.findComponent(PhoneListAddRecordDialog)
        expect(dialog.props("modelValue")).toBeTruthy()
        // RecordFormDialog's QDialog teleports its content to document.body.
        expect(document.body.textContent).toContain("Edit Phone Record")
        expect(document.body.textContent).toContain("Employee: Smith, Amy")
    })

    it("redirects away, without fetching rows, when the caller cannot maintain the list", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        document.body.innerHTML = ""
        // The maintain role is the list's own, so no static route guard can gate this page.
        // The API rejects the writes regardless; this keeps a non-maintainer out of an editor
        // whose every save would fail.
        stubListInfo(false)

        mountPage()
        await flushPromises()

        expect(mockReplace).toHaveBeenCalledWith({ name: "PersonnelHome" })
        expect(getPhoneListData).not.toHaveBeenCalled()
    })

    it("reloads the phone data when the dialog reports a save", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        stubListInfo()
        // RecordFormDialog's QDialog teleports to document.body, so clear any leftover content
        // from a previous test's dialog before asserting on document.body.textContent.
        document.body.innerHTML = ""
        vi.mocked(getPhoneListData).mockResolvedValue([])
        const wrapper = mountPage()
        await flushPromises()
        const callsBeforeSave = vi.mocked(getPhoneListData).mock.calls.length

        await wrapper.findComponent(PhoneListAddRecordDialog).vm.$emit("saved", true)
        await flushPromises()

        expect(vi.mocked(getPhoneListData).mock.calls.length).toBeGreaterThan(callsBeforeSave)
    })
})
