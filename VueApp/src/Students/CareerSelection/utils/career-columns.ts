import type { QTableProps } from "quasar"
import { CAREER_FIELDS } from "./career-fields"

// Career statements run to 5000 characters, which makes the row unreadable. The report shows
// an opening excerpt; the full text is on the detail page and in the Excel export. The excerpt is
// applied in the report's cell slots, not as a column format: QTable's search matches the
// formatted value, so a format would hide everything past the excerpt from search.
const STATEMENT_PREVIEW_LENGTH = 50

function previewStatement(value: string | null | undefined): string {
    const text = value?.trim() ?? ""
    return text.length <= STATEMENT_PREVIEW_LENGTH ? text : `${text.slice(0, STATEMENT_PREVIEW_LENGTH - 1).trimEnd()}…`
}

function formatDate(value: string | null): string {
    return value ? new Date(value).toLocaleDateString() : ""
}

const LAST_UPDATED_COLUMN = {
    name: "lastUpdated",
    label: "Last Updated",
    field: "lastUpdated",
    align: "left" as const,
    sortable: true,
    format: formatDate,
}

const OVERVIEW_COLUMNS: NonNullable<QTableProps["columns"]> = [
    { name: "classLevel", label: "Class", field: "classLevel", align: "center", sortable: true },
    { name: "fullName", label: "Name", field: "fullName", align: "left", sortable: true },
    { name: "email", label: "Email", field: "email", align: "left", sortable: true },
    ...CAREER_FIELDS.map((f) => ({
        name: f.name,
        label: f.label,
        field: f.completedField ?? f.valueField,
        align: f.completedField ? ("center" as const) : ("left" as const),
        sortable: true,
    })),
    LAST_UPDATED_COLUMN,
]

const REPORT_COLUMNS: NonNullable<QTableProps["columns"]> = [
    { name: "classLevel", label: "Class", field: "classLevel", align: "left", sortable: true },
    { name: "fullName", label: "Name", field: "fullName", align: "left", sortable: true },
    { name: "email", label: "Email", field: "email", align: "left", sortable: true },
    ...CAREER_FIELDS.map((f) => ({
        name: f.name,
        label: f.label,
        field: f.valueField,
        align: "left" as const,
        // Paragraphs of free text have no useful order to sort by.
        sortable: !f.statement,
    })),
    LAST_UPDATED_COLUMN,
]

export { OVERVIEW_COLUMNS, REPORT_COLUMNS, previewStatement, STATEMENT_PREVIEW_LENGTH }
