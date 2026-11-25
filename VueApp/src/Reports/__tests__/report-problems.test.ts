import { flushPromises, mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import ReportAdminProblems from "../pages/ReportAdminProblems.vue"
import ReportFailedRuns from "../components/ReportFailedRuns.vue"
import ReportProblemsCheck from "../components/ReportProblemsCheck.vue"
import { reportAdminService } from "../admin/report-admin-service"
import type { ReportCheckResult, ReportFailure } from "../admin/report-admin-types"
import { deferred } from "./deferred"

vi.mock("../admin/report-admin-service", () => ({
    reportAdminService: {
        getFailures: vi.fn<(...args: unknown[]) => unknown>(),
        check: vi.fn<(...args: unknown[]) => unknown>(),
    },
}))

const RouterLinkStub = { props: ["to"], template: "<a><slot /></a>" }
const global = { plugins: [[Quasar, {}]] as never, stubs: { RouterLink: RouterLinkStub } }

const denied: ReportFailure = {
    id: 11,
    reportId: 4,
    reportTitle: "Faculty roster",
    reportDeleted: false,
    action: "ExportExcel",
    loginId: "jdoe",
    ranOn: "2026-11-17T09:30:00",
    parameters: '{"Dept":"VME"}',
    errorNumber: 229,
    errorMessage: "The SELECT permission was denied on the object 'FacultyRoster'.",
}

const sourceGone: ReportFailure = {
    ...denied,
    id: 10,
    reportId: 7,
    reportTitle: "Leave balances",
    reportDeleted: true,
    action: "Run",
    parameters: null,
    errorNumber: null,
    errorMessage: "This report's data source is no longer available.",
}

async function mountFailedRuns(failures: ReportFailure[] | null = [denied, sourceGone]) {
    vi.mocked(reportAdminService.getFailures).mockResolvedValue(failures)
    const wrapper = mount(ReportFailedRuns, { global })
    await flushPromises()
    return wrapper
}

async function runCheck(result: ReportCheckResult | null) {
    vi.mocked(reportAdminService.check).mockResolvedValue(result)
    const wrapper = mount(ReportProblemsCheck, { global })
    await wrapper.findComponent({ name: "QBtn" }).trigger("click")
    await flushPromises()
    return wrapper
}

describe("report failed runs", () => {
    it("lists the last 30 days of failures with SQL Server's reason", async () => {
        expect.hasAssertions()

        const wrapper = await mountFailedRuns()

        expect(reportAdminService.getFailures).toHaveBeenLastCalledWith(30)
        expect(wrapper.text()).toContain("2 failed runs across 2 reports.")
        expect(wrapper.text()).toContain(
            "SQL error 229: The SELECT permission was denied on the object 'FacultyRoster'.",
        )
        expect(wrapper.text()).toContain("This report's data source is no longer available.")
    })

    it("shows how each failed report was run and whether it's since been deleted", async () => {
        expect.hasAssertions()

        const wrapper = await mountFailedRuns()

        expect(wrapper.text()).toContain('Values: {"Dept":"VME"}')
        expect(wrapper.text()).toContain("Excel")
        expect(wrapper.text()).toContain("Deleted")
    })

    it("narrows the list to one report", async () => {
        expect.hasAssertions()
        const wrapper = await mountFailedRuns()

        await wrapper.findAllComponents({ name: "QSelect" })[1]?.vm.$emit("update:modelValue", 7)

        expect(wrapper.text()).toContain("Leave balances")
        expect(wrapper.text()).not.toContain("SQL error 229")
    })

    it("reloads for another period and forgets a report that no longer failed in it", async () => {
        expect.hasAssertions()
        const wrapper = await mountFailedRuns()
        await wrapper.findAllComponents({ name: "QSelect" })[1]?.vm.$emit("update:modelValue", 7)
        vi.mocked(reportAdminService.getFailures).mockResolvedValue([denied])

        await wrapper.findAllComponents({ name: "QSelect" })[0]?.vm.$emit("update:modelValue", 7)
        await flushPromises()

        expect(reportAdminService.getFailures).toHaveBeenLastCalledWith(7)
        expect(wrapper.text()).toContain("1 failed run across 1 report.")
        expect(wrapper.text()).toContain("SQL error 229")
    })

    it("ignores a list for a period that was changed again before it arrived", async () => {
        expect.hasAssertions()
        const wrapper = await mountFailedRuns([])
        const slow = deferred<ReportFailure[] | null>()
        vi.mocked(reportAdminService.getFailures).mockReturnValueOnce(slow.promise).mockResolvedValueOnce([sourceGone])
        const [periods] = wrapper.findAllComponents({ name: "QSelect" })

        await periods?.vm.$emit("update:modelValue", 7)
        await periods?.vm.$emit("update:modelValue", 90)
        await flushPromises()
        slow.resolve([denied])
        await flushPromises()

        expect(wrapper.text()).toContain("Leave balances")
        expect(wrapper.text()).not.toContain("SQL error 229")
    })

    it("says when nothing failed, and when the list couldn't be loaded", async () => {
        expect.hasAssertions()

        const empty = await mountFailedRuns([])
        const broken = await mountFailedRuns(null)

        expect(empty.text()).toContain("No failed runs in this period.")
        expect(broken.text()).toContain("The failed runs couldn't be loaded.")
    })
})

describe("report problems check", () => {
    it("lists each failing report with its problems", async () => {
        expect.hasAssertions()

        const wrapper = await runCheck({
            checked: 12,
            checkedOn: "2026-11-17T09:30:00",
            failing: [
                {
                    id: 7,
                    title: "Leave balances",
                    area: "Personnel",
                    source: "Dropped",
                    problems: ["RAPS has no permission named 'SVMSecure.Retired'."],
                },
            ],
        })

        expect(wrapper.text()).toContain("1 of 12 reports have problems (checked 11/17/2026 9:30 AM).")
        expect(wrapper.text()).toContain("Personnel · Dropped")
        expect(wrapper.text()).toContain("RAPS has no permission named 'SVMSecure.Retired'.")
    })

    it("says when every report passed", async () => {
        expect.hasAssertions()

        const wrapper = await runCheck({ checked: 12, checkedOn: "2026-11-17T09:30:00", failing: [] })

        expect(wrapper.text()).toContain("All 12 reports passed (checked 11/17/2026 9:30 AM).")
    })

    it("says when the check couldn't be run", async () => {
        expect.hasAssertions()

        const wrapper = await runCheck(null)

        expect(wrapper.text()).toContain("The reports couldn't be checked.")
    })
})

describe("report problems page", () => {
    it("shows the check and the failed runs, but doesn't check until asked", async () => {
        expect.hasAssertions()
        vi.mocked(reportAdminService.getFailures).mockResolvedValue([])
        vi.mocked(reportAdminService.check).mockClear()

        const wrapper = mount(ReportAdminProblems, { global })
        await flushPromises()

        expect(wrapper.text()).toContain("Check every report")
        expect(wrapper.text()).toContain("Failed runs")
        expect(reportAdminService.check).not.toHaveBeenCalled()
    })
})
