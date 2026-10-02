import { mount } from "@vue/test-utils"
import PersonnelReport from "../pages/PersonnelReport.vue"

vi.mock("vue-router", () => ({
    useRoute: () => ({ params: { key: "personnel.visa-audit" } }),
}))

describe("personnel report page", () => {
    it("runs the report named by the route, with a breadcrumb back to Personnel", () => {
        expect.hasAssertions()
        const wrapper = mount(PersonnelReport, { global: { stubs: { ReportRunner: true } } })
        const runner = wrapper.findComponent({ name: "ReportRunner" })

        expect(runner.props("reportKey")).toBe("personnel.visa-audit")
        expect(runner.props("parentLabel")).toBe("Personnel")
        expect(runner.props("parentTo")).toStrictEqual({ name: "PersonnelHome" })
    })
})
