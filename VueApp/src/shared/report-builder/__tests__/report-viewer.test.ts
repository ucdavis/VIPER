import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import { ref } from "vue"
import ReportViewer from "../ReportViewer.vue"
import ReportBuilderForm from "../ReportBuilderForm.vue"
import ReportBuilderResults from "../ReportBuilderResults.vue"
import type { ReportRunResult, ReportViewerDefinition } from "../report-builder-types"
import { resultOf, roster } from "./report-builder-fixtures"

/**
 * ReportViewer only arranges what useReportBuilder provides, so the composable is replaced by
 * plain refs the tests set directly.
 */

const { state, calls } = vi.hoisted(() => ({
    state: {} as Record<string, unknown>,
    calls: { slug: null as (() => string) | null, options: null as Record<string, unknown> | null },
}))
vi.mock("../use-report-builder", () => ({
    useReportBuilder: (slug: () => string, options: Record<string, unknown>) => {
        calls.slug = slug
        calls.options = options
        return state
    },
}))

function setState(overrides: Record<string, unknown> = {}) {
    const values = {
        definition: ref<ReportViewerDefinition | null>(roster),
        values: ref({ Name: null }),
        result: ref<ReportRunResult | null>(null),
        errors: ref<string[]>([]),
        loading: ref(false),
        running: ref(false),
        exporting: ref(null),
        nothingToExport: ref(false),
        search: ref(""),
        pagination: ref({ page: 1, rowsPerPage: 25, sortBy: null, descending: false, rowsNumber: 0 }),
        canExport: ref(false),
        ready: ref(true),
        run: vi.fn<() => Promise<void>>(),
        changePage: vi.fn<() => Promise<void>>(),
        changeSearch: vi.fn<() => Promise<void>>(),
        exportAs: vi.fn<() => Promise<void>>(),
    }
    Object.assign(state, values, overrides)
    return values
}

function mountViewer(slots: Record<string, string> = {}) {
    return mount(ReportViewer, {
        props: { slug: roster.slug, params: { Name: "Ada" }, syncQuery: true },
        slots,
        global: { plugins: [[Quasar, {}]] },
    })
}

describe("report viewer - states", () => {
    it("announces that the report is loading", () => {
        expect.hasAssertions()
        setState({ loading: ref(true) })

        expect(mountViewer().get("[role='status']").text()).toBe("Loading report")
    })

    it("explains a report that isn't available", () => {
        expect.hasAssertions()
        setState({ definition: ref(null) })

        expect(mountViewer().text()).toContain("This report isn't available.")
    })

    it("shows the title, description and form", () => {
        expect.hasAssertions()
        setState()
        const wrapper = mountViewer()

        expect(wrapper.get("h2").text()).toBe("Faculty roster")
        expect(wrapper.text()).toContain("Current faculty by department.")
        expect(wrapper.findComponent(ReportBuilderForm).exists()).toBeTruthy()
    })

    it("lets the page supply its own heading", () => {
        expect.hasAssertions()
        setState()

        const wrapper = mountViewer({ heading: `<template #heading="{ title }"><h1>{{ title }}!</h1></template>` })

        expect([wrapper.get("h1").text(), wrapper.find("h2").exists()]).toStrictEqual(["Faculty roster!", false])
    })

    it("gives the page's heading no title for a report that isn't available", () => {
        expect.hasAssertions()
        setState({ definition: ref(null) })

        const wrapper = mountViewer({
            heading: `<template #heading="{ title }"><h1>{{ title ?? "None" }}</h1></template>`,
        })

        expect(wrapper.get("h1").text()).toBe("None")
    })

    it("shows the report's notice and leaves out an empty form", () => {
        expect.hasAssertions()
        setState({ definition: ref({ ...roster, sensitiveNotice: "Confidential", description: null, parameters: [] }) })
        const wrapper = mountViewer()

        expect(wrapper.text()).toContain("Confidential")
        expect(wrapper.findComponent(ReportBuilderForm).exists()).toBeFalsy()
    })

    it("lists the server's messages", () => {
        expect.hasAssertions()
        setState({ errors: ref(["Department is required.", "That page doesn't exist."]) })

        expect(
            mountViewer()
                .findAll("li")
                .map((item) => item.text()),
        ).toStrictEqual(["Department is required.", "That page doesn't exist."])
    })
})

describe("report viewer - wiring", () => {
    it("loads the report by slug with the page's values and URL setting", () => {
        expect.hasAssertions()
        setState()
        mountViewer()

        expect(calls.slug?.()).toBe(roster.slug)
        const params = calls.options?.params as (() => unknown) | undefined
        expect([params?.(), calls.options?.syncQuery]).toStrictEqual([{ Name: "Ada" }, true])
    })

    it("asks for the required fields until they're filled", () => {
        expect.hasAssertions()
        setState({ ready: ref(false) })

        expect(mountViewer().text()).toContain("Fill in the required fields to see the report.")
    })

    it("announces a run that hasn't shown rows yet", () => {
        expect.hasAssertions()
        setState({ running: ref(true) })

        expect(mountViewer().get("[role=status]").text()).toBe("Running report")
    })

    it("runs the report when the form is submitted", () => {
        expect.hasAssertions()
        const { run } = setState()

        mountViewer().findComponent(ReportBuilderForm).vm.$emit("submit")

        expect(run).toHaveBeenCalledOnce()
    })

    it("keeps the form's values in the report state", () => {
        expect.hasAssertions()
        const { values } = setState()

        mountViewer().findComponent(ReportBuilderForm).vm.$emit("update:modelValue", { Name: "Bo" })

        expect(values.value).toStrictEqual({ Name: "Bo" })
    })

    it("shows the rows with the report's settings and passes the table's requests on", () => {
        expect.hasAssertions()
        const { changePage, changeSearch } = setState({ result: ref(resultOf([{ Name: "Ada" }])) })
        const results = mountViewer().findComponent(ReportBuilderResults)

        results.vm.$emit("request", { page: 2 })
        results.vm.$emit("search", "ada")

        expect([results.props("allowExcel"), results.props("allowPdf"), results.props("paginate")]).toStrictEqual([
            true,
            false,
            true,
        ])
        expect([changePage.mock.lastCall, changeSearch.mock.lastCall]).toStrictEqual([[{ page: 2 }], ["ada"]])
    })
})
