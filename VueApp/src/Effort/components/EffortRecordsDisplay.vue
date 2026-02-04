<template>
    <div>
        <!-- Mobile Card View for Regular Records -->
        <div
            v-if="regularRecords.length > 0"
            class="lt-sm q-mb-lg"
        >
            <q-card
                v-for="record in regularRecords"
                :key="record.id"
                flat
                bordered
                class="q-mb-sm"
                :class="{ 'zero-effort-card': isZeroEffort(record) }"
            >
                <q-card-section class="q-py-sm">
                    <div class="row items-center justify-between q-mb-xs">
                        <router-link
                            v-if="showCourseLinks"
                            :to="{
                                name: 'CourseDetail',
                                params: { termCode, courseId: record.course.id },
                            }"
                            class="text-primary text-weight-bold"
                        >
                            {{ formatCourseCode(record.course) }}
                        </router-link>
                        <span
                            v-else
                            class="text-primary text-weight-bold"
                        >
                            {{ formatCourseCode(record.course) }}
                        </span>
                        <div>
                            <q-btn
                                v-if="canEdit"
                                flat
                                dense
                                round
                                icon="edit"
                                color="primary"
                                size="sm"
                                aria-label="Edit effort record"
                                @click="$emit('edit', record)"
                            />
                            <q-btn
                                v-if="canDelete"
                                flat
                                dense
                                round
                                icon="delete"
                                color="negative"
                                size="sm"
                                aria-label="Delete effort record"
                                @click="$emit('delete', record)"
                            />
                        </div>
                    </div>
                    <div class="text-body2 q-mb-xs">{{ record.roleDescription }} &bull; {{ record.effortType }}</div>
                    <div class="row q-gutter-md text-caption text-grey-7">
                        <span>{{ record.course.units }} units</span>
                        <span>Enroll: {{ record.course.enrollment }}</span>
                        <span :class="{ 'text-warning text-weight-bold': isZeroEffort(record) }">
                            {{ record.effortValue ?? 0 }}
                            {{ record.effortLabel === "weeks" ? "Weeks" : "Hours" }}
                        </span>
                    </div>
                </q-card-section>
            </q-card>
        </div>

        <!-- Desktop Table for Regular Records -->
        <EffortRecordsTable
            v-if="regularRecords.length > 0"
            :records="regularRecords"
            :columns="regularColumns"
            :term-code="termCode"
            :can-edit="canEdit"
            :can-delete="canDelete"
            :show-course-links="showCourseLinks"
            :zero-effort-record-ids="zeroEffortRecordIds"
            :no-data-message="noDataMessage"
            table-class="effort-table q-mb-lg gt-xs"
            @edit="(record) => $emit('edit', record)"
            @delete="(record) => $emit('delete', record)"
        />

        <!-- R-Course Section (Resident Teaching) -->
        <div
            v-if="rCourseRecords.length > 0"
            class="q-mb-lg"
        >
            <q-separator class="q-my-md" />
            <h4 class="q-mt-none q-mb-sm">Resident Teaching (R-Course)</h4>

            <!-- Generic R-course with zero effort info -->
            <q-banner
                v-if="hasGenericRCourseWithZeroEffort"
                class="bg-info text-white q-mb-md"
                rounded
            >
                <template #avatar>
                    <q-icon name="info" />
                </template>
                The Resident (R) course was added automatically to allow recording of resident teaching effort. If left
                with 0 effort and verified, it will be automatically removed.
            </q-banner>

            <!-- R-Course Mobile Card View -->
            <div class="lt-sm">
                <q-card
                    v-for="record in rCourseRecords"
                    :key="record.id"
                    flat
                    bordered
                    class="q-mb-sm"
                    :class="{ 'zero-effort-card': isZeroEffort(record) }"
                >
                    <q-card-section class="q-py-sm">
                        <div class="row items-center justify-between q-mb-xs">
                            <router-link
                                v-if="showCourseLinks"
                                :to="{
                                    name: 'CourseDetail',
                                    params: { termCode, courseId: record.course.id },
                                }"
                                class="text-primary text-weight-bold"
                            >
                                {{ formatCourseCode(record.course) }}
                            </router-link>
                            <span
                                v-else
                                class="text-primary text-weight-bold"
                            >
                                {{ formatCourseCode(record.course) }}
                            </span>
                            <div>
                                <q-btn
                                    v-if="canEdit"
                                    flat
                                    dense
                                    round
                                    icon="edit"
                                    color="primary"
                                    size="sm"
                                    aria-label="Edit effort record"
                                    @click="$emit('edit', record)"
                                />
                                <q-btn
                                    v-if="canDelete"
                                    flat
                                    dense
                                    round
                                    icon="delete"
                                    color="negative"
                                    size="sm"
                                    aria-label="Delete effort record"
                                    @click="$emit('delete', record)"
                                />
                            </div>
                        </div>
                        <div class="text-body2 q-mb-xs">
                            {{ record.roleDescription }} &bull; {{ record.effortType }}
                        </div>
                        <div class="row q-gutter-md text-caption text-grey-7">
                            <span :class="{ 'text-warning text-weight-bold': isZeroEffort(record) }">
                                {{ record.effortValue ?? 0 }}
                                {{ record.effortLabel === "weeks" ? "Weeks" : "Hours" }}
                            </span>
                        </div>
                    </q-card-section>
                </q-card>
            </div>

            <!-- R-Course Desktop Table -->
            <EffortRecordsTable
                :records="rCourseRecords"
                :columns="rCourseColumns"
                :term-code="termCode"
                :can-edit="canEdit"
                :can-delete="canDelete"
                :show-course-links="showCourseLinks"
                :zero-effort-record-ids="zeroEffortRecordIds"
                table-class="effort-table gt-xs"
                @edit="(record) => $emit('edit', record)"
                @delete="(record) => $emit('delete', record)"
            />
        </div>

        <!-- No records message (when both regular and R-Course are empty) -->
        <div
            v-if="records.length === 0"
            class="text-center text-grey q-py-lg q-mb-md"
        >
            <q-icon
                name="school"
                size="2em"
                class="q-mb-sm"
            />
            <div>{{ noDataMessage }}</div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue"
import type { QTableColumn } from "quasar"
import type { InstructorEffortRecordDto } from "../types"
import EffortRecordsTable from "./EffortRecordsTable.vue"

type CourseInfo = {
    subjCode: string
    crseNumb: string
    seqNumb: string
}

const props = withDefaults(
    defineProps<{
        records: InstructorEffortRecordDto[]
        termCode: string | number
        canEdit?: boolean
        canDelete?: boolean
        showCourseLinks?: boolean
        noDataMessage?: string
        zeroEffortRecordIds?: number[]
    }>(),
    {
        canEdit: false,
        canDelete: false,
        showCourseLinks: false,
        noDataMessage: "No effort records",
        zeroEffortRecordIds: () => [],
    },
)

defineEmits<{
    edit: [record: InstructorEffortRecordDto]
    delete: [record: InstructorEffortRecordDto]
}>()

const regularRecords = computed(() => {
    return props.records.filter((r) => !r.course.isRCourse)
})

const rCourseRecords = computed(() => {
    return props.records.filter((r) => r.course.isRCourse)
})

const hasGenericRCourseWithZeroEffort = computed(() => {
    return rCourseRecords.value.some(
        (r) => r.course.crn === "RESID" && (r.hours === 0 || r.hours === null) && (r.weeks === 0 || r.weeks === null),
    )
})

function formatCourseCode(course: CourseInfo): string {
    return `${course.subjCode} ${course.crseNumb.trim()}-${course.seqNumb}`
}

function isZeroEffort(record: InstructorEffortRecordDto): boolean {
    if (props.zeroEffortRecordIds.length > 0) {
        return props.zeroEffortRecordIds.includes(record.id)
    }
    return record.effortValue === 0
}

const regularColumns = computed<QTableColumn[]>(() => {
    const cols: QTableColumn[] = [
        {
            name: "course",
            label: "Course",
            field: (row: InstructorEffortRecordDto) => formatCourseCode(row.course),
            align: "left",
            sortable: true,
        },
        {
            name: "units",
            label: "Units",
            field: (row: InstructorEffortRecordDto) => row.course.units,
            align: "left",
        },
        {
            name: "enrollment",
            label: "Enroll",
            field: (row: InstructorEffortRecordDto) => row.course.enrollment,
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
            field: "effortType",
            align: "left",
        },
        {
            name: "effort",
            label: "Effort",
            field: "effortValue",
            align: "left",
        },
    ]

    if (props.canEdit || props.canDelete) {
        cols.push({
            name: "actions",
            label: "Actions",
            field: "id",
            align: "center",
        })
    }

    return cols
})

const rCourseColumns = computed<QTableColumn[]>(() => {
    const cols: QTableColumn[] = [
        {
            name: "course",
            label: "Course",
            field: (row: InstructorEffortRecordDto) => formatCourseCode(row.course),
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
            field: "effortType",
            align: "left",
        },
        {
            name: "effort",
            label: "Effort",
            field: "effortValue",
            align: "left",
        },
    ]

    if (props.canEdit || props.canDelete) {
        cols.push({
            name: "actions",
            label: "Actions",
            field: "id",
            align: "center",
        })
    }

    return cols
})
</script>

<style scoped>
.zero-effort-card {
    background-color: #fff3cd;
}
</style>
