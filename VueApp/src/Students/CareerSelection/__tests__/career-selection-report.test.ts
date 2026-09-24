import { mount, flushPromises } from "@vue/test-utils"
import { Quasar } from "quasar"
import CareerSelectionReport from "../pages/CareerSelectionReport.vue"
import { careerSelectionService } from "../services/career-selection-service"
import { STATEMENT_PREVIEW_LENGTH } from "../utils/career-columns"
import type { StudentCareerReport } from "../types"

/**
 * Tests for the career selection report's statement columns: each shows an excerpt, yet the
 * table search still reaches the whole statement.
 */

vi.mock("vue-router", () => ({
    useRoute: () => ({ params: {}, query: {} }),
    useRouter: () => ({ push: vi.fn<(to: unknown) => void>() }),
}))
vi.mock("@/composables/CheckPagePermission", () => ({ checkHasOnePermission: () => false }))
vi.mock("@/composables/use-scrollable-table-region", () => ({ useScrollableTableRegion: () => {} }))

// Long enough that the search term sits well past the excerpt.
const LONG_PLAN = `${"Start in a mixed practice and build up surgical skills. ".repeat(3)}Then pursue aquaculture.`

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
        shortTermPlans: "",
        longTermPlans: LONG_PLAN,
        mentorName: "",
        lastUpdated: null,
        ...overrides,
    }
}

async function mountReport(rows: StudentCareerReport[]) {
    vi.spyOn(careerSelectionService, "getReport").mockResolvedValue(rows)
    const wrapper = mount(CareerSelectionReport, {
        global: {
            plugins: [[Quasar, {}]],
            stubs: {
                ExportToolbar: { name: "ExportToolbar", template: "<div />", props: ["filter"] },
                ColumnToggle: true,
                CareerRecordLink: { template: "<span>{{ student.fullName }}</span>", props: ["student"] },
                RouterLink: true,
            },
        },
    })
    await flushPromises()
    return wrapper
}

describe("career selection report", () => {
    it("shows a long statement as its excerpt", async () => {
        expect.hasAssertions()
        const wrapper = await mountReport([reportRow()])

        expect(LONG_PLAN.length).toBeGreaterThan(STATEMENT_PREVIEW_LENGTH)
        expect(wrapper.text()).toContain(LONG_PLAN.slice(0, 20))
        expect(wrapper.text()).not.toContain("aquaculture")
    })

    it("finds a student by a word past the excerpt", async () => {
        expect.hasAssertions()
        const wrapper = await mountReport([
            reportRow(),
            reportRow({ personId: 2, rowKey: "2", fullName: "Other, Student", longTermPlans: "Small animal" }),
        ])

        await wrapper.findComponent({ name: "ExportToolbar" }).vm.$emit("update:filter", "aquaculture")
        await flushPromises()

        expect(wrapper.text()).toContain("Student, Test")
        expect(wrapper.text()).not.toContain("Other, Student")
    })
})
