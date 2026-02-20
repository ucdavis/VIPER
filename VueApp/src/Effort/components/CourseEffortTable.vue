<template>
    <div>
        <!-- Loading state -->
        <div
            v-if="isLoading"
            class="text-grey q-my-md"
            role="status"
        >
            Loading effort records...
        </div>

        <!-- Error state -->
        <q-banner
            v-else-if="loadError"
            class="bg-negative text-white q-mb-md"
            rounded
            role="alert"
        >
            <template #avatar>
                <q-icon
                    name="error"
                    color="white"
                />
            </template>
            {{ loadError }}
        </q-banner>

        <template v-else-if="records.length > 0">
            <!-- Mobile Card View -->
            <div class="lt-sm">
                <div
                    v-for="group in groupedRecords"
                    :key="group.personId"
                    class="q-mb-md"
                >
                    <div class="text-weight-bold q-mb-xs">
                        <router-link
                            v-if="canViewInstructors"
                            :to="{
                                name: 'InstructorDetail',
                                params: { termCode, personId: group.personId },
                            }"
                            class="text-primary"
                        >
                            {{ group.instructorName }}
                        </router-link>
                        <span v-else>{{ group.instructorName }}</span>
                    </div>
                    <q-card
                        v-for="record in group.records"
                        :key="record.effortId"
                        flat
                        bordered
                        class="q-mb-sm"
                    >
                        <q-card-section class="q-py-sm">
                            <div class="row items-center justify-between q-mb-xs">
                                <span>{{ record.roleDescription }}</span>
                                <div>
                                    <q-btn
                                        v-if="record.canEdit"
                                        flat
                                        dense
                                        round
                                        icon="edit"
                                        color="primary"
                                        size="0.75rem"
                                        aria-label="Edit effort record"
                                        @click="$emit('edit', record)"
                                    />
                                    <q-btn
                                        v-if="record.canDelete"
                                        flat
                                        dense
                                        round
                                        icon="delete"
                                        color="negative"
                                        size="0.75rem"
                                        aria-label="Delete effort record"
                                        @click="$emit('delete', record)"
                                    />
                                </div>
                            </div>
                            <div class="text-body2 q-mb-xs">
                                {{ record.effortTypeDescription }} ({{ record.effortTypeId }}) &bull;
                                <span :class="{ 'zero-effort-text': record.effortValue === 0 }">
                                    {{ record.effortValue }} {{ record.effortLabel }}
                                </span>
                            </div>
                            <div
                                v-if="record.notes"
                                class="text-caption text-grey-8 q-mt-xs notes-pre-line"
                            >
                                {{ record.notes }}
                            </div>
                        </q-card-section>
                    </q-card>
                </div>
            </div>

            <!-- Desktop Table -->
            <q-table
                :rows="sortedRecords"
                :columns="columns"
                row-key="effortId"
                dense
                flat
                bordered
                hide-pagination
                wrap-cells
                :rows-per-page-options="[0]"
                class="effort-record-table course-effort-table gt-xs"
            >
                <template #header-cell-effortType="headerProps">
                    <q-th :props="headerProps">
                        <span class="effort-type-header-content">
                            {{ headerProps.col.label }}
                            <q-btn
                                flat
                                round
                                dense
                                icon="help_outline"
                                size="xs"
                                aria-label="Effort type legend"
                            >
                                <q-tooltip
                                    class="effort-type-legend-tooltip bg-white text-dark shadow-4"
                                    anchor="bottom middle"
                                    self="top middle"
                                >
                                    <div class="text-subtitle2 q-mb-sm text-dark">Effort Type Legend</div>
                                    <div
                                        v-for="effortType in uniqueEffortTypes"
                                        :key="effortType.code"
                                        class="legend-item"
                                    >
                                        <span class="text-weight-bold">{{ effortType.code }}</span>
                                        <span> - {{ effortType.description }}</span>
                                    </div>
                                </q-tooltip>
                            </q-btn>
                        </span>
                    </q-th>
                </template>
                <template #body="slotProps">
                    <!-- Multi-record instructor: group header row -->
                    <q-tr
                        v-if="isFirstInGroup(slotProps.row.effortId) && !isSingleRecord(slotProps.row.personId)"
                        class="instructor-group-header"
                        :class="{ 'group-header-hover': hoveredPersonId === slotProps.row.personId }"
                        @mouseenter="hoveredPersonId = slotProps.row.personId"
                        @mouseleave="hoveredPersonId = null"
                        @focusin="hoveredPersonId = slotProps.row.personId"
                        @focusout="hoveredPersonId = null"
                    >
                        <q-td
                            :colspan="columns.length"
                            class="q-py-sm"
                        >
                            <router-link
                                v-if="canViewInstructors"
                                :to="{
                                    name: 'InstructorDetail',
                                    params: { termCode, personId: slotProps.row.personId },
                                }"
                                class="text-primary text-weight-bold"
                            >
                                {{ slotProps.row.instructorName }}
                            </router-link>
                            <span
                                v-else
                                class="text-weight-bold"
                            >
                                {{ slotProps.row.instructorName }}
                            </span>
                        </q-td>
                    </q-tr>
                    <!-- Detail row -->
                    <q-tr
                        :props="slotProps"
                        :class="{
                            'row-hover': hoveredRowId === slotProps.row.effortId,
                            'single-record-row': isSingleRecord(slotProps.row.personId),
                        }"
                        @mouseenter="onRowEnter(slotProps.row)"
                        @mouseleave="onRowLeave"
                        @focusin="onRowEnter(slotProps.row)"
                        @focusout="onRowLeave"
                    >
                        <q-td
                            v-for="col in slotProps.cols"
                            :key="col.name"
                            :props="slotProps"
                            :class="{ 'zero-effort': col.name === 'effort' && slotProps.row.effortValue === 0 }"
                        >
                            <!-- Instructor name cell -->
                            <template v-if="col.name === 'instructor'">
                                <template v-if="isSingleRecord(slotProps.row.personId)">
                                    <router-link
                                        v-if="canViewInstructors"
                                        :to="{
                                            name: 'InstructorDetail',
                                            params: { termCode, personId: slotProps.row.personId },
                                        }"
                                        class="text-primary text-weight-bold"
                                    >
                                        {{ slotProps.row.instructorName }}
                                    </router-link>
                                    <span
                                        v-else
                                        class="text-weight-bold"
                                    >
                                        {{ slotProps.row.instructorName }}
                                    </span>
                                </template>
                            </template>

                            <!-- Effort cell with unit label -->
                            <template v-else-if="col.name === 'effort'">
                                {{ slotProps.row.effortValue }}
                                {{ slotProps.row.effortLabel }}
                            </template>

                            <!-- Actions cell -->
                            <template v-else-if="col.name === 'actions'">
                                <q-btn
                                    v-if="slotProps.row.canEdit"
                                    flat
                                    dense
                                    round
                                    icon="edit"
                                    color="primary"
                                    size="0.75rem"
                                    aria-label="Edit effort record"
                                    @click="$emit('edit', slotProps.row)"
                                >
                                    <q-tooltip>Edit</q-tooltip>
                                </q-btn>
                                <q-btn
                                    v-if="slotProps.row.canDelete"
                                    flat
                                    dense
                                    round
                                    icon="delete"
                                    color="negative"
                                    size="0.75rem"
                                    aria-label="Delete effort record"
                                    @click="$emit('delete', slotProps.row)"
                                >
                                    <q-tooltip>Delete</q-tooltip>
                                </q-btn>
                            </template>

                            <!-- Default cell -->
                            <template v-else>
                                {{ col.value }}
                            </template>
                        </q-td>
                    </q-tr>
                </template>
            </q-table>
        </template>

        <!-- Empty state -->
        <div
            v-else
            class="text-center text-grey q-py-lg"
        >
            <q-icon
                name="school"
                size="2em"
                class="q-mb-sm"
            />
            <div>No instructor effort records for this course</div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed, ref } from "vue"
import type { QTableColumn } from "quasar"
import type { CourseEffortRecordDto } from "../types"
import { useEffortPermissions } from "../composables/use-effort-permissions"
import "../effort-record-table.css"

const props = defineProps<{
    records: CourseEffortRecordDto[]
    termCode: string | number
    isLoading?: boolean
    loadError?: string | null
}>()

defineEmits<{
    edit: [record: CourseEffortRecordDto]
    delete: [record: CourseEffortRecordDto]
}>()

const { hasViewDept, hasViewAllDepartments } = useEffortPermissions()
const hoveredRowId = ref<number | null>(null)
const hoveredPersonId = ref<number | null>(null)

const canViewInstructors = computed(() => hasViewDept.value || hasViewAllDepartments.value)

const hasAnyActions = computed(() => {
    return props.records.some((r) => r.canEdit || r.canDelete)
})

// Sort records by instructor name, then by session type for consistent grouping
const sortedRecords = computed(() => {
    return [...props.records].sort((a, b) => {
        const nameCompare = a.instructorName.localeCompare(b.instructorName)
        if (nameCompare !== 0) return nameCompare
        return a.effortTypeDescription.localeCompare(b.effortTypeDescription)
    })
})

// Unique effort types for the legend tooltip
const uniqueEffortTypes = computed(() => {
    const seen = new Map<string, string>()
    for (const record of sortedRecords.value) {
        if (!seen.has(record.effortTypeId)) {
            seen.set(record.effortTypeId, record.effortTypeDescription)
        }
    }
    return Array.from(seen.entries())
        .map(([code, description]) => ({ code, description }))
        .sort((a, b) => a.code.localeCompare(b.code))
})

// Group records by instructor for mobile view
const groupedRecords = computed(() => {
    const groups = new Map<number, { personId: number; instructorName: string; records: CourseEffortRecordDto[] }>()
    for (const record of sortedRecords.value) {
        if (!groups.has(record.personId)) {
            groups.set(record.personId, {
                personId: record.personId,
                instructorName: record.instructorName,
                records: [],
            })
        }
        groups.get(record.personId)!.records.push(record)
    }
    return [...groups.values()]
})

// Count records per instructor to decide single-row vs grouped layout
const recordCountByPerson = computed(() => {
    const counts = new Map<number, number>()
    for (const record of props.records) {
        counts.set(record.personId, (counts.get(record.personId) ?? 0) + 1)
    }
    return counts
})

// Track which effortIds are the first row for their instructor group (for rendering group headers)
const firstRowIdPerGroup = computed(() => {
    const seen = new Set<number>()
    const firstIds = new Set<number>()
    for (const record of sortedRecords.value) {
        if (!seen.has(record.personId)) {
            seen.add(record.personId)
            firstIds.add(record.effortId)
        }
    }
    return firstIds
})

function isFirstInGroup(effortId: number): boolean {
    return firstRowIdPerGroup.value.has(effortId)
}

function isSingleRecord(personId: number): boolean {
    return recordCountByPerson.value.get(personId) === 1
}

function onRowEnter(row: CourseEffortRecordDto) {
    hoveredRowId.value = row.effortId
    hoveredPersonId.value = row.personId
}

function onRowLeave() {
    hoveredRowId.value = null
    hoveredPersonId.value = null
}

const columns = computed<QTableColumn[]>(() => {
    const cols: QTableColumn[] = [
        {
            name: "instructor",
            label: "Instructor",
            field: "instructorName",
            align: "left",
        },
        {
            name: "role",
            label: "Role",
            field: "roleDescription",
            align: "left",
        },
        {
            name: "effortType",
            label: "Effort Type",
            field: "effortTypeId",
            align: "left",
            style: "width: 110px; min-width: 110px",
            headerStyle: "width: 110px; min-width: 110px",
        },
        {
            name: "effort",
            label: "Effort",
            field: "effortValue",
            align: "left",
            style: "width: 120px; min-width: 120px",
            headerStyle: "width: 120px; min-width: 120px",
        },
    ]

    if (hasAnyActions.value) {
        cols.push({
            name: "actions",
            label: "Actions",
            field: "effortId",
            align: "center",
            style: "width: 110px; min-width: 110px",
            headerStyle: "width: 110px; min-width: 110px",
        })
    }

    return cols
})
</script>

<style scoped>
.course-effort-table {
    width: 100%;
}

/* Instructor group header row */
.course-effort-table :deep(.instructor-group-header td) {
    background-color: #f5f5f5;
}

/* Highlight group header when hovering any row in the group */
.course-effort-table :deep(.group-header-hover td) {
    background-color: #eee;
}

.notes-pre-line {
    white-space: pre-line;
}
</style>
