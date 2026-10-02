import type { QTableColumn } from "quasar"
import { formatReportValue } from "./report-format"
import { flagText } from "./report-tones"
import type { ReportAlignment, ReportCellValue, ReportResult, ReportRowResult } from "./report-types"

/**
 * Turns a report result's columns into Quasar table columns. Report column keys are camelCase
 * property names, so the two extra columns use names no report column can have.
 */

const ROW_NUMBER_COLUMN = "#number"
const NOTES_COLUMN = "#notes"

const ALIGNMENTS: Record<ReportAlignment, "left" | "center" | "right"> = {
    Left: "left",
    Center: "center",
    Right: "right",
}

/** True when any row has a highlight or badge, which adds a Notes column as in the exports. */
function hasNotes(result: ReportResult): boolean {
    return result.groups.some((group) => group.rows.some((row) => row.flags.length > 0))
}

function toTableColumns(result: ReportResult): QTableColumn<ReportRowResult>[] {
    const columns: QTableColumn<ReportRowResult>[] = result.columns.map((column) => ({
        name: column.key,
        label: column.label,
        field: (row: ReportRowResult): ReportCellValue => row.values[column.key] ?? null,
        align: ALIGNMENTS[column.align],
        format: (value: ReportCellValue) => formatReportValue(value, column),
        sortable: true,
    }))

    if (result.rowNumbers) {
        columns.unshift({
            name: ROW_NUMBER_COLUMN,
            label: "#",
            field: (row: ReportRowResult) => row.number,
            align: "right",
            sortable: true,
        })
    }

    if (hasNotes(result)) {
        columns.push({ name: NOTES_COLUMN, label: "Notes", field: flagText, align: "left" })
    }
    return columns
}

export { NOTES_COLUMN, ROW_NUMBER_COLUMN, hasNotes, toTableColumns }
