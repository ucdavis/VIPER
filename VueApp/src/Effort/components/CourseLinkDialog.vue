<template>
    <q-dialog
        :model-value="modelValue"
        persistent
        maximized-on-mobile
        @keydown.escape="handleClose"
    >
        <q-card style="min-width: 500px; max-width: 700px">
            <q-card-section class="row items-center q-pb-none">
                <div class="text-h6">Link Courses</div>
                <q-space />
                <q-btn
                    icon="close"
                    flat
                    round
                    dense
                    aria-label="Close dialog"
                    @click="handleClose"
                />
            </q-card-section>

            <q-card-section v-if="course">
                <div class="text-subtitle1 q-mb-md">
                    Parent Course: <strong>{{ course.courseCode }}-{{ course.seqNumb }}</strong>
                    <span class="text-grey-7">(CRN: {{ course.crn }})</span>
                </div>

                <!-- Existing child relationships -->
                <div class="q-mb-md">
                    <div class="text-subtitle2 q-mb-sm">Linked Child Courses</div>

                    <q-table
                        v-if="childRelationships.length > 0"
                        :rows="childRelationships"
                        :columns="columns"
                        row-key="id"
                        dense
                        flat
                        bordered
                        hide-pagination
                        :rows-per-page-options="[0]"
                    >
                        <template #body-cell-relationshipType="slotProps">
                            <q-td :props="slotProps">
                                <q-badge
                                    :color="slotProps.row.relationshipType === 'CrossList' ? 'positive' : 'info'"
                                    :label="slotProps.row.relationshipType === 'CrossList' ? 'Cross List' : 'Section'"
                                />
                            </q-td>
                        </template>
                        <template #body-cell-actions="slotProps">
                            <q-td :props="slotProps">
                                <q-btn
                                    icon="delete"
                                    color="negative"
                                    dense
                                    flat
                                    round
                                    size="sm"
                                    :loading="deletingId === slotProps.row.id"
                                    aria-label="Delete relationship"
                                    @click="confirmDelete(slotProps.row)"
                                >
                                    <q-tooltip>Remove link</q-tooltip>
                                </q-btn>
                            </q-td>
                        </template>
                    </q-table>

                    <div
                        v-else
                        class="text-grey-6 q-pa-sm"
                    >
                        No linked courses yet.
                    </div>
                </div>

                <!-- Add new relationship -->
                <q-separator class="q-my-md" />

                <div class="text-subtitle2 q-mb-sm">Add Child Course</div>

                <div class="row q-col-gutter-sm items-center no-wrap">
                    <div class="col">
                        <q-select
                            v-model="selectedChildCourse"
                            :options="filteredCourses"
                            :option-label="(c: CourseDto) => `${c.courseCode}-${c.seqNumb} (${c.units} units)`"
                            :option-value="(c: CourseDto) => c.id"
                            label="Select Course"
                            dense
                            options-dense
                            outlined
                            :loading="isLoadingAvailable"
                            clearable
                            use-input
                            :input-debounce="INPUT_DEBOUNCE_MS"
                            @filter="filterCourses"
                        >
                            <template #option="scope">
                                <q-item v-bind="scope.itemProps">
                                    <q-item-section>
                                        <q-item-label>{{ scope.opt.courseCode }}-{{ scope.opt.seqNumb }}</q-item-label>
                                        <q-item-label caption>
                                            CRN: {{ scope.opt.crn }} &bull; {{ scope.opt.enrollment }} enrolled &bull;
                                            {{ scope.opt.units }} units
                                        </q-item-label>
                                    </q-item-section>
                                </q-item>
                            </template>
                        </q-select>
                    </div>
                    <div class="col-auto">
                        <q-option-group
                            v-model="relationshipType"
                            :options="relationshipTypeOptions"
                            type="radio"
                            inline
                            dense
                            class="no-wrap"
                        />
                    </div>
                    <div class="col-auto">
                        <q-btn
                            label="Add"
                            color="primary"
                            dense
                            :disable="!selectedChildCourse || !relationshipType"
                            :loading="isAdding"
                            @click="addRelationship"
                        />
                    </div>
                </div>

                <div class="text-caption text-grey-7 q-mt-sm">
                    <div><strong>Cross List:</strong> Same course under different subject codes.</div>
                    <div><strong>Section:</strong> Multiple sections of the same course.</div>
                </div>
            </q-card-section>

            <q-card-actions align="right">
                <q-btn
                    label="Close"
                    flat
                    @click="handleClose"
                />
            </q-card-actions>
        </q-card>
    </q-dialog>
</template>

<script setup lang="ts">
import { ref, watch, computed } from "vue"
import { useQuasar } from "quasar"
import type { QTableColumn } from "quasar"
import { effortService } from "../services/effort-service"
import type { CourseDto, CourseRelationshipDto } from "../types"

const props = defineProps<{
    modelValue: boolean
    course: CourseDto | null
}>()

const emit = defineEmits<{
    "update:modelValue": [value: boolean]
    updated: []
}>()

const $q = useQuasar()

const INPUT_DEBOUNCE_MS = 300

// Close handler for X button, Close button, or Escape key
function handleClose() {
    emit("update:modelValue", false)
}

// State
const childRelationships = ref<CourseRelationshipDto[]>([])
const availableCourses = ref<CourseDto[]>([])
const filteredCourses = ref<CourseDto[]>([])
const selectedChildCourse = ref<CourseDto | null>(null)
const relationshipType = ref<"CrossList" | "Section">("CrossList")
const isLoadingAvailable = ref(false)
const isAdding = ref(false)
const deletingId = ref<number | null>(null)

const relationshipTypeOptions = [
    { label: "Cross List", value: "CrossList" },
    { label: "Section", value: "Section" },
]

const columns = computed<QTableColumn[]>(() => [
    {
        name: "course",
        label: "Course",
        field: (row: CourseRelationshipDto) =>
            row.childCourse ? `${row.childCourse.courseCode}-${row.childCourse.seqNumb}` : "",
        align: "left",
        sortable: true,
    },
    {
        name: "crn",
        label: "CRN",
        field: (row: CourseRelationshipDto) => row.childCourse?.crn ?? "",
        align: "left",
    },
    {
        name: "enrollment",
        label: "Enrollment",
        field: (row: CourseRelationshipDto) => row.childCourse?.enrollment ?? 0,
        align: "left",
    },
    {
        name: "units",
        label: "Units",
        field: (row: CourseRelationshipDto) => row.childCourse?.units ?? 0,
        align: "left",
    },
    {
        name: "relationshipType",
        label: "Type",
        field: "relationshipType",
        align: "center",
    },
    {
        name: "actions",
        label: "",
        field: "actions",
        align: "center",
    },
])

// Load data when dialog opens
watch(
    () => props.modelValue,
    async (open) => {
        if (open && props.course) {
            await loadData()
        } else {
            // Reset state when dialog closes
            childRelationships.value = []
            availableCourses.value = []
            selectedChildCourse.value = null
            relationshipType.value = "CrossList"
        }
    },
)

async function loadData() {
    if (!props.course) return

    isLoadingAvailable.value = true

    try {
        const [relationshipsResult, available] = await Promise.all([
            effortService.getCourseRelationships(props.course.id),
            effortService.getAvailableChildCourses(props.course.id),
        ])

        childRelationships.value = relationshipsResult.childRelationships
        availableCourses.value = available
        filteredCourses.value = available
    } finally {
        isLoadingAvailable.value = false
    }
}

function filterCourses(val: string, update: (fn: () => void) => void) {
    update(() => {
        if (!val) {
            filteredCourses.value = availableCourses.value
        } else {
            const needle = val.toLowerCase()
            filteredCourses.value = availableCourses.value.filter(
                (c) =>
                    c.courseCode.toLowerCase().includes(needle) ||
                    c.seqNumb.toLowerCase().includes(needle) ||
                    c.crn.toLowerCase().includes(needle),
            )
        }
    })
}

async function addRelationship() {
    if (!props.course || !selectedChildCourse.value) return

    isAdding.value = true

    try {
        const result = await effortService.createCourseRelationship(props.course.id, {
            childCourseId: selectedChildCourse.value.id,
            relationshipType: relationshipType.value,
        })

        if (result.success) {
            $q.notify({ type: "positive", message: "Course linked successfully" })
            selectedChildCourse.value = null
            await loadData()
            emit("updated")
        } else {
            $q.notify({ type: "negative", message: result.error ?? "Failed to link course" })
        }
    } finally {
        isAdding.value = false
    }
}

function confirmDelete(relationship: CourseRelationshipDto) {
    const childCourse = relationship.childCourse
    const courseName = childCourse ? `${childCourse.courseCode}-${childCourse.seqNumb}` : "this course"

    $q.dialog({
        title: "Remove Link",
        message: `Are you sure you want to remove the link to ${courseName}?`,
        cancel: true,
        persistent: true,
    }).onOk(() => deleteRelationship(relationship))
}

async function deleteRelationship(relationship: CourseRelationshipDto) {
    if (!props.course) return

    deletingId.value = relationship.id

    try {
        const success = await effortService.deleteCourseRelationship(props.course.id, relationship.id)

        if (success) {
            $q.notify({ type: "positive", message: "Link removed successfully" })
            await loadData()
            emit("updated")
        } else {
            $q.notify({ type: "negative", message: "Failed to remove link" })
        }
    } finally {
        deletingId.value = null
    }
}
</script>
