<template>
    <q-table
        :rows="rows"
        :columns="columns"
        :pagination="ALL_ROWS"
        row-key="number"
        flat
        bordered
        dense
        hide-bottom
        class="q-mb-sm"
    >
        <template #body-cell="cell">
            <q-td
                :props="cell"
                :class="highlightClass(cell.row.flags, reportColumnKey(cell.col.name), visibleKeys)"
            >
                {{ cell.value }}
                <StatusBadge
                    v-for="(badge, index) in badges(cell.row, cell.col.name)"
                    :key="index"
                    :color="badgeColor(badge.tone)"
                    :label="badge.label"
                    class="q-ml-xs"
                />
            </q-td>
        </template>
    </q-table>
</template>

<script setup lang="ts">
import { computed } from "vue"
import StatusBadge from "@/components/StatusBadge.vue"
import { NOTES_COLUMN, ROW_NUMBER_COLUMN, toTableColumns } from "./report-columns"
import { badgeColor, cellBadges, highlightClass } from "./report-tones"
import type { ReportFlag, ReportResult, ReportRowResult } from "./report-types"

/**
 * One group of a report as a table: every row, with highlights as tinted cells and badges
 * beside their values. Highlight labels also appear in the Notes column.
 */
const { result, rows } = defineProps<{
    result: ReportResult
    rows: ReportRowResult[]
}>()

// Reports keep their rows in the server's order and are short enough to show at once.
const ALL_ROWS = { rowsPerPage: 0 }
const EXTRA_COLUMNS = new Set([ROW_NUMBER_COLUMN, NOTES_COLUMN])

const columns = computed(() => toTableColumns(result))
const visibleKeys = computed(() => new Set(result.columns.map((column) => column.key)))
const firstColumnKey = computed(() => result.columns[0]?.key)

/** The report column a table column shows, or null for the row number and Notes columns. */
function reportColumnKey(name: string): string | null {
    return EXTRA_COLUMNS.has(name) ? null : name
}

function badges(row: ReportRowResult, name: string): ReportFlag[] {
    return EXTRA_COLUMNS.has(name) ? [] : cellBadges(row.flags, name, name === firstColumnKey.value)
}
</script>

<style src="./report-tones.css"></style>
