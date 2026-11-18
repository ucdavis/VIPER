import { flushPromises, mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import ReportAdminList from "../pages/ReportAdminList.vue"
import { reportAdminService } from "../admin/report-admin-service"
import type { ReportAdminListItem } from "../admin/report-admin-types"

vi.mock("../admin/report-admin-service", () => ({
    reportAdminService: { list: vi.fn<(...args: unknown[]) => unknown>() },
}))

const RouterLinkStub = { props: ["to"], template: "<a><slot /></a>" }

const roster: ReportAdminListItem = {
    id: 4,
    slug: "8a6f1d2e-0b7c-4a59-9d3e-1f2a3b4c5d6e",
    title: "Faculty roster",
    area: "Personnel",
    source: "FacultyRoster",
    modifiedOn: "2026-11-16T09:30:00",
    modifiedBy: "jdoe",
    deletedOn: null,
    permissions: ["SVMSecure.Personnel", "SVMSecure.Gone"],
    missingPermissions: ["SVMSecure.Gone"],
}

async function mountList(reports: ReportAdminListItem[] | null = [roster]) {
    vi.mocked(reportAdminService.list).mockResolvedValue(reports)
    const wrapper = mount(ReportAdminList, {
        global: { plugins: [[Quasar, {}]], stubs: { RouterLink: RouterLinkStub } },
    })
    await flushPromises()
    return wrapper
}

describe("report admin list", () => {
    it("lists the reports with who can view them, flagging permissions RAPS lacks", async () => {
        expect.hasAssertions()

        const wrapper = await mountList()

        expect(wrapper.text()).toContain("Faculty roster")
        expect(wrapper.text()).toContain("SVMSecure.Gone (not in RAPS)")
        expect(wrapper.text()).toContain("11/16/2026 9:30 AM by jdoe")
    })

    it("marks deleted reports and says when a report is for admins only", async () => {
        expect.hasAssertions()

        const wrapper = await mountList([
            { ...roster, deletedOn: "2026-11-16T10:00:00", permissions: [], missingPermissions: [] },
        ])

        expect(wrapper.text()).toContain("Deleted")
        expect(wrapper.text()).toContain("Admins only")
    })

    it("shows deleted reports when asked", async () => {
        expect.hasAssertions()
        const wrapper = await mountList()

        await wrapper.findComponent({ name: "QToggle" }).vm.$emit("update:modelValue", true)
        await flushPromises()

        expect(reportAdminService.list).toHaveBeenLastCalledWith(true)
    })

    it("says when the list couldn't be loaded", async () => {
        expect.hasAssertions()

        const wrapper = await mountList(null)

        expect(wrapper.text()).toContain("The reports couldn't be loaded.")
    })
})
