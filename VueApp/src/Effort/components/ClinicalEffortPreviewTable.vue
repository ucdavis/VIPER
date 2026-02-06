<template>
    <div>
        <div class="row items-center justify-between q-mb-sm">
            <div class="text-subtitle2">{{ title }} ({{ rows.length }} {{ inflect("record", rows.length) }})</div>
            <q-input
                v-model="filter"
                placeholder="Search..."
                dense
                outlined
                clearable
                class="compact-search"
            >
                <template #prepend>
                    <q-icon
                        name="search"
                        size="xs"
                    />
                </template>
            </q-input>
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
                    <q-badge
                        :color="getStatusColor(getStatus(slotProps.row))"
                        :label="getStatus(slotProps.row)"
                    />
                </q-td>
            </template>
        </q-table>
    </div>
</template>

<script setup lang="ts">
import { ref, computed } from "vue"
import type { QTableColumn } from "quasar"
import { inflect } from "inflection"
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

const filter = ref("")

function getInstructorName(row: ClinicalEffortRow): string {
    return "instructorName" in row ? row.instructorName : row.personName
}

function getCourseCode(row: ClinicalEffortRow): string {
    return "courseNumber" in row ? row.courseNumber : row.courseCode
}

function rowKey(row: ClinicalEffortRow): string {
    const mothraId = row.mothraId
    const course = getCourseCode(row)
    const status = "status" in row ? row.status : ""
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

<style scoped>
.compact-search {
    width: 10rem;
}

.compact-search :deep(.q-field__control) {
    height: 1.75rem;
    min-height: 1.75rem;
}

.compact-search :deep(.q-field__native) {
    font-size: 0.75rem;
    padding: 0;
}

.compact-search :deep(.q-field__prepend) {
    height: 1.75rem;
    padding-left: 0;
    padding-right: 0.25rem;
}

.compact-search :deep(.q-field__append) {
    height: 1.75rem;
    padding: 0 0.25rem;
}
</style>
