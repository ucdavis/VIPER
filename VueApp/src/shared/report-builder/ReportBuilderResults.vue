<template>
    <div>
        <ExportToolbar
            class="q-mb-sm"
            :filter="search"
            show-search
            :excel-export="allowExcel && canExport ? () => exportAs('xlsx') : undefined"
            :pdf-export="allowPdf && canExport ? () => exportAs('pdf') : undefined"
            :busy="exporting !== null"
            @update:filter="emit('search', $event)"
        />
        <StatusBanner
            v-if="result.truncated"
            type="warning"
        >
            Only the first {{ ROW_CAP }} rows are shown. Narrow the filters to see the rest.
        </StatusBanner>
        <StatusBanner
            v-if="nothingToExport"
            type="info"
            live="assertive"
        >
            There's nothing to download: the report has no rows.
        </StatusBanner>
        <StatusBanner
            v-if="result.rows.length === 0 && !running"
            type="info"
        >
            No rows match these filters.
        </StatusBanner>
        <ReportBuilderTable
            v-else
            :columns="result.columns"
            :rows="result.rows"
            :pagination="pagination"
            :paginate="paginate"
            :title="title"
            :loading="running"
            @request="emit('request', $event)"
        />
    </div>
</template>

<script setup lang="ts">
import ExportToolbar from "@/components/ExportToolbar.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import ReportBuilderTable from "./ReportBuilderTable.vue"
import type { ReportPagination } from "./use-report-builder"
import type { ReportExportFormat, ReportRunResult } from "./report-builder-types"

/**
 * A run's rows with the search box, the downloads the report allows, and notes for a result that
 * was cut short or is empty.
 */
defineProps<{
    result: ReportRunResult
    pagination: ReportPagination
    paginate: boolean
    title: string
    search: string
    allowExcel: boolean
    allowPdf: boolean
    canExport: boolean
    running: boolean
    exporting: ReportExportFormat | null
    nothingToExport: boolean
    /** Downloads the rows on screen; the toolbar's button spins until it settles. */
    exportAs: (format: ReportExportFormat) => Promise<void>
}>()

const emit = defineEmits<{
    (e: "request", pagination: Omit<ReportPagination, "rowsNumber">): void
    (e: "search", text: string): void
}>()

// The server's row cap (ReportLimits.MaxRows).
const ROW_CAP = "50,000"
</script>
