import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import SVMFrequentNumberTable from "../components/SVMFrequentNumberTable.vue"
import type { SVMFrequentNumberRecord } from "../types/svm-phone-types"

const numbers: SVMFrequentNumberRecord[] = [{ label: "Front Desk", phone: "530-555-1000", entryId: 1 }]

function mountTable(editRecords: boolean, rows: SVMFrequentNumberRecord[] = numbers, search = "") {
    return mount(SVMFrequentNumberTable, {
        props: { frequentNumbers: rows, loading: false, editRecords, search },
        global: { plugins: [Quasar] },
    })
}

function hasAddButton(wrapper: ReturnType<typeof mountTable>): boolean {
    return wrapper.findAllComponents({ name: "QBtn" }).some((btn) => btn.props("icon") === "add")
}

/** The ascending/descending toggle beside the sort dropdown, told apart by its arrow icon. */
function directionButton(wrapper: ReturnType<typeof mountTable>) {
    return wrapper.findAllComponents({ name: "QBtn" }).find((btn) => String(btn.props("icon")).startsWith("arrow_"))!
}

/** The mobile list, which renders alongside the table and is shown by CSS below 1024px. */
function listItems(wrapper: ReturnType<typeof mountTable>) {
    return wrapper.findComponent({ name: "QList" }).findAllComponents({ name: "QItem" })
}

describe("sVMFrequentNumberTable.vue - editRecords gating", () => {
    it("shows the add button and edit/delete action buttons when editRecords is true", () => {
        expect.hasAssertions()
        const wrapper = mountTable(true)

        expect(hasAddButton(wrapper)).toBeTruthy()
        // Two per view: the table renders alongside the list and CSS decides which one is seen.
        expect(wrapper.findAllComponents({ name: "RecordActionButton" })).toHaveLength(4)
        expect(listItems(wrapper)[0].findAllComponents({ name: "RecordActionButton" })).toHaveLength(2)
    })

    it("hides the add button and edit/delete action buttons when editRecords is false", () => {
        expect.hasAssertions()
        const wrapper = mountTable(false)

        expect(hasAddButton(wrapper)).toBeFalsy()
        expect(wrapper.findAllComponents({ name: "RecordActionButton" })).toHaveLength(0)
    })
})

describe("sVMFrequentNumberTable.vue - narrow screen list", () => {
    it("heads each entry with its location and carries the number beneath it, unlabelled", () => {
        expect.hasAssertions()

        const [location, number] = listItems(mountTable(false))[0].findAll(".q-item__label")

        expect(location.text()).toBe("Front Desk")
        // A place and its number: naming the second line would only repeat what it obviously is.
        expect(number.text()).toBe("530-555-1000")
    })

    it("leaves the number at full strength rather than dimming it as a caption", () => {
        expect.hasAssertions()

        const [, number] = listItems(mountTable(false))[0].findAll(".q-item__label")

        expect(number.classes()).not.toContain("q-item__label--caption")
        expect(number.classes()).toContain("text-body2")
    })

    it("shows the section heading whether or not the list can be edited", () => {
        expect.hasAssertions()

        expect(mountTable(false).find("h2").text()).toContain("Frequently Called Numbers")
        expect(mountTable(true).find("h2").text()).toContain("Frequently Called Numbers")
    })

    it("applies the search box to the list, the way the filter prop does for the table", () => {
        expect.hasAssertions()
        const rows: SVMFrequentNumberRecord[] = [
            { label: "Front Desk", phone: "530-555-1000", entryId: 1 },
            { label: "Pharmacy", phone: "530-555-2000", entryId: 2 },
        ]

        const items = listItems(mountTable(false, rows, "pharmacy"))

        expect(items).toHaveLength(1)
        expect(items[0].text()).toContain("Pharmacy")
    })

    it("matches the search against the number as well as the location", () => {
        expect.hasAssertions()
        const rows: SVMFrequentNumberRecord[] = [
            { label: "Front Desk", phone: "530-555-1000", entryId: 1 },
            { label: "Pharmacy", phone: "530-555-2000", entryId: 2 },
        ]

        const items = listItems(mountTable(false, rows, "555-2000"))

        expect(items).toHaveLength(1)
        expect(items[0].text()).toContain("Pharmacy")
    })

    it("shows an empty message when nothing matches", () => {
        expect.hasAssertions()

        const items = listItems(mountTable(false, numbers, "nothing matches this"))

        expect(items).toHaveLength(1)
        expect(items[0].text()).toContain("No numbers to display.")
    })
})

describe("sVMFrequentNumberTable.vue - narrow screen sorting", () => {
    const unsortedNumbers: SVMFrequentNumberRecord[] = [
        { label: "Pharmacy", phone: "530-555-3000", entryId: 1 },
        { label: "Front Desk", phone: "530-555-1000", entryId: 2 },
        { label: "Imaging", phone: "530-555-2000", entryId: 3 },
    ]

    function locations(wrapper: ReturnType<typeof mountTable>): string[] {
        return listItems(wrapper).map((item) => item.find(".q-item__label").text())
    }

    it("offers only the sortable columns, under their own labels", () => {
        expect.hasAssertions()
        const wrapper = mountTable(false, unsortedNumbers)

        const options = wrapper.findComponent({ name: "QSelect" }).props("options")

        // The phone column is not sortable on desktop, so it is not offered here either.
        expect(options).toStrictEqual([{ label: "Location", value: "label" }])
    })

    it("leaves the entries in source order until a sort is chosen", () => {
        expect.hasAssertions()

        const wrapper = mountTable(false, unsortedNumbers)

        expect(locations(wrapper)).toStrictEqual(["Pharmacy", "Front Desk", "Imaging"])
    })

    it("reorders the entries when a sort field is chosen", async () => {
        expect.hasAssertions()
        const wrapper = mountTable(false, unsortedNumbers)

        await wrapper.findComponent({ name: "QSelect" }).setValue("label")

        expect(locations(wrapper)).toStrictEqual(["Front Desk", "Imaging", "Pharmacy"])
    })

    it("reverses the order when the direction is toggled", async () => {
        expect.hasAssertions()
        const wrapper = mountTable(false, unsortedNumbers)
        await wrapper.findComponent({ name: "QSelect" }).setValue("label")

        await directionButton(wrapper).trigger("click")

        expect(locations(wrapper)).toStrictEqual(["Pharmacy", "Imaging", "Front Desk"])
    })

    it("hands the same sort to the table, so widening the window keeps it", async () => {
        expect.hasAssertions()
        const wrapper = mountTable(false, unsortedNumbers)

        await wrapper.findComponent({ name: "QSelect" }).setValue("label")
        await directionButton(wrapper).trigger("click")

        expect(wrapper.findComponent({ name: "QTable" }).props("pagination")).toMatchObject({
            sortBy: "label",
            descending: true,
        })
    })

    it("disables the direction toggle until a field is chosen to sort by", () => {
        expect.hasAssertions()

        const wrapper = mountTable(false, unsortedNumbers)

        expect(directionButton(wrapper).props("disable")).toBeTruthy()
    })
})
