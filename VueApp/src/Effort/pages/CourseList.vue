<template>
    <div class="q-pa-md">
        <h2>Course List</h2>

        <!-- Term selector and filters -->
        <div class="row q-col-gutter-sm q-mb-md items-center">
            <div class="col-12 col-sm-4 col-md-3">
                <q-select
                    v-model="selectedTermCode"
                    :options="terms"
                    option-label="termName"
                    option-value="termCode"
                    emit-value
                    map-options
                    label="Term"
                    dense
                    options-dense
                    outlined
                    @update:model-value="loadCourses"
                />
            </div>
            <div class="col-6 col-sm-3 col-md-2">
                <q-select
                    v-model="selectedDept"
                    :options="deptOptions"
                    label="Department"
                    dense
                    options-dense
                    outlined
                    clearable
                    @update:model-value="loadCourses"
                />
            </div>
            <div class="col-6 col-sm-3 col-md-2">
                <q-input
                    v-model="searchText"
                    label="Search"
                    dense
                    outlined
                    clearable
                >
                    <template #append>
                        <q-icon name="search" />
                    </template>
                </q-input>
            </div>

            <div class="col-12 col-sm-auto">
                <!-- Add course actions -->
                <q-btn-dropdown
                    v-if="canAddCourse"
                    color="primary"
                    label="Add Course"
                    dense
                    class="full-width-xs"
                >
                    <q-list>
                        <q-item
                            v-if="hasImportCourse"
                            v-close-popup
                            clickable
                            @click="showImportDialog = true"
                        >
                            <q-item-section avatar>
                                <q-icon name="cloud_download" />
                            </q-item-section>
                            <q-item-section>Import from Banner</q-item-section>
                        </q-item>
                        <q-item
                            v-if="hasEditCourse || isAdmin"
                            v-close-popup
                            clickable
                            @click="showAddDialog = true"
                        >
                            <q-item-section avatar>
                                <q-icon name="add" />
                            </q-item-section>
                            <q-item-section>Manual Entry</q-item-section>
                        </q-item>
                    </q-list>
                </q-btn-dropdown>
            </div>
        </div>

        <!-- Loading state -->
        <div
            v-if="isLoading"
            class="text-grey q-my-md"
        >
            Loading courses...
        </div>

        <!-- Courses table -->
        <q-table
            v-else
            :rows="filteredCourses"
            :columns="columns"
            row-key="id"
            dense
            flat
            bordered
            :pagination="pagination"
            :rows-per-page-options="[25, 50, 100, 0]"
        >
            <template #body-cell-courseCode="props">
                <q-td :props="props">
                    <div>
                        <span class="text-weight-medium">{{ props.row.courseCode }}</span>
                        <span class="text-grey-7 q-ml-xs">-{{ props.row.seqNumb }}</span>
                    </div>
                    <div class="text-caption text-grey-7 lt-sm">
                        {{ props.row.enrollment }} enrolled &bull; {{ props.row.units }} units
                    </div>
                </q-td>
            </template>
            <template #body-cell-actions="props">
                <q-td :props="props">
                    <q-btn
                        v-if="canEditCourse(props.row)"
                        icon="edit"
                        color="primary"
                        dense
                        flat
                        round
                        size="sm"
                        @click="openEditDialog(props.row)"
                    >
                        <q-tooltip>Edit course</q-tooltip>
                    </q-btn>
                    <q-btn
                        v-if="hasDeleteCourse"
                        icon="delete"
                        color="negative"
                        dense
                        flat
                        round
                        size="sm"
                        @click="confirmDeleteCourse(props.row)"
                    >
                        <q-tooltip>Delete course</q-tooltip>
                    </q-btn>
                </q-td>
            </template>
            <template #no-data>
                <div class="full-width row flex-center text-grey q-gutter-sm q-py-lg">
                    <q-icon
                        name="school"
                        size="2em"
                    />
                    <span>No courses found for this term</span>
                </div>
            </template>
        </q-table>

        <!-- Import Dialog -->
        <CourseImportDialog
            v-model="showImportDialog"
            :term-code="selectedTermCode"
            @imported="onCourseImported"
        />

        <!-- Edit Dialog -->
        <CourseEditDialog
            v-model="showEditDialog"
            :course="selectedCourse"
            :departments="departments"
            :enrollment-only="editEnrollmentOnly"
            @updated="onCourseUpdated"
        />

        <!-- Add Dialog -->
        <CourseAddDialog
            v-model="showAddDialog"
            :term-code="selectedTermCode"
            :departments="departments"
            @created="onCourseCreated"
        />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useQuasar } from "quasar"
import { effortService } from "../services/effort-service"
import { useEffortPermissions } from "../composables/use-effort-permissions"
import type { CourseDto, TermDto } from "../types"
import type { QTableColumn } from "quasar"
import CourseImportDialog from "../components/CourseImportDialog.vue"
import CourseEditDialog from "../components/CourseEditDialog.vue"
import CourseAddDialog from "../components/CourseAddDialog.vue"

const $q = useQuasar()
const { hasImportCourse, hasEditCourse, hasDeleteCourse, hasManageRCourseEnrollment, isAdmin } = useEffortPermissions()

// State
const terms = ref<TermDto[]>([])
const selectedTermCode = ref<number | null>(null)
const courses = ref<CourseDto[]>([])
const departments = ref<string[]>([])
const selectedDept = ref<string | null>(null)
const searchText = ref("")
const isLoading = ref(false)
const pagination = ref({ rowsPerPage: 50 })

// Dialogs
const showImportDialog = ref(false)
const showEditDialog = ref(false)
const showAddDialog = ref(false)
const selectedCourse = ref<CourseDto | null>(null)
const editEnrollmentOnly = ref(false)

// Computed
const canAddCourse = computed(() => hasImportCourse.value || hasEditCourse.value || isAdmin.value)

const deptOptions = computed(() => {
    const uniqueDepts = [...new Set(courses.value.map((c) => c.custDept))].sort()
    return ["", ...uniqueDepts]
})

const filteredCourses = computed(() => {
    let result = courses.value

    if (selectedDept.value) {
        result = result.filter((c) => c.custDept === selectedDept.value)
    }

    if (searchText.value) {
        const search = searchText.value.toLowerCase()
        result = result.filter(
            (c) =>
                c.courseCode.toLowerCase().includes(search) ||
                c.crn.toLowerCase().includes(search) ||
                c.seqNumb.toLowerCase().includes(search),
        )
    }

    return result
})

const columns = computed<QTableColumn[]>(() => [
    {
        name: "courseCode",
        label: "Course",
        field: "courseCode",
        align: "left",
        sortable: true,
    },
    {
        name: "crn",
        label: "CRN",
        field: "crn",
        align: "left",
        sortable: true,
    },
    {
        name: "enrollment",
        label: "Enrollment",
        field: "enrollment",
        align: "left",
        sortable: true,
        classes: "gt-xs",
        headerClasses: "gt-xs",
    },
    {
        name: "units",
        label: "Units",
        field: "units",
        align: "left",
        sortable: true,
        classes: "gt-xs",
        headerClasses: "gt-xs",
    },
    {
        name: "custDept",
        label: "Dept",
        field: "custDept",
        align: "left",
        sortable: true,
    },
    {
        name: "actions",
        label: "",
        field: "actions",
        align: "center",
    },
])

// Methods
function canEditCourse(course: CourseDto): boolean {
    if (isAdmin.value || hasEditCourse.value) return true
    // ManageRCourseEnrollment only for R-courses (course number ends with R)
    if (hasManageRCourseEnrollment.value && course.crseNumb.trim().toUpperCase().endsWith("R")) return true
    return false
}

async function loadTerms() {
    terms.value = await effortService.getTerms()
    if (terms.value.length > 0) {
        // Default to the most recent open term, or the first term
        const openTerm = terms.value.find((t) => t.isOpen)
        selectedTermCode.value = openTerm?.termCode ?? terms.value[0]?.termCode ?? null
        await loadCourses()
    }
}

async function loadCourses() {
    if (!selectedTermCode.value) return

    isLoading.value = true
    try {
        const [coursesResult, deptsResult] = await Promise.all([
            effortService.getCourses(selectedTermCode.value),
            effortService.getDepartments(),
        ])
        courses.value = coursesResult
        departments.value = deptsResult
    } finally {
        isLoading.value = false
    }
}

function openEditDialog(course: CourseDto) {
    selectedCourse.value = course
    // Determine if this is enrollment-only edit (ManageRCourseEnrollment without EditCourse)
    editEnrollmentOnly.value = !isAdmin.value && !hasEditCourse.value && hasManageRCourseEnrollment.value
    showEditDialog.value = true
}

function confirmDeleteCourse(course: CourseDto) {
    $q.dialog({
        title: "Delete Course",
        message: `Are you sure you want to delete ${course.courseCode}-${course.seqNumb} (CRN: ${course.crn})? This will also delete all associated effort records.`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        // Check record count first
        const { recordCount } = await effortService.canDeleteCourse(course.id)
        if (recordCount > 0) {
            $q.dialog({
                title: "Confirm Delete",
                message: `This course has ${recordCount} effort record(s) that will also be deleted. Are you sure you want to proceed?`,
                cancel: true,
                persistent: true,
            }).onOk(() => deleteCourse(course.id))
        } else {
            await deleteCourse(course.id)
        }
    })
}

async function deleteCourse(courseId: number) {
    const success = await effortService.deleteCourse(courseId)
    if (success) {
        $q.notify({ type: "positive", message: "Course deleted successfully" })
        await loadCourses()
    } else {
        $q.notify({ type: "negative", message: "Failed to delete course" })
    }
}

function onCourseImported() {
    $q.notify({ type: "positive", message: "Course imported successfully" })
    loadCourses()
}

function onCourseUpdated() {
    $q.notify({ type: "positive", message: "Course updated successfully" })
    loadCourses()
}

function onCourseCreated() {
    $q.notify({ type: "positive", message: "Course created successfully" })
    loadCourses()
}

onMounted(loadTerms)
</script>

<style scoped>
@media (width <= 599px) {
    .full-width-xs {
        width: 100%;
    }
}
</style>
