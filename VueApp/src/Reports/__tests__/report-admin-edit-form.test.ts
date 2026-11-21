import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import ReportAdminEditForm from "../components/ReportAdminEditForm.vue"
import { toForm } from "../admin/report-admin-form"
import { rosterReport, saved } from "./report-admin-fixtures"

/**
 * ReportAdminEditForm only wires its children's v-models and events to its own props, so each
 * child is stubbed and its events are emitted directly: the children's own behavior is covered by
 * their own tests.
 */
const global = {
    plugins: [[Quasar, {}]] as [typeof Quasar, object][],
    stubs: {
        ReportAdminStatus: true,
        ReportAdminBasics: true,
        ReportAdminColumns: true,
        ReportAdminParameters: true,
        ReportAdminDisplay: true,
        ReportAdminPermissions: true,
        ReportAdminPreview: true,
        ReportAdminHistory: true,
    },
}

function mountForm(overrides: Record<string, unknown> = {}) {
    return mount(ReportAdminEditForm, {
        props: {
            modelValue: toForm(rosterReport),
            definition: saved,
            sources: [],
            sourcesFailed: false,
            columnsError: null,
            sourceColumns: [],
            canAddMissing: false,
            targets: [],
            isView: true,
            views: [],
            errors: [],
            conflict: false,
            saving: false,
            isDirty: false,
            request: rosterReport,
            changeSource: vi.fn<() => Promise<void>>(),
            addMissingColumns: vi.fn<() => void>(),
            save: vi.fn<() => Promise<void>>(),
            setDeleted: vi.fn<() => Promise<void>>(),
            reload: vi.fn<() => Promise<void>>(),
            applyVersion: vi.fn<() => Promise<void>>(),
            ...overrides,
        },
        global,
    })
}

function saveButton(wrapper: ReturnType<typeof mountForm>) {
    return wrapper.findAllComponents({ name: "QBtn" }).find((button) => button.props("type") === "submit")
}

describe("report admin edit form", () => {
    it("deletes and restores through the status bar", () => {
        expect.hasAssertions()
        const setDeleted = vi.fn<() => Promise<void>>()
        const wrapper = mountForm({ setDeleted })

        wrapper.findComponent({ name: "ReportAdminStatus" }).vm.$emit("delete")
        wrapper.findComponent({ name: "ReportAdminStatus" }).vm.$emit("restore")

        expect(setDeleted.mock.calls).toStrictEqual([[true], [false]])
    })

    it("replaces the whole form from basics and display", () => {
        expect.hasAssertions()
        const wrapper = mountForm()
        const form = toForm(rosterReport)

        wrapper.findComponent({ name: "ReportAdminBasics" }).vm.$emit("update:modelValue", { ...form, title: "New" })
        wrapper.findComponent({ name: "ReportAdminDisplay" }).vm.$emit("update:modelValue", { ...form, area: "Effort" })

        const updates = wrapper.emitted("update:modelValue")!
        expect(updates.map((update) => update[0])).toStrictEqual([
            { ...form, title: "New" },
            { ...form, area: "Effort" },
        ])
    })

    // Columns/parameters/permissions each v-model one field of the form rather than the whole
    // thing, so a child's update mutates that field on the shared form object in place - it
    // doesn't itself emit update:modelValue (ReportAdminEditForm only forwards whole-form
    // replacements upward). The mutation still reaches whoever holds the same form reference,
    // including the test's own modelValue prop.
    it("updates the form's columns, parameters and permissions in place", () => {
        expect.hasAssertions()
        const wrapper = mountForm()

        wrapper.findComponent({ name: "ReportAdminColumns" }).vm.$emit("update:modelValue", [])
        wrapper.findComponent({ name: "ReportAdminParameters" }).vm.$emit("update:modelValue", [])
        wrapper.findComponent({ name: "ReportAdminPermissions" }).vm.$emit("update:modelValue", ["SVMSecure.Effort"])

        const form = wrapper.props("modelValue") as ReturnType<typeof toForm>
        expect([form.columns, form.parameters, form.permissions]).toStrictEqual([[], [], ["SVMSecure.Effort"]])
        expect(wrapper.emitted("update:modelValue")).toBeUndefined()
    })

    it("asks for a new source", () => {
        expect.hasAssertions()
        const changeSource = vi.fn<() => Promise<void>>()
        const wrapper = mountForm({ changeSource })

        wrapper.findComponent({ name: "ReportAdminBasics" }).vm.$emit("source", "usp_Leave")

        expect(changeSource).toHaveBeenCalledWith("usp_Leave")
    })

    it("adds the source's missing columns", () => {
        expect.hasAssertions()
        const addMissingColumns = vi.fn<() => void>()
        const wrapper = mountForm({ addMissingColumns })

        wrapper.findComponent({ name: "ReportAdminColumns" }).vm.$emit("add-missing")

        expect(addMissingColumns).toHaveBeenCalledOnce()
    })

    it("applies an earlier version from history", () => {
        expect.hasAssertions()
        const applyVersion = vi.fn<() => Promise<void>>()
        const wrapper = mountForm({ applyVersion })

        wrapper.findComponent({ name: "ReportAdminHistory" }).vm.$emit("load", rosterReport)

        expect(applyVersion).toHaveBeenCalledWith(rosterReport)
    })

    it("submits the form to save", async () => {
        expect.hasAssertions()
        const save = vi.fn<() => Promise<void>>()
        const wrapper = mountForm({ save })

        await wrapper.get("form").trigger("submit")

        expect(save).toHaveBeenCalledOnce()
    })

    it("shows the errors, a reload action when they conflict, and unsaved-changes note", async () => {
        expect.hasAssertions()
        const reload = vi.fn<() => Promise<void>>()
        const wrapper = mountForm({ errors: ["Someone else saved it."], conflict: true, isDirty: true, reload })

        expect(wrapper.text()).toContain("Someone else saved it.")
        expect(wrapper.text()).toContain("Unsaved changes")

        await wrapper
            .findAll("button")
            .find((button) => button.text() === "Reload the saved report")
            ?.trigger("click")

        expect(reload).toHaveBeenCalledOnce()
    })

    it("labels and disables the save button for a new, saved or deleted report", () => {
        expect.hasAssertions()
        const creating = mountForm({ definition: null })
        const deleted = mountForm({ definition: { ...saved, deletedOn: "2026-11-16T10:00:00" } })

        expect(saveButton(creating)?.props("label")).toBe("Create report")
        expect(saveButton(mountForm())?.props("label")).toBe("Save")
        expect(saveButton(deleted)?.props("disable")).toBeTruthy()
        expect(mountForm({ saving: true }).text()).toContain("Saving")
    })
})
