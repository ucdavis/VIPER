import { flushPromises, mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import { ref } from "vue"
import ReportRunner from "../ReportRunner.vue"
import ReportParameterForm from "../ReportParameterForm.vue"
import { sampleResult } from "./report-fixtures"
import type { ReportDefinitionMetadata, ReportExportFormat, ReportResult } from "../report-types"

/**
 * ReportRunner only arranges what useReport provides, so useReport is replaced by plain refs
 * the tests set directly.
 */

const { state, keyGetter } = vi.hoisted(() => ({
    state: {} as Record<string, unknown>,
    keyGetter: { current: null as (() => string) | null },
}))
vi.mock("../use-report", () => ({
    useReport: (key: () => string) => {
        keyGetter.current = key
        return state
    },
}))

const definition: ReportDefinitionMetadata = {
    key: "test.sample",
    title: "Sample report",
    area: "Test",
    description: "A report used by tests.",
    parameters: [{ name: "search", label: "Search", type: "Text", required: false, defaultValue: null, options: [] }],
    columns: [],
}

function setState(overrides: Record<string, unknown> = {}) {
    const values = {
        definition: ref<ReportDefinitionMetadata | null>(definition),
        values: ref({ search: null }),
        result: ref<ReportResult | null>(null),
        errors: ref<string[]>([]),
        loading: ref(false),
        notFound: ref(false),
        running: ref(false),
        exporting: ref<ReportExportFormat | null>(null),
        run: vi.fn<() => Promise<void>>(),
        exportAs: vi.fn<(format: ReportExportFormat) => Promise<void>>(),
    }
    Object.assign(state, values, overrides)
    return values
}

function mountRunner() {
    return mount(ReportRunner, {
        props: { reportKey: "test.sample", parentLabel: "Test", parentTo: "/" },
        global: { plugins: [[Quasar, {}]], stubs: { RouterLink: { template: "<a><slot /></a>" } } },
    })
}

describe("ReportRunner - states", () => {
    it("announces that the report is loading", () => {
        expect.hasAssertions()
        setState({ loading: ref(true) })

        expect(mountRunner().get("[role='status']").text()).toBe("Loading report")
    })

    it("explains a report that isn't available", () => {
        expect.hasAssertions()
        setState({ notFound: ref(true), definition: ref(null) })
        const wrapper = mountRunner()

        expect(wrapper.get("h1").text()).toContain("Report not available")
        expect(wrapper.text()).toContain("may not be built yet")
    })

    it("shows the title, description and parameter form", () => {
        expect.hasAssertions()
        setState()
        const wrapper = mountRunner()

        expect(wrapper.get("h1").text()).toContain("Sample report")
        expect(wrapper.text()).toContain("A report used by tests.")
        expect(wrapper.find("form").exists()).toBeTruthy()
    })

    it("loads the report named by its key", () => {
        expect.hasAssertions()
        setState()
        mountRunner()

        expect(keyGetter.current?.()).toBe("test.sample")
    })

    it("keeps the form's values in the report state", () => {
        expect.hasAssertions()
        const { values } = setState()
        const wrapper = mountRunner()

        wrapper.findComponent(ReportParameterForm).vm.$emit("update:modelValue", { search: "smith" })

        expect(values.value).toStrictEqual({ search: "smith" })
    })

    it("leaves out the form for a report without parameters", () => {
        expect.hasAssertions()
        setState({ definition: ref({ ...definition, parameters: [] }) })

        expect(mountRunner().find("form").exists()).toBeFalsy()
    })

    it("lists the server's messages", () => {
        expect.hasAssertions()
        setState({ errors: ref(["Start date is required.", "End date is required."]) })

        expect(
            mountRunner()
                .findAll("li")
                .map((item) => item.text()),
        ).toStrictEqual(["Start date is required.", "End date is required."])
    })

    it("announces a run in progress", () => {
        expect.hasAssertions()
        setState({ running: ref(true) })

        expect(mountRunner().get("[role='status']").text()).toBe("Running report")
    })
})

describe("ReportRunner - results and exports", () => {
    it("runs the report when the form is submitted", async () => {
        expect.hasAssertions()
        const { run } = setState()
        const wrapper = mountRunner()

        await wrapper.get("form").trigger("submit")
        await flushPromises()

        expect(run).toHaveBeenCalledOnce()
    })

    it("shows the result with its export buttons", () => {
        expect.hasAssertions()
        setState({ result: ref(sampleResult()) })
        const wrapper = mountRunner()

        expect(wrapper.find(".report-view").exists()).toBeTruthy()
        expect(wrapper.find(".export-excel").exists()).toBeTruthy()
        expect(wrapper.find(".export-pdf").exists()).toBeTruthy()
    })

    it.each([
        [".export-excel", "xlsx"],
        [".export-pdf", "pdf"],
    ])("exports from %s as %s", async (selector, format) => {
        expect.hasAssertions()
        const { exportAs } = setState({ result: ref(sampleResult()) })

        await mountRunner().get(selector).trigger("click")

        expect(exportAs).toHaveBeenCalledWith(format)
    })

    it("shows which export is in progress and blocks the others", () => {
        expect.hasAssertions()
        setState({ result: ref(sampleResult()), exporting: ref("csv") })
        const wrapper = mountRunner()
        const csv = wrapper.findAll("button").find((button) => button.text().endsWith("CSV"))

        expect(csv?.find(".q-spinner").exists()).toBeTruthy()
        expect(wrapper.get(".export-excel").attributes("disabled")).toBeDefined()
    })

    it("exports CSV from its own button", async () => {
        expect.hasAssertions()
        const { exportAs } = setState({ result: ref(sampleResult()) })
        const wrapper = mountRunner()
        const csv = wrapper.findAll("button").find((button) => button.text().endsWith("CSV"))

        await csv?.trigger("click")

        expect(exportAs).toHaveBeenCalledWith("csv")
    })
})
