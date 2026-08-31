import { mount, flushPromises } from "@vue/test-utils"
import { Quasar, Notify } from "quasar"
import SVMPhonesMaintain from "../pages/SVMPhonesMaintain.vue"
import SVMAddRecordDialog from "../components/SVMAddRecordDialog.vue"
import SVMAddFrequentNumberDialog from "../components/SVMAddFrequentNumberDialog.vue"
import SVMFrequentNumberTable from "../components/SVMFrequentNumberTable.vue"
import { getFrequentlyCalledNumbers, getSVMData } from "../composables/svm-data-fetch.ts"
import { svmUnitService } from "../services/svm-unit-service"
import { svmFrequentNumberService } from "../services/svm-frequent-number-service.ts"
import type { SVMFrequentNumberRecord, SVMPhoneDisplayRecord, SVMPhoneSection } from "../types/svm-phone-types"
import { apiError, apiResult } from "./test-utils"

/**
 * SVMPhonesMaintain reports delete outcomes as toasts rather than as a page banner, since the
 * reload that follows a delete would wipe page-local state before the user read it. Reaching one
 * means driving the real child chain (SVMPhoneSectionTable / SVMFrequentNumberTable ->
 * RecordActionButton -> the confirm dialog -> the service), because the page exposes no state a
 * test could set directly.
 *
 * The page maintains two independent lists - unit rows and frequently called numbers - through
 * near-identical add/edit/delete paths, so both are exercised here.
 */

vi.mock("../composables/svm-data-fetch.ts", () => ({
    getSVMData: vi.fn<(...args: unknown[]) => unknown>(),
    getFrequentlyCalledNumbers: vi.fn<(...args: unknown[]) => unknown>(),
}))
vi.mock("../services/svm-unit-service", () => ({
    svmUnitService: {
        addUnitData: vi.fn<(...args: unknown[]) => unknown>(),
        updateUnitData: vi.fn<(...args: unknown[]) => unknown>(),
        deleteRow: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))
vi.mock("../services/svm-frequent-number-service.ts", () => ({
    svmFrequentNumberService: {
        addFrequentNumber: vi.fn<(...args: unknown[]) => unknown>(),
        updateFrequentNumber: vi.fn<(...args: unknown[]) => unknown>(),
        deleteFrequentNumber: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))
// Stub only the public useQuasar export, so the toasts the page raises can be asserted directly.
// Quasar components resolve $q through their own internals, so QTable and friends still render.
const { mockNotify } = vi.hoisted(() => ({ mockNotify: vi.fn<(...args: unknown[]) => unknown>() }))
vi.mock("quasar", async (importOriginal) => {
    const actual = await importOriginal<typeof import("quasar")>()
    return { ...actual, useQuasar: () => ({ notify: mockNotify }) }
})

const { mockConfirmAction } = vi.hoisted(() => ({
    mockConfirmAction: vi.fn<(...args: unknown[]) => unknown>(),
}))
vi.mock("@/composables/use-confirm-dialog", () => ({
    useConfirmDialog: () => ({ confirmAction: mockConfirmAction }),
}))

function sectionWithDeletableRow(rowOverrides: Partial<SVMPhoneDisplayRecord> = {}): SVMPhoneSection {
    return {
        title: "VMDO",
        id: 1,
        cols: [
            { name: "unitName", label: "Unit", field: "unitName", align: "left" },
            { name: "edit", label: "Edit", field: "edit", align: "left" },
            { name: "delete", label: "Delete", field: "delete", align: "left" },
        ],
        rows: [
            {
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
                deanDirectorModifiedBy: null,
                adminStaffFullName: null,
                adminStaffDisplayName: "",
                adminStaffInterim: null,
                adminStaffIam: null,
                adminStaffUnitPersonId: null,
                adminStaffPhone: null,
                adminStaffModifiedDate: null,
                adminStaffModifiedBy: null,
                entryId: 1,
                isOnlyRowForUnit: true,
                ...rowOverrides,
            },
        ],
    }
}

const frequentNumber: SVMFrequentNumberRecord = { label: "Front Desk", phone: "530-555-1000", entryId: 7 }

const personSelectorStub = {
    props: ["modelValue", "label", "listId"],
    emits: ["update:modelValue"],
    template: "<div class='selector-stub' />",
}

/**
 * Mounts the page with the data the test cares about, waiting out the onMounted load.
 * Anything not named here loads empty.
 */
async function mountPage(
    data: {
        sections?: SVMPhoneSection[]
        frequentNumbers?: SVMFrequentNumberRecord[]
        loadError?: string | null
    } = {},
) {
    vi.mocked(getSVMData).mockResolvedValue({
        newSections: data.sections ?? [],
        newUnitOptions: [],
        newUnitFaxNumbers: [],
        newUnitAdminStaff: [],
        error: data.loadError ?? null,
    })
    vi.mocked(getFrequentlyCalledNumbers).mockResolvedValue({
        rows: data.frequentNumbers ?? [],
        error: null,
    })

    const wrapper = mount(SVMPhonesMaintain, {
        global: {
            plugins: [[Quasar, { plugins: { Notify } }]],
            stubs: { PersonSelector: personSelectorStub },
        },
    })
    await flushPromises()
    return wrapper
}

type Page = Awaited<ReturnType<typeof mountPage>>

function findAddButton(root: Pick<Page, "findAllComponents">) {
    return root.findAllComponents({ name: "QBtn" }).find((btn) => btn.props("icon") === "add")
}

/** Clicks an edit/delete button within one table, so the two tables' buttons cannot be confused. */
async function clickAction(root: Pick<Page, "findAllComponents">, action: "edit" | "delete"): Promise<void> {
    const button = root.findAllComponents({ name: "RecordActionButton" }).find((btn) => btn.props("action") === action)
    expect(button).toBeTruthy()
    await button!.vm.$emit("action")
    await flushPromises()
}

/**
 * Per-test reset, called as the first line of each test rather than from a beforeEach, since
 * vitest/no-hooks is on and the rest of the suite keeps its setup inside the test body.
 * Pass false to decline the confirmation dialog.
 */
function resetMocks(confirmed = true) {
    vi.clearAllMocks()
    mockConfirmAction.mockResolvedValue(confirmed)
    // RecordFormDialog's QDialog teleports to document.body, which outlives the wrapper, so a
    // previous test's dialog would still be there when asserting on document.body.textContent.
    document.body.innerHTML = ""
}

describe("sVMPhonesMaintain.vue - load failures", () => {
    it("shows no banner when the data loaded", async () => {
        expect.hasAssertions()

        const wrapper = await mountPage()

        expect(wrapper.findComponent({ name: "StatusBanner" }).exists()).toBeFalsy()
    })

    it("banners a failed load, rather than rendering empty tables as if the list were empty", async () => {
        expect.hasAssertions()

        const wrapper = await mountPage({ loadError: "The phone list could not be loaded." })

        const banner = wrapper.findComponent({ name: "StatusBanner" })
        expect(banner.exists()).toBeTruthy()
        expect(banner.text()).toContain("could not be loaded")
    })

    it("still renders the sections that did arrive alongside the banner", async () => {
        expect.hasAssertions()

        // GetSVMData reports its error when either of its own reads failed, so the sections can be
        // fully populated next to a banner. That is the premise the filter guard depends on.
        const wrapper = await mountPage({
            sections: [sectionWithDeletableRow()],
            loadError: "The phone list could not be loaded.",
        })

        expect(wrapper.findComponent({ name: "SVMPhoneSectionTable" }).exists()).toBeTruthy()
    })

    it("keeps the filter over the sections that did arrive", async () => {
        expect.hasAssertions()

        const wrapper = await mountPage({
            sections: [sectionWithDeletableRow()],
            loadError: "The phone list could not be loaded.",
        })

        // Gating the filter on the error would leave a full page of sections with no way to search
        // them. The banner above already says what was lost.
        expect(wrapper.findComponent({ name: "PhoneListFilter" }).exists()).toBeTruthy()
    })

    it("clears the banner once a later load succeeds", async () => {
        expect.hasAssertions()
        const wrapper = await mountPage({ loadError: "The phone list could not be loaded." })
        vi.mocked(getSVMData).mockResolvedValue({
            newSections: [],
            newUnitOptions: [],
            newUnitFaxNumbers: [],
            newUnitAdminStaff: [],
            error: null,
        })

        // A save triggers the same reload path the page uses everywhere else.
        wrapper.findComponent({ name: "SVMAddRecordDialog" }).vm.$emit("saved")
        await flushPromises()

        expect(wrapper.findComponent({ name: "StatusBanner" }).exists()).toBeFalsy()
    })
})

describe("sVMPhonesMaintain.vue - delete outcomes", () => {
    it("raises no toast on a normal load", async () => {
        expect.hasAssertions()
        resetMocks()

        await mountPage()

        expect(mockNotify).not.toHaveBeenCalled()
    })

    it("raises a toast carrying the server message when a delete fails", async () => {
        expect.hasAssertions()
        resetMocks()
        vi.mocked(svmUnitService.deleteRow).mockResolvedValue(apiError(["Failed to delete record"]))
        const wrapper = await mountPage({ sections: [sectionWithDeletableRow()] })

        await clickAction(wrapper, "delete")

        // A failed delete is transient, so it is reported as a toast rather than a banner,
        // which would otherwise persist past the reload that follows.
        expect(mockNotify).toHaveBeenCalledWith(
            expect.objectContaining({ type: "negative", message: "Failed to delete record" }),
        )
    })

    it("opens the add dialog scoped to the clicked section", async () => {
        expect.hasAssertions()
        resetMocks()
        const wrapper = await mountPage({ sections: [sectionWithDeletableRow()] })

        await findAddButton(wrapper.findComponent({ name: "SVMPhoneSectionTable" }))!.trigger("click")
        await flushPromises()

        const dialog = wrapper.findComponent(SVMAddRecordDialog)
        expect(dialog.props("modelValue")).toBeTruthy()
        expect(dialog.props("section")).toStrictEqual({ label: "VMDO", value: "1" })
    })

    it("opens the edit dialog pre-filled when edit is clicked on a row", async () => {
        expect.hasAssertions()
        resetMocks()
        const wrapper = await mountPage({ sections: [sectionWithDeletableRow()] })

        await clickAction(wrapper.findComponent({ name: "SVMPhoneSectionTable" }), "edit")

        const dialog = wrapper.findComponent(SVMAddRecordDialog)
        expect(dialog.props("modelValue")).toBeTruthy()
        // RecordFormDialog's QDialog teleports its content to document.body.
        expect(document.body.textContent).toContain("Edit Phone Record")
        expect(document.body.textContent).toContain("Unit: Dean's Office")
    })

    it("reloads the phone data when the dialog reports a save", async () => {
        expect.hasAssertions()
        resetMocks()
        const wrapper = await mountPage()
        const callsBeforeSave = vi.mocked(getSVMData).mock.calls.length

        await wrapper.findComponent(SVMAddRecordDialog).vm.$emit("saved", true)
        await flushPromises()

        expect(vi.mocked(getSVMData).mock.calls.length).toBeGreaterThan(callsBeforeSave)
    })
})

describe("sVMPhonesMaintain.vue - delete confirmation", () => {
    it("names both people in the delete confirmation when the row is the unit's last", async () => {
        expect.hasAssertions()
        resetMocks(false)
        const wrapper = await mountPage({
            sections: [sectionWithDeletableRow({ adminStaffFullName: "Staff Person" })],
        })

        await clickAction(wrapper, "delete")

        // IsOnlyRowForUnit means no other row is left to list the admin staff, so this delete
        // really does remove both.
        expect(mockConfirmAction).toHaveBeenCalledWith(
            expect.objectContaining({ message: expect.stringContaining("Dean Person and Staff Person") }),
        )
    })

    it("names only the dean/director when another row still lists the admin staff", async () => {
        expect.hasAssertions()
        resetMocks(false)
        const wrapper = await mountPage({
            sections: [sectionWithDeletableRow({ adminStaffFullName: "Staff Person", isOnlyRowForUnit: false })],
        })

        await clickAction(wrapper, "delete")

        // The admin staff survives this delete, so promising their removal would be wrong.
        const [confirmArgs] = vi.mocked(mockConfirmAction).mock.calls[0] as [{ message: string }]
        expect(confirmArgs.message).toContain("Dean Person")
        expect(confirmArgs.message).not.toContain("Staff Person")
    })

    it("does not delete anything when the confirmation is declined", async () => {
        expect.hasAssertions()
        resetMocks(false)
        const wrapper = await mountPage({ sections: [sectionWithDeletableRow()] })

        await clickAction(wrapper, "delete")

        expect(svmUnitService.deleteRow).not.toHaveBeenCalled()
    })

    it("deletes the row by its row key, in a single call", async () => {
        expect.hasAssertions()
        resetMocks()
        vi.mocked(svmUnitService.deleteRow).mockResolvedValue(apiResult({ result: true }))
        const wrapper = await mountPage({
            sections: [sectionWithDeletableRow({ adminStaffFullName: "Staff Person" })],
        })

        await clickAction(wrapper, "delete")

        // One call, not one per underlying record: the leader and the admin staff are removed
        // together server-side, so the pair cannot half-apply.
        expect(svmUnitService.deleteRow).toHaveBeenCalledExactlyOnceWith(1)
    })
})

describe("sVMPhonesMaintain.vue - frequently called numbers", () => {
    it("opens the frequent number dialog in add mode", async () => {
        expect.hasAssertions()
        resetMocks()
        const wrapper = await mountPage({ frequentNumbers: [frequentNumber] })

        await findAddButton(wrapper.findComponent(SVMFrequentNumberTable))!.trigger("click")
        await flushPromises()

        const dialog = wrapper.findComponent(SVMAddFrequentNumberDialog)
        expect(dialog.props("modelValue")).toBeTruthy()
        // Null rather than a leftover row: the dialog reads this prop to decide add vs edit.
        expect(dialog.props("editFrequentData")).toBeNull()
    })

    it("opens the frequent number dialog pre-filled when edit is clicked on a row", async () => {
        expect.hasAssertions()
        resetMocks()
        const wrapper = await mountPage({ frequentNumbers: [frequentNumber] })

        await clickAction(wrapper.findComponent(SVMFrequentNumberTable), "edit")

        const dialog = wrapper.findComponent(SVMAddFrequentNumberDialog)
        expect(dialog.props("modelValue")).toBeTruthy()
        expect(dialog.props("editFrequentData")).toStrictEqual(frequentNumber)
    })

    it("clears the edited row when the dialog closes, so the next add starts blank", async () => {
        expect.hasAssertions()
        resetMocks()
        const wrapper = await mountPage({ frequentNumbers: [frequentNumber] })
        await clickAction(wrapper.findComponent(SVMFrequentNumberTable), "edit")

        // Closing the dialog is what resets the edit target. Without it, the next add would open
        // pre-filled with this row and overwrite it on save.
        await wrapper.findComponent(SVMAddFrequentNumberDialog).vm.$emit("update:modelValue", false)
        await flushPromises()

        expect(wrapper.findComponent(SVMAddFrequentNumberDialog).props("editFrequentData")).toBeNull()
    })

    it("names the number's location in the delete confirmation", async () => {
        expect.hasAssertions()
        resetMocks(false)
        const wrapper = await mountPage({ frequentNumbers: [frequentNumber] })

        await clickAction(wrapper.findComponent(SVMFrequentNumberTable), "delete")

        expect(mockConfirmAction).toHaveBeenCalledWith(
            expect.objectContaining({ message: expect.stringContaining("Front Desk") }),
        )
    })

    it("does not delete a frequent number when the confirmation is declined", async () => {
        expect.hasAssertions()
        resetMocks(false)
        const wrapper = await mountPage({ frequentNumbers: [frequentNumber] })

        await clickAction(wrapper.findComponent(SVMFrequentNumberTable), "delete")

        expect(svmFrequentNumberService.deleteFrequentNumber).not.toHaveBeenCalled()
    })

    it("deletes the frequent number by its entry id and reloads", async () => {
        expect.hasAssertions()
        resetMocks()
        vi.mocked(svmFrequentNumberService.deleteFrequentNumber).mockResolvedValue(apiResult({ result: true }))
        const wrapper = await mountPage({ frequentNumbers: [frequentNumber] })
        const callsBeforeDelete = vi.mocked(getFrequentlyCalledNumbers).mock.calls.length

        await clickAction(wrapper.findComponent(SVMFrequentNumberTable), "delete")

        expect(svmFrequentNumberService.deleteFrequentNumber).toHaveBeenCalledExactlyOnceWith(7)
        expect(mockNotify).toHaveBeenCalledWith(
            expect.objectContaining({ type: "positive", message: "Record deleted" }),
        )
        expect(vi.mocked(getFrequentlyCalledNumbers).mock.calls.length).toBeGreaterThan(callsBeforeDelete)
    })

    it("raises a toast carrying the server message when a frequent number delete fails", async () => {
        expect.hasAssertions()
        resetMocks()
        vi.mocked(svmFrequentNumberService.deleteFrequentNumber).mockResolvedValue(
            apiError(["Failed to delete record"]),
        )
        const wrapper = await mountPage({ frequentNumbers: [frequentNumber] })

        await clickAction(wrapper.findComponent(SVMFrequentNumberTable), "delete")

        expect(mockNotify).toHaveBeenCalledWith(
            expect.objectContaining({ type: "negative", message: "Failed to delete record" }),
        )
    })

    it("reloads the phone data when the frequent number dialog reports a save", async () => {
        expect.hasAssertions()
        resetMocks()
        const wrapper = await mountPage()
        const callsBeforeSave = vi.mocked(getFrequentlyCalledNumbers).mock.calls.length

        await wrapper.findComponent(SVMAddFrequentNumberDialog).vm.$emit("saved", true)
        await flushPromises()

        expect(vi.mocked(getFrequentlyCalledNumbers).mock.calls.length).toBeGreaterThan(callsBeforeSave)
    })

    /**
     * A sticky element can only travel within its own parent. The filter was briefly wrapped in a
     * div holding nothing else, which pinned it to a box its own height, so it scrolled away with
     * the page and never appeared to stick at all.
     */
    it("leaves the filter among the lists it filters, so it has room to stick to", async () => {
        expect.hasAssertions()
        resetMocks()
        const wrapper = await mountPage({ sections: [sectionWithDeletableRow()] })

        const filterParent = wrapper.find(".phone-list-filter").element.parentElement

        expect(filterParent?.childElementCount).toBeGreaterThan(1)
    })
})
