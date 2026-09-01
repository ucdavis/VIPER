import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import MobileSortControl from "../components/MobileSortControl.vue"
import type { SortOption } from "../composables/use-mobile-table-rows"

const options: SortOption[] = [
    { label: "Name", value: "name" },
    { label: "Office", value: "office" },
]

function mountControl(sortBy: string | null = null, descending = false, sortOptions = options) {
    return mount(MobileSortControl, {
        props: { modelValue: sortBy, descending, options: sortOptions },
        global: { plugins: [Quasar] },
    })
}

function directionButton(control: ReturnType<typeof mountControl>) {
    return control.findAllComponents({ name: "QBtn" }).find((btn) => String(btn.props("icon")).startsWith("arrow_"))!
}

describe("mobileSortControl.vue", () => {
    it("renders nothing for a table with no sortable column", () => {
        expect.hasAssertions()

        expect(mountControl(null, false, []).findComponent({ name: "QSelect" }).exists()).toBeFalsy()
    })

    it("offers the options it is given", () => {
        expect.hasAssertions()

        expect(mountControl().findComponent({ name: "QSelect" }).props("options")).toStrictEqual(options)
    })

    it("reports the chosen field to the caller", async () => {
        expect.hasAssertions()
        const wrapper = mountControl()

        await wrapper.findComponent({ name: "QSelect" }).setValue("office")

        expect(wrapper.emitted("update:modelValue")?.at(-1)).toStrictEqual(["office"])
    })

    it("reports a direction change to the caller", async () => {
        expect.hasAssertions()
        const wrapper = mountControl("name")

        await directionButton(wrapper).trigger("click")

        expect(wrapper.emitted("update:descending")?.at(-1)).toStrictEqual([true])
    })

    it("disables the direction toggle until a field is chosen", () => {
        expect.hasAssertions()

        expect(directionButton(mountControl()).props("disable")).toBeTruthy()
    })

    it("names the direction button for the action it performs, not the state it is in", () => {
        expect.hasAssertions()

        expect(directionButton(mountControl("name", false)).attributes("aria-label")).toBe("Sort descending")
        expect(directionButton(mountControl("name", true)).attributes("aria-label")).toBe("Sort ascending")
    })

    it("gives the field select a real label, so it has an accessible name", () => {
        expect.hasAssertions()

        expect(mountControl().findComponent({ name: "QSelect" }).props("label")).toBe("Sort by")
    })
})
