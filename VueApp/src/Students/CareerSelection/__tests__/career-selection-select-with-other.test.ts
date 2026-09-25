import { mount } from "@vue/test-utils"
import { Quasar, QInput, QSelect } from "quasar"
import CareerSelectionSelectWithOther from "../components/CareerSelectionSelectWithOther.vue"
import type { CareerDropdownOption } from "../types"

/**
 * Tests for the dropdown that pairs with a free-text field: the text field belongs to the
 * catch-all option, and appears only while that option is selected.
 */

const OPTIONS: CareerDropdownOption[] = [
    { label: "Academia", value: 1, isOther: false },
    { label: "Other", value: 9, isOther: true },
]

function mountSelect(selectModel: CareerDropdownOption | null, otherModel = "", readOnly = false) {
    return mount(CareerSelectionSelectWithOther, {
        props: {
            label: "Career Direction",
            options: OPTIONS,
            readOnly,
            selectModel,
            otherModel,
            emptyLabel: "Not selected",
        },
        global: { plugins: [[Quasar, {}]] },
    })
}

/** The standalone label element above the field; its id is generated per instance. */
function standaloneLabel(wrapper: ReturnType<typeof mountSelect>) {
    return wrapper.get("div[id]")
}

describe("career selection select with other", () => {
    it("shows the question above the field rather than as a floating label", () => {
        expect.hasAssertions()
        const wrapper = mountSelect(OPTIONS[0])

        // A floating label is clipped to one ellipsized line, which cannot carry a full question,
        // so the dropdown is named by the visible text above it instead.
        expect(wrapper.findComponent(QSelect).props("label")).toBeUndefined()
        expect(standaloneLabel(wrapper).text()).toBe("Career Direction")
    })

    it("names the combobox from that question", () => {
        expect.hasAssertions()
        // Guards the one fragile part of this: QSelect only ever puts Quasar's own attributes on
        // the hidden focus target carrying role="combobox", so useSelectAriaLabel sets the name
        // there by hand. If Quasar changes that internal, this is what catches it.
        const wrapper = mountSelect(OPTIONS[0])

        expect(wrapper.get('[role="combobox"]').attributes("aria-labelledby")).toBe(
            standaloneLabel(wrapper).attributes("id"),
        )
    })

    it("keeps naming the combobox once the field becomes editable", async () => {
        expect.hasAssertions()
        // Quasar rebuilds the focus target across the readonly boundary, dropping the attribute
        // with it, so the composable has to re-apply it on update rather than only on mount.
        const wrapper = mountSelect(OPTIONS[0], "", true)
        await wrapper.setProps({ readOnly: false })

        expect(wrapper.get('[role="combobox"]').attributes("aria-labelledby")).toBe(
            standaloneLabel(wrapper).attributes("id"),
        )
    })

    it("shows the question once, not as both a standalone label and a floating one", () => {
        expect.hasAssertions()
        const wrapper = mountSelect(OPTIONS[0])

        expect(wrapper.text().split("Career Direction").length - 1).toBe(1)
    })

    it("hides the free-text field for an ordinary choice", () => {
        expect.hasAssertions()
        const wrapper = mountSelect(OPTIONS[0])

        expect(wrapper.findComponent(QInput).exists()).toBeFalsy()
    })

    it("hides the free-text field while nothing is selected", () => {
        expect.hasAssertions()
        const wrapper = mountSelect(null)

        expect(wrapper.findComponent(QInput).exists()).toBeFalsy()
    })

    it("shows the free-text field for the catch-all", () => {
        expect.hasAssertions()
        const wrapper = mountSelect(OPTIONS[1], "Wildlife rehabilitation")

        const input = wrapper.findComponent(QInput)
        expect(input.exists()).toBeTruthy()
        expect(input.props("modelValue")).toBe("Wildlife rehabilitation")
    })

    it("holds the free text to the column's length", () => {
        expect.hasAssertions()
        // The entity caps career free text at 200 characters.
        const wrapper = mountSelect(OPTIONS[1])

        expect(wrapper.findComponent(QInput).props("maxlength")).toBe("200")
    })

    it("shows the empty label in place of no selection", () => {
        expect.hasAssertions()
        const wrapper = mountSelect(null)

        expect(wrapper.findComponent(QSelect).props("displayValue")).toBe("Not selected")
    })

    it("shows the chosen option's label once one is selected", () => {
        expect.hasAssertions()
        const wrapper = mountSelect(OPTIONS[0])

        expect(wrapper.findComponent(QSelect).props("displayValue")).toBe("Academia")
    })

    it("locks both fields when the record is read-only", () => {
        expect.hasAssertions()
        const wrapper = mountSelect(OPTIONS[1], "Wildlife rehabilitation", true)

        expect(wrapper.findComponent(QSelect).props("readonly")).toBeTruthy()
        expect(wrapper.findComponent(QInput).props("readonly")).toBeTruthy()
    })

    it("reports a changed selection to the form", async () => {
        expect.hasAssertions()
        const wrapper = mountSelect(OPTIONS[0])

        await wrapper.findComponent(QSelect).setValue(OPTIONS[1])

        expect(wrapper.emitted("update:selectModel")).toStrictEqual([[OPTIONS[1]]])
    })
})
