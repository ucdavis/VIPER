<template>
    <div class="q-pa-md">
        <!-- Breadcrumb -->
        <q-breadcrumbs class="q-mb-md">
            <q-breadcrumbs-el
                label="Instructors"
                :to="{ name: 'InstructorList', params: { termCode } }"
            />
            <q-breadcrumbs-el :label="instructor?.fullName ?? 'Instructor'" />
        </q-breadcrumbs>

        <!-- Loading state -->
        <div
            v-if="isLoading"
            class="text-grey q-my-md"
        >
            Loading instructor...
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
                    :to="{ name: 'InstructorList', params: { termCode } }"
                />
            </template>
        </q-banner>

        <!-- Instructor content -->
        <template v-else-if="instructor">
            <!-- Instructor Header -->
            <div class="q-mb-md">
                <h2 class="q-my-none q-mb-sm">
                    Effort for {{ instructor.firstName }} {{ instructor.lastName }} - {{ currentTermName }}
                </h2>
                <div class="row items-center q-gutter-sm">
                    <EffortActionButtons
                        v-if="canEdit"
                        @import="openImportDialog"
                        @add="openAddDialog"
                    />
                    <q-space class="gt-xs" />
                    <div
                        v-if="instructor.isVerified"
                        class="text-positive"
                    >
                        <q-icon
                            name="check_circle"
                            size="sm"
                        />
                        Verified on {{ formatEffortDate(instructor.effortVerified) }}
                    </div>
                </div>
            </div>

            <!-- Zero Effort Warning -->
            <ZeroEffortBanner
                :show="hasZeroEffort"
                mode="staff"
            />

            <!-- Clinical Effort Note -->
            <p
                v-if="hasClinicalEffort"
                class="text-grey-8"
            >
                Enter clinical effort as weeks. Enter all other effort as hours.
            </p>

            <!-- Effort Records Display -->
            <EffortRecordsDisplay
                :records="effortRecords"
                :term-code="termCode"
                :can-edit="canEdit"
                :can-delete="canDelete"
                show-course-links
                no-data-message="No effort records for this instructor"
                @edit="openEditDialog"
                @delete="confirmDelete"
            />

            <!-- Cross-Listed / Sectioned Courses Section -->
            <CrossListedCoursesSection
                :courses="allChildCourses"
                :show-parent-course="true"
            />
        </template>

        <!-- Add Effort Dialog -->
        <EffortRecordAddDialog
            v-model="showAddDialog"
            :person-id="personId"
            :term-code="termCodeNum"
            :is-verified="instructor?.isVerified"
            :pre-selected-course-id="preSelectedCourseId"
            :existing-records="effortRecords"
            @created="onRecordCreated"
        />

        <!-- Edit Effort Dialog -->
        <EffortRecordEditDialog
            v-model="showEditDialog"
            :record="selectedRecord"
            :term-code="termCodeNum"
            :is-verified="instructor?.isVerified"
            @updated="onRecordUpdated"
        />

        <!-- Course Import Dialog -->
        <CourseImportDialog
            v-model="showImportDialog"
            :term-code="termCodeNum"
            :term-name="currentTermName"
            @imported="onCourseImportedWithNotify"
        />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRoute } from "vue-router"
import { useQuasar } from "quasar"
import { instructorService } from "../services/instructor-service"
import { recordService } from "../services/record-service"
import { termService } from "../services/term-service"
import { useEffortPermissions } from "../composables/use-effort-permissions"
import { useEffortRecordManagement, formatEffortDate } from "../composables/use-effort-record-management"
import type { PersonDto, TermDto, InstructorEffortRecordDto } from "../types"
import EffortRecordAddDialog from "../components/EffortRecordAddDialog.vue"
import EffortRecordEditDialog from "../components/EffortRecordEditDialog.vue"
import CourseImportDialog from "../components/CourseImportDialog.vue"
import EffortRecordsDisplay from "../components/EffortRecordsDisplay.vue"
import EffortActionButtons from "../components/EffortActionButtons.vue"
import ZeroEffortBanner from "../components/ZeroEffortBanner.vue"
import CrossListedCoursesSection from "../components/CrossListedCoursesSection.vue"

const route = useRoute()
const $q = useQuasar()
const { hasEditEffort, isAdmin } = useEffortPermissions()

// Route params
const termCode = computed(() => route.params.termCode as string)
const personId = computed(() => parseInt(route.params.personId as string, 10))
const termCodeNum = computed(() => parseInt(termCode.value, 10))

// State
const instructor = ref<PersonDto | null>(null)
const effortRecords = ref<InstructorEffortRecordDto[]>([])
const terms = ref<TermDto[]>([])
const isLoading = ref(true)
const loadError = ref<string | null>(null)
const canEditTerm = ref(false)

// Composable
const {
    showAddDialog,
    showEditDialog,
    showImportDialog,
    selectedRecord,
    preSelectedCourseId,
    openAddDialog,
    openEditDialog,
    openImportDialog,
    onRecordCreated,
    onRecordUpdated,
    onCourseImported,
    deleteRecord,
} = useEffortRecordManagement(reloadData)

// Computed
const currentTermName = computed(() => {
    const code = parseInt(termCode.value, 10)
    const term = terms.value.find((t) => t.termCode === code)
    return term?.termName ?? ""
})

const hasClinicalEffort = computed(() => {
    return effortRecords.value.some((r) => r.effortType === "CLI")
})

const hasZeroEffort = computed(() => {
    return effortRecords.value.some((r) => {
        // Exclude auto-generated R-course (RESID) from zero effort warning
        if (r.course.crn === "RESID") return false
        if (r.effortType === "CLI") {
            return !r.weeks || r.weeks <= 0
        }
        return !r.hours || r.hours <= 0
    })
})

type ChildCourseWithParent = {
    id: number
    parentCourseId: number
    parentCourseCode: string
    subjCode: string
    crseNumb: string
    seqNumb: string
    units: number
    enrollment: number
    relationshipType: string
}

const allChildCourses = computed<ChildCourseWithParent[]>(() => {
    const children: ChildCourseWithParent[] = []
    const seenIds = new Set<number>()

    for (const record of effortRecords.value) {
        if (record.childCourses && record.childCourses.length > 0) {
            for (const child of record.childCourses) {
                if (!seenIds.has(child.id)) {
                    seenIds.add(child.id)
                    children.push({
                        ...child,
                        parentCourseId: record.course.id,
                        parentCourseCode: `${record.course.subjCode} ${record.course.crseNumb.trim()}-${record.course.seqNumb}`,
                    })
                }
            }
        }
    }

    return children
})

// Permission checks
const canEdit = computed(() => canEditTerm.value && (hasEditEffort.value || isAdmin.value))
const canDelete = computed(() => canEditTerm.value && (hasEditEffort.value || isAdmin.value))

// Methods
function confirmDelete(record: InstructorEffortRecordDto) {
    const verificationWarning = instructor.value?.isVerified
        ? "\n\nNote: This instructor's effort has been verified. Deleting this record will clear the verification status and require re-verification."
        : ""

    $q.dialog({
        title: "Delete Effort Record",
        message: `Are you sure you want to delete the effort record for ${record.course.subjCode} ${record.course.crseNumb.trim()} (${record.effortType})?${verificationWarning}`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        await deleteRecord(record.id, record.modifiedDate)
    })
}

function onCourseImportedWithNotify(courseId: number) {
    $q.notify({
        type: "positive",
        message: "Course imported successfully. Now add effort for this course.",
    })
    onCourseImported(courseId)
}

async function loadEffortRecords() {
    effortRecords.value = await instructorService.getInstructorEffortRecords(personId.value, termCodeNum.value)
}

async function loadInstructor() {
    instructor.value = await instructorService.getInstructor(personId.value, termCodeNum.value)
}

async function reloadData() {
    await Promise.all([loadEffortRecords(), loadInstructor()])
}

async function loadData() {
    isLoading.value = true
    loadError.value = null

    try {
        const [instructorResult, recordsResult, termsResult, canEditResult] = await Promise.all([
            instructorService.getInstructor(personId.value, termCodeNum.value),
            instructorService.getInstructorEffortRecords(personId.value, termCodeNum.value),
            termService.getTerms(),
            recordService.canEditTerm(termCodeNum.value),
        ])

        if (!instructorResult) {
            loadError.value = "Instructor not found or you do not have permission to view them."
            return
        }

        instructor.value = instructorResult
        effortRecords.value = recordsResult
        terms.value = termsResult
        canEditTerm.value = canEditResult
    } catch {
        loadError.value = "Failed to load instructor. Please try again."
    } finally {
        isLoading.value = false
    }
}

onMounted(loadData)
</script>
