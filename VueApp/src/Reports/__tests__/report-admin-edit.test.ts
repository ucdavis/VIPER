import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import { ref } from "vue"
import ReportAdminEdit from "../pages/ReportAdminEdit.vue"
import { emptyForm, toForm } from "../admin/report-admin-form"
import type { ReportAdminDefinition } from "../admin/report-admin-types"
import { rosterReport, saved } from "./report-admin-fixtures"

/**
 * The edit page only arranges what useReportAdminEdit provides, so the composable is replaced by
 * plain refs the tests set directly.
 */

const { state, calls, route } = vi.hoisted(() => ({
    state: {} as Record<string, unknown>,
    calls: { id: null as { value: number | null } | null },
    route: { params: {} as Record<string, string> },
}))
vi.mock("../admin/use-report-admin-edit", () => ({
    useReportAdminEdit: (id: { value: number | null }) => {
        calls.id = id
        return state
    },
}))
vi.mock("vue-router", () => ({ useRoute: () => route, onBeforeRouteLeave: vi.fn<() => void>() }))

function setState(overrides: Record<string, unknown> = {}) {
    const values = {
        form: ref(toForm(rosterReport)),
        definition: ref<ReportAdminDefinition | null>(saved),
        sources: ref([]),
        source: ref(null),
        loading: ref(false),
        notFound: ref(false),
        sourcesFailed: ref(false),
        saving: ref(false),
        errors: ref<string[]>([]),
        conflict: ref(false),
        isDirty: ref(false),
        isView: ref(true),
        views: ref([]),
        targets: ref([]),
        sourceColumns: ref([]),
        canAddMissing: ref(false),
        changeSource: vi.fn<() => Promise<void>>(),
        addMissingColumns: vi.fn<() => void>(),
        save: vi.fn<() => Promise<void>>(),
        setDeleted: vi.fn<() => Promise<void>>(),
        applyVersion: vi.fn<() => Promise<void>>(),
        reload: vi.fn<() => Promise<void>>(),
    }
    Object.assign(state, values, overrides)
    return values
}

function mountPage(id?: string) {
    route.params = id === undefined ? {} : { id }
    return mount(ReportAdminEdit, {
        global: {
            plugins: [[Quasar, {}]],
            stubs: {
                RouterLink: { template: "<a><slot /></a>" },
                ReportAdminPreview: true,
                ReportAdminHistory: { name: "ReportAdminHistory", props: ["reportId"], template: "<div />" },
                ReportAdminPermissions: true,
                ReportAdminParameters: true,
                ReportAdminColumns: true,
            },
        },
    })
}

describe("report admin edit page", () => {
    it("edits the report named in the address", () => {
        expect.hasAssertions()
        setState()

        const wrapper = mountPage("4")

        expect(calls.id?.value).toBe(4)
        expect(wrapper.get("h1").text()).toContain("Faculty roster")
        expect(wrapper.findComponent({ name: "ReportAdminHistory" }).props("reportId")).toBe(4)
    })

    it("starts a new report without an id", () => {
        expect.hasAssertions()
        setState({ definition: ref(null), form: ref(emptyForm()) })

        const wrapper = mountPage()

        expect(calls.id?.value).toBeNull()
        expect(wrapper.get("h1").text()).toContain("New report")
        expect(wrapper.text()).toContain("Create report")
    })

    it("announces that the report is loading, and explains a missing one", () => {
        expect.hasAssertions()
        setState({ loading: ref(true) })
        const loading = mountPage("4")
        setState({ notFound: ref(true) })
        const missing = mountPage("9")

        expect(loading.get("[role='status']").text()).toBe("Loading report")
        expect(missing.text()).toContain("There's no report with that number.")
    })

    it("lists the server's messages and offers a reload after a conflicting save", async () => {
        expect.hasAssertions()
        const { reload } = setState({ errors: ref(["Someone else saved it."]), conflict: ref(true) })
        const wrapper = mountPage("4")

        await wrapper
            .findAll("button")
            .find((button) => button.text() === "Reload the saved report")
            ?.trigger("click")

        expect(wrapper.text()).toContain("Someone else saved it.")
        expect(reload).toHaveBeenCalledOnce()
    })

    it("notes unsaved changes", () => {
        expect.hasAssertions()
        setState({ isDirty: ref(true) })

        expect(mountPage("4").text()).toContain("Unsaved changes")
    })

    it("can't save a deleted report", () => {
        expect.hasAssertions()
        setState({ definition: ref({ ...saved, deletedOn: "2026-11-16T10:00:00" }) })

        const save = mountPage("4")
            .findAllComponents({ name: "QBtn" })
            .find((button) => button.props("label") === "Save")

        expect(save?.props("disable")).toBeTruthy()
    })

    it("passes deleting and picking a source on", () => {
        expect.hasAssertions()
        const { setDeleted, changeSource } = setState()
        const wrapper = mountPage("4")

        wrapper.findComponent({ name: "ReportAdminStatus" }).vm.$emit("restore")
        wrapper.findComponent({ name: "ReportAdminBasics" }).vm.$emit("source", "usp_Leave")

        expect(setDeleted).toHaveBeenCalledWith(false)
        expect(changeSource).toHaveBeenCalledWith("usp_Leave")
    })
})
