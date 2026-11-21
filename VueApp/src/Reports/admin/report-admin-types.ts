import type {
    ReportChoice,
    ReportColumnFormat,
    ReportParameterType,
} from "@/shared/report-builder/report-builder-types"

/**
 * Types mirroring the Report Builder admin API (ReportBuilderAdminController). Enum values
 * arrive as their C# names; dates as ISO strings.
 */

type ReportSourceKind = "View" | "Procedure"
type ReportFilterOperator = "Equals" | "Contains" | "From" | "To" | "In"
type ReportParameterBinding = "UserInput" | "CurrentUserLoginId" | "CurrentUserMothraId"
type ReportHistoryAction = "Created" | "Updated" | "Deleted" | "Restored"

interface ReportSourceSummary {
    name: string
    kind: ReportSourceKind
    description: string | null
}

/** The kind of value a source column holds, from its SQL type. */
type ReportValueKind =
    | "Text"
    | "WholeNumber"
    | "FractionalNumber"
    | "Date"
    | "DateTime"
    | "Time"
    | "Boolean"
    | "UniqueId"
    | "Binary"
    | "Other"

interface ReportSourceColumn {
    name: string
    sqlType: string
    kind: ReportValueKind
    isSupported: boolean
}

interface ReportSourceParameter {
    name: string
    sqlType: string
    isSupported: boolean
}

interface ReportSourceDetail extends ReportSourceSummary {
    columns: ReportSourceColumn[]
    parameters: ReportSourceParameter[]
    /** Why SQL Server couldn't describe a procedure's columns; null when they're known. */
    columnsError: string | null
    /** Whether the source pages itself (views, and procedures taking @Skip and @Take). */
    pagesInSql: boolean
}

interface ReportColumnEdit {
    sourceColumn: string
    label: string | null
    format: ReportColumnFormat
    sortable: boolean
    visible: boolean
}

interface ReportParameterEdit {
    name: string
    label: string | null
    type: ReportParameterType
    operator: ReportFilterOperator | null
    required: boolean
    defaultValue: string | null
    choicesSource: string | null
    choicesList: ReportChoice[] | null
    binding: ReportParameterBinding
}

/** A report's editable fields: the body of a save, and what a read's `report` holds. */
interface ReportEditRequest {
    title: string
    description: string | null
    area: string | null
    source: string
    paginate: boolean
    pageSize: number | null
    allowExcel: boolean
    allowPdf: boolean
    pdfLandscape: boolean
    sensitiveNotice: string | null
    orderBy: string | null
    orderByDescending: boolean
    permissions: string[]
    columns: ReportColumnEdit[]
    parameters: ReportParameterEdit[]
    /** The saved version this edit started from; a save from an older one is refused. */
    lastModifiedOn: string | null
}

interface ReportAdminDefinition {
    id: number
    slug: string
    createdOn: string | null
    createdBy: string | null
    modifiedOn: string | null
    modifiedBy: string | null
    deletedOn: string | null
    deletedBy: string | null
    report: ReportEditRequest
    /** Permissions the report names that RAPS no longer has. */
    missingPermissions: string[]
}

interface ReportAdminListItem {
    id: number
    slug: string
    title: string
    area: string | null
    source: string
    modifiedOn: string | null
    modifiedBy: string | null
    deletedOn: string | null
    permissions: string[]
    missingPermissions: string[]
}

interface ReportHistoryItem {
    id: number
    action: ReportHistoryAction | null
    changedOn: string | null
    changedBy: string | null
}

interface ReportHistoryVersion {
    version: ReportHistoryItem
    report: ReportEditRequest | null
}

type ReportRunAction = "Run" | "ExportExcel" | "ExportPdf"

/** A run or download that failed because of the report or its source. */
interface ReportFailure {
    id: number
    reportId: number
    reportTitle: string
    reportDeleted: boolean
    action: ReportRunAction | null
    loginId: string | null
    ranOn: string | null
    /** The values the user chose, as JSON. */
    parameters: string | null
    errorNumber: number | null
    errorMessage: string
}

interface ReportCheckProblem {
    id: number
    title: string
    area: string | null
    source: string
    problems: string[]
}

/** Every live report checked as a save would check it; only the failing ones are listed. */
interface ReportCheckResult {
    checked: number
    checkedOn: string
    failing: ReportCheckProblem[]
}

interface ReportPermissionOption {
    permission: string
    description: string | null
}

export type {
    ReportAdminDefinition,
    ReportAdminListItem,
    ReportCheckProblem,
    ReportCheckResult,
    ReportColumnEdit,
    ReportEditRequest,
    ReportFailure,
    ReportFilterOperator,
    ReportHistoryAction,
    ReportHistoryItem,
    ReportHistoryVersion,
    ReportParameterBinding,
    ReportParameterEdit,
    ReportPermissionOption,
    ReportRunAction,
    ReportSourceColumn,
    ReportSourceDetail,
    ReportSourceKind,
    ReportSourceParameter,
    ReportSourceSummary,
    ReportValueKind,
}
