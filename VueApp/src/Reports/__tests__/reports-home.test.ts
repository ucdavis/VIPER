import { flushPromises, mount } from "@vue/test-utils"
import { createPinia, setActivePinia } from "pinia"
import { Quasar } from "quasar"
import ReportsHome from "../pages/ReportsHome.vue"
import { reportBuilderService } from "@/shared/report-builder/report-builder-service"
import { useUserStore } from "@/store/UserStore"

vi.mock("@/shared/report-builder/report-builder-service", () => ({
    reportBuilderService: { getCatalog: vi.fn<(...args: unknown[]) => unknown>() },
}))

async function mountHome() {
    setActivePinia(createPinia())
    const wrapper = mount(ReportsHome, { global: { plugins: [[Quasar, {}]] } })
    await flushPromises()
    return wrapper
}

describe("reports home page", () => {
    it("announces that the list is loading", () => {
        expect.hasAssertions()
        // eslint-disable-next-line avoid-new -- a request that never settles holds the page in its loading state
        vi.mocked(reportBuilderService.getCatalog).mockReturnValue(new Promise(vi.fn<() => void>()))
        setActivePinia(createPinia())

        const wrapper = mount(ReportsHome, { global: { plugins: [[Quasar, {}]] } })

        expect(wrapper.get("[role='status']").text()).toBe("Loading reports")
    })

    it("lists the reports by area, linking to each", async () => {
        expect.hasAssertions()
        vi.mocked(reportBuilderService.getCatalog).mockResolvedValue([
            { slug: "a", title: "Roster", description: "Current faculty.", area: "Personnel" },
            { slug: "b", title: "Courses", description: null, area: "Effort" },
        ])

        const wrapper = await mountHome()

        expect(wrapper.findAll("h2").map((heading) => heading.text())).toStrictEqual(["Effort", "Personnel"])
        expect(wrapper.text()).toContain("Current faculty.")
        expect(wrapper.findComponent({ name: "QItem" }).props("to")).toStrictEqual({
            name: "Report",
            params: { slug: "b" },
        })
    })

    it("says when the user has no reports", async () => {
        expect.hasAssertions()
        vi.mocked(reportBuilderService.getCatalog).mockResolvedValue([])

        const wrapper = await mountHome()

        expect(wrapper.text()).toContain("There are no reports for you to view yet.")
    })

    it("says when the list couldn't be loaded", async () => {
        expect.hasAssertions()
        vi.mocked(reportBuilderService.getCatalog).mockResolvedValue(null)

        const wrapper = await mountHome()

        expect(wrapper.text()).toContain("The report list couldn't be loaded.")
    })

    it("links admins to managing reports", async () => {
        expect.hasAssertions()
        vi.mocked(reportBuilderService.getCatalog).mockResolvedValue([])
        const visitor = await mountHome()
        setActivePinia(createPinia())
        useUserStore().setPermissions(["SVMSecure.Reports.Admin"])

        const admin = mount(ReportsHome, { global: { plugins: [[Quasar, {}]] } })
        await flushPromises()

        expect(visitor.text()).not.toContain("Manage reports")
        expect(admin.text()).toContain("Manage reports")
    })
})
