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

// Mirrors buildColumns, including the listFirst format the card list reads instead of a tick.
const cols = [
    { name: "name", label: "Name", field: "name", align: "left" as const },
    { name: "phone", label: "Phone", field: "phone", align: "left" as const },
    { name: "office", label: "Office", field: "office", align: "left" as const },
    {
        name: "listFirst",
        label: "List First",
        field: "listFirst",
        align: "center" as const,
        format: (listFirst: boolean) => (listFirst ? "Yes" : ""),
    },
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

/** The mobile list, which renders alongside the table and is shown by CSS below 1024px. */
function listItems(wrapper: ReturnType<typeof mountTable>) {
    return wrapper.findComponent({ name: "QList" }).findAllComponents({ name: "QItem" })
}

describe("phoneListUnitTable.vue - narrow screen list", () => {
    it("renders one list item per row, headed by the person's name", () => {
        expect.hasAssertions()
        const wrapper = mountTable({
            unit: makeUnit([
                makeRow({ unitPersonId: 1, name: "Smith, Amy" }),
                makeRow({ unitPersonId: 2, name: "Jones, Bo" }),
            ]),
            loading: false,
            isMaintain: false,
            search: "",
        })

        const items = listItems(wrapper)

        expect(items).toHaveLength(2)
        expect(items[0].find(".q-item__label").text()).toBe("Smith, Amy")
        expect(items[1].find(".q-item__label").text()).toBe("Jones, Bo")
    })

    it("labels every value with its column label, since there are no column headers at this width", () => {
        expect.hasAssertions()
        const wrapper = mountTable({
            unit: makeUnit([makeRow({ phone: "530-555-1000", office: "Room 100" })]),
            loading: false,
            isMaintain: false,
            search: "",
        })

        const text = listItems(wrapper)[0].text()

        expect(text).toContain("Phone: 530-555-1000")
        expect(text).toContain("Office: Room 100")
    })

    it("mails the name from the read-only list, as the table's own name cell does", () => {
        expect.hasAssertions()
        const wrapper = mountTable({
            unit: makeUnit([makeRow({ employeeMailId: "asmith", name: "Smith, Amy" })]),
            loading: false,
            isMaintain: false,
            search: "",
        })

        const link = listItems(wrapper)[0].find("a[href='mailto:asmith@ucdavis.edu']")

        expect(link.exists()).toBeTruthy()
        expect(link.text()).toBe("Smith, Amy")
    })

    it("leaves the name unlinked in the list when the person has no mail id", () => {
        expect.hasAssertions()
        const wrapper = mountTable({
            unit: makeUnit([makeRow({ employeeMailId: "", name: "Smith, Amy" })]),
            loading: false,
            isMaintain: false,
            search: "",
        })

        const [item] = listItems(wrapper)

        expect(item.find("a[href^='mailto:']").exists()).toBeFalsy()
        expect(item.text()).toContain("Smith, Amy")
    })

    it("leaves the name unlinked in the maintain list, which is for editing rather than contacting", () => {
        expect.hasAssertions()
        const wrapper = mountTable({
            unit: makeUnit([makeRow({ employeeMailId: "asmith" })]),
            loading: false,
            isMaintain: true,
            search: "",
        })

        expect(listItems(wrapper)[0].find("a[href^='mailto:']").exists()).toBeFalsy()
    })

    it("reads listFirst as text, and drops the line entirely when it is not set", () => {
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

        const items = listItems(wrapper)

        expect(items[0].text()).toContain("List First: Yes")
        expect(items[1].text()).not.toContain("List First")
    })

    it("applies the search box to the list, the way the filter prop does for the table", () => {
        expect.hasAssertions()
        const wrapper = mountTable({
            unit: makeUnit([
                makeRow({ unitPersonId: 1, name: "Smith, Amy" }),
                makeRow({ unitPersonId: 2, name: "Jones, Bo" }),
            ]),
            loading: false,
            isMaintain: false,
            search: "jones",
        })

        const items = listItems(wrapper)

        expect(items).toHaveLength(1)
        expect(items[0].text()).toContain("Jones, Bo")
    })

    it("gives each list item its own edit and delete buttons in maintain mode", async () => {
        expect.hasAssertions()
        const row = makeRow()
        const wrapper = mountTable({ unit: makeUnit([row]), loading: false, isMaintain: true, search: "" })
        const actions = listItems(wrapper)[0].findAllComponents({ name: "RecordActionButton" })

        expect(actions.map((button) => button.props("action"))).toStrictEqual(["edit", "delete"])

        await actions[0].vm.$emit("action")

        expect(wrapper.emitted("editRecord")?.[0]).toStrictEqual([row])
    })

    it("leaves the list items without action buttons in read-only mode", () => {
        expect.hasAssertions()
        const wrapper = mountTable({
            unit: makeUnit([makeRow()]),
            loading: false,
            isMaintain: false,
            search: "",
        })

        expect(listItems(wrapper)[0].findAllComponents({ name: "RecordActionButton" })).toHaveLength(0)
    })
})
