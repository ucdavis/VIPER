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
            role="status"
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
            <div class="q-mb-md">
                <h2 class="q-my-none q-mb-sm">
                    Effort for {{ course.courseCode }}-{{ course.seqNumb }} - {{ currentTermName }}
                </h2>
                <div class="row items-center q-gutter-sm q-pl-sm">
                    <q-btn
                        v-if="canEditCourse && !isResidentCourse"
                        icon="edit"
                        label="Edit Course"
                        color="primary"
                        dense
                        outline
                        aria-label="Edit course"
                        @click="showEditDialog = true"
                    />
                    <q-btn
                        v-if="hasLinkCourses && !parentRelationship && !isResidentCourse"
                        icon="link"
                        label="Link Courses"
                        color="secondary"
                        dense
                        outline
                        aria-label="Link courses"
                        @click="showLinkDialog = true"
                    />
                </div>
            </div>

            <!-- R-Course Notice -->
            <q-banner
                v-if="isResidentCourse"
                class="bg-info text-white q-mb-md"
                rounded
            >
                <template #avatar>
                    <q-icon name="info" />
                </template>
                The Resident (R) course was added automatically to allow recording of resident teaching effort. If left
                with 0 effort and verified, it will be automatically removed.
            </q-banner>

            <!-- Course Info (hidden for resident teaching course) -->
            <div
                v-if="!isResidentCourse"
                class="row q-col-gutter-md q-mb-lg"
            >
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
                v-if="!isResidentCourse && !parentRelationship && childRelationships.length === 0"
                class="text-grey-6 q-mb-lg"
            >
                This course has no linked courses.
            </div>

            <q-separator class="q-my-md" />

            <!-- CERE eval link — directs to Faculty Eval Viewer when harvest data exists -->
            <div
                v-if="showCereLink"
                class="q-pa-sm q-mb-md bg-blue-1 rounded-borders"
            >
                <q-icon
                    name="assessment"
                    class="q-mr-xs"
                />
                Evaluation data is available in the
                <a
                    :href="evalViewerUrl"
                    target="_blank"
                    rel="noopener"
                    class="text-primary"
                >
                    Faculty Eval Viewer
                    <q-icon
                        name="open_in_new"
                        size="xs"
                    />
                </a>
            </div>

            <!-- Tabbed layout: effort + eval (when ad-hoc eval is available) -->
            <template v-if="showEvalTab">
                <q-tabs
                    v-model="activeTab"
                    dense
                    align="left"
                    active-color="primary"
                    indicator-color="primary"
                    no-caps
                    class="text-grey-7"
                >
                    <q-tab
                        name="effort"
                        :label="effortTabLabel"
                    />
                    <q-tab
                        name="evaluation"
                        :label="evaluationTabLabel"
                    />
                </q-tabs>
                <q-separator />
                <q-tab-panels
                    v-model="activeTab"
                    keep-alive
                >
                    <q-tab-panel
                        name="effort"
                        class="q-px-none"
                    >
                        <div class="row items-center q-gutter-sm q-mb-sm">
                            <q-btn
                                v-if="canEditEffort"
                                icon="add"
                                label="Add Effort"
                                color="primary"
                                dense
                                aria-label="Add instructor effort"
                                @click="showAddEffortDialog = true"
                            />
                        </div>
                        <CourseEffortTable
                            :records="effortRecords"
                            :term-code="termCode"
                            :is-loading="isLoadingEffort"
                            :load-error="effortLoadError"
                            @edit="openEditEffortDialog"
                            @delete="confirmDeleteEffort"
                        />
                    </q-tab-panel>
                    <q-tab-panel
                        name="evaluation"
                        class="q-px-none"
                    >
                        <EvaluationStatusMatrix
                            :evaluation-data="evaluationData"
                            :can-edit-ad-hoc="hasEditAdHocEval && evaluationData.canEditAdHoc"
                            :is-loading="isLoadingEval"
                            :load-error="evalLoadError"
                            :term-code="termCode"
                            @add="openAddEvalDialog"
                            @edit="openEditEvalDialog"
                            @delete="confirmDeleteEval"
                        />
                    </q-tab-panel>
                </q-tab-panels>
            </template>

            <!-- Direct effort section (no tabs) -->
            <template v-else>
                <div class="row items-center q-gutter-sm q-mb-sm">
                    <q-btn
                        v-if="canEditEffort"
                        icon="add"
                        label="Add Effort"
                        color="primary"
                        dense
                        aria-label="Add instructor effort"
                        @click="showAddEffortDialog = true"
                    />
                </div>
                <CourseEffortTable
                    :records="effortRecords"
                    :term-code="termCode"
                    :is-loading="isLoadingEffort"
                    :load-error="effortLoadError"
                    @edit="openEditEffortDialog"
                    @delete="confirmDeleteEffort"
                />
            </template>
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

        <!-- Add Effort Dialog -->
        <AddCourseEffortDialog
            v-model="showAddEffortDialog"
            :course-id="courseId"
            :term-code="termCodeNum"
            @created="onEffortCreated"
        />

        <!-- Edit Effort Dialog -->
        <EffortRecordEditDialog
            v-model="showEditEffortDialog"
            :record="selectedEffortRecord"
            :term-code="termCodeNum"
            @updated="onEffortUpdated"
        />

        <!-- Edit Evaluation Dialog -->
        <EditEvaluationDialog
            v-if="evalDialogProps"
            v-model="showEvalDialog"
            :course-id="evalDialogProps.courseId"
            :instructor-name="evalDialogProps.instructorName"
            :course-name="evalDialogProps.courseName"
            :crn="evalDialogProps.crn"
            :mothra-id="evalDialogProps.mothraId"
            :existing-data="evalDialogProps.existingData"
            @created="onEvalCreated"
            @updated="onEvalUpdated"
        />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, inject, onMounted, watch } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useQuasar } from "quasar"
import type { QTableColumn } from "quasar"
import { courseService } from "../services/course-service"
import { recordService } from "../services/record-service"
import { termService } from "../services/term-service"
import { useEffortPermissions } from "../composables/use-effort-permissions"
import type {
    CourseDto,
    CourseRelationshipDto,
    CourseEffortRecordDto,
    InstructorEffortRecordDto,
    TermDto,
    CourseEvaluationStatusDto,
    CourseEvalEntryDto,
} from "../types"
import CourseEditDialog from "../components/CourseEditDialog.vue"
import CourseLinkDialog from "../components/CourseLinkDialog.vue"
import CourseEffortTable from "../components/CourseEffortTable.vue"
import AddCourseEffortDialog from "../components/AddCourseEffortDialog.vue"
import EffortRecordEditDialog from "../components/EffortRecordEditDialog.vue"
import EvaluationStatusMatrix from "../components/EvaluationStatusMatrix.vue"
import EditEvaluationDialog from "../components/EditEvaluationDialog.vue"

const route = useRoute()
const router = useRouter()
const $q = useQuasar()
const viperOneUrl = inject<string>("viperOneUrl", "/")
const {
    hasEditCourse,
    hasCreateEffort,
    hasVerifyEffort,
    hasManageRCourseEnrollment,
    hasLinkCourses,
    hasEditAdHocEval,
    hasViewEvalResults,
    isAdmin,
} = useEffortPermissions()

// Route params
const termCode = computed(() => route.params.termCode as string)
const courseId = computed(() => parseInt(route.params.courseId as string, 10))
const termCodeNum = computed(() => parseInt(termCode.value, 10))

// State
const course = ref<CourseDto | null>(null)
const departments = ref<string[]>([])
const terms = ref<TermDto[]>([])
const parentRelationship = ref<CourseRelationshipDto | null>(null)
const childRelationships = ref<CourseRelationshipDto[]>([])
const isLoading = ref(true)
const loadError = ref<string | null>(null)
const deletingId = ref<number | null>(null)

// Effort state
const effortRecords = ref<CourseEffortRecordDto[]>([])
const isLoadingEffort = ref(false)
const effortLoadError = ref<string | null>(null)
const selectedEffortRecord = ref<InstructorEffortRecordDto | null>(null)
const canAddEffortForCourse = ref(false)

// Evaluation state
const evaluationData = ref<CourseEvaluationStatusDto>({ canEditAdHoc: true, instructors: [], courses: [] })
const isLoadingEval = ref(false)
const evalLoadError = ref<string | null>(null)

// Evaluation dialog state
type EvalDialogProps = {
    courseId: number
    instructorName: string
    courseName: string
    crn: string
    mothraId: string
    existingData: CourseEvalEntryDto | null
}
const showEvalDialog = ref(false)
const evalDialogProps = ref<EvalDialogProps | null>(null)

// Tab state — synced with URL param, permission-aware
const validTabs = ["effort", "evaluation"] as const
const requestedTab = validTabs.includes(route.params.tab as (typeof validTabs)[number])
    ? (route.params.tab as string)
    : "effort"
const initialTab = requestedTab === "evaluation" && !hasViewEvalResults.value ? "effort" : requestedTab
const activeTab = ref(initialTab)

// Coerce tab when route or permissions change (back/forward, deep links)
watch(
    () => [route.params.tab, hasViewEvalResults.value] as const,
    ([tabParam, canView]) => {
        const requested = tabParam === "evaluation" ? "evaluation" : "effort"
        const allowed = requested === "evaluation" && !canView ? "effort" : requested
        if (activeTab.value !== allowed) activeTab.value = allowed
    },
)

// Dialogs
const showEditDialog = ref(false)
const showLinkDialog = ref(false)
const showAddEffortDialog = ref(false)
const showEditEffortDialog = ref(false)

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

const isResidentCourse = computed(() => course.value?.isRCourse === true && course.value?.crn === "RESID")

const currentTermName = computed(() => {
    const code = termCodeNum.value
    const term = terms.value.find((t) => t.termCode === code)
    return term?.termName ?? ""
})

const canEditEffort = computed(
    () => canAddEffortForCourse.value && (hasCreateEffort.value || hasVerifyEffort.value || isAdmin.value),
)

const effortTabLabel = computed(() =>
    effortRecords.value.length > 0 ? `Effort (${effortRecords.value.length})` : "Effort",
)
const evaluationTabLabel = computed(() => {
    const instructors = evaluationData.value.instructors
    if (instructors.length === 0) return "Evaluations"
    const withEvals = instructors.filter((i) => i.evaluations.some((e) => e.status !== "None")).length
    return `Evaluations (${withEvals}/${instructors.length})`
})

// Show eval tab only when user has permission AND no CERE data (ad-hoc mode)
const showEvalTab = computed(() => hasViewEvalResults.value && evaluationData.value.canEditAdHoc)

// Show CERE deep link when eval harvest data exists
const showCereLink = computed(
    () => hasViewEvalResults.value && !isLoadingEval.value && !evaluationData.value.canEditAdHoc,
)

// Deep link to Faculty Eval Viewer for CERE data
const evalViewerUrl = computed(
    () =>
        `${viperOneUrl}faculty/eval/default.cfm?Page=RptCourseWrapper&Hash=${termCode.value}${course.value?.crn ?? ""}`,
)

// Coerce to effort tab when eval tab disappears (CERE data detected)
watch(showEvalTab, (canShow) => {
    if (!canShow && activeTab.value === "evaluation") {
        activeTab.value = "effort"
    }
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
let loadToken = 0

async function loadCourse() {
    const token = ++loadToken
    const requestedCourseId = courseId.value

    isLoading.value = true
    loadError.value = null

    try {
        const [courseResult, deptsResult, relationshipsResult, termsResult] = await Promise.all([
            courseService.getCourse(requestedCourseId),
            courseService.getDepartments(),
            courseService.getCourseRelationships(requestedCourseId),
            termService.getTerms(),
        ])

        // Abort if a newer request has been initiated
        if (token !== loadToken) return

        if (!courseResult) {
            loadError.value = "Course not found or you do not have permission to view it."
            return
        }

        course.value = courseResult
        departments.value = deptsResult
        terms.value = termsResult
        parentRelationship.value = relationshipsResult.parentRelationship
        childRelationships.value = relationshipsResult.childRelationships

        // Load effort records and evaluation data after course data is available (non-blocking)
        loadEffortRecords()
        loadEvaluationData()
    } catch {
        if (token !== loadToken) return
        loadError.value = "Failed to load course. Please try again."
    } finally {
        if (token === loadToken) {
            isLoading.value = false
        }
    }
}

async function loadRelationships() {
    if (!course.value) return

    const relationshipsResult = await courseService.getCourseRelationships(course.value.id)
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
        const success = await courseService.deleteCourseRelationship(course.value.id, relationship.id)

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

async function loadEffortRecords() {
    if (!course.value) return
    const requestedId = course.value.id

    isLoadingEffort.value = true
    effortLoadError.value = null

    try {
        const effortResponse = await courseService.getCourseEffort(requestedId)
        if (courseId.value !== requestedId) return
        effortRecords.value = effortResponse.records
        canAddEffortForCourse.value = effortResponse.canAddEffort
    } catch {
        if (courseId.value !== requestedId) return
        effortLoadError.value = "Failed to load effort records."
    } finally {
        if (courseId.value === requestedId) {
            isLoadingEffort.value = false
        }
    }
}

function openEditEffortDialog(record: CourseEffortRecordDto) {
    // Map CourseEffortRecordDto to InstructorEffortRecordDto shape for the edit dialog
    selectedEffortRecord.value = {
        id: record.effortId,
        courseId: 0,
        personId: record.personId,
        termCode: termCodeNum.value,
        effortType: record.effortTypeId,
        effortTypeDescription: record.effortTypeDescription,
        role: record.roleId,
        roleDescription: record.roleDescription,
        hours: record.hours,
        weeks: record.weeks,
        crn: "",
        notes: record.notes,
        modifiedDate: record.modifiedDate,
        effortValue: record.effortValue,
        effortLabel: record.effortLabel,
        course: course.value!,
        childCourses: [],
    }
    showEditEffortDialog.value = true
}

function confirmDeleteEffort(record: CourseEffortRecordDto) {
    $q.dialog({
        title: "Delete Effort Record",
        message: `Are you sure you want to delete the effort record for ${record.instructorName} (${record.effortTypeDescription})?`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        try {
            const result = await recordService.deleteEffortRecord(record.effortId, record.modifiedDate)
            if (result.success) {
                $q.notify({ type: "positive", message: "Effort record deleted successfully" })
                await loadEffortRecords()
            } else {
                $q.notify({ type: "negative", message: result.error ?? "Failed to delete effort record" })
                if (result.isConflict) {
                    await loadEffortRecords()
                }
            }
        } catch {
            $q.notify({ type: "negative", message: "An error occurred while deleting the record" })
        }
    })
}

async function loadEvaluationData() {
    if (!course.value || !hasViewEvalResults.value) return
    const requestedId = course.value.id

    isLoadingEval.value = true
    evalLoadError.value = null

    try {
        const result = await courseService.getCourseEvaluations(requestedId)
        if (courseId.value !== requestedId) return
        evaluationData.value = result
    } catch {
        if (courseId.value !== requestedId) return
        evalLoadError.value = "Failed to load evaluation data."
    } finally {
        if (courseId.value === requestedId) {
            isLoadingEval.value = false
        }
    }
}

function openAddEvalDialog(entry: {
    courseId: number
    crn: string
    mothraId: string
    instructorName: string
    courseName: string
}) {
    evalDialogProps.value = {
        courseId: entry.courseId,
        instructorName: entry.instructorName,
        courseName: entry.courseName,
        crn: entry.crn,
        mothraId: entry.mothraId,
        existingData: null,
    }
    showEvalDialog.value = true
}

function openEditEvalDialog(entry: {
    courseId: number
    crn: string
    mothraId: string
    instructorName: string
    courseName: string
    data: CourseEvalEntryDto
}) {
    evalDialogProps.value = {
        courseId: entry.courseId,
        instructorName: entry.instructorName,
        courseName: entry.courseName,
        crn: entry.crn,
        mothraId: entry.mothraId,
        existingData: entry.data,
    }
    showEvalDialog.value = true
}

function confirmDeleteEval(entry: { courseId: number; quantId: number; instructorName: string; courseName: string }) {
    $q.dialog({
        title: "Delete Evaluation",
        message: `Are you sure you want to delete the ad-hoc evaluation for ${entry.instructorName} on ${entry.courseName}?`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        try {
            const success = await courseService.deleteEvaluation(entry.courseId, entry.quantId)
            if (success) {
                $q.notify({ type: "positive", message: "Evaluation deleted successfully" })
                await loadEvaluationData()
            } else {
                $q.notify({ type: "negative", message: "Failed to delete evaluation" })
            }
        } catch {
            $q.notify({ type: "negative", message: "An error occurred while deleting the evaluation" })
        }
    })
}

async function onEvalCreated() {
    $q.notify({ type: "positive", message: "Evaluation created successfully" })
    await loadEvaluationData()
}

async function onEvalUpdated() {
    $q.notify({ type: "positive", message: "Evaluation updated successfully" })
    await loadEvaluationData()
}

async function onEffortCreated() {
    $q.notify({ type: "positive", message: "Effort record created successfully" })
    await loadEffortRecords()
}

async function onEffortUpdated() {
    $q.notify({ type: "positive", message: "Effort record updated successfully" })
    await loadEffortRecords()
}

async function onCourseUpdated() {
    $q.notify({ type: "positive", message: "Course updated successfully" })
    await loadCourse()
}

async function onRelationshipsUpdated() {
    await loadRelationships()
}

onMounted(loadCourse)

// Reload course data when navigating between courses (same component, different route params)
watch(courseId, loadCourse)

// Sync tab selection to URL
watch(activeTab, (tab) => {
    const tabParam = tab === "effort" ? undefined : tab
    if (route.params.tab !== tabParam) {
        router.replace({
            name: "CourseDetail",
            params: { ...route.params, tab: tabParam },
        })
    }
})
</script>
