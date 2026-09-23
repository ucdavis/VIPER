import type { QTableColumn } from "quasar"
import { useDateFunctions } from "@/composables/DateFunctions"
import { CAREER_FIELDS } from "./career-fields"

/**
 * A roster column. `searchable: false` keeps a column out of the grid's search: the overview's
 * completeness columns hold true/false behind an icon, which the legacy app never searched.
 */
type CareerColumn = QTableColumn & { searchable?: boolean }

const { formatDate } = useDateFunctions()

// Career statements run to 5000 characters, which makes the row unreadable. The report shows
// an opening excerpt; the full text is on the detail page and in the Excel export. The excerpt is
// applied in the report's cell slots, not as a column format: the grid's search matches the
// formatted value, so a format would hide everything past the excerpt from search, which the
// legacy app searched in full.
const STATEMENT_PREVIEW_LENGTH = 50

function previewStatement(value: string | null | undefined): string {
    const text = value?.trim() ?? ""
    return text.length <= STATEMENT_PREVIEW_LENGTH ? text : `${text.slice(0, STATEMENT_PREVIEW_LENGTH - 1).trimEnd()}…`
}

// The app's shared date format, in the browser's locale. Search matches the same text, so a date
// reads and searches alike.
const LAST_UPDATED_COLUMN: CareerColumn = {
    name: "lastUpdated",
    label: "Last Updated",
    field: "lastUpdated",
    align: "left",
    sortable: true,
    format: (value: string | null) => (value ? formatDate(value) : ""),
}

const OVERVIEW_COLUMNS: CareerColumn[] = [
    { name: "classLevel", label: "Class", field: "classLevel", align: "center", sortable: true },
    { name: "fullName", label: "Name", field: "fullName", align: "left", sortable: true },
    { name: "email", label: "Email", field: "email", align: "left", sortable: true },
    ...CAREER_FIELDS.map(
        (f): CareerColumn => ({
            name: f.name,
            label: f.label,
            field: f.completedField ?? f.valueField,
            align: f.completedField ? "center" : "left",
            sortable: true,
            searchable: f.completedField === undefined,
        }),
    ),
    LAST_UPDATED_COLUMN,
]

const REPORT_COLUMNS: CareerColumn[] = [
    { name: "classLevel", label: "Class", field: "classLevel", align: "left", sortable: true },
    { name: "fullName", label: "Name", field: "fullName", align: "left", sortable: true },
    { name: "email", label: "Email", field: "email", align: "left", sortable: true },
    ...CAREER_FIELDS.map(
        (f): CareerColumn => ({
            name: f.name,
            label: f.label,
            field: f.valueField,
            align: "left",
            // Paragraphs of free text have no useful order to sort by.
            sortable: !f.statement,
        }),
    ),
    LAST_UPDATED_COLUMN,
]

/** A cell's text as the grid shows it: the column's field, through its format if it has one. */
function cellText(column: CareerColumn, row: Record<string, unknown>): string {
    const value = typeof column.field === "function" ? column.field(row) : row[column.field]
    const shown = column.format ? column.format(value, row) : value
    return shown === null || shown === undefined ? "" : String(shown)
}

/**
 * The grid's search, as the legacy app's: a row matches when any searchable column contains the
 * search text, ignoring case. Every column counts, including ones the user has hidden, so a row
 * can match on something not on screen. The text is matched as one phrase within a single cell,
 * with only the spaces around it ignored, so a stray space before or after a word does not hide
 * every row.
 */
function searchRows<Row extends Record<string, unknown>>(
    rows: readonly Row[],
    searchText: string,
    columns: readonly CareerColumn[],
): Row[] {
    const needle = searchText.trim().toLowerCase()
    if (needle === "") {
        return [...rows]
    }
    const searched = columns.filter((c) => c.searchable !== false)
    return rows.filter((row) => searched.some((c) => cellText(c, row).toLowerCase().includes(needle)))
}

export { OVERVIEW_COLUMNS, REPORT_COLUMNS, previewStatement, searchRows, STATEMENT_PREVIEW_LENGTH }
export type { CareerColumn }
