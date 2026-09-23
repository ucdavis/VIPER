import { mount, flushPromises } from "@vue/test-utils"
import { Quasar, Screen } from "quasar"
import { h } from "vue"
import CareerSelectionTable from "../components/CareerSelectionTable.vue"
import { CAREER_FIELDS, type CareerField } from "../utils/career-fields"
import { REPORT_COLUMNS } from "../utils/career-columns"
import type { StudentCareerReport } from "../types"

/**
 * Tests for the roster table the overview and report share. It owns the student's name and email
 * and the phone card, and hands every career field to the page through its two slots.
 */

vi.mock("@/composables/use-scrollable-table-region", () => ({ useScrollableTableRegion: () => {} }))

function reportRow(overrides: Partial<StudentCareerReport> = {}): StudentCareerReport {
    return {
        personId: 1,
        rowKey: "1",
        hasDetailRoute: true,
        fullName: "Student, Test",
        classLevel: "V2",
        email: "tstudent@ucdavis.edu",
        direction: "Private Practice",
        primaryFocus: "Equine",
        secondaryFocus: "",
        postGrad: "Internship",
        shortTermPlans: "Rural practice",
        longTermPlans: "Teach",
        mentorName: "",
        lastUpdated: null,
        ...overrides,
    }
}

const statementFields = CAREER_FIELDS.filter((f) => f.statement)

function mountTable(xs = false) {
    Screen.xs = xs
    return mount(CareerSelectionTable, {
        props: {
            rows: [reportRow()],
            columns: REPORT_COLUMNS,
            loading: false,
            rowsPerPage: 25,
            canEdit: true,
            label: "Test table",
            cellFields: statementFields,
            excelExport: vi.fn<() => Promise<void>>(),
            pdfExport: vi.fn<() => void>(),
        },
        slots: {
            "field-cell": ({ field }: { field: CareerField }) => h("td", { class: "page-cell" }, `cell:${field.name}`),
            "card-field": ({ field, value }: { field: CareerField; value: string | null | undefined }) =>
                h("p", { class: "page-card-line" }, `${field.name}=${value ?? ""}`),
        },
        global: {
            plugins: [[Quasar, {}]],
            stubs: {
                ExportToolbar: true,
                CareerRecordLink: { template: "<span>{{ student.fullName }}</span>", props: ["student"] },
            },
        },
    })
}

describe("career selection table", () => {
    afterEach(() => {
        Screen.xs = false
    })

    it("names and emails each student itself", async () => {
        expect.hasAssertions()
        const wrapper = mountTable()
        await flushPromises()

        expect(wrapper.text()).toContain("Student, Test")
        expect(wrapper.find("a[href='mailto:tstudent@ucdavis.edu']").exists()).toBeTruthy()
    })

    it("hands the page only the cells it asked to render", async () => {
        expect.hasAssertions()
        const wrapper = mountTable()
        await flushPromises()

        expect(wrapper.findAll(".page-cell").map((c) => c.text())).toStrictEqual(["cell:shortTerm", "cell:longTerm"])
        // A field the page did not claim keeps the table's own cell.
        expect(wrapper.text()).toContain("Private Practice")
    })

    it("gives the page a card line per career field, with its value, on a phone", async () => {
        expect.hasAssertions()
        const wrapper = mountTable(true)
        await flushPromises()

        const lines = wrapper.findAll(".page-card-line").map((l) => l.text())
        expect(lines).toHaveLength(CAREER_FIELDS.length)
        expect(lines).toContain("direction=Private Practice")
        expect(lines).toContain("longTerm=Teach")
    })
})
