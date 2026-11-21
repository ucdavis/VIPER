import { flushPromises, mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import ReportAdminPermissions from "../components/ReportAdminPermissions.vue"
import ReportAdminPreview from "../components/ReportAdminPreview.vue"
import ReportAdminHistory from "../components/ReportAdminHistory.vue"
import ReportAdminStatus from "../components/ReportAdminStatus.vue"
import ReportBuilderTable from "@/shared/report-builder/ReportBuilderTable.vue"
import { reportAdminService } from "../admin/report-admin-service"
import { rosterReport, saved } from "./report-admin-fixtures"
import { deferred } from "./deferred"
import type { ReportHistoryVersion } from "../admin/report-admin-types"

vi.mock("../admin/report-admin-service", () => ({
    reportAdminService: {
        searchPermissions: vi.fn<(...args: unknown[]) => unknown>(),
        preview: vi.fn<(...args: unknown[]) => unknown>(),
        getHistory: vi.fn<(...args: unknown[]) => unknown>(),
        getVersion: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))
const { mockConfirm } = vi.hoisted(() => ({ mockConfirm: vi.fn<(...args: unknown[]) => Promise<boolean>>() }))
vi.mock("@/composables/use-confirm-dialog", () => ({ useConfirmDialog: () => ({ confirmAction: mockConfirm }) }))

const service = vi.mocked(reportAdminService)
const global = {
    plugins: [[Quasar, {}]] as [typeof Quasar, object][],
    stubs: { RouterLink: { template: "<a><slot /></a>" } },
}

function button(wrapper: ReturnType<typeof mount>, text: string) {
    return wrapper.findAll("button").find((candidate) => candidate.text().includes(text))
}

describe("report permissions section", () => {
    it("searches RAPS as the admin types", async () => {
        expect.hasAssertions()
        service.searchPermissions.mockResolvedValue([{ permission: "SVMSecure.Effort", description: "Effort" }])
        const wrapper = mount(ReportAdminPermissions, { props: { modelValue: [] }, global })
        const select = wrapper.findComponent({ name: "QSelect" })

        select.vm.$emit("filter", "eff", (update: () => void) => update(), vi.fn<() => void>())
        await flushPromises()

        expect(service.searchPermissions).toHaveBeenCalledWith("eff")
        expect(select.props("options")).toStrictEqual([{ permission: "SVMSecure.Effort", description: "Effort" }])

        select.vm.$emit("update:modelValue", ["SVMSecure.Effort"])

        expect(wrapper.emitted("update:modelValue")).toStrictEqual([[["SVMSecure.Effort"]]])
    })

    it("warns about chosen permissions RAPS no longer has", () => {
        expect.hasAssertions()

        const one = mount(ReportAdminPermissions, {
            props: { modelValue: ["SVMSecure.Gone"], missing: ["SVMSecure.Gone"] },
            global,
        })
        const two = mount(ReportAdminPermissions, { props: { modelValue: ["A", "B"], missing: ["A", "B"] }, global })
        const removed = mount(ReportAdminPermissions, {
            props: { modelValue: [], missing: ["SVMSecure.Gone"] },
            global,
        })

        expect(one.text()).toContain("RAPS no longer has SVMSecure.Gone, so nobody holds it. Remove it before")
        expect(two.text()).toContain("A, B, so nobody holds them. Remove them")
        expect(removed.text()).not.toContain("no longer has")
    })

    // QSelect virtualizes its option list, which jsdom's layout never populates, so a mount of the
    // real component never renders the #option/#no-option slots. Stub QSelect to render them
    // directly instead.
    const QSelectStub = {
        name: "QSelect",
        props: ["options"],
        template: `
            <div>
                <template v-if="options.length === 0"><slot name="no-option" /></template>
                <template v-for="opt in options" :key="opt.permission">
                    <slot name="option" :opt="opt" :item-props="{}" />
                </template>
            </div>
        `,
    }
    const stubbedGlobal = { ...global, stubs: { ...global.stubs, QSelect: QSelectStub } }

    it("shows each matching permission, with its description when it has one", async () => {
        expect.hasAssertions()
        service.searchPermissions.mockResolvedValue([
            { permission: "SVMSecure.Effort", description: "Effort access" },
            { permission: "SVMSecure.CTS", description: null },
        ])
        const wrapper = mount(ReportAdminPermissions, { props: { modelValue: [] }, global: stubbedGlobal })

        wrapper
            .findComponent({ name: "QSelect" })
            .vm.$emit("filter", "s", (update: () => void) => update(), vi.fn<() => void>())
        await flushPromises()

        expect(wrapper.text()).toContain("SVMSecure.Effort")
        expect(wrapper.text()).toContain("Effort access")
        expect(wrapper.text()).toContain("SVMSecure.CTS")
    })

    it("says when nothing matches", () => {
        expect.hasAssertions()

        const wrapper = mount(ReportAdminPermissions, { props: { modelValue: [] }, global: stubbedGlobal })

        expect(wrapper.text()).toContain("No matching RAPS permissions")
    })
})

describe("report preview section", () => {
    const page = { columns: [], rows: [{ Name: "Ada" }], page: 1, perPage: 50, totalRows: 75, truncated: false }

    it("previews the unsaved report from the first page", async () => {
        expect.hasAssertions()
        service.preview.mockResolvedValue({ result: page, errors: [] })
        const wrapper = mount(ReportAdminPreview, { props: { report: rosterReport }, global })

        await button(wrapper, "Preview")?.trigger("click")
        await flushPromises()

        expect(service.preview).toHaveBeenCalledWith(rosterReport, expect.objectContaining({ page: 1, perPage: null }))
        expect(wrapper.findComponent(ReportBuilderTable).props("pagination")).toMatchObject({ page: 1, rowsNumber: 75 })
    })

    it("pages and sorts the preview", async () => {
        expect.hasAssertions()
        service.preview.mockResolvedValue({ result: { ...page, totalRows: null }, errors: [] })
        const wrapper = mount(ReportAdminPreview, { props: { report: rosterReport }, global })
        await button(wrapper, "Preview")?.trigger("click")
        await flushPromises()

        wrapper
            .findComponent(ReportBuilderTable)
            .vm.$emit("request", { page: 2, rowsPerPage: 50, sortBy: "Name", descending: true })
        await flushPromises()

        expect(service.preview).toHaveBeenLastCalledWith(
            rosterReport,
            expect.objectContaining({ page: 2, sortBy: "Name", sortDescending: true }),
        )
        expect(wrapper.findComponent(ReportBuilderTable).props("pagination")).toMatchObject({ page: 2, rowsNumber: 51 })
    })

    it("lists why the preview couldn't run", async () => {
        expect.hasAssertions()
        service.preview.mockResolvedValue({ result: null, errors: ["Show at least one column."] })
        const wrapper = mount(ReportAdminPreview, { props: { report: rosterReport }, global })

        await button(wrapper, "Preview")?.trigger("click")
        await flushPromises()

        expect(wrapper.text()).toContain("Show at least one column.")
        expect(wrapper.findComponent(ReportBuilderTable).exists()).toBeFalsy()
    })
})

describe("report history section", () => {
    it("lists the versions and loads one into the form", async () => {
        expect.hasAssertions()
        service.getHistory.mockResolvedValue([
            { id: 9, action: "Updated", changedOn: "2026-11-16T09:30:00", changedBy: "jdoe" },
            { id: 8, action: null, changedOn: null, changedBy: null },
        ])
        service.getVersion.mockResolvedValue({
            version: { id: 9, action: "Updated", changedOn: null, changedBy: null },
            report: rosterReport,
        })
        const wrapper = mount(ReportAdminHistory, { props: { reportId: 4 }, global })

        await button(wrapper, "Show earlier versions")?.trigger("click")
        await flushPromises()
        await button(wrapper, "Load into form")?.trigger("click")
        await flushPromises()

        expect(wrapper.text()).toContain("11/16/2026 9:30 AM by jdoe")
        expect(wrapper.text()).toContain("Saved")
        expect(wrapper.emitted("load")).toStrictEqual([[rosterReport]])
    })

    it("says when the history or a version can't be read", async () => {
        expect.hasAssertions()
        service.getHistory.mockResolvedValueOnce(null)
        const failed = mount(ReportAdminHistory, { props: { reportId: 4 }, global })
        await button(failed, "Show earlier versions")?.trigger("click")
        await flushPromises()
        service.getHistory.mockResolvedValueOnce([{ id: 9, action: "Created", changedOn: null, changedBy: null }])
        service.getVersion.mockResolvedValue({
            version: { id: 9, action: "Created", changedOn: null, changedBy: null },
            report: null,
        })
        const unreadable = mount(ReportAdminHistory, { props: { reportId: 4 }, global })
        await button(unreadable, "Show earlier versions")?.trigger("click")
        await flushPromises()
        await button(unreadable, "Load into form")?.trigger("click")
        await flushPromises()

        expect(failed.text()).toContain("The history couldn't be loaded.")
        expect(unreadable.text()).toContain("That version can't be read.")
        expect(unreadable.emitted("load")).toBeUndefined()
    })

    it("ignores an older selection's response arriving after a newer one", async () => {
        expect.hasAssertions()
        service.getHistory.mockResolvedValue([
            { id: 9, action: "Updated", changedOn: null, changedBy: null },
            { id: 8, action: "Created", changedOn: null, changedBy: null },
        ])
        const slow = deferred<ReportHistoryVersion | null>()
        service.getVersion.mockReturnValueOnce(slow.promise)
        const wrapper = mount(ReportAdminHistory, { props: { reportId: 4 }, global })
        await button(wrapper, "Show earlier versions")?.trigger("click")
        await flushPromises()
        const loadButtons = wrapper.findAll("button").filter((candidate) => candidate.text().includes("Load into form"))

        await loadButtons[0]?.trigger("click")
        service.getVersion.mockResolvedValue({
            version: { id: 8, action: "Created", changedOn: null, changedBy: null },
            report: { ...rosterReport, title: "Eight" },
        })
        await loadButtons[1]?.trigger("click")
        await flushPromises()
        slow.resolve({
            version: { id: 9, action: "Updated", changedOn: null, changedBy: null },
            report: { ...rosterReport, title: "Nine" },
        })
        await flushPromises()

        expect(wrapper.emitted("load")).toStrictEqual([[{ ...rosterReport, title: "Eight" }]])
    })
})

describe("report status bar", () => {
    it("shows who changed it last and deletes after confirming", async () => {
        expect.hasAssertions()
        mockConfirm.mockResolvedValueOnce(false).mockResolvedValueOnce(true)
        const wrapper = mount(ReportAdminStatus, { props: { definition: saved }, global })

        await button(wrapper, "Delete")?.trigger("click")
        await flushPromises()
        await button(wrapper, "Delete")?.trigger("click")
        await flushPromises()

        expect(wrapper.text()).toContain("Last changed 11/16/2026 9:30 AM by jdoe")
        expect(wrapper.emitted("delete")).toHaveLength(1)
    })

    it("explains and restores a deleted report", async () => {
        expect.hasAssertions()
        const wrapper = mount(ReportAdminStatus, {
            props: { definition: { ...saved, deletedOn: "2026-11-16T10:00:00", deletedBy: "admin", modifiedBy: null } },
            global,
        })

        await button(wrapper, "Restore")?.trigger("click")

        expect(wrapper.text()).toContain("This report was deleted by admin")
        expect(wrapper.text()).toContain("Last changed 11/16/2026 9:30 AM")
        expect(wrapper.emitted("restore")).toHaveLength(1)
    })

    it("shows nothing for a report that isn't saved yet", () => {
        expect.hasAssertions()

        expect(mount(ReportAdminStatus, { props: { definition: null }, global }).text()).toBe("")
    })
})
