import type {
    ReportChartResult,
    ReportGroupResult,
    ReportPivotResult,
    ReportResult,
    ReportRowResult,
} from "../report-types"

/** A small grouped result with every kind of section, shared by the component tests. */

const ada: ReportRowResult = {
    number: 1,
    values: { name: "Ada", salary: 120_000, hired: "1995-07-01" },
    flags: [
        { kind: "Highlight", tone: "Warning", label: "60+", columnKey: null },
        { kind: "Badge", tone: "Info", label: "Emeritus", columnKey: null },
    ],
}

const bo: ReportRowResult = {
    number: 2,
    values: { name: "Bo", salary: 90_000, hired: null },
    flags: [{ kind: "Highlight", tone: "Negative", label: "Retirement age", columnKey: "salary" }],
}

const vmeGroup: ReportGroupResult = {
    label: "VME",
    rows: [ada, bo],
    subtotals: [{ label: "Salaries", columnKey: "salary", value: 210_000 }],
    charts: [{ title: "Ages: VME", kind: "Bar", points: [{ category: "60+", value: 1 }] }],
}

const samplePivot: ReportPivotResult = {
    title: "Age by department",
    columnKeys: ["APC", "VME"],
    rows: [{ key: "60+", values: [0, 1], total: 1 }],
    columnTotals: [1, 2],
    grandTotal: 3,
}

const sampleChart: ReportChartResult = { title: "Ages", kind: "Pie", points: [{ category: "Under 30", value: 2 }] }

const cy: ReportRowResult = { number: 3, values: { name: "Cy", salary: 150_000, hired: "2010-01-15" }, flags: [] }

function sampleResult(overrides: Partial<ReportResult> = {}): ReportResult {
    return {
        key: "test.sample",
        title: "Sample report",
        generatedAt: "2026-09-30T14:05:00",
        rowCount: 3,
        rowNumbers: true,
        columns: [
            { key: "name", label: "Name", format: "Text", align: "Left", decimals: null },
            { key: "salary", label: "Salary", format: "Currency", align: "Right", decimals: 2 },
            { key: "hired", label: "Hired", format: "Date", align: "Left", decimals: null },
        ],
        summary: [{ label: "People", columnKey: null, value: 3 }],
        groups: [vmeGroup, { label: "APC", rows: [cy], subtotals: [], charts: [] }],
        totals: [{ label: "All salaries", columnKey: "salary", value: 360_000 }],
        pivots: [samplePivot],
        charts: [sampleChart],
        ...overrides,
    }
}

export { ada, bo, cy, sampleChart, samplePivot, sampleResult, vmeGroup }
