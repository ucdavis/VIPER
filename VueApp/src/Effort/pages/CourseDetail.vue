<template>
    <div class="q-pa-md">
        <!-- Breadcrumb -->
        <q-breadcrumbs class="q-mb-md">
            <q-breadcrumbs-el
                label="Courses"
                :to="{ name: 'CourseList', params: { termCode } }"
            />
            <q-breadcrumbs-el :label="course?.courseCode ?? 'Course'" />
        </q-breadcrumbs>

        <!-- Loading state -->
        <div
            v-if="isLoading"
            class="text-grey q-my-md"
        >
            Loading course...
        </div>

        <!-- Error state -->
        <q-banner
            v-else-if="loadError"
            class="bg-negative text-white q-mb-md"
        >
            {{ loadError }}
            <template #action>
                <q-btn
                    flat
                    label="Go Back"
                    :to="{ name: 'CourseList', params: { termCode } }"
                />
            </template>
        </q-banner>

        <!-- Course content -->
        <template v-else-if="course">
            <!-- Course Header -->
            <div class="row items-center q-mb-md">
                <h2 class="q-my-none">
                    {{ course.courseCode }}-{{ course.seqNumb }}
                    <span class="text-grey-7 text-subtitle1">(CRN: {{ course.crn }})</span>
                </h2>
                <q-space />
                <div class="q-gutter-sm">
                    <q-btn
                        v-if="canEditCourse"
                        icon="edit"
                        color="primary"
                        dense
                        flat
                        round
                        @click="showEditDialog = true"
                    >
                        <q-tooltip>Edit course</q-tooltip>
                    </q-btn>
                    <q-btn
                        v-if="hasLinkCourses"
                        icon="link"
                        color="secondary"
                        dense
                        flat
                        round
                        @click="showLinkDialog = true"
                    >
                        <q-tooltip>Link courses</q-tooltip>
                    </q-btn>
                </div>
            </div>

            <!-- Course Info -->
            <div class="row q-col-gutter-md q-mb-lg">
                <div class="col-auto">
                    <div class="text-caption text-grey-7">Enrollment</div>
                    <div class="text-body1">{{ course.enrollment }}</div>
                </div>
                <div class="col-auto">
                    <div class="text-caption text-grey-7">Units</div>
                    <div class="text-body1">{{ course.units }}</div>
                </div>
                <div class="col-auto">
                    <div class="text-caption text-grey-7">Custodial Department</div>
                    <div class="text-body1">{{ course.custDept }}</div>
                </div>
            </div>

            <!-- Parent Relationship Banner -->
            <q-banner
                v-if="parentRelationship"
                class="bg-blue-1 q-mb-md"
                rounded
            >
                <template #avatar>
                    <q-icon
                        name="subdirectory_arrow_right"
                        color="primary"
                    />
                </template>
                This course is a
                <q-badge
                    :color="parentRelationship.relationshipType === 'CrossList' ? 'positive' : 'info'"
                    :label="parentRelationship.relationshipType === 'CrossList' ? 'Cross List' : 'Section'"
                    class="q-mx-xs"
                />
                child of
                <router-link
                    v-if="parentRelationship.parentCourse"
                    :to="{
                        name: 'CourseDetail',
                        params: { termCode, courseId: parentRelationship.parentCourseId },
                    }"
                    class="text-primary text-weight-medium"
                >
                    {{ parentRelationship.parentCourse.courseCode }}-{{ parentRelationship.parentCourse.seqNumb }}
                </router-link>
                <span v-else>Course {{ parentRelationship.parentCourseId }}</span>
            </q-banner>

            <!-- Child Relationships Section -->
            <div
                v-if="childRelationships.length > 0"
                class="q-mb-lg"
            >
                <h3 class="q-mt-none q-mb-sm">Child Courses</h3>
                <q-table
                    :rows="childRelationships"
                    :columns="childColumns"
                    row-key="id"
                    dense
                    flat
                    bordered
                    hide-pagination
                    :rows-per-page-options="[0]"
                >
                    <template #body-cell-course="slotProps">
                        <q-td :props="slotProps">
                            <router-link
                                v-if="slotProps.row.childCourse"
                                :to="{
                                    name: 'CourseDetail',
                                    params: { termCode, courseId: slotProps.row.childCourseId },
                                }"
                                class="text-primary"
                            >
                                {{ slotProps.row.childCourse.courseCode }}-{{ slotProps.row.childCourse.seqNumb }}
                            </router-link>
                            <span v-else>Course {{ slotProps.row.childCourseId }}</span>
                        </q-td>
                    </template>
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
                                v-if="hasLinkCourses"
                                icon="delete"
                                color="negative"
                                dense
                                flat
                                round
                                size="sm"
                                :loading="deletingId === slotProps.row.id"
                                aria-label="Delete relationship"
                                @click="confirmDeleteRelationship(slotProps.row)"
                            >
                                <q-tooltip>Remove link</q-tooltip>
                            </q-btn>
                        </q-td>
                    </template>
                </q-table>
            </div>

            <!-- No relationships message when course is neither parent nor child -->
            <div
                v-if="!parentRelationship && childRelationships.length === 0"
                class="text-grey-6 q-mb-lg"
            >
                This course has no linked courses.
                <span v-if="hasLinkCourses">
                    Click the
                    <q-icon name="link" />
                    button to link child courses.
                </span>
            </div>
        </template>

        <!-- Edit Dialog -->
        <CourseEditDialog
            v-model="showEditDialog"
            :course="course"
            :departments="departments"
            :enrollment-only="editEnrollmentOnly"
            @updated="onCourseUpdated"
        />

        <!-- Link Dialog -->
        <CourseLinkDialog
            v-model="showLinkDialog"
            :course="course"
            @updated="onRelationshipsUpdated"
        />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRoute } from "vue-router"
import { useQuasar } from "quasar"
import type { QTableColumn } from "quasar"
import { effortService } from "../services/effort-service"
import { useEffortPermissions } from "../composables/use-effort-permissions"
import type { CourseDto, CourseRelationshipDto } from "../types"
import CourseEditDialog from "../components/CourseEditDialog.vue"
import CourseLinkDialog from "../components/CourseLinkDialog.vue"

const route = useRoute()
const $q = useQuasar()
const { hasEditCourse, hasManageRCourseEnrollment, hasLinkCourses, isAdmin } = useEffortPermissions()

// Route params
const termCode = computed(() => route.params.termCode as string)
const courseId = computed(() => parseInt(route.params.courseId as string, 10))

// State
const course = ref<CourseDto | null>(null)
const departments = ref<string[]>([])
const parentRelationship = ref<CourseRelationshipDto | null>(null)
const childRelationships = ref<CourseRelationshipDto[]>([])
const isLoading = ref(true)
const loadError = ref<string | null>(null)
const deletingId = ref<number | null>(null)

// Dialogs
const showEditDialog = ref(false)
const showLinkDialog = ref(false)

// Computed
const canEditCourse = computed(() => {
    if (!course.value) return false
    if (isAdmin.value || hasEditCourse.value) return true
    // ManageRCourseEnrollment only for R-courses (course number ends with R)
    if (hasManageRCourseEnrollment.value && course.value.crseNumb.trim().toUpperCase().endsWith("R")) return true
    return false
})

const editEnrollmentOnly = computed(() => {
    return !isAdmin.value && !hasEditCourse.value && hasManageRCourseEnrollment.value
})

const childColumns = computed<QTableColumn[]>(() => [
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

// Methods
async function loadCourse() {
    isLoading.value = true
    loadError.value = null

    try {
        const [courseResult, deptsResult, relationshipsResult] = await Promise.all([
            effortService.getCourse(courseId.value),
            effortService.getDepartments(),
            effortService.getCourseRelationships(courseId.value),
        ])

        if (!courseResult) {
            loadError.value = "Course not found or you do not have permission to view it."
            return
        }

        course.value = courseResult
        departments.value = deptsResult
        parentRelationship.value = relationshipsResult.parentRelationship
        childRelationships.value = relationshipsResult.childRelationships
    } catch {
        loadError.value = "Failed to load course. Please try again."
    } finally {
        isLoading.value = false
    }
}

async function loadRelationships() {
    if (!course.value) return

    const relationshipsResult = await effortService.getCourseRelationships(course.value.id)
    parentRelationship.value = relationshipsResult.parentRelationship
    childRelationships.value = relationshipsResult.childRelationships
}

function confirmDeleteRelationship(relationship: CourseRelationshipDto) {
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
    if (!course.value) return

    deletingId.value = relationship.id

    try {
        const success = await effortService.deleteCourseRelationship(course.value.id, relationship.id)

        if (success) {
            $q.notify({ type: "positive", message: "Link removed successfully" })
            await loadRelationships()
        } else {
            $q.notify({ type: "negative", message: "Failed to remove link" })
        }
    } finally {
        deletingId.value = null
    }
}

async function onCourseUpdated() {
    $q.notify({ type: "positive", message: "Course updated successfully" })
    await loadCourse()
}

async function onRelationshipsUpdated() {
    await loadRelationships()
}

onMounted(loadCourse)
</script>
