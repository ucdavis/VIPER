<template>
    <q-table
        :rows="records"
        :columns="columns"
        row-key="id"
        dense
        flat
        bordered
        hide-pagination
        wrap-cells
        :rows-per-page-options="[0]"
        :class="tableClass"
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
            <q-tr
                :props="slotProps"
                :class="{
                    'has-notes': slotProps.row.notes,
                    'row-hover': hoveredRowId === slotProps.row.id,
                }"
                @mouseenter="hoveredRowId = slotProps.row.id"
                @mouseleave="hoveredRowId = null"
                @focusin="hoveredRowId = slotProps.row.id"
                @focusout="hoveredRowId = null"
            >
                <q-td
                    v-for="col in slotProps.cols"
                    :key="col.name"
                    :props="slotProps"
                    :class="{ 'zero-effort': col.name === 'effort' && isZeroEffort(slotProps.row) }"
                >
                    <!-- Course cell with optional link -->
                    <template v-if="col.name === 'course'">
                        <router-link
                            v-if="showCourseLinks"
                            :to="{
                                name: 'CourseDetail',
                                params: { termCode, courseId: slotProps.row.course.id },
                            }"
                            class="text-primary"
                        >
                            {{ formatCourseCode(slotProps.row.course) }}
                        </router-link>
                        <span v-else>
                            {{ formatCourseCode(slotProps.row.course) }}
                        </span>
                    </template>

                    <!-- Effort cell with units label -->
                    <template v-else-if="col.name === 'effort'">
                        {{ slotProps.row.effortValue ?? 0 }}
                        {{ slotProps.row.effortLabel === "weeks" ? "Weeks" : "Hours" }}
                    </template>

                    <!-- Actions cell -->
                    <template v-else-if="col.name === 'actions'">
                        <q-btn
                            v-if="canEdit"
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
                            v-if="canDelete"
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

            <!-- Notes sub-row (only when notes exist) -->
            <q-tr
                v-if="slotProps.row.notes"
                class="notes-subrow"
                :class="{ 'row-hover': hoveredRowId === slotProps.row.id }"
                @mouseenter="hoveredRowId = slotProps.row.id"
                @mouseleave="hoveredRowId = null"
                @focusin="hoveredRowId = slotProps.row.id"
                @focusout="hoveredRowId = null"
            >
                <q-td
                    :colspan="slotProps.cols.length"
                    class="text-caption text-grey-8 q-pt-none q-pb-sm"
                    style="white-space: pre-line"
                >
                    {{ slotProps.row.notes }}
                </q-td>
            </q-tr>
        </template>
        <template #no-data>
            <div class="full-width row flex-center text-grey q-gutter-sm q-py-lg">
                <q-icon
                    name="school"
                    size="2em"
                />
                <span>{{ noDataMessage }}</span>
            </div>
        </template>
    </q-table>
</template>

<script setup lang="ts">
import { computed, ref } from "vue"
import type { QTableColumn } from "quasar"
import type { InstructorEffortRecordDto } from "../types"
import "../effort-record-table.css"

type CourseInfo = {
    subjCode: string
    crseNumb: string
    seqNumb: string
}

type EffortTypeLegendItem = {
    code: string
    description: string
}

const props = withDefaults(
    defineProps<{
        records: InstructorEffortRecordDto[]
        columns: QTableColumn[]
        termCode: string | number
        canEdit?: boolean
        canDelete?: boolean
        showCourseLinks?: boolean
        noDataMessage?: string
        tableClass?: string
        zeroEffortRecordIds?: number[]
    }>(),
    {
        canEdit: false,
        canDelete: false,
        showCourseLinks: false,
        noDataMessage: "No effort records",
        tableClass: "effort-record-table effort-table gt-xs",
        zeroEffortRecordIds: () => [],
    },
)

defineEmits<{
    edit: [record: InstructorEffortRecordDto]
    delete: [record: InstructorEffortRecordDto]
}>()

const hoveredRowId = ref<number | null>(null)

function formatCourseCode(course: CourseInfo): string {
    return `${course.subjCode} ${course.crseNumb.trim()}-${course.seqNumb}`
}

function isZeroEffort(record: InstructorEffortRecordDto): boolean {
    if (props.zeroEffortRecordIds.length > 0) {
        return props.zeroEffortRecordIds.includes(record.id)
    }
    return record.effortValue === 0
}

const uniqueEffortTypes = computed<EffortTypeLegendItem[]>(() => {
    const seen = new Map<string, string>()
    for (const record of props.records) {
        if (!seen.has(record.effortType)) {
            seen.set(record.effortType, record.effortTypeDescription)
        }
    }
    return Array.from(seen.entries())
        .map(([code, description]) => ({ code, description }))
        .sort((a, b) => a.code.localeCompare(b.code))
})
</script>

<style scoped>
.effort-table {
    width: 100%;
}

.effort-table :deep(.has-notes td) {
    border-bottom: none;
}
</style>
