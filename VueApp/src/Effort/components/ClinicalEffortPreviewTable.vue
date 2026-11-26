<template>
    <div>
        <div class="row items-center justify-between q-mb-sm">
            <div class="text-subtitle2">{{ title }} ({{ rows.length }} {{ inflect("record", rows.length) }})</div>
            <CompactSearch v-model="filter" />
        </div>
        <q-table
            :rows="rows"
            :columns="tableColumns"
            :row-key="rowKey"
            :filter="filter"
            dense
            flat
            bordered
            :pagination="pagination"
        >
            <template #body-cell-weeks="slotProps">
                <q-td :props="slotProps">
                    <template v-if="hasWeeksChange(slotProps.row)">
                        <span class="text-strike text-grey-7">{{ getExistingWeeks(slotProps.row) }}</span>
                        <q-icon
                            name="arrow_forward"
                            size="xs"
                            class="q-mx-xs"
                        />
                        <span class="text-bold text-info">{{ slotProps.row.weeks }}</span>
                    </template>
                    <template v-else>
                        {{ slotProps.row.weeks }}
                    </template>
                </q-td>
            </template>
            <template
                v-if="showStatus"
                #body-cell-status="slotProps"
            >
                <q-td :props="slotProps">
                    <StatusBadge v-bind="getStatusBadgeProps(slotProps.row)" />
                </q-td>
            </template>
        </q-table>
    </div>
</template>

<script setup lang="ts">
import { ref, computed } from "vue"
import type { QTableColumn } from "quasar"
import { inflect } from "inflection"
import StatusBadge from "@/components/StatusBadge.vue"
import CompactSearch from "./CompactSearch.vue"
import type { ClinicalAssignmentPreview, HarvestRecordPreview } from "../types"

type ClinicalEffortRow = ClinicalAssignmentPreview | HarvestRecordPreview

const props = withDefaults(
    defineProps<{
        rows: ClinicalEffortRow[]
        title?: string
        showStatus?: boolean
        pagination?: { rowsPerPage: number }
    }>(),
    {
        title: "Clinical Effort",
        showStatus: false,
        pagination: () => ({ rowsPerPage: 10 }),
    },
)

const filter = ref<string | null>("")

function getInstructorName(row: ClinicalEffortRow): string {
    return "instructorName" in row ? row.instructorName : row.personName
}

function getCourseCode(row: ClinicalEffortRow): string {
    return "courseNumber" in row ? row.courseNumber : row.courseCode
}

function rowKey(row: ClinicalEffortRow): string {
    const mothraId = row.mothraId
    const course = getCourseCode(row)
    const status = getStatus(row)
    return `${mothraId}-${course}-${status}`
}

function getStatus(row: ClinicalEffortRow): string {
    if ("status" in row) return row.status
    if ("isNew" in row) return row.isNew ? "New" : "Exists"
    return ""
}

function hasWeeksChange(row: ClinicalEffortRow): boolean {
    return "existingWeeks" in row && row.existingWeeks !== null
}

function getExistingWeeks(row: ClinicalEffortRow): number | null {
    return "existingWeeks" in row ? row.existingWeeks : null
}

function getStatusColor(status: string): string {
    switch (status) {
        case "New":
            return "positive"
        case "Exists":
            return "grey-6"
        case "Update":
            return "info"
        case "Delete":
            return "negative"
        case "Skip":
            return "grey-6"
        default:
            return "grey"
    }
}

function getStatusBadgeProps(row: ClinicalEffortRow) {
    const label = getStatus(row)
    return { label, color: getStatusColor(label) }
}

const tableColumns = computed<QTableColumn[]>(() => {
    const columns: QTableColumn[] = [
        {
            name: "instructor",
            label: "Instructor",
            field: (row) => getInstructorName(row),
            align: "left",
            sortable: true,
        },
        {
            name: "course",
            label: "Course",
            field: (row) => getCourseCode(row),
            align: "left",
            sortable: true,
        },
        { name: "effortType", label: "Type", field: "effortType", align: "left", sortable: true },
        { name: "weeks", label: "Weeks", field: "weeks", align: "center", sortable: true },
        { name: "roleName", label: "Role", field: "roleName", align: "left", sortable: true },
    ]

    if (props.showStatus) {
        columns.push({
            name: "status",
            label: "Status",
            field: (row) => getStatus(row),
            align: "left",
            sortable: true,
        })
    }

    return columns
})
</script>
