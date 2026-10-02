import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import ReportView from "../ReportView.vue"
import { sampleResult, vmeGroup } from "./report-fixtures"

function mountView(result = sampleResult()) {
    return mount(ReportView, { props: { result }, global: { plugins: [[Quasar, {}]] } })
}

describe("ReportView", () => {
    it("says when the report was generated and how many rows it has", () => {
        expect.hasAssertions()
        const wrapper = mountView()

        expect(wrapper.get("p").text()).toBe("Generated 09/30/2026 2:05 PM · 3 rows")
    })

    it("uses the singular for one row", () => {
        expect.hasAssertions()
        const wrapper = mountView(sampleResult({ rowCount: 1 }))

        expect(wrapper.get("p").text()).toContain("1 row")
    })

    it("lays out the sections in export order", () => {
        expect.hasAssertions()
        const wrapper = mountView()

        expect(wrapper.findAll("h2, h3").map((heading) => heading.text())).toStrictEqual([
            "VME",
            "Ages: VME",
            "APC",
            "Age by department",
            "Ages",
        ])
        expect(wrapper.findAll("dt").map((term) => term.text())).toStrictEqual(["People", "Salaries", "All salaries"])
    })

    it("gives an ungrouped report no group heading", () => {
        expect.hasAssertions()
        const ungrouped = sampleResult({
            groups: [{ ...vmeGroup, label: null, charts: [] }],
            pivots: [],
            charts: [],
        })

        expect(mountView(ungrouped).find("h2").exists()).toBeFalsy()
    })

    it("says so when no rows match, but still shows the summary", () => {
        expect.hasAssertions()
        const wrapper = mountView(sampleResult({ rowCount: 0, groups: [] }))

        expect(wrapper.text()).toContain("No rows match these parameters.")
        expect(wrapper.get("dt").text()).toBe("People")
        expect(wrapper.find("table").exists()).toBeFalsy()
    })
})
