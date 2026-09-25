<script setup lang="ts" generic="Row extends CareerTableRow">
import { computed, ref, useTemplateRef } from "vue"
import type { QTable, QTableProps, QTableSlots } from "quasar"
import type { RouteLocationRaw } from "vue-router"
import ExportToolbar from "@/components/ExportToolbar.vue"
import ColumnToggle from "@/components/ColumnToggle.vue"
import StudentEmail from "@/Students/components/StudentEmail.vue"
import CareerRecordLink from "./CareerRecordLink.vue"
import CareerSelectionRowCard from "./CareerSelectionRowCard.vue"
import { useScrollableTableRegion } from "@/composables/use-scrollable-table-region"
import { CAREER_FIELDS, type CareerField } from "../utils/career-fields"
import type { CareerTableRow } from "../types"

/**
 * The career selection roster table, shared by the overview and the report: the export toolbar
 * and column picker, the student name and email cells, and the card each row becomes on a phone.
 * The pages differ only in how a career field reads, which they supply through the two slots.
 */

const props = defineProps<{
    rows: Row[]
    columns: NonNullable<QTableProps["columns"]>
    loading: boolean
    rowsPerPage: number
    canEdit: boolean
    label: string // Accessible name for the table's scrolling region.
    cellFields: CareerField[] // The career fields whose table cells the page renders itself.
    excelExport: () => Promise<void>
    csvExport?: () => Promise<void>
    pdfExport: () => void
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

// Column visibility lasts only as long as the page is open, matching the legacy tool.
const visibleColumns = ref(props.columns.map((c) => c.name))
const visibleCareerFields = computed(() => CAREER_FIELDS.filter((f) => visibleColumns.value.includes(f.name)))

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
        :pagination="{ rowsPerPage }"
        dense
        :grid="$q.screen.xs"
        :wrap-cells="!$q.screen.xs"
        :visible-columns="visibleColumns"
    >
        <template #top-right>
            <ExportToolbar
                v-model:filter="filter"
                show-search
                :excel-export="excelExport"
                :csv-export="csvExport"
                :pdf-export="pdfExport"
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
