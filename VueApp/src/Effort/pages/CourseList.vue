<template>
    <div class="q-pa-md">
        <h2>Course List</h2>

        <!-- Term selector and filters -->
        <div class="row q-col-gutter-sm q-mb-md items-center">
            <div class="col-12 col-sm-6 col-md-3">
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
                />
            </div>
            <div class="col-12 col-sm-6 col-md-3">
                <q-select
                    v-model="selectedDept"
                    :options="deptOptions"
                    label="Department"
                    dense
                    options-dense
                    outlined
                    clearable
                />
            </div>
            <div class="col-12 col-sm-6 col-md-3">
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

            <div class="col-12 col-sm-6 col-md-auto">
                <!-- Add course actions -->
                <q-btn-dropdown
                    v-if="canAddCourse"
                    color="primary"
                    icon="add"
                    label="Add Course"
                    dense
                    class="full-width-xs"
                >
                    <q-list>
                        <q-item
                            v-if="hasImportCourse"
                            v-close-popup
                            clickable
                            @click="openImportDialog"
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
                            @click="openAddDialog"
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

        <!-- Grouped courses by department -->
        <template v-else-if="groupedCourses.length > 0 || residentCourses.length > 0">
            <div
                v-for="deptGroup in groupedCourses"
                :key="deptGroup.dept"
                class="q-mb-sm"
            >
                <!-- Department header -->
                <div
                    class="dept-header text-white q-pa-sm row items-center"
                    :class="{
                        'dept-header--collapsible': hasMultipleDepts,
                    }"
                    :tabindex="hasMultipleDepts ? 0 : undefined"
                    :role="hasMultipleDepts ? 'button' : undefined"
                    :aria-expanded="hasMultipleDepts ? !collapsedDepts.has(deptGroup.dept) : undefined"
                    @click="hasMultipleDepts && toggleDeptCollapse(deptGroup.dept)"
                    @keyup.enter="hasMultipleDepts && toggleDeptCollapse(deptGroup.dept)"
                    @keyup.space.prevent="hasMultipleDepts && toggleDeptCollapse(deptGroup.dept)"
                >
                    <q-icon
                        v-if="hasMultipleDepts"
                        :name="collapsedDepts.has(deptGroup.dept) ? 'expand_more' : 'expand_less'"
                        size="sm"
                        class="q-mr-xs"
                    />
                    <span class="text-weight-bold">{{ deptGroup.dept }} ({{ deptGroup.courses.length }})</span>
                </div>

                <!-- Courses table per department -->
                <q-table
                    v-show="!collapsedDepts.has(deptGroup.dept)"
                    :rows="deptGroup.courses"
                    :columns="columns"
                    row-key="id"
                    dense
                    flat
                    bordered
                    hide-pagination
                    :rows-per-page-options="[0]"
                    class="dept-table"
                >
                    <template #body-cell-courseCode="props">
                        <q-td :props="props">
                            <div class="course-info">
                                <router-link
                                    :to="{
                                        name: 'CourseDetail',
                                        params: { termCode: selectedTermCode, courseId: props.row.id },
                                    }"
                                    class="text-weight-medium text-primary"
                                    >{{ props.row.courseCode }}-{{ props.row.seqNumb }}</router-link
                                >
                                <q-badge
                                    v-if="!isSvmDept(props.row.custDept)"
                                    color="negative"
                                    text-color="white"
                                >
                                    Non-SVM Dept
                                    <q-tooltip
                                        >Expected SVM departments: APC, DVM, PHR, PMI, VMB, VME, VET, VSR</q-tooltip
                                    >
                                </q-badge>
                                <q-badge
                                    v-if="props.row.enrollment === 0"
                                    color="orange-8"
                                    text-color="white"
                                >
                                    0 Enrollment
                                    <q-tooltip>This course has zero enrollment</q-tooltip>
                                </q-badge>
                            </div>
                            <div class="text-caption text-grey-7 lt-sm">
                                {{ props.row.enrollment }} enrolled &bull; {{ props.row.units }} units
                            </div>
                        </q-td>
                    </template>
                    <template #body-cell-actions="props">
                        <q-td :props="props">
                            <div class="actions-cell">
                                <q-btn
                                    v-if="hasLinkCourses && !props.row.parentCourseId"
                                    icon="link"
                                    color="secondary"
                                    dense
                                    flat
                                    round
                                    size="0.75rem"
                                    :aria-label="`Link courses to ${props.row.courseCode}`"
                                    @click="openLinkDialog(props.row)"
                                >
                                    <q-tooltip>Link courses</q-tooltip>
                                </q-btn>
                                <q-btn
                                    v-if="canEditCourse(props.row)"
                                    icon="edit"
                                    color="primary"
                                    dense
                                    flat
                                    round
                                    size="0.75rem"
                                    :aria-label="`Edit ${props.row.courseCode}`"
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
                                    size="0.75rem"
                                    :aria-label="`Delete ${props.row.courseCode}`"
                                    @click="confirmDeleteCourse(props.row)"
                                >
                                    <q-tooltip>Delete course</q-tooltip>
                                </q-btn>
                            </div>
                        </q-td>
                    </template>
                </q-table>
            </div>
        </template>

        <!-- Resident Teaching (R-Course) section -->
        <template v-if="!isLoading && residentCourses.length > 0">
            <q-separator class="q-my-md" />
            <div class="dept-header text-white q-pa-sm row items-center">
                <span class="text-weight-bold">Resident Teaching (R-Course) ({{ residentCourses.length }})</span>
            </div>
            <q-banner
                class="bg-info text-white q-mb-none"
                square
            >
                <template #avatar>
                    <q-icon name="info" />
                </template>
                The Resident (R) course was added automatically to allow recording of resident teaching effort. If left
                with 0 effort and verified, it will be automatically removed.
            </q-banner>
            <q-table
                :rows="residentCourses"
                :columns="rCourseColumns"
                row-key="id"
                dense
                flat
                bordered
                hide-pagination
                :rows-per-page-options="[0]"
                class="dept-table"
            >
                <template #body-cell-courseCode="props">
                    <q-td :props="props">
                        <router-link
                            :to="{
                                name: 'CourseDetail',
                                params: { termCode: selectedTermCode, courseId: props.row.id },
                            }"
                            class="text-weight-medium text-primary"
                            >{{ props.row.courseCode }}-{{ props.row.seqNumb }}</router-link
                        >
                    </q-td>
                </template>
            </q-table>
        </template>

        <!-- No data state -->
        <div
            v-if="!isLoading && groupedCourses.length === 0 && residentCourses.length === 0"
            class="full-width row flex-center text-grey q-gutter-sm q-py-lg"
        >
            <q-icon
                name="school"
                size="2em"
            />
            <span>No courses found for this term</span>
        </div>

        <!-- Import Dialog -->
        <CourseImportDialog
            v-model="showImportDialog"
            :term-code="selectedTermCode"
            :term-name="selectedTermName"
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

        <!-- Link Dialog -->
        <CourseLinkDialog
            v-model="showLinkDialog"
            :course="selectedCourse"
            @updated="onRelationshipsUpdated"
        />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from "vue"
import { useQuasar } from "quasar"
import { useRoute, useRouter } from "vue-router"
import { courseService } from "../services/course-service"
import { termService } from "../services/term-service"
import { useEffortPermissions } from "../composables/use-effort-permissions"
import type { CourseDto, TermDto } from "../types"
import type { QTableColumn } from "quasar"
import CourseImportDialog from "../components/CourseImportDialog.vue"
import CourseEditDialog from "../components/CourseEditDialog.vue"
import CourseAddDialog from "../components/CourseAddDialog.vue"
import CourseLinkDialog from "../components/CourseLinkDialog.vue"
import { inflect } from "inflection"
import "../dept-table.css"

const $q = useQuasar()
const route = useRoute()
const router = useRouter()
const { hasImportCourse, hasEditCourse, hasDeleteCourse, hasManageRCourseEnrollment, hasLinkCourses, isAdmin } =
    useEffortPermissions()

// State
const terms = ref<TermDto[]>([])
const selectedTermCode = ref<number | null>(null)
const courses = ref<CourseDto[]>([])
const departments = ref<string[]>([])
const selectedDept = ref<string | null>(null)
const searchText = ref("")
const isLoading = ref(false)
const collapsedDepts = ref<Set<string>>(new Set())

// Dialogs
const showImportDialog = ref(false)
const showEditDialog = ref(false)
const showAddDialog = ref(false)
const showLinkDialog = ref(false)
const selectedCourse = ref<CourseDto | null>(null)
const editEnrollmentOnly = ref(false)

// Computed
const canAddCourse = computed(() => hasImportCourse.value || hasEditCourse.value || isAdmin.value)
const selectedTermName = computed(() => terms.value.find((t) => t.termCode === selectedTermCode.value)?.termName ?? "")

const deptOptions = computed(() => {
    // Use the authorized departments from the API (not just those with courses)
    // so ViewDept users see their department even when it has no courses for the term
    if (departments.value.length > 0) {
        return [...departments.value].sort()
    }
    return [...new Set(courses.value.map((c) => c.custDept).filter(Boolean))].sort()
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

function isResidentCourse(course: CourseDto): boolean {
    return course.isRCourse && course.crn === "RESID"
}

// Separate resident teaching course from regular courses
const residentCourses = computed(() => filteredCourses.value.filter(isResidentCourse))

// Group regular courses by department (excluding resident course)
const groupedCourses = computed(() => {
    const groups: Record<string, CourseDto[]> = {}

    for (const course of filteredCourses.value) {
        if (isResidentCourse(course)) continue
        const dept = course.custDept || "Unknown"
        const groupArray = groups[dept] ?? (groups[dept] = [])
        groupArray.push(course)
    }

    return Object.keys(groups)
        .sort((a, b) => {
            if (a === "Unknown") return 1
            if (b === "Unknown") return -1
            return a.localeCompare(b)
        })
        .map((dept) => ({
            dept,
            courses: groups[dept] ?? [],
        }))
})

const hasMultipleDepts = computed(() => groupedCourses.value.length > 1)

const validDepts = new Set(["APC", "PMI", "PHR", "VMB", "VME", "VSR", "VET", "DVM"])
function isSvmDept(dept: string): boolean {
    return validDepts.has(dept)
}

function toggleDeptCollapse(dept: string) {
    if (collapsedDepts.value.has(dept)) {
        collapsedDepts.value.delete(dept)
    } else {
        collapsedDepts.value.add(dept)
    }
}

const columns = computed<QTableColumn[]>(() => [
    {
        name: "courseCode",
        label: "Course",
        field: "courseCode",
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
        style: "width: 100px; min-width: 100px",
        headerStyle: "width: 100px; min-width: 100px",
    },
    {
        name: "units",
        label: "Units",
        field: "units",
        align: "left",
        sortable: true,
        classes: "gt-xs",
        headerClasses: "gt-xs",
        style: "width: 80px; min-width: 80px",
        headerStyle: "width: 80px; min-width: 80px",
    },
    {
        name: "actions",
        label: "Actions",
        field: "actions",
        align: "center",
        style: "width: 140px; min-width: 140px",
        headerStyle: "width: 140px; min-width: 140px",
    },
])

const rCourseColumns = computed<QTableColumn[]>(() => [
    {
        name: "courseCode",
        label: "Course",
        field: "courseCode",
        align: "left",
        sortable: true,
    },
])

// Race condition protection for async loads
let loadToken = 0

// Methods
function canEditCourse(course: CourseDto): boolean {
    if (isAdmin.value || hasEditCourse.value) return true
    // ManageRCourseEnrollment only for R-courses (course number ends with R)
    if (hasManageRCourseEnrollment.value && course.crseNumb.trim().toUpperCase().endsWith("R")) return true
    return false
}

async function loadTerms() {
    const paramTermCode = route.params.termCode ? parseInt(route.params.termCode as string, 10) : null

    terms.value = await termService.getTerms()

    // Set termCode after terms loaded to prevent flash of raw number in dropdown
    if (paramTermCode && terms.value.some((t) => t.termCode === paramTermCode)) {
        selectedTermCode.value = paramTermCode
        await loadCourses()
    } else {
        router.replace({ name: "EffortHome" })
    }
}

// Keep URL in sync with selected term when user changes the dropdown
watch(selectedTermCode, (newTermCode, oldTermCode) => {
    if (newTermCode && oldTermCode !== null) {
        router.replace({ name: "CourseList", params: { termCode: newTermCode.toString() } })
        loadCourses()
    }
})

// Handle browser back/forward navigation that changes the termCode param
watch(
    () => route.params.termCode,
    (newTermCode) => {
        const parsed = newTermCode ? parseInt(newTermCode as string, 10) : null
        if (parsed && parsed !== selectedTermCode.value && terms.value.some((t) => t.termCode === parsed)) {
            selectedTermCode.value = parsed
        }
    },
)

async function loadCourses() {
    if (!selectedTermCode.value) return

    const token = ++loadToken
    isLoading.value = true

    try {
        const [coursesResult, deptsResult] = await Promise.all([
            courseService.getCourses(selectedTermCode.value),
            courseService.getDepartments(),
        ])

        // Abort if a newer request has been initiated
        if (token !== loadToken) return

        courses.value = coursesResult
        departments.value = deptsResult
    } finally {
        if (token === loadToken) {
            isLoading.value = false
        }
    }
}

function openImportDialog() {
    showAddDialog.value = false
    showImportDialog.value = true
}

function openAddDialog() {
    showImportDialog.value = false
    showAddDialog.value = true
}

function openEditDialog(course: CourseDto) {
    selectedCourse.value = course
    // Determine if this is enrollment-only edit (ManageRCourseEnrollment without EditCourse)
    editEnrollmentOnly.value = !isAdmin.value && !hasEditCourse.value && hasManageRCourseEnrollment.value
    showEditDialog.value = true
}

function openLinkDialog(course: CourseDto) {
    selectedCourse.value = course
    showLinkDialog.value = true
}

function confirmDeleteCourse(course: CourseDto) {
    $q.dialog({
        title: "Delete Course",
        message: `Are you sure you want to delete ${course.courseCode}-${course.seqNumb}? This will also delete all associated effort records.`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        // Check record count first
        const { recordCount } = await courseService.canDeleteCourse(course.id)
        if (recordCount > 0) {
            $q.dialog({
                title: "Confirm Delete",
                message: `This course has ${recordCount} effort ${inflect("record", recordCount)} that will also be deleted. Are you sure you want to proceed?`,
                cancel: true,
                persistent: true,
            }).onOk(() => deleteCourse(course.id))
        } else {
            await deleteCourse(course.id)
        }
    })
}

async function deleteCourse(courseId: number) {
    const success = await courseService.deleteCourse(courseId)
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

function onRelationshipsUpdated() {
    // Reload courses so ParentCourseId changes are reflected (affects link button visibility)
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

.course-info {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.125rem 0.5rem;
}

.actions-cell {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
}
</style>
