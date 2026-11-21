import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import ReportAdminParameters from "../components/ReportAdminParameters.vue"
import ReportAdminParameterFields from "../components/ReportAdminParameterFields.vue"
import ReportAdminChoices from "../components/ReportAdminChoices.vue"
import { newParameter } from "../admin/report-admin-form"
import type { FormParameter } from "../admin/report-admin-form"

const global = { plugins: [[Quasar, {}]] as [typeof Quasar, object][] }

type Emitter = { emitted: (name: string) => unknown[][] | undefined }

function lastEmitted(wrapper: Emitter): unknown {
    return wrapper.emitted("update:modelValue")?.at(-1)?.[0]
}

function lastParameter(wrapper: Emitter): FormParameter {
    return lastEmitted(wrapper) as FormParameter
}

function lastParameters(wrapper: Emitter): FormParameter[] {
    return lastEmitted(wrapper) as FormParameter[]
}

function parameter(change: Partial<FormParameter> = {}): FormParameter {
    return { ...newParameter(true), name: "Dept", ...change }
}

function mountFields(model: FormParameter, isView = true) {
    return mount(ReportAdminParameterFields, {
        props: { modelValue: model, targets: ["Dept", "Hired"], isView, views: ["DeptChoices"] },
        global,
    })
}

function select(wrapper: ReturnType<typeof mountFields>, label: string) {
    return wrapper
        .findAllComponents({ name: "QSelect" })
        .find((field) => String(field.props("label")).startsWith(label))
}

function mountChoices(model: FormParameter) {
    return mount(ReportAdminChoices, { props: { modelValue: model, views: ["DeptChoices"] }, global })
}

function mountParameters(parameters: FormParameter[], isView = true) {
    return mount(ReportAdminParameters, {
        props: { modelValue: parameters, targets: ["Dept"], isView, views: [] },
        global,
    })
}

describe("report parameter fields", () => {
    it("offers the operators that suit the type, for a view", () => {
        expect.hasAssertions()

        const options = select(mountFields(parameter({ type: "Number", operator: "From" })), "Match")?.props(
            "options",
        ) as {
            value: string
        }[]

        expect(options.map((option) => option.value)).toStrictEqual(["Equals", "From", "To"])
    })

    it("has no operator for a procedure, and names the field for it", () => {
        expect.hasAssertions()
        const wrapper = mountFields(parameter({ operator: null }), false)

        expect(select(wrapper, "Match")).toBeUndefined()
        expect(select(wrapper, "Procedure parameter")).toBeDefined()
    })

    it("changes the type, fixing the operator to suit it", () => {
        expect.hasAssertions()
        const wrapper = mountFields(parameter())

        select(wrapper, "Type")?.vm.$emit("update:modelValue", "MultiChoice")

        expect([lastParameter(wrapper).type, lastParameter(wrapper).operator]).toStrictEqual(["MultiChoice", "In"])
    })

    it("updates the name, label, binding, operator, default and required flag", () => {
        expect.hasAssertions()
        const wrapper = mountFields(parameter())

        select(wrapper, "Column to filter")?.vm.$emit("update:modelValue", null)
        expect(lastParameter(wrapper).name).toBe("")
        wrapper.findAllComponents({ name: "QInput" })[0]?.vm.$emit("update:modelValue", " ")
        expect(lastParameter(wrapper).label).toBeNull()
        select(wrapper, "Match")?.vm.$emit("update:modelValue", "Contains")
        expect(lastParameter(wrapper).operator).toBe("Contains")
        wrapper.findAllComponents({ name: "QInput" })[1]?.vm.$emit("update:modelValue", "VME")
        expect(lastParameter(wrapper).defaultValue).toBe("VME")
        wrapper.findComponent({ name: "QCheckbox" }).vm.$emit("update:modelValue", true)
        expect(lastParameter(wrapper).required).toBeTruthy()
    })

    it("explains a parameter filled from the signed-in user, without type or choices", () => {
        expect.hasAssertions()
        const wrapper = mountFields(parameter({ binding: "CurrentUserLoginId" }))

        select(wrapper, "Value comes from")?.vm.$emit("update:modelValue", "UserInput")

        expect(wrapper.text()).toContain("Filled in from the signed-in user")
        expect(select(wrapper, "Type")).toBeUndefined()
        expect(lastParameter(wrapper).binding).toBe("UserInput")
    })

    it("shows the choices for choice parameters only", () => {
        expect.hasAssertions()

        expect(
            mountFields(parameter({ type: "Choice" }))
                .findComponent(ReportAdminChoices)
                .exists(),
        ).toBeTruthy()
        expect(mountFields(parameter()).findComponent(ReportAdminChoices).exists()).toBeFalsy()
    })

    it("passes a change to the choices on", () => {
        expect.hasAssertions()
        const wrapper = mountFields(parameter({ type: "Choice" }))

        wrapper
            .findComponent(ReportAdminChoices)
            .vm.$emit("update:modelValue", parameter({ type: "Choice", choicesText: "VME" }))

        expect(lastParameter(wrapper).choicesText).toBe("VME")
    })
})

describe("report choices", () => {
    it("edits a fixed list as text", () => {
        expect.hasAssertions()
        const wrapper = mountChoices(parameter({ type: "Choice" }))

        wrapper.findComponent({ name: "QInput" }).vm.$emit("update:modelValue", null)

        expect(lastParameter(wrapper).choicesText).toBe("")
    })

    it("switches to a view and picks it", async () => {
        expect.hasAssertions()
        const wrapper = mountChoices(parameter({ type: "Choice" }))

        wrapper.findComponent({ name: "QOptionGroup" }).vm.$emit("update:modelValue", "view")
        await wrapper.setProps({ modelValue: lastParameter(wrapper) })
        wrapper.findComponent({ name: "QSelect" }).vm.$emit("update:modelValue", "DeptChoices")

        expect([lastParameter(wrapper).choicesFrom, lastParameter(wrapper).choicesSource]).toStrictEqual([
            "view",
            "DeptChoices",
        ])
    })
})

describe("report parameters section", () => {
    it("explains what parameters do for a view and a procedure", () => {
        expect.hasAssertions()

        expect(mountParameters([]).text()).toContain("Filters the user fills in")
        expect(mountParameters([], false).text()).toContain("Values passed to the procedure.")
    })

    it("adds, changes and removes parameters", async () => {
        expect.hasAssertions()
        const first = parameter()
        const second = parameter({ name: "Unit" })
        const adding = mountParameters([first])
        const changing = mountParameters([first, second])
        const removing = mountParameters([first])

        await adding
            .findAll("button")
            .find((button) => button.text().includes("Add parameter"))
            ?.trigger("click")
        changing
            .findAllComponents(ReportAdminParameterFields)[0]
            ?.vm.$emit("update:modelValue", { ...first, label: "Department" })
        await removing.get(`[aria-label="Remove Dept"]`).trigger("click")

        expect(lastParameters(adding)).toHaveLength(2)
        expect(lastParameters(changing)).toStrictEqual([{ ...first, label: "Department" }, second])
        expect(lastParameters(removing)).toStrictEqual([])
    })

    it("reorders parameters", () => {
        expect.hasAssertions()
        const first = parameter()
        const second = parameter({ name: "Unit" })
        const wrapper = mountParameters([first, second])

        wrapper.findComponent({ name: "SortableList" }).vm.$emit("update:modelValue", [second, first])

        expect(lastParameters(wrapper)).toStrictEqual([second, first])
    })

    it("announces a moved parameter by name", () => {
        expect.hasAssertions()

        const { announce } = mountParameters([parameter()])
            .findComponent({ name: "SortableList" })
            .props() as unknown as {
            announce: (item: FormParameter, index: number, total: number) => string
        }

        expect([announce(parameter(), 0, 2), announce(parameter({ name: "" }), 1, 2)]).toStrictEqual([
            "Dept moved to position 1 of 2",
            "Parameter moved to position 2 of 2",
        ])
    })
})
