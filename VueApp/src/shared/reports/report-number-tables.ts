import type { ReportChartResult, ReportPivotResult } from "./report-types"

/**
 * Pivots and chart data as simple labelled-number tables, the way the PDF export shows them.
 */

interface NumberRow {
    label: string
    values: (number | null)[]
}

interface NumberTable {
    /** The label column's header, then one header per value. */
    headers: string[]
    rows: NumberRow[]
    total: NumberRow | null
}

const CATEGORY_LABEL = "Category"
const TOTAL_LABEL = "Total"

function pivotTable(pivot: ReportPivotResult): NumberTable {
    return {
        headers: [CATEGORY_LABEL, ...pivot.columnKeys, TOTAL_LABEL],
        rows: pivot.rows.map((row) => ({ label: row.key, values: [...row.values, row.total] })),
        total: { label: TOTAL_LABEL, values: [...pivot.columnTotals, pivot.grandTotal] },
    }
}

function chartTable(chart: ReportChartResult): NumberTable {
    return {
        headers: [CATEGORY_LABEL, "Value"],
        rows: chart.points.map((point) => ({ label: point.category, values: [point.value] })),
        total: null,
    }
}

export { chartTable, pivotTable }
export type { NumberRow, NumberTable }
