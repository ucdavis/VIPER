/**
 * Types mirroring the Report Builder API (web/Areas/ReportBuilder). Enum values arrive as their
 * C# names.
 */

type ReportColumnFormat = "Text" | "Number" | "Currency" | "Percent" | "Date" | "DateTime" | "YesNo"
type ReportParameterType = "Text" | "Number" | "Date" | "Boolean" | "Choice" | "MultiChoice"
type ReportExportFormat = "xlsx" | "pdf"

/** A form value: a list for multi-choice parameters, otherwise one string (dates as "yyyy-MM-dd"). */
type ReportParameterValue = string | string[] | null
type ReportParameterValues = Record<string, ReportParameterValue>

/** A cell as the API sends it; dates and times are ISO strings. */
type ReportCellValue = string | number | boolean | null
type ReportRow = Record<string, ReportCellValue>

/** A report the signed-in user may view. */
interface ReportCatalogItem {
    slug: string
    title: string
    description: string | null
    area: string | null
}

interface ReportChoice {
    value: string
    label: string
}

interface ReportResultColumn {
    /** The source column; also the key of the value in each row. */
    name: string
    label: string
    format: ReportColumnFormat
    sortable: boolean
}

interface ReportViewerParameter {
    name: string
    label: string
    type: ReportParameterType
    required: boolean
    defaultValue: string | null
    /** The choices, for choice parameters; null for the others. */
    choices: ReportChoice[] | null
}

interface ReportViewerDefinition {
    slug: string
    title: string
    description: string | null
    area: string | null
    sensitiveNotice: string | null
    paginate: boolean
    pageSize: number
    allowExcel: boolean
    allowPdf: boolean
    sortBy: string | null
    sortDescending: boolean
    columns: ReportResultColumn[]
    parameters: ReportViewerParameter[]
}

/** What a run asks for. Every parameter value is a list, so multi-choice parameters fit. */
interface ReportRunRequest {
    parameters: Record<string, string[]>
    page: number
    perPage: number | null
    sortBy: string | null
    sortDescending: boolean
    search: string | null
}

interface ReportRunResult {
    columns: ReportResultColumn[]
    rows: ReportRow[]
    page: number
    perPage: number
    /** Rows across all pages; null when the source can't say. */
    totalRows: number | null
    /** True when the row cap cut the result short. */
    truncated: boolean
}

export type {
    ReportCatalogItem,
    ReportCellValue,
    ReportChoice,
    ReportColumnFormat,
    ReportExportFormat,
    ReportParameterType,
    ReportParameterValue,
    ReportParameterValues,
    ReportResultColumn,
    ReportRow,
    ReportRunRequest,
    ReportRunResult,
    ReportViewerDefinition,
    ReportViewerParameter,
}
