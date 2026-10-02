/**
 * Types mirroring the reports API (web/Areas/Reports). Enum values arrive as their C# names.
 */

type ReportParameterType = "Choice" | "MultiChoice" | "Date" | "Text" | "Number" | "Boolean"
type ReportColumnFormat = "Text" | "Number" | "Currency" | "Percent" | "Date" | "List"
type ReportAlignment = "Left" | "Center" | "Right"
type ReportTone = "Neutral" | "Info" | "Positive" | "Warning" | "Negative" | "Muted"
type ReportFlagKind = "Highlight" | "Badge"
type ReportChartKind = "Bar" | "Line" | "Pie"
type ReportExportFormat = "csv" | "xlsx" | "pdf"

/** A parameter value as the form holds it; dates are "yyyy-MM-dd" strings. */
type ReportParameterValue = string | string[] | number | boolean | null
type ReportParameterValues = Record<string, ReportParameterValue>

/** A cell value as the API sends it; dates are ISO strings. */
type ReportCellValue = string | string[] | number | boolean | null

/**
 * A report the signed-in user may run, as listed by the reports catalog. `available` is false
 * for a planned report that is listed ahead of time but not built yet.
 */
interface ReportCatalogItem {
    key: string
    title: string
    area: string
    description: string
    available: boolean
}

interface ReportOption {
    value: string
    label: string
}

interface ReportParameterMetadata {
    name: string
    label: string
    type: ReportParameterType
    required: boolean
    defaultValue: ReportParameterValue
    options: ReportOption[]
}

interface ReportColumnMetadata {
    key: string
    label: string
    format: ReportColumnFormat
    align: ReportAlignment
    decimals: number | null
}

interface ReportDefinitionMetadata {
    key: string
    title: string
    area: string
    description: string
    parameters: ReportParameterMetadata[]
    columns: ReportColumnMetadata[]
}

interface ReportFlag {
    kind: ReportFlagKind
    tone: ReportTone
    label: string
    columnKey: string | null
}

interface ReportRowResult {
    number: number
    values: Record<string, ReportCellValue>
    flags: ReportFlag[]
}

interface ReportTotal {
    label: string
    columnKey: string | null
    value: number | null
}

interface ReportChartPoint {
    category: string
    value: number | null
}

interface ReportChartResult {
    title: string
    kind: ReportChartKind
    points: ReportChartPoint[]
}

interface ReportGroupResult {
    label: string | null
    rows: ReportRowResult[]
    subtotals: ReportTotal[]
    charts: ReportChartResult[]
}

interface ReportPivotRow {
    key: string
    values: (number | null)[]
    total: number | null
}

interface ReportPivotResult {
    title: string
    columnKeys: string[]
    rows: ReportPivotRow[]
    columnTotals: (number | null)[]
    grandTotal: number | null
}

interface ReportResult {
    key: string
    title: string
    generatedAt: string
    rowCount: number
    rowNumbers: boolean
    columns: ReportColumnMetadata[]
    summary: ReportTotal[]
    groups: ReportGroupResult[]
    totals: ReportTotal[]
    pivots: ReportPivotResult[]
    charts: ReportChartResult[]
}

export type {
    ReportAlignment,
    ReportCatalogItem,
    ReportCellValue,
    ReportChartKind,
    ReportChartPoint,
    ReportChartResult,
    ReportColumnFormat,
    ReportColumnMetadata,
    ReportDefinitionMetadata,
    ReportExportFormat,
    ReportFlag,
    ReportFlagKind,
    ReportGroupResult,
    ReportOption,
    ReportParameterMetadata,
    ReportParameterType,
    ReportParameterValue,
    ReportParameterValues,
    ReportPivotResult,
    ReportPivotRow,
    ReportResult,
    ReportRowResult,
    ReportTone,
    ReportTotal,
}
