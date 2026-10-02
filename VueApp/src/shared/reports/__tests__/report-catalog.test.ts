import { flushPromises, mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import { createMemoryHistory, createRouter } from "vue-router"
import ReportCatalog from "../ReportCatalog.vue"
import { reportService } from "../report-service"
import type { ReportCatalogItem } from "../report-types"

/**
 * ReportCatalog lists an area's reports as links to the area's report route. It stays visible
 * while loading and after a failed load, but disappears for a user who may run none of the
 * area's reports.
 */

vi.mock("../report-service", () => ({
    reportService: { getCatalog: vi.fn<(...args: unknown[]) => unknown>() },
}))

const built: ReportCatalogItem = {
    key: "personnel.employees-on-leave",
    title: "Employees on Leave",
    area: "Personnel",
    description: "Faculty and staff on leave.",
    available: true,
}

const planned: ReportCatalogItem = {
    key: "personnel.visa-audit",
    title: "Visa Audit",
    area: "Personnel",
    description: "Visa types and end dates.",
    available: false,
}

// Never resolves, to hold the component in its loading state.
function neverResolves<T>(): Promise<T> {
    // eslint-disable-next-line avoid-new, no-empty-function -- deliberately pending forever, to simulate an in-flight fetch
    return new Promise<T>(() => {})
}

async function mountCatalog(props: Record<string, unknown> = {}) {
    const router = createRouter({
        history: createMemoryHistory(),
        routes: [
            { path: "/", component: { template: "<div />" } },
            { path: "/reports/:key", component: { template: "<div />" }, name: "TestReport" },
        ],
    })
    await router.push("/")
    const wrapper = mount(ReportCatalog, {
        props: { area: "Personnel", reportRoute: "TestReport", ...props },
        global: { plugins: [[Quasar, {}], router] },
    })
    await flushPromises()
    return wrapper
}

describe("ReportCatalog", () => {
    it("asks for the catalog of its area", async () => {
        expect.hasAssertions()
        vi.mocked(reportService.getCatalog).mockResolvedValue([])

        await mountCatalog({ area: "Effort" })

        expect(reportService.getCatalog).toHaveBeenCalledWith("Effort")
    })

    it("links each report to the area's report route", async () => {
        expect.hasAssertions()
        vi.mocked(reportService.getCatalog).mockResolvedValue([built, planned])

        const wrapper = await mountCatalog()
        const links = wrapper.findAll("a")

        expect(links.map((link) => link.attributes("href"))).toStrictEqual([
            "/reports/personnel.employees-on-leave",
            "/reports/personnel.visa-audit",
        ])
        expect(links.map((link) => link.attributes("role"))).toStrictEqual(["link", "link"])
        expect(links[0]?.text()).toContain("Faculty and staff on leave.")
    })

    it("marks only planned reports as coming soon", async () => {
        expect.hasAssertions()
        vi.mocked(reportService.getCatalog).mockResolvedValue([built, planned])

        const wrapper = await mountCatalog()
        const links = wrapper.findAll("a")

        expect(links[0]?.text()).not.toContain("Coming soon")
        expect(links[1]?.text()).toContain("Coming soon")
    })

    it("labels the section with its heading", async () => {
        expect.hasAssertions()
        vi.mocked(reportService.getCatalog).mockResolvedValue([built])

        const wrapper = await mountCatalog({ heading: "Personnel reports" })
        const heading = wrapper.get("h2")

        expect(heading.text()).toBe("Personnel reports")
        expect(wrapper.get("section").attributes("aria-labelledby")).toBe(heading.attributes("id"))
    })

    it("uses a default heading", async () => {
        expect.hasAssertions()
        vi.mocked(reportService.getCatalog).mockResolvedValue([built])

        const wrapper = await mountCatalog()

        expect(wrapper.get("h2").text()).toBe("Reports")
    })

    it("announces that it is loading until the catalog arrives", async () => {
        expect.hasAssertions()
        vi.mocked(reportService.getCatalog).mockReturnValue(neverResolves())

        const wrapper = await mountCatalog()

        expect(wrapper.get("[role='status']").text()).toBe("Loading reports")
        expect(wrapper.find("a").exists()).toBeFalsy()
    })

    it("reports a failed load instead of hiding", async () => {
        expect.hasAssertions()
        vi.mocked(reportService.getCatalog).mockResolvedValue(null)

        const wrapper = await mountCatalog()

        expect(wrapper.find("h2").exists()).toBeTruthy()
        expect(wrapper.text()).toContain("The report list could not be loaded.")
    })

    it("renders nothing when the user may run none of the area's reports", async () => {
        expect.hasAssertions()
        vi.mocked(reportService.getCatalog).mockResolvedValue([])

        const wrapper = await mountCatalog()

        expect(wrapper.find("section").exists()).toBeFalsy()
    })
})
