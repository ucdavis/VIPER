import type { ReportRunResult, ReportViewerDefinition, ReportViewerParameter } from "../report-builder-types"

/** Shared test data: a roster report with one of each kind of parameter. */

const textParameter: ReportViewerParameter = {
    name: "Name",
    label: "Name",
    type: "Text",
    required: false,
    defaultValue: null,
    choices: null,
}

const deptParameter: ReportViewerParameter = {
    name: "Dept",
    label: "Department",
    type: "MultiChoice",
    required: false,
    defaultValue: null,
    choices: [
        { value: "VME", label: "Medicine" },
        { value: "PMI", label: "Pathology" },
    ],
}

const hiredParameter: ReportViewerParameter = {
    name: "Hired",
    label: "Hired from",
    type: "Date",
    required: true,
    defaultValue: "2026-01-01",
    choices: null,
}

const roster: ReportViewerDefinition = {
    slug: "8a6f1d2e-0b7c-4a59-9d3e-1f2a3b4c5d6e",
    title: "Faculty roster",
    description: "Current faculty by department.",
    area: "Personnel",
    sensitiveNotice: null,
    paginate: true,
    pageSize: 25,
    allowExcel: true,
    allowPdf: false,
    sortBy: "Name",
    sortDescending: false,
    columns: [
        { name: "Name", label: "Name", format: "Text", sortable: true },
        { name: "Salary", label: "Salary", format: "Currency", sortable: false },
    ],
    parameters: [textParameter, deptParameter, hiredParameter],
}

function resultOf(rows: ReportRunResult["rows"], overrides: Partial<ReportRunResult> = {}): ReportRunResult {
    return {
        columns: roster.columns,
        rows,
        page: 1,
        perPage: 25,
        totalRows: rows.length,
        truncated: false,
        ...overrides,
    }
}

export { deptParameter, hiredParameter, resultOf, roster, textParameter }
