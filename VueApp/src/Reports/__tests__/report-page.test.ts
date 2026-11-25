import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import { defineComponent } from "vue"
import ReportPage from "../pages/ReportPage.vue"
import BreadcrumbHeading from "@/components/BreadcrumbHeading.vue"

vi.mock("vue-router", () => ({ useRoute: () => ({ params: { slug: "8a6f1d2e" } }) }))

/** Stands in for ReportViewer, giving the heading slot the title a loaded report would. */
function viewerStub(title: string | null) {
    return defineComponent({
        name: "ReportViewer",
        props: { slug: { type: String, required: true }, syncQuery: Boolean },
        setup: () => ({ title }),
        template: `<div><slot name="heading" :title="title" /></div>`,
    })
}

function mountPage(title: string | null) {
    return mount(ReportPage, {
        global: {
            plugins: [[Quasar, {}]],
            stubs: { ReportViewer: viewerStub(title), RouterLink: { template: "<a><slot /></a>" } },
        },
    })
}

describe("report page", () => {
    it("shows the report named in the URL, with its values in the URL", () => {
        expect.hasAssertions()

        const viewer = mountPage("Faculty roster").findComponent({ name: "ReportViewer" })

        expect([viewer.props("slug"), viewer.props("syncQuery")]).toStrictEqual(["8a6f1d2e", true])
    })

    it("heads the page with the report's title, under Reports", () => {
        expect.hasAssertions()

        const heading = mountPage("Faculty roster").findComponent(BreadcrumbHeading)

        expect(heading.props()).toStrictEqual({
            label: "Faculty roster",
            parentLabel: "Reports",
            parentTo: { name: "ReportsHome" },
        })
    })

    it("says a report that isn't available isn't", () => {
        expect.hasAssertions()

        expect(mountPage(null).findComponent(BreadcrumbHeading).props("label")).toBe("Report not available")
    })
})
