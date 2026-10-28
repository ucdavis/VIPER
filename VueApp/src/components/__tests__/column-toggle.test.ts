import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import type { QTableProps } from "quasar"
import ColumnToggle from "@/components/ColumnToggle.vue"

/**
 * ColumnToggle is the checklist behind a table's `visible-columns`. QMenu renders into a portal
 * that jsdom cannot measure, so it is stubbed to a passthrough and the checkboxes are driven
 * directly.
 */

const columns: NonNullable<QTableProps["columns"]> = [
    { name: "fullName", label: "Name", field: "fullName" },
    { name: "email", label: "Email", field: "email" },
    { name: "mentor", label: "Mentor", field: "mentorName" },
]

function mountToggle(modelValue: string[]) {
    return mount(ColumnToggle, {
        props: { columns, modelValue },
        global: {
            plugins: [[Quasar, {}]],
            stubs: { QMenu: { template: "<div><slot /></div>" } },
        },
    })
}

const checkboxes = (wrapper: ReturnType<typeof mountToggle>) => wrapper.findAllComponents({ name: "QCheckbox" })

describe("column toggle", () => {
    it("offers every column by its header label", () => {
        expect.hasAssertions()
        const labels = checkboxes(mountToggle(["fullName", "email", "mentor"])).map((c) => c.props("label"))

        expect(labels).toStrictEqual(["Name", "Email", "Mentor"])
    })

    it("checks the columns the table is showing", () => {
        expect.hasAssertions()
        const checked = checkboxes(mountToggle(["fullName", "mentor"])).map((c) => c.props("modelValue"))

        expect(checked).toStrictEqual([
            ["fullName", "mentor"],
            ["fullName", "mentor"],
            ["fullName", "mentor"],
        ])
    })

    it("drops a column from the selection when it is unchecked", async () => {
        expect.hasAssertions()
        const wrapper = mountToggle(["fullName", "email", "mentor"])

        await checkboxes(wrapper)[1].trigger("click")

        expect(wrapper.emitted("update:modelValue")).toStrictEqual([[["fullName", "mentor"]]])
    })

    it("puts a column back when it is checked again", async () => {
        expect.hasAssertions()
        const wrapper = mountToggle(["fullName"])

        await checkboxes(wrapper)[2].trigger("click")

        expect(wrapper.emitted("update:modelValue")).toStrictEqual([[["fullName", "mentor"]]])
    })

    it("names the button for the columns it controls", () => {
        expect.hasAssertions()
        expect(mountToggle([]).findComponent({ name: "QBtn" }).props("label")).toBe("Columns")
    })
})
