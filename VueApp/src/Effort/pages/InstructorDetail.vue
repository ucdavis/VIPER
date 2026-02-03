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
                    <q-btn
                        v-if="canEdit"
                        color="secondary"
                        label="Import Course"
                        icon="cloud_download"
                        dense
                        @click="showImportDialog = true"
                    />
                    <q-btn
                        v-if="canEdit"
                        color="primary"
                        label="Add Effort"
                        icon="add"
                        dense
                        @click="openAddDialog"
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
                        Verified on {{ formatDate(instructor.effortVerified) }}
                    </div>
                </div>
            </div>

            <!-- Zero Effort Warning -->
            <q-banner
                v-if="hasZeroEffort"
                class="bg-warning q-mb-md"
                rounded
            >
                <template #avatar>
                    <q-icon
                        name="warning"
                        color="dark"
                    />
                </template>
                NOTE: This instructor has one or more effort items documented as ZERO effort. Effort cannot be verified
                until these items have been updated or removed.
            </q-banner>

            <!-- Clinical Effort Note -->
            <p
                v-if="hasClinicalEffort"
                class="text-grey-8"
            >
                Enter clinical effort as weeks. Enter all other effort as hours.
            </p>

            <!-- Mobile Card View -->
            <div class="lt-sm q-mb-lg">
                <div
                    v-if="effortRecords.length === 0"
                    class="text-center text-grey q-py-lg"
                >
                    <q-icon
                        name="school"
                        size="2em"
                        class="q-mb-sm"
                    />
                    <div>No effort records for this instructor</div>
                </div>
                <q-card
                    v-for="record in effortRecords"
                    :key="record.id"
                    flat
                    bordered
                    class="q-mb-sm"
                    :class="{ 'zero-effort-card': record.effortValue === 0 }"
                >
                    <q-card-section class="q-py-sm">
                        <div class="row items-center justify-between q-mb-xs">
                            <router-link
                                :to="{
                                    name: 'CourseDetail',
                                    params: { termCode, courseId: record.course.id },
                                }"
                                class="text-primary text-weight-bold"
                            >
                                {{ record.course.subjCode }}
                                {{ record.course.crseNumb.trim() }}-{{ record.course.seqNumb }}
                            </router-link>
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
                                    @click="openEditDialog(record)"
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
                                    @click="confirmDelete(record)"
                                />
                            </div>
                        </div>
                        <div class="text-body2 q-mb-xs">
                            {{ record.roleDescription }} &bull; {{ record.effortType }}
                        </div>
                        <div class="row q-gutter-md text-caption text-grey-7">
                            <span>{{ record.course.units }} units</span>
                            <span>Enroll: {{ record.course.enrollment }}</span>
                            <span :class="{ 'text-warning': record.effortValue === 0 }">
                                {{ record.effortValue ?? 0 }}
                                {{ record.effortLabel === "weeks" ? "Weeks" : "Hours" }}
                            </span>
                        </div>
                    </q-card-section>
                </q-card>
            </div>

            <!-- Effort Records Table (hidden on mobile) -->
            <q-table
                :rows="effortRecords"
                :columns="columns"
                row-key="id"
                dense
                flat
                bordered
                hide-pagination
                wrap-cells
                :rows-per-page-options="[0]"
                class="effort-table q-mb-lg gt-xs"
            >
                <template #body-cell-course="props">
                    <q-td :props="props">
                        <router-link
                            :to="{
                                name: 'CourseDetail',
                                params: { termCode, courseId: props.row.course.id },
                            }"
                            class="text-primary"
                        >
                            {{ props.row.course.subjCode }}
                            {{ props.row.course.crseNumb.trim() }}-{{ props.row.course.seqNumb }}
                        </router-link>
                    </q-td>
                </template>
                <template #body-cell-effort="props">
                    <q-td
                        :props="props"
                        :class="{ 'zero-effort': props.row.effortValue === 0 }"
                    >
                        {{ props.row.effortValue ?? 0 }}
                        {{ props.row.effortLabel === "weeks" ? "Weeks" : "Hours" }}
                    </q-td>
                </template>
                <template #body-cell-actions="props">
                    <q-td :props="props">
                        <q-btn
                            v-if="canEdit"
                            flat
                            dense
                            round
                            icon="edit"
                            color="primary"
                            size="sm"
                            @click="openEditDialog(props.row)"
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
                            @click="confirmDelete(props.row)"
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
                        <span>No effort records for this instructor</span>
                    </div>
                </template>
            </q-table>

            <!-- Cross-Listed / Sectioned Courses Section -->
            <div
                v-if="allChildCourses.length > 0"
                class="q-mb-lg"
            >
                <h4 class="q-mt-none q-mb-sm">Cross-Listed / Sectioned Courses</h4>
                <!-- Mobile card view for child courses -->
                <div class="lt-sm">
                    <q-card
                        v-for="child in allChildCourses"
                        :key="`${child.parentCourseId}-${child.id}`"
                        flat
                        bordered
                        class="q-mb-sm"
                        :class="child.relationshipType === 'CrossList' ? 'crosslist-card' : 'section-card'"
                    >
                        <q-card-section class="q-py-sm">
                            <div class="row items-center justify-between q-mb-xs">
                                <span class="text-weight-bold">
                                    {{ child.subjCode }} {{ child.crseNumb.trim() }}-{{ child.seqNumb }}
                                </span>
                                <q-badge
                                    :color="child.relationshipType === 'CrossList' ? 'positive' : 'info'"
                                    :label="child.relationshipType === 'CrossList' ? 'Cross List' : 'Section'"
                                />
                            </div>
                            <div class="text-caption text-grey-7">
                                Parent: {{ child.parentCourseCode }} &bull; {{ child.units }} units &bull; Enroll:
                                {{ child.enrollment }}
                            </div>
                        </q-card-section>
                    </q-card>
                </div>
                <!-- Table view for child courses (desktop) -->
                <q-table
                    :rows="allChildCourses"
                    :columns="childColumns"
                    row-key="id"
                    dense
                    flat
                    bordered
                    hide-pagination
                    :rows-per-page-options="[0]"
                    class="gt-xs"
                >
                    <template #body-cell-relationshipType="slotProps">
                        <q-td :props="slotProps">
                            <q-badge
                                :color="slotProps.row.relationshipType === 'CrossList' ? 'positive' : 'info'"
                                :label="slotProps.row.relationshipType === 'CrossList' ? 'Cross List' : 'Section'"
                            />
                        </q-td>
                    </template>
                </q-table>
            </div>
        </template>

        <!-- Add Effort Dialog -->
        <EffortRecordAddDialog
            v-model="showAddDialog"
            :person-id="personId"
            :term-code="termCodeNum"
            :is-verified="instructor?.isVerified"
            :pre-selected-course-id="preSelectedCourseId"
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
            @imported="onCourseImported"
        />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRoute } from "vue-router"
import { useQuasar, type QTableColumn } from "quasar"
import { instructorService } from "../services/instructor-service"
import { recordService } from "../services/record-service"
import { termService } from "../services/term-service"
import type { PersonDto, TermDto, InstructorEffortRecordDto } from "../types"
import EffortRecordAddDialog from "../components/EffortRecordAddDialog.vue"
import EffortRecordEditDialog from "../components/EffortRecordEditDialog.vue"
import CourseImportDialog from "../components/CourseImportDialog.vue"

const route = useRoute()
const $q = useQuasar()

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

// Dialog state
const showAddDialog = ref(false)
const showEditDialog = ref(false)
const showImportDialog = ref(false)
const selectedRecord = ref<InstructorEffortRecordDto | null>(null)
const preSelectedCourseId = ref<number | null>(null)

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
        if (r.effortType === "CLI") {
            return !r.weeks || r.weeks <= 0
        }
        return !r.hours || r.hours <= 0
    })
})

// Flatten all child courses from effort records with parent course info
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
                // Avoid duplicates if same course appears in multiple effort records
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

const childColumns: QTableColumn[] = [
    {
        name: "course",
        label: "Course",
        field: (row: ChildCourseWithParent) => `${row.subjCode} ${row.crseNumb.trim()}-${row.seqNumb}`,
        align: "left",
    },
    {
        name: "units",
        label: "Units",
        field: "units",
        align: "left",
    },
    {
        name: "enrollment",
        label: "Enroll",
        field: "enrollment",
        align: "left",
    },
    {
        name: "parentCourse",
        label: "Parent Course",
        field: "parentCourseCode",
        align: "left",
    },
    {
        name: "relationshipType",
        label: "Type",
        field: "relationshipType",
        align: "center",
    },
]

// Permission checks - based on canEditTerm which checks term status and EditWhenClosed permission
const canEdit = computed(() => canEditTerm.value)
const canDelete = computed(() => canEditTerm.value)

const columns = computed<QTableColumn[]>(() => {
    const cols: QTableColumn[] = [
        {
            name: "course",
            label: "Course",
            field: (row: InstructorEffortRecordDto) =>
                `${row.course.subjCode} ${row.course.crseNumb.trim()}-${row.course.seqNumb}`,
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

    // Add actions column if user can edit or delete
    if (canEdit.value || canDelete.value) {
        cols.push({
            name: "actions",
            label: "Actions",
            field: "id",
            align: "center",
        })
    }

    return cols
})

// Methods
function formatDate(dateString: string | null): string {
    if (!dateString) return ""
    const date = new Date(dateString)
    return date.toLocaleDateString("en-US", {
        month: "numeric",
        day: "numeric",
        year: "numeric",
        hour: "numeric",
        minute: "2-digit",
    })
}

function openAddDialog() {
    preSelectedCourseId.value = null
    showAddDialog.value = true
}

function openEditDialog(record: InstructorEffortRecordDto) {
    selectedRecord.value = record
    showEditDialog.value = true
}

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

async function deleteRecord(recordId: number, originalModifiedDate: string | null) {
    try {
        const result = await recordService.deleteEffortRecord(recordId, originalModifiedDate)
        if (result.success) {
            $q.notify({
                type: "positive",
                message: "Effort record deleted successfully",
            })
            // Reload both effort records and instructor data since deletes clear verification status
            await Promise.all([loadEffortRecords(), loadInstructor()])
        } else {
            $q.notify({
                type: "negative",
                message: result.error ?? "Failed to delete effort record",
            })
            // Reload data if it was a concurrency conflict
            if (result.isConflict) {
                await Promise.all([loadEffortRecords(), loadInstructor()])
            }
        }
    } catch {
        $q.notify({
            type: "negative",
            message: "An error occurred while deleting the record",
        })
    }
}

async function onRecordCreated() {
    preSelectedCourseId.value = null
    $q.notify({
        type: "positive",
        message: "Effort record created successfully",
    })
    // Reload both effort records and instructor data since creates clear verification status
    await Promise.all([loadEffortRecords(), loadInstructor()])
}

async function onRecordUpdated() {
    $q.notify({
        type: "positive",
        message: "Effort record updated successfully",
    })
    // Reload both effort records and instructor data since updates clear verification status
    await Promise.all([loadEffortRecords(), loadInstructor()])
}

async function onCourseImported(courseId: number) {
    $q.notify({
        type: "positive",
        message: "Course imported successfully. Now add effort for this course.",
    })
    // Open the Add Effort dialog with the imported course pre-selected
    preSelectedCourseId.value = courseId
    showAddDialog.value = true
}

async function loadEffortRecords() {
    effortRecords.value = await instructorService.getInstructorEffortRecords(personId.value, termCodeNum.value)
}

async function loadInstructor() {
    instructor.value = await instructorService.getInstructor(personId.value, termCodeNum.value)
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

<style scoped>
.effort-table {
    width: 100%;
}

.effort-table :deep(.zero-effort) {
    background-color: #fff3cd;
    color: #856404;
}

.zero-effort-card {
    background-color: #fff3cd;
}

.crosslist-card {
    border-left: 3px solid #21ba45;
}

.section-card {
    border-left: 3px solid #2196f3;
}
</style>
