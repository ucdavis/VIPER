import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import ReportAdminBasics from "../components/ReportAdminBasics.vue"
import ReportAdminColumns from "../components/ReportAdminColumns.vue"
import ReportAdminDisplay from "../components/ReportAdminDisplay.vue"
import { newColumn, toForm } from "../admin/report-admin-form"
import type { FormColumn, ReportAdminForm } from "../admin/report-admin-form"
import { rosterReport } from "./report-admin-fixtures"

const global = { plugins: [[Quasar, {}]] as [typeof Quasar, object][] }

type Emitter = { emitted: (name: string) => unknown[][] | undefined }

function lastEmitted(wrapper: Emitter): unknown {
    return wrapper.emitted("update:modelValue")?.at(-1)?.[0]
}

function lastForm(wrapper: Emitter): ReportAdminForm {
    return lastEmitted(wrapper) as ReportAdminForm
}

function lastColumns(wrapper: Emitter): FormColumn[] {
    return lastEmitted(wrapper) as FormColumn[]
}

function mountBasics(props: Record<string, unknown> = {}) {
    return mount(ReportAdminBasics, {
        props: {
            modelValue: toForm(rosterReport),
            sources: [
                { name: "FacultyRoster", kind: "View", description: "Current faculty" },
                { name: "usp_Leave", kind: "Procedure", description: null },
            ],
            ...props,
        },
        global,
    })
}

function mountColumns(columns: FormColumn[], sourceColumns = ["Name", "Hired"], canAddMissing = false) {
    return mount(ReportAdminColumns, { props: { modelValue: columns, sourceColumns, canAddMissing }, global })
}

function mountDisplay(form: Partial<ReportAdminForm> = {}, sourceColumns = ["Name", "Hired"]) {
    return mount(ReportAdminDisplay, {
        props: { modelValue: { ...toForm(rosterReport), ...form }, sourceColumns },
        global,
    })
}

describe("report basics section", () => {
    it("offers each source with its kind and description", () => {
        expect.hasAssertions()

        const select = mountBasics().findAllComponents({ name: "QSelect" }).at(-1)

        expect(select?.props("options")).toStrictEqual([
            { value: "FacultyRoster", label: "FacultyRoster", caption: "View: Current faculty" },
            { value: "usp_Leave", label: "usp_Leave", caption: "Procedure" },
        ])
    })

    it("updates the title, area and description, keeping blanks as empty", async () => {
        expect.hasAssertions()
        const wrapper = mountBasics()
        const [title, area, description] = wrapper.findAllComponents({ name: "QInput" })

        title?.vm.$emit("update:modelValue", "Roster")
        await wrapper.setProps({ modelValue: lastForm(wrapper) })
        area?.vm.$emit("update:modelValue", "  ")
        await wrapper.setProps({ modelValue: lastForm(wrapper) })
        description?.vm.$emit("update:modelValue", "A roster of faculty.")

        expect([lastForm(wrapper).title, lastForm(wrapper).area, lastForm(wrapper).description]).toStrictEqual([
            "Roster",
            null,
            "A roster of faculty.",
        ])
    })

    it("asks for a new source rather than changing it itself", () => {
        expect.hasAssertions()
        const wrapper = mountBasics()

        wrapper.findAllComponents({ name: "QSelect" }).at(-1)?.vm.$emit("update:modelValue", null)

        expect(wrapper.emitted("source")).toStrictEqual([[""]])
    })

    it("explains a missing source list and a procedure SQL Server can't describe", () => {
        expect.hasAssertions()

        const text = mountBasics({ sourcesFailed: true, columnsError: "Uses a temp table." }).text()

        expect(text).toContain("The list of data sources couldn't be loaded.")
        expect(text).toContain("SQL Server can't describe this procedure's columns (Uses a temp table.)")
    })

    // QSelect virtualizes its option list, which jsdom's layout never populates, so a mount of
    // the real component never renders the #option slot. Stub QSelect to render it directly.
    it("shows each source's kind and description in the option list", () => {
        expect.hasAssertions()
        const QSelectStub = {
            name: "QSelect",
            props: ["options"],
            template: `
                <template v-for="opt in options" :key="opt.value">
                    <slot name="option" :opt="opt" :item-props="{}" />
                </template>
            `,
        }

        const wrapper = mount(ReportAdminBasics, {
            props: {
                modelValue: toForm(rosterReport),
                sources: [
                    { name: "FacultyRoster", kind: "View", description: "Current faculty" },
                    { name: "usp_Leave", kind: "Procedure", description: null },
                ],
            },
            global: { ...global, stubs: { QSelect: QSelectStub } },
        })

        expect(wrapper.text()).toContain("FacultyRoster")
        expect(wrapper.text()).toContain("View: Current faculty")
        expect(wrapper.text()).toContain("usp_Leave")
        expect(wrapper.text()).toContain("Procedure")
    })
})

describe("report columns section", () => {
    it("edits a column's heading, format and flags, leaving the others alone", async () => {
        expect.hasAssertions()
        const column = newColumn("Name")
        const other = newColumn("Hired")
        const wrapper = mountColumns([column, other])

        wrapper.findComponent({ name: "QInput" }).vm.$emit("update:modelValue", "Full name")
        await wrapper.setProps({ modelValue: lastColumns(wrapper) })
        wrapper.findComponent({ name: "QInput" }).vm.$emit("update:modelValue", " ")
        await wrapper.setProps({ modelValue: lastColumns(wrapper) })
        wrapper.findAllComponents({ name: "QCheckbox" })[0]?.vm.$emit("update:modelValue", false)
        await wrapper.setProps({ modelValue: lastColumns(wrapper) })
        wrapper.findAllComponents({ name: "QCheckbox" })[1]?.vm.$emit("update:modelValue", false)

        expect(lastColumns(wrapper)).toStrictEqual([{ ...column, label: null, visible: false, sortable: false }, other])
    })

    it("changes the source column and format", () => {
        expect.hasAssertions()
        const column = newColumn("Name")
        const wrapper = mountColumns([column])
        const [name, format] = wrapper.findAllComponents({ name: "QSelect" })

        name?.vm.$emit("update:modelValue", null)
        format?.vm.$emit("update:modelValue", "Date")

        expect(lastColumns(wrapper)[0]?.format).toBe("Date")
        expect(wrapper.emitted("update:modelValue")?.[0]?.[0]).toStrictEqual([{ ...column, sourceColumn: "" }])
    })

    it("lets the admin type column names a procedure can't describe", () => {
        expect.hasAssertions()

        const select = mountColumns([newColumn()], []).findComponent({ name: "QSelect" })

        expect([select.props("useInput"), select.props("newValueMode")]).toStrictEqual([true, "add-unique"])
    })

    it("adds and removes columns", async () => {
        expect.hasAssertions()
        const column = newColumn("Name")
        const adding = mountColumns([column])
        const removing = mountColumns([column])

        await adding
            .findAll("button")
            .find((button) => button.text().includes("Add column"))
            ?.trigger("click")
        await removing.get(`[aria-label="Remove Name"]`).trigger("click")

        expect(lastColumns(adding)).toHaveLength(2)
        expect(lastColumns(removing)).toStrictEqual([])
    })

    it("reorders columns", () => {
        expect.hasAssertions()
        const first = newColumn("Name")
        const second = newColumn("Hired")
        const wrapper = mountColumns([first, second])

        wrapper.findComponent({ name: "SortableList" }).vm.$emit("update:modelValue", [second, first])

        expect(lastColumns(wrapper)).toStrictEqual([second, first])
    })

    it("offers the source's other columns when there are some", async () => {
        expect.hasAssertions()
        const wrapper = mountColumns([], ["Name"], true)

        await wrapper
            .findAll("button")
            .find((button) => button.text().includes("other columns"))
            ?.trigger("click")

        expect(wrapper.emitted("addMissing")).toHaveLength(1)
        expect(wrapper.text()).toContain("No columns yet.")
    })

    it("announces a moved column by name", () => {
        expect.hasAssertions()

        const { announce } = mountColumns([newColumn("Name")])
            .findComponent({ name: "SortableList" })
            .props() as unknown as {
            announce: (column: FormColumn, index: number, total: number) => string
        }

        expect([announce(newColumn("Name"), 1, 3), announce(newColumn(), 0, 1)]).toStrictEqual([
            "Name moved to position 2 of 3",
            "Column moved to position 1 of 1",
        ])
    })
})

describe("report display section", () => {
    it("sorts by the source's columns, or the report's when the source can't be described", () => {
        expect.hasAssertions()

        const known = mountDisplay().findComponent({ name: "QSelect" }).props("options")
        const unknown = mountDisplay({}, []).findComponent({ name: "QSelect" }).props("options")

        expect([known, unknown]).toStrictEqual([["Name", "Hired"], ["Name"]])
    })

    it("keeps the page size as a number, or empty for the default", () => {
        expect.hasAssertions()
        const wrapper = mountDisplay()
        const pageSize = wrapper.findComponent({ name: "QInput" })

        pageSize.vm.$emit("update:modelValue", "50")
        const fifty = lastForm(wrapper).pageSize
        pageSize.vm.$emit("update:modelValue", "")

        expect([fifty, lastForm(wrapper).pageSize]).toStrictEqual([50, null])
    })

    it("updates the sort, downloads and notice", () => {
        expect.hasAssertions()
        const wrapper = mountDisplay()

        wrapper.findComponent({ name: "QSelect" }).vm.$emit("update:modelValue", null)
        const sort = lastForm(wrapper).orderBy
        wrapper.findAllComponents({ name: "QCheckbox" })[0]?.vm.$emit("update:modelValue", false)
        const excel = lastForm(wrapper).allowExcel
        wrapper.findAllComponents({ name: "QCheckbox" })[1]?.vm.$emit("update:modelValue", true)
        const pdf = lastForm(wrapper).allowPdf
        wrapper.findAllComponents({ name: "QCheckbox" })[2]?.vm.$emit("update:modelValue", true)
        const landscape = lastForm(wrapper).pdfLandscape
        wrapper.findAllComponents({ name: "QInput" }).at(-1)?.vm.$emit("update:modelValue", " ")
        const blanked = lastForm(wrapper).sensitiveNotice
        wrapper.findAllComponents({ name: "QInput" }).at(-1)?.vm.$emit("update:modelValue", "Confidential data")

        expect([sort, excel, pdf, landscape, blanked, lastForm(wrapper).sensitiveNotice]).toStrictEqual([
            null,
            false,
            true,
            true,
            null,
            "Confidential data",
        ])
    })

    it("turns paging and descending sort on and off", () => {
        expect.hasAssertions()
        const wrapper = mountDisplay()
        const [paginate, descending] = wrapper.findAllComponents({ name: "QToggle" })

        paginate?.vm.$emit("update:modelValue", false)
        const paged = lastForm(wrapper).paginate
        descending?.vm.$emit("update:modelValue", true)

        expect([paged, lastForm(wrapper).orderByDescending]).toStrictEqual([false, true])
    })
})
