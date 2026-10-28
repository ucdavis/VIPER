import { mount, flushPromises, type VueWrapper } from "@vue/test-utils"
import { QTable, Quasar, Screen } from "quasar"
import { h } from "vue"
import ColumnToggle from "@/components/ColumnToggle.vue"
import ExportToolbar from "@/components/ExportToolbar.vue"
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

const COLUMN_STORAGE_KEY = "test-hidden-columns"

// Hidden columns persist in localStorage, so each test starts from a clean browser.
beforeEach(() => {
    localStorage.clear()
})

type MountOptions = {
    xs?: boolean // Mount as a phone, where each row becomes a card.
    rows?: StudentCareerReport[]
    rowsPerPage?: number
    // The toolbar is stubbed unless a test needs the column picker it holds in its prepend slot.
    realToolbar?: boolean
}

function mountTable({ xs = false, rows = [reportRow()], rowsPerPage = 25, realToolbar = false }: MountOptions = {}) {
    Screen.xs = xs
    return mount(CareerSelectionTable, {
        props: {
            rows,
            columns: REPORT_COLUMNS,
            loading: false,
            rowsPerPage,
            canEdit: true,
            label: "Test table",
            cellFields: statementFields,
            columnStorageKey: COLUMN_STORAGE_KEY,
            excelExport: vi.fn<(rowKeys: string[]) => Promise<void>>(),
            pdfExport: vi.fn<(rowKeys: string[]) => Promise<void>>(),
        },
        slots: {
            "field-cell": ({ field }: { field: CareerField }) => h("td", { class: "page-cell" }, `cell:${field.name}`),
            "card-field": ({ field, value }: { field: CareerField; value: string | null | undefined }) =>
                h("p", { class: "page-card-line" }, `${field.name}=${value ?? ""}`),
        },
        global: {
            plugins: [[Quasar, {}]],
            stubs: {
                ExportToolbar: !realToolbar,
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
        const wrapper = mountTable({ xs: true })
        await flushPromises()

        const lines = wrapper.findAll(".page-card-line").map((l) => l.text())
        expect(lines).toHaveLength(CAREER_FIELDS.length)
        expect(lines).toContain("direction=Private Practice")
        expect(lines).toContain("longTerm=Teach")
    })
})

describe("career selection table column visibility", () => {
    // The column picker sits in the toolbar's slot, so these tests need the real toolbar.
    const mountWithPicker = () => mountTable({ realToolbar: true })

    function shownColumns(wrapper: VueWrapper): string[] {
        return [...(wrapper.findComponent(ColumnToggle).props("modelValue") as string[])]
    }

    async function hideColumns(wrapper: VueWrapper, ...names: string[]): Promise<void> {
        const toggle = wrapper.findComponent(ColumnToggle)
        const shown = (toggle.props("modelValue") as string[]).filter((name) => !names.includes(name))
        toggle.vm.$emit("update:modelValue", shown)
        await flushPromises()
    }

    it("shows every column the first time", async () => {
        expect.hasAssertions()
        const wrapper = mountWithPicker()
        await flushPromises()

        expect(shownColumns(wrapper)).toStrictEqual(REPORT_COLUMNS.map((c) => c.name))
    })

    it("remembers hidden columns for the next visit", async () => {
        expect.hasAssertions()
        const first = mountWithPicker()
        await flushPromises()
        await hideColumns(first, "email", "mentor")
        first.unmount()

        const next = mountWithPicker()
        await flushPromises()

        expect(shownColumns(next)).not.toContain("email")
        expect(shownColumns(next)).not.toContain("mentor")
        expect(shownColumns(next)).toContain("fullName")
        // Stored as the hidden columns, so a column added later shows by default.
        expect([...JSON.parse(localStorage.getItem(COLUMN_STORAGE_KEY)!)]).toStrictEqual(["email", "mentor"])
    })

    it("ignores a stored column name that no longer matches a column", async () => {
        expect.hasAssertions()
        localStorage.setItem(COLUMN_STORAGE_KEY, JSON.stringify(["email", "retiredColumn"]))

        const wrapper = mountWithPicker()
        await flushPromises()

        expect(shownColumns(wrapper)).toStrictEqual(
            REPORT_COLUMNS.map((c) => c.name).filter((name) => name !== "email"),
        )
    })

    it("shows every column when the stored value is not a list", async () => {
        expect.hasAssertions()
        localStorage.setItem(COLUMN_STORAGE_KEY, JSON.stringify({ email: true }))

        const wrapper = mountWithPicker()
        await flushPromises()

        expect(shownColumns(wrapper)).toStrictEqual(REPORT_COLUMNS.map((c) => c.name))
    })
})

describe("career selection table exports", () => {
    // Seeded out of name order, and with a MothraId key, as an unmapped student carries.
    function exportRows(): StudentCareerReport[] {
        return [
            reportRow({ personId: 1, rowKey: "1", fullName: "Beta, Ann", direction: "Academia" }),
            reportRow({ personId: 2, rowKey: "2", fullName: "Alpha, Bo" }),
            reportRow({ personId: 0, rowKey: "STU00003", fullName: "Gamma, Cy" }),
        ]
    }

    /** Runs one toolbar export as a click would, returning the keys the page's handler received. */
    async function runExport(wrapper: VueWrapper, exportProp: "excelExport" | "pdfExport"): Promise<string[]> {
        const run = wrapper.findComponent(ExportToolbar).props(exportProp) as () => Promise<void>
        await run()
        const handler = (wrapper.props() as Record<string, unknown>)[exportProp] as ReturnType<typeof vi.fn>
        // Spread: the keys are built in the component, and toStrictEqual rejects a foreign Array.
        return [...(handler.mock.lastCall?.[0] as string[])]
    }

    async function search(wrapper: VueWrapper, text: string): Promise<void> {
        wrapper.findComponent(ExportToolbar).vm.$emit("update:filter", text)
        await flushPromises()
    }

    it("starts sorted by name, whatever order the rows arrive in", async () => {
        expect.hasAssertions()
        const wrapper = mountTable({ rows: exportRows() })
        await flushPromises()

        // Name is the second column, after Class.
        const names = wrapper.findAll("tbody tr").map((tr) => tr.findAll("td")[1]!.text())
        expect(names).toStrictEqual(["Alpha, Bo", "Beta, Ann", "Gamma, Cy"])
    })

    it("hands every export the keys of the rows the grid shows, in its order", async () => {
        expect.hasAssertions()
        const wrapper = mountTable({ rows: exportRows() })
        await flushPromises()

        expect(await runExport(wrapper, "excelExport")).toStrictEqual(["2", "1", "STU00003"])
        expect(await runExport(wrapper, "pdfExport")).toStrictEqual(["2", "1", "STU00003"])
    })

    it("narrows the keys to the rows the search lets through", async () => {
        expect.hasAssertions()
        const wrapper = mountTable({ rows: exportRows() })
        await flushPromises()

        await search(wrapper, "academia")

        expect(await runExport(wrapper, "excelExport")).toStrictEqual(["1"])
    })

    it("orders the keys by the column the grid is sorted on", async () => {
        expect.hasAssertions()
        const wrapper = mountTable({ rows: exportRows() })
        await flushPromises()

        // Career Direction puts Beta's "Academia" first, unlike the default name order.
        ;(wrapper.findComponent(QTable).vm as unknown as { sort: (col: string) => void }).sort("direction")
        await flushPromises()

        expect(await runExport(wrapper, "pdfExport")).toStrictEqual(["1", "2", "STU00003"])
    })

    it("finds rows by a column the user has hidden, as the legacy app did", async () => {
        expect.hasAssertions()
        localStorage.setItem(COLUMN_STORAGE_KEY, JSON.stringify(["mentor"]))
        const rows = exportRows()
        rows[0]!.mentorName = "Vet, Zed"
        const wrapper = mountTable({ rows })
        await flushPromises()

        await search(wrapper, "  vet, zed ")

        expect(await runExport(wrapper, "excelExport")).toStrictEqual(["1"])
    })

    it("hands over an empty list when the search matches nothing, for a headers-only file", async () => {
        expect.hasAssertions()
        const wrapper = mountTable({ rows: exportRows() })
        await flushPromises()

        await search(wrapper, "no such student")

        expect(await runExport(wrapper, "excelExport")).toStrictEqual([])
    })

    it("includes the rows on later pages, not just the one on screen", async () => {
        expect.hasAssertions()
        // Mounted one row to a page: QTable reads the page size once, so it cannot be set later.
        const wrapper = mountTable({ rows: exportRows(), rowsPerPage: 1 })
        await flushPromises()

        expect(wrapper.findAll("tbody tr")).toHaveLength(1)
        expect(await runExport(wrapper, "excelExport")).toStrictEqual(["2", "1", "STU00003"])
    })
})
