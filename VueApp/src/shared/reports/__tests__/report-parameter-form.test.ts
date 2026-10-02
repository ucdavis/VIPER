import { flushPromises, mount } from "@vue/test-utils"
import { QSelect, Quasar } from "quasar"
import ReportParameterForm from "../ReportParameterForm.vue"
import type { ReportParameterMetadata, ReportParameterType, ReportParameterValues } from "../report-types"

function parameter(name: string, type: ReportParameterType, required = false): ReportParameterMetadata {
    return {
        name,
        label: name,
        type,
        required,
        defaultValue: null,
        options: [
            { value: "S", label: "Senate" },
            { value: "F", label: "Federation" },
        ],
    }
}

const parameters = [
    parameter("facultyType", "Choice", true),
    parameter("departments", "MultiChoice"),
    parameter("startDate", "Date"),
    parameter("search", "Text"),
    parameter("minimumAge", "Number"),
    parameter("includeEmeriti", "Boolean"),
]

const empty: ReportParameterValues = {
    facultyType: null,
    departments: [],
    startDate: null,
    search: null,
    minimumAge: null,
    includeEmeriti: null,
}

function mountForm(modelValue: ReportParameterValues = empty) {
    return mount(ReportParameterForm, {
        props: { parameters, modelValue, running: false },
        global: { plugins: [[Quasar, {}]] },
    })
}

function lastModel(wrapper: ReturnType<typeof mountForm>): ReportParameterValues | undefined {
    return wrapper.emitted("update:modelValue")?.at(-1)?.[0] as ReportParameterValues | undefined
}

describe("ReportParameterForm", () => {
    it("renders an input for each parameter type", () => {
        expect.hasAssertions()
        const wrapper = mountForm()

        expect(wrapper.findAll(".q-select")).toHaveLength(2)
        expect(wrapper.find("input[type='date']").exists()).toBeTruthy()
        expect(wrapper.find("input[type='text']").exists()).toBeTruthy()
        expect(wrapper.find("input[type='number']").exists()).toBeTruthy()
        expect(wrapper.find(".q-checkbox").exists()).toBeTruthy()
    })

    it("marks required fields in their labels", () => {
        expect.hasAssertions()
        const wrapper = mountForm()

        expect(wrapper.text()).toContain("facultyType *")
        expect(wrapper.text()).not.toContain("search *")
    })

    it("shows the current text, date and number values", () => {
        expect.hasAssertions()
        const wrapper = mountForm({ ...empty, startDate: "2026-07-01", search: "smith", minimumAge: 60 })

        expect((wrapper.get("input[type='date']").element as HTMLInputElement).value).toBe("2026-07-01")
        expect((wrapper.get("input[type='text']").element as HTMLInputElement).value).toBe("smith")
        expect((wrapper.get("input[type='number']").element as HTMLInputElement).value).toBe("60")
    })

    it("stores numbers as numbers and cleared inputs as null", async () => {
        expect.hasAssertions()
        const wrapper = mountForm()
        const input = wrapper.get("input[type='number']")

        await input.setValue("60")
        expect(lastModel(wrapper)?.minimumAge).toBe(60)
        await input.setValue("")
        expect(lastModel(wrapper)?.minimumAge).toBeNull()
    })

    it("keeps text and dates as strings", async () => {
        expect.hasAssertions()
        const wrapper = mountForm()

        await wrapper.get("input[type='date']").setValue("2026-07-01")
        expect(lastModel(wrapper)?.startDate).toBe("2026-07-01")
        await wrapper.get("input[type='text']").setValue("smith")
        expect(lastModel(wrapper)?.search).toBe("smith")
    })

    it("stores the chosen options of a select", () => {
        expect.hasAssertions()
        const wrapper = mountForm()
        const [choice, multiChoice] = wrapper.findAllComponents(QSelect)

        choice?.vm.$emit("update:modelValue", "F")
        expect(lastModel(wrapper)?.facultyType).toBe("F")
        multiChoice?.vm.$emit("update:modelValue", ["S", "F"])
        expect(lastModel(wrapper)?.departments).toStrictEqual(["S", "F"])
    })

    it("updates a checkbox value without losing the others", async () => {
        expect.hasAssertions()
        const wrapper = mountForm({ ...empty, search: "smith" })

        await wrapper.get(".q-checkbox").trigger("click")

        expect(lastModel(wrapper)).toStrictEqual({ ...empty, search: "smith", includeEmeriti: true })
    })

    it("blocks submitting until required fields are filled", async () => {
        expect.hasAssertions()
        const wrapper = mountForm()

        await wrapper.get("form").trigger("submit")
        await flushPromises()

        expect(wrapper.emitted("submit")).toBeUndefined()
        expect(wrapper.text()).toContain("facultyType is required.")
    })

    it("submits when the required fields are filled", async () => {
        expect.hasAssertions()
        const wrapper = mountForm({ ...empty, facultyType: "S" })

        await wrapper.get("form").trigger("submit")
        await flushPromises()

        expect(wrapper.emitted("submit")).toHaveLength(1)
    })
})
