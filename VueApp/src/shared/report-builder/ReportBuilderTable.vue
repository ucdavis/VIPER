<template>
    <q-table
        ref="table"
        :rows="tableRows"
        :columns="tableColumns"
        v-model:pagination="tablePagination"
        :rows-per-page-options="rowsPerPageOptions"
        :loading="loading"
        :hide-pagination="!paginate"
        :virtual-scroll="!paginate"
        row-key="key"
        binary-state-sort
        flat
        bordered
        dense
        class="sticky-header-table"
        @request="onRequest"
    />
</template>

<script setup lang="ts">
import { computed, ref, useTemplateRef, watch } from "vue"
import type { QTable, QTableColumn, QTableProps } from "quasar"
import { useScrollableTableRegion } from "@/composables/use-scrollable-table-region"
import { formatCell, isNumericFormat } from "./report-builder-format"
import type { ReportPagination } from "./use-report-builder"
import type { ReportCellValue, ReportResultColumn, ReportRow } from "./report-builder-types"

/**
 * The report's rows. Paging and sorting happen on the server: the table asks for them through
 * `request`, and the report sends back the page to show. Clicking the sorted column flips its
 * direction rather than clearing the sort.
 *
 * QTable shows a `pagination` prop only when it's bound with `v-model:pagination`; given the prop
 * alone it keeps its own copy from the first render, so the pager never moved and every click on a
 * heading asked for the same sort again. So the table is bound to a copy that follows the report's.
 */
const {
    columns,
    rows,
    pagination,
    paginate,
    title,
    loading = false,
} = defineProps<{
    columns: ReportResultColumn[]
    rows: ReportRow[]
    pagination: ReportPagination
    paginate: boolean
    /** Names the table's scrolling region for screen readers. */
    title: string
    loading?: boolean
}>()

const emit = defineEmits<{ (e: "request", pagination: Omit<ReportPagination, "rowsNumber">): void }>()

const PAGE_SIZES = [10, 25, 50, 100, 250, 500]

/** A row with a key the table can track; rows have no id of their own. */
type TableRow = { key: number; values: ReportRow }

const tableRows = computed<TableRow[]>(() => rows.map((values, key) => ({ key, values })))

const tableColumns = computed<QTableColumn<TableRow>[]>(() =>
    columns.map((column) => ({
        name: column.name,
        label: column.label,
        field: (row: TableRow): ReportCellValue => row.values[column.name] ?? null,
        format: (value: ReportCellValue) => formatCell(value, column.format),
        align: isNumericFormat(column.format) ? "right" : "left",
        sortable: column.sortable,
    })),
)

/** The usual page sizes, plus the report's own, in order, when it isn't one of them. */
const rowsPerPageOptions = computed(() => {
    const size = pagination.rowsPerPage
    if (size <= 0 || PAGE_SIZES.includes(size)) {
        return PAGE_SIZES
    }
    return [...PAGE_SIZES.filter((option) => option < size), size, ...PAGE_SIZES.filter((option) => option > size)]
})

// The table may tidy this copy (it does on first render); the report's own pagination still wins
// whenever it changes, and changes go to the report only through `request`.
const tablePagination = ref<ReportPagination>({ ...pagination })
watch(
    () => pagination,
    (next) => {
        tablePagination.value = { ...next }
    },
)

function onRequest(request: Parameters<NonNullable<QTableProps["onRequest"]>>[0]): void {
    const { page, rowsPerPage, sortBy, descending } = request.pagination
    emit("request", {
        page: page ?? 1,
        rowsPerPage: rowsPerPage ?? pagination.rowsPerPage,
        sortBy: sortBy ?? null,
        descending: descending ?? false,
    })
}

useScrollableTableRegion(useTemplateRef<QTable>("table"), title)
</script>
