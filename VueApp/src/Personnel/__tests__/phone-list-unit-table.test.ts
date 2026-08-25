import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import PhoneListUnitTable from "../components/PhoneListUnitTable.vue"
import type { PhoneListDisplayRecord, PhoneListUnit } from "../types/phone-list-phone-types"

/**
 * PhoneListUnitTable is the UI enforcement point for the same maintain/internal-access
 * permission model already covered at the service layer (PhoneListUnitService,
 * PhonePersonLookupService): isMaintain gates the "add" button and swaps the name cell between
 * a maintainer-facing default render and a viewer-facing mailto link (or plain text when the
 * person has no mail id).
 */

const cols = [
    { name: "name", label: "Name", field: "name", align: "left" as const },
    { name: "listFirst", label: "List First", field: "listFirst", align: "center" as const },
    { name: "edit", label: "Edit", field: "edit", align: "left" as const },
    { name: "delete", label: "Delete", field: "delete", align: "left" as const },
]

function makeRow(overrides: Partial<PhoneListDisplayRecord> = {}): PhoneListDisplayRecord {
    return {
        fullName: "Amy Smith",
        name: "Smith, Amy",
        employeeIam: "asmith",
        employeeMailId: "asmith",
        phone: "530-555-1000",
        directPhone: "530-555-2000",
        office: "Room 100",
        listFirst: false,
        unitPersonId: 1,
        unitId: 10,
        unitName: "Dean's Office",
        modifiedBy: null,
        modifiedDate: null,
        ...overrides,
    }
}

function makeUnit(rows: PhoneListDisplayRecord[]): PhoneListUnit {
    return { name: "Dean's Office", id: 10, cols, rows }
}

function mountTable(props: { unit: PhoneListUnit; loading: boolean; isMaintain: boolean; search: string }) {
    return mount(PhoneListUnitTable, {
        props,
        global: { plugins: [Quasar] },
    })
}

// QTable always renders a .q-table__title element from its own :title prop, so it can't
// distinguish the custom top-left slot (the "add" button) from the base title - check for the
// button itself instead.
function hasAddButton(wrapper: ReturnType<typeof mountTable>): boolean {
    return wrapper.findAllComponents({ name: "QBtn" }).some((btn) => btn.props("icon") === "add")
}

describe("phoneListUnitTable.vue - isMaintain gating", () => {
    it("shows the add button and skips the mailto-link cell when isMaintain is true", () => {
        expect.hasAssertions()
        const wrapper = mountTable({
            unit: makeUnit([makeRow({ employeeMailId: "asmith" })]),
            loading: false,
            isMaintain: true,
            search: "",
        })

        expect(hasAddButton(wrapper)).toBeTruthy()
        expect(wrapper.find("a[href^='mailto:']").exists()).toBeFalsy()
    })

    it("hides the add button and links the name as a mailto anchor when the row has a mail id and isMaintain is false", () => {
        expect.hasAssertions()
        const wrapper = mountTable({
            unit: makeUnit([makeRow({ employeeMailId: "asmith", name: "Smith, Amy" })]),
            loading: false,
            isMaintain: false,
            search: "",
        })

        expect(hasAddButton(wrapper)).toBeFalsy()
        const link = wrapper.find("a[href='mailto:asmith@ucdavis.edu']")
        expect(link.exists()).toBeTruthy()
        expect(link.text()).toBe("Smith, Amy")
    })

    it("shows plain text instead of a mailto link when the row has no mail id and isMaintain is false", () => {
        expect.hasAssertions()
        const wrapper = mountTable({
            unit: makeUnit([makeRow({ employeeMailId: "", name: "Smith, Amy" })]),
            loading: false,
            isMaintain: false,
            search: "",
        })

        expect(wrapper.find("a[href^='mailto:']").exists()).toBeFalsy()
        expect(wrapper.text()).toContain("Smith, Amy")
    })

    it("shows a check icon only on the row where listFirst is true", () => {
        expect.hasAssertions()
        const wrapper = mountTable({
            unit: makeUnit([
                makeRow({ unitPersonId: 1, name: "First, Person", listFirst: true }),
                makeRow({ unitPersonId: 2, name: "Second, Person", listFirst: false }),
            ]),
            loading: false,
            isMaintain: true,
            search: "",
        })

        const checkIcons = wrapper.findAllComponents({ name: "QIcon" }).filter((icon) => icon.props("name") === "check")
        expect(checkIcons).toHaveLength(1)
    })
})
