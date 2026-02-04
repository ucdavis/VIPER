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
        <template #body-cell-course="slotProps">
            <q-td :props="slotProps">
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
            </q-td>
        </template>
        <template #body-cell-effort="slotProps">
            <q-td
                :props="slotProps"
                :class="{ 'zero-effort': isZeroEffort(slotProps.row) }"
            >
                {{ slotProps.row.effortValue ?? 0 }}
                {{ slotProps.row.effortLabel === "weeks" ? "Weeks" : "Hours" }}
            </q-td>
        </template>
        <template #body-cell-actions="slotProps">
            <q-td :props="slotProps">
                <q-btn
                    v-if="canEdit"
                    flat
                    dense
                    round
                    icon="edit"
                    color="primary"
                    size="sm"
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
                    size="sm"
                    aria-label="Delete effort record"
                    @click="$emit('delete', slotProps.row)"
                >
                    <q-tooltip>Delete</q-tooltip>
                </q-btn>
            </q-td>
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
import type { QTableColumn } from "quasar"
import type { InstructorEffortRecordDto } from "../types"

type CourseInfo = {
    subjCode: string
    crseNumb: string
    seqNumb: string
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
        tableClass: "effort-table gt-xs",
        zeroEffortRecordIds: () => [],
    },
)

defineEmits<{
    edit: [record: InstructorEffortRecordDto]
    delete: [record: InstructorEffortRecordDto]
}>()

function formatCourseCode(course: CourseInfo): string {
    return `${course.subjCode} ${course.crseNumb.trim()}-${course.seqNumb}`
}

function isZeroEffort(record: InstructorEffortRecordDto): boolean {
    if (props.zeroEffortRecordIds.length > 0) {
        return props.zeroEffortRecordIds.includes(record.id)
    }
    return record.effortValue === 0
}
</script>

<style scoped>
.effort-table {
    width: 100%;
}

.effort-table :deep(.zero-effort) {
    background-color: #fff3cd;
    color: #856404;
}
</style>
