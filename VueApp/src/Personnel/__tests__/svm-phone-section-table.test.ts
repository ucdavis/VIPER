import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import SVMPhoneSectionTable from "../components/SVMPhoneSectionTable.vue"
import type { SVMPhoneDisplayRecord, SVMPhoneSection } from "../types/svm-phone-types"

type Columns = NonNullable<SVMPhoneSection["cols"]>

const cols: Columns = [{ name: "unitName", label: "Unit", field: "unitName", align: "left" }]

// The read-only column set, in the order getSections builds it, keeping which columns are
// sortable: the phone and fax columns are not, so they must stay out of the sort dropdown. The
// dean/director label is per-section, so it is deliberately not the string "Dean/Director" here.
const readOnlyCols: Columns = [
    { name: "unitName", label: "Unit", field: "unitName", align: "left", sortable: true },
    { name: "abbreviation", label: "Abbrv", field: "unitAbbrv", align: "left", sortable: true },
    { name: "location", label: "Location", field: "officeLocation", align: "left", sortable: true },
    { name: "deanDirector", label: "Director", field: "deanDirectorDisplayName", align: "left", sortable: true },
    { name: "dirPhone", label: "Phone", field: "deanDirectorPhone", align: "left" },
    { name: "fax", label: "Fax", field: "officeFax", align: "left" },
]

function makeRow(overrides: Partial<SVMPhoneDisplayRecord> = {}): SVMPhoneDisplayRecord {
    return {
        sectionName: "VMDO",
        unitName: "Dean's Office",
        unitId: 10,
        unitAbbrv: "DO",
        officeLocation: "Room 100",
        officeFax: "530-555-3000",
        deanDirectorFullName: "Dinah Deanly",
        deanDirectorDisplayName: "Dinah Deanly",
        deanDirectorInterim: null,
        deanDirectorIam: "dean01",
        deanDirectorUnitPersonId: 1,
        deanDirectorPhone: "530-555-1000",
        deanDirectorModifiedDate: null,
        deanDirectorModifiedBy: null,
        adminStaffFullName: "Sam Staffly",
        adminStaffDisplayName: "Sam Staffly",
        adminStaffInterim: null,
        adminStaffIam: "staff01",
        adminStaffUnitPersonId: 2,
        adminStaffPhone: "530-555-2000",
        adminStaffModifiedDate: null,
        adminStaffModifiedBy: null,
        entryId: 1,
        isOnlyRowForUnit: true,
        ...overrides,
    }
}

function makeSection(sectionCols: Columns = cols, rows: SVMPhoneDisplayRecord[] = []): SVMPhoneSection {
    return { title: "VMDO", id: 1, cols: sectionCols, rows }
}

type MountOptions = { sectionCols?: Columns; rows?: SVMPhoneDisplayRecord[]; search?: string }

function mountTable(isModify: boolean, { sectionCols = cols, rows = [], search = "" }: MountOptions = {}) {
    return mount(SVMPhoneSectionTable, {
        props: { section: makeSection(sectionCols, rows), loading: false, isModify, search },
        global: { plugins: [Quasar] },
    })
}

function hasAddButton(wrapper: ReturnType<typeof mountTable>): boolean {
    return wrapper.findAllComponents({ name: "QBtn" }).some((btn) => btn.props("icon") === "add")
}

/** The mobile list, which renders alongside the table and is shown by CSS below 1024px. */
function listItems(wrapper: ReturnType<typeof mountTable>) {
    return wrapper.findComponent({ name: "QList" }).findAllComponents({ name: "QItem" })
}

/** The ascending/descending toggle beside the sort dropdown, told apart by its arrow icon. */
function directionButton(wrapper: ReturnType<typeof mountTable>) {
    return wrapper.findAllComponents({ name: "QBtn" }).find((btn) => String(btn.props("icon")).startsWith("arrow_"))!
}

describe("sVMPhoneSectionTable.vue - isModify gating", () => {
    it("shows the add button when isModify is true", () => {
        expect.hasAssertions()
        const wrapper = mountTable(true)

        expect(hasAddButton(wrapper)).toBeTruthy()
    })

    it("hides the add button when isModify is false", () => {
        expect.hasAssertions()
        const wrapper = mountTable(false)

        expect(hasAddButton(wrapper)).toBeFalsy()
    })
})

describe("sVMPhoneSectionTable.vue - narrow screen list", () => {
    it("shows the section heading whether or not the list can be edited", () => {
        expect.hasAssertions()

        // The table only titles itself in maintain mode; the cards have no column headers to
        // identify the section, so the heading is always rendered here.
        expect(mountTable(false).find("h2").text()).toContain("VMDO")
        expect(mountTable(true).find("h2").text()).toContain("VMDO")
    })

    it("renders one list item per row, headed by the unit name", () => {
        expect.hasAssertions()
        const rows = [makeRow(), makeRow({ entryId: 2, unitName: "Business Office" })]

        const items = listItems(mountTable(false, { sectionCols: readOnlyCols, rows }))

        expect(items).toHaveLength(2)
        expect(items[0].text()).toContain("Dean's Office")
        expect(items[1].text()).toContain("Business Office")
    })

    it("labels every value with its column label, since there are no column headers at this width", () => {
        expect.hasAssertions()

        const text = listItems(mountTable(false, { sectionCols: readOnlyCols, rows: [makeRow()] }))[0].text()

        // Including the fields the desktop table would have had to drop to stay readable.
        expect(text).toContain("Location: Room 100")
        expect(text).toContain("Fax: 530-555-3000")
        // Taken from the column, not hard-coded, so a section's own director title is used.
        expect(text).toContain("Director: Dinah Deanly")
    })

    it("carries the abbreviation in the heading rather than on a line of its own", () => {
        expect.hasAssertions()
        const wrapper = mountTable(false, { sectionCols: readOnlyCols, rows: [makeRow()] })

        const [item] = listItems(wrapper)

        expect(item.find(".q-item__label").text()).toBe("Dean's Office (DO)")
        expect(item.text()).not.toContain("Abbrv:")
    })

    it("heads the card with the unit name alone when the unit has no abbreviation", () => {
        expect.hasAssertions()
        const wrapper = mountTable(false, { sectionCols: readOnlyCols, rows: [makeRow({ unitAbbrv: null })] })

        expect(listItems(wrapper)[0].find(".q-item__label").text()).toBe("Dean's Office")
    })

    it("leaves the abbreviation out of the heading for a section that does not use one", () => {
        expect.hasAssertions()
        const withoutAbbrv = readOnlyCols.filter((col) => col.name !== "abbreviation")

        const wrapper = mountTable(false, { sectionCols: withoutAbbrv, rows: [makeRow()] })

        expect(listItems(wrapper)[0].find(".q-item__label").text()).toBe("Dean's Office")
    })

    it("mutes only the field name, leaving each value at full strength", () => {
        expect.hasAssertions()
        const wrapper = mountTable(false, { sectionCols: readOnlyCols, rows: [makeRow()] })

        const detail = listItems(wrapper)[0].findAll(".q-item__label").at(1)

        // The caption prop would dim the whole line and shrink it to .75rem.
        expect(detail?.classes()).not.toContain("q-item__label--caption")
        expect(detail?.find("span.text-grey").text()).toBe("Location:")
    })

    it("omits a line for a field this unit has no value for", () => {
        expect.hasAssertions()

        const text = listItems(
            mountTable(false, { sectionCols: readOnlyCols, rows: [makeRow({ officeFax: null })] }),
        )[0].text()

        expect(text).not.toContain("Fax:")
    })

    it("applies the search box to the list, the way the filter prop does for the table", () => {
        expect.hasAssertions()
        const rows = [makeRow(), makeRow({ entryId: 2, unitName: "Business Office" })]

        const items = listItems(mountTable(false, { sectionCols: readOnlyCols, rows, search: "business" }))

        expect(items).toHaveLength(1)
        expect(items[0].text()).toContain("Business Office")
    })

    it("matches the search against any column, not only the unit name", () => {
        expect.hasAssertions()
        const rows = [makeRow(), makeRow({ entryId: 2, unitName: "Business Office", officeLocation: "Room 200" })]

        const items = listItems(mountTable(false, { sectionCols: readOnlyCols, rows, search: "room 200" }))

        expect(items).toHaveLength(1)
        expect(items[0].text()).toContain("Business Office")
    })

    it("shows an empty message when nothing matches", () => {
        expect.hasAssertions()

        const items = listItems(
            mountTable(false, { sectionCols: readOnlyCols, rows: [makeRow()], search: "nothing matches this" }),
        )

        expect(items).toHaveLength(1)
        expect(items[0].text()).toContain("No records to display.")
    })

    it("gives each list item its own edit and delete buttons in maintain mode", async () => {
        expect.hasAssertions()
        const wrapper = mountTable(true, { sectionCols: readOnlyCols, rows: [makeRow()] })
        const actions = listItems(wrapper)[0].findAllComponents({ name: "RecordActionButton" })

        expect(actions.map((button) => button.props("action"))).toStrictEqual(["edit", "delete"])

        await actions[0].vm.$emit("action")

        expect(wrapper.emitted("editRecord")?.[0]).toStrictEqual([makeRow()])
    })

    it("leaves the list items without action buttons in read-only mode", () => {
        expect.hasAssertions()

        const actions = listItems(
            mountTable(false, { sectionCols: readOnlyCols, rows: [makeRow()] }),
        )[0].findAllComponents({
            name: "RecordActionButton",
        })

        expect(actions).toHaveLength(0)
    })
})

describe("sVMPhoneSectionTable.vue - narrow screen sorting", () => {
    const unsortedRows = [
        makeRow({ entryId: 1, unitName: "Dean's Office", officeLocation: "Room 300" }),
        makeRow({ entryId: 2, unitName: "Business Office", officeLocation: "Room 100" }),
        makeRow({ entryId: 3, unitName: "Analytics Office", officeLocation: "Room 200" }),
    ]

    function headings(wrapper: ReturnType<typeof mountTable>): string[] {
        return listItems(wrapper).map((item) => item.find(".q-item__label").text())
    }

    it("offers only the sortable columns, under their own labels", () => {
        expect.hasAssertions()
        const wrapper = mountTable(false, { sectionCols: readOnlyCols, rows: unsortedRows })

        const options = wrapper.findComponent({ name: "QSelect" }).props("options")

        // Phone and Fax are not sortable columns, so they are not offered.
        expect(options).toStrictEqual([
            { label: "Unit", value: "unitName" },
            { label: "Abbrv", value: "abbreviation" },
            { label: "Location", value: "location" },
            { label: "Director", value: "deanDirector" },
        ])
    })

    it("leaves the rows in source order until a sort is chosen", () => {
        expect.hasAssertions()

        const wrapper = mountTable(false, { sectionCols: readOnlyCols, rows: unsortedRows })

        expect(headings(wrapper)).toStrictEqual(["Dean's Office (DO)", "Business Office (DO)", "Analytics Office (DO)"])
    })

    it("reorders the cards when a sort field is chosen", async () => {
        expect.hasAssertions()
        const wrapper = mountTable(false, { sectionCols: readOnlyCols, rows: unsortedRows })

        await wrapper.findComponent({ name: "QSelect" }).setValue("unitName")

        expect(headings(wrapper)).toStrictEqual(["Analytics Office (DO)", "Business Office (DO)", "Dean's Office (DO)"])
    })

    it("sorts on a field the heading does not show", async () => {
        expect.hasAssertions()
        const wrapper = mountTable(false, { sectionCols: readOnlyCols, rows: unsortedRows })

        await wrapper.findComponent({ name: "QSelect" }).setValue("location")

        expect(headings(wrapper)).toStrictEqual(["Business Office (DO)", "Analytics Office (DO)", "Dean's Office (DO)"])
    })

    it("reverses the order when the direction is toggled", async () => {
        expect.hasAssertions()
        const wrapper = mountTable(false, { sectionCols: readOnlyCols, rows: unsortedRows })
        await wrapper.findComponent({ name: "QSelect" }).setValue("unitName")

        await directionButton(wrapper).trigger("click")

        expect(headings(wrapper)).toStrictEqual(["Dean's Office (DO)", "Business Office (DO)", "Analytics Office (DO)"])
    })

    it("hands the same sort to the table, so widening the window keeps it", async () => {
        expect.hasAssertions()
        const wrapper = mountTable(false, { sectionCols: readOnlyCols, rows: unsortedRows })

        await wrapper.findComponent({ name: "QSelect" }).setValue("location")
        await directionButton(wrapper).trigger("click")

        expect(wrapper.findComponent({ name: "QTable" }).props("pagination")).toMatchObject({
            sortBy: "location",
            descending: true,
        })
    })

    it("names the direction button for the action it performs, not the state it is in", async () => {
        expect.hasAssertions()
        const wrapper = mountTable(false, { sectionCols: readOnlyCols, rows: unsortedRows })
        await wrapper.findComponent({ name: "QSelect" }).setValue("unitName")

        expect(directionButton(wrapper).attributes("aria-label")).toBe("Sort descending")

        await directionButton(wrapper).trigger("click")

        expect(directionButton(wrapper).attributes("aria-label")).toBe("Sort ascending")
    })

    it("disables the direction toggle until a field is chosen to sort by", () => {
        expect.hasAssertions()

        const wrapper = mountTable(false, { sectionCols: readOnlyCols, rows: unsortedRows })

        expect(directionButton(wrapper).props("disable")).toBeTruthy()
    })

    it("offers no sort control for a section with no sortable columns", () => {
        expect.hasAssertions()
        const unsortable = readOnlyCols.map((col) => ({ ...col, sortable: false }))

        const wrapper = mountTable(false, { sectionCols: unsortable, rows: unsortedRows })

        expect(wrapper.findComponent({ name: "QSelect" }).exists()).toBeFalsy()
    })
})
