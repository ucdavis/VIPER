<template>
    <div>
        <!-- Loading state -->
        <div
            v-if="isLoading"
            class="text-grey q-my-md"
        >
            Loading evaluation data...
        </div>

        <!-- Error state -->
        <q-banner
            v-else-if="loadError"
            class="bg-negative text-white q-mb-md"
            rounded
        >
            <template #avatar>
                <q-icon
                    name="error"
                    color="white"
                />
            </template>
            {{ loadError }}
        </q-banner>

        <!-- Matrix content -->
        <template v-else-if="evaluationData.instructors.length > 0">
            <!-- Single-course compact view -->
            <q-table
                v-if="isSingleCourse"
                :rows="sortedInstructors"
                :columns="singleCourseColumns"
                row-key="personId"
                dense
                flat
                bordered
                hide-pagination
                :rows-per-page-options="[0]"
                class="eval-status-table"
            >
                <template #body-cell-instructor="slotProps">
                    <q-td :props="slotProps">
                        <router-link
                            v-if="canViewInstructors"
                            :to="{
                                name: 'InstructorDetail',
                                params: { termCode: props.termCode, personId: slotProps.row.personId },
                            }"
                            class="text-primary"
                        >
                            {{ slotProps.row.instructorName }}
                        </router-link>
                        <span v-else>{{ slotProps.row.instructorName }}</span>
                    </q-td>
                </template>
                <template #body-cell-status="slotProps">
                    <q-td :props="slotProps">
                        <EvalStatusCell
                            :entry="getEvalEntry(slotProps.row, 0)"
                            :can-edit-ad-hoc="canEditAdHoc"
                            :mothra-id="slotProps.row.mothraId"
                            :instructor-name="slotProps.row.instructorName"
                            :course-name="evaluationData.courses[0]?.courseName ?? ''"
                            @add="onAdd"
                            @edit="onEdit"
                            @delete="onDelete"
                        />
                    </q-td>
                </template>
                <template #body-cell-source="slotProps">
                    <q-td :props="slotProps">
                        <span class="text-caption text-grey-7">
                            {{ getSourceLabel(getEvalEntry(slotProps.row, 0)) }}
                        </span>
                    </q-td>
                </template>
            </q-table>

            <!-- Multi-course matrix view -->
            <q-table
                v-else
                :rows="sortedInstructors"
                :columns="matrixColumns"
                row-key="personId"
                dense
                flat
                bordered
                hide-pagination
                :rows-per-page-options="[0]"
                class="eval-status-table"
            >
                <template #body-cell-instructor="slotProps">
                    <q-td :props="slotProps">
                        <router-link
                            v-if="canViewInstructors"
                            :to="{
                                name: 'InstructorDetail',
                                params: { termCode: props.termCode, personId: slotProps.row.personId },
                            }"
                            class="text-primary"
                        >
                            {{ slotProps.row.instructorName }}
                        </router-link>
                        <span v-else>{{ slotProps.row.instructorName }}</span>
                    </q-td>
                </template>
                <template
                    v-for="(course, idx) in evaluationData.courses"
                    :key="course.courseId"
                    #[cellSlotName(idx)]="slotProps"
                >
                    <q-td :props="slotProps">
                        <EvalStatusCell
                            :entry="getEvalEntry(slotProps.row, idx)"
                            :can-edit-ad-hoc="canEditAdHoc"
                            :mothra-id="slotProps.row.mothraId"
                            :instructor-name="slotProps.row.instructorName"
                            :course-name="course.courseName"
                            show-source
                            @add="onAdd"
                            @edit="onEdit"
                            @delete="onDelete"
                        />
                    </q-td>
                </template>
            </q-table>
        </template>

        <!-- Empty state -->
        <div
            v-else
            class="text-center text-grey q-py-lg"
        >
            <q-icon
                name="assessment"
                size="2em"
                class="q-mb-sm"
            />
            <div>No evaluation data available for this course</div>
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue"
import type { QTableColumn } from "quasar"
import type { CourseEvaluationStatusDto, InstructorEvalStatusDto, CourseEvalEntryDto } from "../types"
import { useEffortPermissions } from "../composables/use-effort-permissions"
import EvalStatusCell from "./EvalStatusCell.vue"

const props = defineProps<{
    evaluationData: CourseEvaluationStatusDto
    canEditAdHoc: boolean
    isLoading: boolean
    loadError: string | null
    termCode: string | number
}>()

const emit = defineEmits<{
    add: [entry: { courseId: number; crn: string; mothraId: string; instructorName: string; courseName: string }]
    edit: [
        entry: {
            courseId: number
            crn: string
            mothraId: string
            instructorName: string
            courseName: string
            data: CourseEvalEntryDto
        },
    ]
    delete: [entry: { courseId: number; quantId: number; instructorName: string; courseName: string }]
}>()

const { hasViewDept, hasViewAllDepartments } = useEffortPermissions()
const canViewInstructors = computed(() => hasViewDept.value || hasViewAllDepartments.value)

const isSingleCourse = computed(() => props.evaluationData.courses.length <= 1)

// Sort instructors alphabetically by name
const sortedInstructors = computed(() =>
    [...props.evaluationData.instructors].sort((a, b) => a.instructorName.localeCompare(b.instructorName)),
)

function getEvalEntry(instructor: InstructorEvalStatusDto, courseIndex: number): CourseEvalEntryDto | null {
    const course = props.evaluationData.courses[courseIndex]
    if (!course) return null
    return instructor.evaluations.find((e) => e.courseId === course.courseId) ?? null
}

function cellSlotName(courseIndex: number): string {
    return `body-cell-course_${courseIndex}`
}

function getSourceLabel(entry: CourseEvalEntryDto | null): string {
    if (!entry || entry.status === "None") return "\u2014"
    if (entry.status === "CERE") return "CERE"
    if (entry.status === "AdHoc") return "Ad-Hoc"
    return ""
}

// Single-course columns
const singleCourseColumns = computed<QTableColumn[]>(() => [
    {
        name: "instructor",
        label: "Instructor",
        field: "instructorName",
        align: "left",
        sortable: true,
    },
    {
        name: "status",
        label: "Evaluation",
        field: "personId",
        align: "center",
    },
    {
        name: "source",
        label: "Source",
        field: "personId",
        align: "center",
    },
])

// Multi-course matrix columns
const matrixColumns = computed<QTableColumn[]>(() => {
    const cols: QTableColumn[] = [
        {
            name: "instructor",
            label: "Instructor",
            field: "instructorName",
            align: "left",
            sortable: true,
        },
    ]

    props.evaluationData.courses.forEach((course, idx) => {
        cols.push({
            name: `course_${idx}`,
            label: `${course.courseName} (${course.crn})`,
            field: "personId",
            align: "center",
        })
    })

    return cols
})

function onAdd(entry: { courseId: number; crn: string; mothraId: string; instructorName: string; courseName: string }) {
    emit("add", entry)
}

function onEdit(entry: {
    courseId: number
    crn: string
    mothraId: string
    instructorName: string
    courseName: string
    data: CourseEvalEntryDto
}) {
    emit("edit", entry)
}

function onDelete(entry: { courseId: number; quantId: number; instructorName: string; courseName: string }) {
    emit("delete", entry)
}
</script>

<style scoped>
.eval-status-table {
    width: 100%;
}
</style>
