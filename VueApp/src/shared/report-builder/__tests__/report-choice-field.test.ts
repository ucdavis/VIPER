import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import ReportChoiceField from "../ReportChoiceField.vue"
import { deptParameter } from "./report-builder-fixtures"

const choices = deptParameter.choices!

function mountField(multiple = false) {
    return mount(ReportChoiceField, {
        props: { modelValue: null, label: "Department", choices, multiple },
        global: { plugins: [[Quasar, {}]] },
    })
}

/** Types into the select the way QSelect reports it: a filter event with an update callback. */
function filterTo(wrapper: ReturnType<typeof mountField>, text: string): void {
    wrapper
        .findComponent({ name: "QSelect" })
        .vm.$emit("filter", text, (update: () => void) => update(), vi.fn<() => void>())
}

describe("choice field", () => {
    it("narrows the choices to the typed text, ignoring case", async () => {
        expect.hasAssertions()
        const wrapper = mountField()

        filterTo(wrapper, "PATH")
        await wrapper.vm.$nextTick()

        expect(wrapper.findComponent({ name: "QSelect" }).props("options")).toStrictEqual([choices[1]])
    })

    it("shows every choice when the text is cleared", async () => {
        expect.hasAssertions()
        const wrapper = mountField()

        filterTo(wrapper, "path")
        filterTo(wrapper, " ")
        await wrapper.vm.$nextTick()

        expect(wrapper.findComponent({ name: "QSelect" }).props("options")).toStrictEqual(choices)
    })

    it("follows a new list of choices", async () => {
        expect.hasAssertions()
        const wrapper = mountField(true)

        await wrapper.setProps({ choices: [choices[0]!] })

        expect(wrapper.findComponent({ name: "QSelect" }).props("options")).toStrictEqual([choices[0]])
    })

    it("passes the picked value on", () => {
        expect.hasAssertions()
        const wrapper = mountField()

        wrapper.findComponent({ name: "QSelect" }).vm.$emit("update:modelValue", "VME")

        expect(wrapper.emitted("update:modelValue")).toStrictEqual([["VME"]])
    })

    // QSelect virtualizes its option list, which jsdom's layout never populates, so a mount of
    // the real component never renders the #no-option slot. Stub QSelect to render it directly.
    it("says when nothing matches", () => {
        expect.hasAssertions()
        const QSelectStub = { name: "QSelect", template: `<div><slot name="no-option" /></div>` }

        const wrapper = mount(ReportChoiceField, {
            props: { modelValue: null, label: "Department", choices },
            global: { plugins: [[Quasar, {}]], stubs: { QSelect: QSelectStub } },
        })

        expect(wrapper.text()).toContain("No matching choices")
    })
})
