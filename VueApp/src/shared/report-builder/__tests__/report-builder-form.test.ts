import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import ReportBuilderForm from "../ReportBuilderForm.vue"
import ReportChoiceField from "../ReportChoiceField.vue"
import type { ReportParameterValues, ReportViewerParameter } from "../report-builder-types"
import { deptParameter, hiredParameter, textParameter } from "./report-builder-fixtures"

const paidParameter: ReportViewerParameter = {
    name: "Paid",
    label: "Paid",
    type: "Boolean",
    required: false,
    defaultValue: null,
    choices: null,
}
const countParameter: ReportViewerParameter = { ...textParameter, name: "Count", label: "Count", type: "Number" }

function mountForm(parameters: ReportViewerParameter[], values: ReportParameterValues = {}) {
    return mount(ReportBuilderForm, {
        props: {
            parameters,
            modelValue: values,
            "onUpdate:modelValue": vi.fn<(values: ReportParameterValues) => void>(),
        },
        global: { plugins: [[Quasar, {}]] },
    })
}

describe("report form", () => {
    it("shows a field per parameter, marking the required ones", () => {
        expect.hasAssertions()

        const wrapper = mountForm([textParameter, hiredParameter])

        expect(wrapper.findAll("input").map((input) => input.attributes("type"))).toStrictEqual(["text", "date"])
        expect(wrapper.text()).toContain("Hired from *")
    })

    it("offers choices for choice parameters, and Yes or No for a yes/no parameter", () => {
        expect.hasAssertions()

        const fields = mountForm([deptParameter, paidParameter]).findAllComponents(ReportChoiceField)

        expect(fields.map((field) => field.props("choices").map((choice) => choice.label))).toStrictEqual([
            ["Medicine", "Pathology"],
            ["Yes", "No"],
        ])
        expect(fields.map((field) => field.props("multiple"))).toStrictEqual([true, false])
    })

    it("treats a choice parameter without choices as having none", () => {
        expect.hasAssertions()

        const field = mountForm([{ ...deptParameter, choices: null }]).findComponent(ReportChoiceField)

        expect(field.props("choices")).toStrictEqual([])
    })

    it("keeps typed values as text, and a cleared field as empty", async () => {
        expect.hasAssertions()
        const wrapper = mountForm([countParameter, textParameter], { Count: null, Name: "Ada" })

        await wrapper.findAll("input")[0]!.setValue("12")
        await wrapper.setProps({
            modelValue: wrapper.emitted<[ReportParameterValues]>("update:modelValue")!.at(-1)![0],
        })
        await wrapper.findAll("input")[1]!.setValue("")

        expect(wrapper.emitted<[ReportParameterValues]>("update:modelValue")!.at(-1)![0]).toStrictEqual({
            Count: "12",
            Name: null,
        })
    })

    it("passes a choice on to the model", () => {
        expect.hasAssertions()
        const wrapper = mountForm([deptParameter], { Dept: [] })

        wrapper.findComponent(ReportChoiceField).vm.$emit("update:modelValue", ["VME"])

        expect(wrapper.emitted<[ReportParameterValues]>("update:modelValue")!.at(-1)![0]).toStrictEqual({
            Dept: ["VME"],
        })
    })

    it("has no Run button: the report runs when the values change", () => {
        expect.hasAssertions()

        expect(mountForm([textParameter]).find("button[type=submit]").exists()).toBeFalsy()
    })

    it("submits when every required field is filled", async () => {
        expect.hasAssertions()
        const wrapper = mountForm([hiredParameter], { Hired: "2026-01-01" })

        await wrapper.get("form").trigger("submit")
        await vi.waitFor(() => expect(wrapper.emitted("submit")).toHaveLength(1))
    })

    it("explains an empty required field instead of submitting", async () => {
        expect.hasAssertions()
        const wrapper = mountForm([hiredParameter], { Hired: null })

        await wrapper.get("form").trigger("submit")
        await vi.waitFor(() => expect(wrapper.text()).toContain("Hired from is required."))

        expect(wrapper.emitted("submit")).toBeUndefined()
    })
})
