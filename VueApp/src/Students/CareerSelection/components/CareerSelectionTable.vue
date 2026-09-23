<script setup lang="ts" generic="Row extends CareerTableRow">
import { computed, ref, useTemplateRef } from "vue"
import { useStorage } from "@vueuse/core"
import type { QTable, QTableSlots } from "quasar"
import type { RouteLocationRaw } from "vue-router"
import ExportToolbar from "@/components/ExportToolbar.vue"
import ColumnToggle from "@/components/ColumnToggle.vue"
import StudentEmail from "@/Students/components/StudentEmail.vue"
import CareerRecordLink from "./CareerRecordLink.vue"
import CareerSelectionRowCard from "./CareerSelectionRowCard.vue"
import { useScrollableTableRegion } from "@/composables/use-scrollable-table-region"
import { CAREER_FIELDS, type CareerField } from "../utils/career-fields"
import { searchRows, type CareerColumn } from "../utils/career-columns"
import type { CareerTableRow } from "../types"

/**
 * The career selection roster table, shared by the overview and the report: the export toolbar
 * and column picker, the student name and email cells, and the card each row becomes on a phone.
 * The pages differ only in how a career field reads, which they supply through the two slots.
 */

const props = defineProps<{
    rows: Row[]
    columns: CareerColumn[]
    loading: boolean
    rowsPerPage: number
    canEdit: boolean
    label: string // Accessible name for the table's scrolling region.
    cellFields: CareerField[] // The career fields whose table cells the page renders itself.
    // Where the grid remembers its hidden columns. The overview and report pass the same key, as
    // they were one table in legacy: their columns share names, so a column hidden on one is
    // hidden on the other.
    columnStorageKey: string
    // Each export is handed the keys of the rows the grid shows, searched and sorted, so the file
    // matches the screen.
    excelExport: (rowKeys: string[]) => Promise<void>
    csvExport?: (rowKeys: string[]) => Promise<void>
    pdfExport: (rowKeys: string[]) => Promise<void>
    reportRoute?: RouteLocationRaw
    overviewRoute?: RouteLocationRaw
}>()

defineSlots<{
    /** A table cell for one of cellFields. `cell` is QTable's body-cell scope. */
    "field-cell"(scope: { field: CareerField; cell: Parameters<QTableSlots["body-cell"]>[0] }): unknown
    /** One career field's line on a row's phone card; called for each visible field. */
    "card-field"(scope: { field: CareerField; row: Row; value: string | null | undefined }): unknown
}>()

const filter = ref("")
const tableRef = useTemplateRef<QTable>("tableRef")

// QTable's own search looks only at the columns on screen; this one searches every column the
// page passes, hidden or not, as the legacy app did. QTable hands over only the visible columns,
// so the full list is taken from the props instead.
function filterRows(rows: readonly Row[], searchText: string): Row[] {
    return searchRows(rows as readonly (Row & Record<string, unknown>)[], searchText, props.columns)
}

// Starts sorted by name, as the legacy roster did, so the header shows the sort from the first
// render. QTable reads this once; a click on a column header still re-sorts.
const initialPagination = { rowsPerPage: props.rowsPerPage, sortBy: "fullName", descending: false }

// The columns a user hides are remembered in this browser across visits, as legacy's saved table
// state did. Hidden rather than shown columns are stored, so a column added later appears, and a
// stored name that no longer matches a column is simply ignored.
const hiddenColumns = useStorage<string[]>(props.columnStorageKey, [])
const visibleColumns = computed({
    get: () => {
        const hidden = Array.isArray(hiddenColumns.value) ? hiddenColumns.value : []
        return props.columns.map((c) => c.name).filter((name) => !hidden.includes(name))
    },
    set: (shown: string[]) => {
        hiddenColumns.value = props.columns.map((c) => c.name).filter((name) => !shown.includes(name))
    },
})
const visibleCareerFields = computed(() => CAREER_FIELDS.filter((f) => visibleColumns.value.includes(f.name)))

/**
 * The keys of every row the search lets through, in the grid's sort order and across all pages,
 * not just the one on screen. Read when an export runs, so it reflects the grid at that moment.
 */
function shownRowKeys(): string[] {
    const rows = (tableRef.value?.filteredSortedRows ?? props.rows) as Row[]
    return rows.map((row) => row.rowKey)
}

// The toolbar calls its handlers with no arguments; these supply the grid's rows.
const exportExcel = () => props.excelExport(shownRowKeys())
const exportPdf = () => props.pdfExport(shownRowKeys())
const exportCsv = computed(() => {
    const { csvExport } = props
    return csvExport === undefined ? undefined : () => csvExport(shownRowKeys())
})

function fieldValue(row: Row, field: CareerField): string | null | undefined {
    return (row as Record<string, unknown>)[field.valueField] as string | null | undefined
}

useScrollableTableRegion(tableRef, props.label)
</script>

<template>
    <q-table
        ref="tableRef"
        :rows="rows"
        :columns="columns"
        row-key="rowKey"
        :loading="loading"
        :filter="filter"
        :filter-method="filterRows"
        :pagination="initialPagination"
        dense
        :grid="$q.screen.xs"
        :wrap-cells="!$q.screen.xs"
        :visible-columns="visibleColumns"
    >
        <template #top-right>
            <ExportToolbar
                v-model:filter="filter"
                show-search
                :excel-export="exportExcel"
                :csv-export="exportCsv"
                :pdf-export="exportPdf"
                :report-route="reportRoute"
                :overview-route="overviewRoute"
            >
                <template #prepend>
                    <ColumnToggle
                        v-model="visibleColumns"
                        :columns="columns"
                    />
                </template>
            </ExportToolbar>
        </template>

        <template #body-cell-fullName="cell">
            <q-td :props="cell">
                <CareerRecordLink
                    :student="cell.row"
                    :can-edit="canEdit"
                />
            </q-td>
        </template>

        <template #body-cell-email="cell">
            <q-td :props="cell">
                <StudentEmail :email="cell.row.email" />
            </q-td>
        </template>

        <template
            v-for="f in cellFields"
            :key="f.name"
            #[`body-cell-${f.name}`]="cell"
        >
            <slot
                name="field-cell"
                :field="f"
                :cell="cell"
            />
        </template>

        <template #item="item">
            <CareerSelectionRowCard
                :student="item.row"
                :can-edit="canEdit"
                :visible-columns="visibleColumns"
            >
                <template
                    v-for="f in visibleCareerFields"
                    :key="f.name"
                >
                    <slot
                        name="card-field"
                        :field="f"
                        :row="item.row"
                        :value="fieldValue(item.row, f)"
                    />
                </template>
            </CareerSelectionRowCard>
        </template>
    </q-table>
</template>
