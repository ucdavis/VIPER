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
            :grid="$q.screen.lt.md"
        >
            <template
                v-if="showStatus"
                #body-cell-status="slotProps"
            >
                <q-td :props="slotProps">
                    <q-badge
                        :color="getStatusColor(slotProps.row.status)"
                        :label="slotProps.row.status"
                    />
                </q-td>
            </template>
            <!-- Mobile card view -->
            <template #item="slotProps">
                <div class="q-pa-xs col-12">
                    <q-card
                        flat
                        bordered
                        class="q-pa-sm"
                    >
                        <div class="row items-center q-mb-xs">
                            <q-badge
                                v-if="showStatus"
                                :color="getStatusColor(slotProps.row.status)"
                                :label="slotProps.row.status"
                                class="q-mr-sm"
                            />
                            <span class="text-weight-medium">{{ getInstructorName(slotProps.row) }}</span>
                        </div>
                        <div class="text-caption text-grey-8">
                            <span class="text-weight-medium">{{ getCourseCode(slotProps.row) }}</span>
                            &middot; {{ slotProps.row.effortType }} &middot; {{ getWeeks(slotProps.row) }}
                            {{ inflect("week", getWeeks(slotProps.row)) }}
                        </div>
                        <div
                            v-if="slotProps.row.roleName"
                            class="text-caption text-grey-6"
                        >
                            {{ slotProps.row.roleName }}
                        </div>
                    </q-card>
                </div>
            </template>
        </q-table>
    </div>
</template>

<script setup lang="ts">
import { ref, computed } from "vue"
import { useQuasar } from "quasar"
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

const $q = useQuasar()
const filter = ref("")

function getInstructorName(row: ClinicalEffortRow): string {
    return "instructorName" in row ? row.instructorName : row.personName
}

function getCourseCode(row: ClinicalEffortRow): string {
    return "courseNumber" in row ? row.courseNumber : row.courseCode
}

function getWeeks(row: ClinicalEffortRow): number {
    return row.weeks ?? 0
}

function rowKey(row: ClinicalEffortRow): string {
    const mothraId = row.mothraId
    const course = getCourseCode(row)
    const status = "status" in row ? row.status : ""
    return `${mothraId}-${course}-${status}`
}

function getStatusColor(status: string): string {
    switch (status) {
        case "New":
            return "positive"
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
    const columns: QTableColumn[] = []

    if (props.showStatus) {
        columns.push({ name: "status", label: "Status", field: "status", align: "left", sortable: true })
    }

    columns.push(
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
    )

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
