<template>
    <div class="q-pa-md">
        <!-- Breadcrumb -->
        <q-breadcrumbs class="q-mb-md">
            <q-breadcrumbs-el
                label="Effort Home"
                :to="{ name: 'EffortHomeWithTerm', params: { termCode } }"
            />
            <q-breadcrumbs-el label="My Effort" />
        </q-breadcrumbs>

        <!-- Loading state -->
        <div
            v-if="isLoading"
            class="text-grey q-my-md"
        >
            Loading your effort data...
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
                    :to="{ name: 'EffortHomeWithTerm', params: { termCode } }"
                />
            </template>
        </q-banner>

        <!-- No instructor record for this term (empty DTO returned) -->
        <q-banner
            v-else-if="myEffort && myEffort.effortRecords.length === 0 && !myEffort.instructor.personId"
            class="bg-info text-white q-mb-md"
            rounded
        >
            <template #avatar>
                <q-icon name="school" />
            </template>
            No effort records found for you in this term.
        </q-banner>

        <!-- Content when loaded -->
        <template v-else-if="myEffort">
            <!-- Header -->
            <div class="q-mb-md row items-center justify-between">
                <h2 class="q-my-none header-title">
                    My Effort<template v-if="myEffort.termName"> for {{ myEffort.termName }}</template>
                    <a
                        href="https://ucdsvm.knowledgeowl.com/help/how-do-i-update-and-verify-my-teaching-effort"
                        target="_blank"
                        rel="noopener"
                        class="help-link text-primary lt-sm"
                        aria-label="Need help? View documentation"
                    >
                        <q-icon
                            name="help_outline"
                            size="xs"
                        />
                    </a>
                </h2>
                <a
                    href="https://ucdsvm.knowledgeowl.com/help/how-do-i-update-and-verify-my-teaching-effort"
                    target="_blank"
                    rel="noopener"
                    class="help-link text-primary gt-xs"
                    aria-label="Need help? View documentation"
                >
                    Need Help
                    <q-icon
                        name="help_outline"
                        size="xs"
                    />
                </a>
            </div>

            <!-- Add Effort and Import Course Buttons -->
            <div
                v-if="myEffort.canEdit"
                class="row q-mb-md q-gutter-sm"
            >
                <q-btn
                    color="primary"
                    icon="add"
                    label="Add Effort"
                    dense
                    aria-label="Add effort record"
                    @click="openAddDialog"
                />
                <q-btn
                    color="secondary"
                    icon="cloud_download"
                    dense
                    outline
                    aria-label="Import course from Banner"
                    @click="openImportDialog"
                >
                    <span class="lt-sm q-ml-xs">Import Course</span>
                    <span class="gt-xs q-ml-xs">Import Course from Banner</span>
                </q-btn>
            </div>

            <!-- Already verified banner -->
            <q-banner
                v-if="myEffort.instructor.isVerified"
                class="bg-positive text-white q-mb-md"
                rounded
            >
                <template #avatar>
                    <q-icon name="check_circle" />
                </template>
                Your effort was verified on {{ formatDate(myEffort.instructor.effortVerified) }}
            </q-banner>

            <!-- Zero effort warning -->
            <q-banner
                v-if="myEffort.hasZeroEffort"
                class="bg-warning q-mb-md"
                rounded
            >
                <template #avatar>
                    <q-icon
                        name="warning"
                        color="dark"
                    />
                </template>
                <div>
                    <strong>You have effort items with ZERO effort.</strong>
                    You will not be able to verify your effort until these items have been updated to document
                    hours/weeks or are removed.
                </div>
            </q-banner>

            <!-- Clinical effort note -->
            <p
                v-if="hasClinicalEffort"
                class="text-grey-8"
            >
                Enter clinical effort as weeks. Enter all other effort as hours.
            </p>

            <!-- No records message with verification option -->
            <q-banner
                v-if="myEffort.effortRecords.length === 0 && !myEffort.instructor.isVerified"
                class="bg-info text-white q-mb-md"
                rounded
            >
                <template #avatar>
                    <q-icon name="info" />
                </template>
                <div>
                    No effort records found for you in this term. If this is correct, you can verify that you had no
                    teaching effort for {{ myEffort.termName }}.
                </div>
            </q-banner>

            <!-- No records message (already verified) -->
            <div
                v-else-if="myEffort.effortRecords.length === 0"
                class="text-center text-grey q-py-lg q-mb-md"
            >
                <q-icon
                    name="school"
                    size="2em"
                    class="q-mb-sm"
                />
                <div>No effort records for this term.</div>
            </div>

            <!-- Effort Records Display -->
            <EffortRecordsDisplay
                v-if="myEffort.effortRecords.length > 0"
                :records="myEffort.effortRecords"
                :term-code="termCode"
                :can-edit="myEffort.canEdit"
                :can-delete="myEffort.canEdit"
                :zero-effort-record-ids="myEffort.zeroEffortRecordIds"
                no-data-message="No effort records for this term"
                @edit="openEditDialog"
                @delete="confirmDelete"
            />

            <!-- Cross-Listed / Sectioned Courses Section -->
            <div
                v-if="myEffort.crossListedCourses.length > 0"
                class="q-mb-lg"
            >
                <h4 class="q-mt-none q-mb-sm">Cross-Listed / Sectioned Courses</h4>
                <q-table
                    :rows="myEffort.crossListedCourses"
                    :columns="childColumns"
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
                </q-table>
            </div>

            <!-- Verification Section -->
            <div
                v-if="!myEffort.instructor.isVerified"
                class="q-mt-lg"
            >
                <q-separator class="q-mb-md" />
                <h3 class="q-my-none q-mb-md">
                    {{ myEffort.effortRecords.length === 0 ? "Verify No Effort" : "Verify Effort" }}
                </h3>

                <div
                    v-if="myEffort.canVerify"
                    class="q-gutter-sm"
                >
                    <q-checkbox
                        v-model="verifyConfirmed"
                        :label="
                            myEffort.effortRecords.length === 0
                                ? 'I verify that I had no teaching effort for this term'
                                : 'I verify that the information above accurately represents my effort'
                        "
                    />
                    <div class="q-mt-md">
                        <q-btn
                            label="Submit Verification"
                            color="primary"
                            :disable="!verifyConfirmed"
                            :loading="isVerifying"
                            @click="submitVerification"
                        />
                    </div>
                </div>
                <div
                    v-else-if="!myEffort.hasVerifyPermission"
                    class="text-grey-7"
                >
                    You do not have permission to verify effort. Please contact your department administrator.
                </div>
                <div
                    v-else
                    class="text-grey-7"
                >
                    Please resolve the zero-effort items above before verifying.
                </div>
            </div>
        </template>

        <!-- Add Effort Dialog -->
        <EffortRecordAddDialog
            v-model="showAddDialog"
            :person-id="myEffort?.instructor?.personId ?? 0"
            :term-code="termCodeNum"
            :is-verified="myEffort?.instructor?.isVerified ?? false"
            :pre-selected-course-id="preSelectedCourseId"
            :existing-records="myEffort?.effortRecords ?? []"
            @created="onRecordCreated"
        />

        <!-- Edit Effort Dialog -->
        <EffortRecordEditDialog
            v-model="showEditDialog"
            :record="selectedRecord"
            :term-code="termCodeNum"
            @updated="onRecordUpdated"
        />

        <!-- Import Course Dialog -->
        <CourseImportForSelfDialog
            v-model="showImportDialog"
            :term-code="termCodeNum"
            :term-name="myEffort?.termName ?? ''"
            @imported="onCourseImported"
        />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRoute } from "vue-router"
import { useQuasar, type QTableColumn } from "quasar"
import { verificationService } from "../services/verification-service"
import { recordService } from "../services/record-service"
import type { MyEffortDto, InstructorEffortRecordDto, ChildCourseDto, EffortTypeOptionDto } from "../types"
import { VerificationErrorCodes } from "../types"
import EffortRecordAddDialog from "../components/EffortRecordAddDialog.vue"
import EffortRecordEditDialog from "../components/EffortRecordEditDialog.vue"
import CourseImportForSelfDialog from "../components/CourseImportForSelfDialog.vue"
import EffortRecordsDisplay from "../components/EffortRecordsDisplay.vue"

const route = useRoute()
const $q = useQuasar()

// Route params
const termCode = computed(() => route.params.termCode as string)
const termCodeNum = computed(() => parseInt(termCode.value, 10))

// State
const myEffort = ref<MyEffortDto | null>(null)
const effortTypes = ref<EffortTypeOptionDto[]>([])
const isLoading = ref(true)
const loadError = ref<string | null>(null)
const verifyConfirmed = ref(false)
const isVerifying = ref(false)

// Dialog state
const showAddDialog = ref(false)
const showEditDialog = ref(false)
const showImportDialog = ref(false)
const selectedRecord = ref<InstructorEffortRecordDto | null>(null)
const preSelectedCourseId = ref<number | null>(null)

// Computed
const hasClinicalEffort = computed(() => {
    return myEffort.value?.effortRecords.some((r) => r.effortType === "CLI") ?? false
})

const childColumns: QTableColumn[] = [
    {
        name: "course",
        label: "Course",
        field: (row: ChildCourseDto) => `${row.subjCode} ${row.crseNumb.trim()}-${row.seqNumb}`,
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
        name: "relationshipType",
        label: "Type",
        field: "relationshipType",
        align: "center",
    },
]

// Methods
function formatDate(dateString: string | null): string {
    if (!dateString) return ""
    const date = new Date(dateString)
    return date.toLocaleString("en-US", {
        month: "numeric",
        day: "numeric",
        year: "numeric",
        hour: "numeric",
        minute: "2-digit",
    })
}

function openAddDialog() {
    showAddDialog.value = true
}

function openEditDialog(record: InstructorEffortRecordDto) {
    selectedRecord.value = record
    showEditDialog.value = true
}

function openImportDialog() {
    showImportDialog.value = true
}

function onCourseImported(courseId: number) {
    // After importing a course, open the Add Effort dialog with the course pre-selected
    preSelectedCourseId.value = courseId
    showAddDialog.value = true
}

/**
 * Checks if the effort type is restricted for the given course classification.
 * A restricted type is one that cannot be re-added after deletion because it's
 * not allowed on the course's category (DVM, 199/299, or R-course).
 * Uses AND logic: check ALL applicable classifications.
 */
function isRestrictedEffortType(record: InstructorEffortRecordDto): boolean {
    const effortType = effortTypes.value.find((et) => et.id === record.effortType)
    if (!effortType) return false

    const course = record.course
    // Check each classification - if course has it AND type is not allowed, it's restricted
    if (course.isDvm && !effortType.allowedOnDvm) return true
    if (course.is199299 && !effortType.allowedOn199299) return true
    if (course.isRCourse && !effortType.allowedOnRCourses) return true
    return false
}

function confirmDelete(record: InstructorEffortRecordDto) {
    const isRestricted = isRestrictedEffortType(record)
    const effortType = effortTypes.value.find((et) => et.id === record.effortType)
    const effortTypeDesc = effortType?.description ?? record.effortType

    let message = `Are you sure you want to delete the effort record for ${record.course.subjCode} ${record.course.crseNumb.trim()} (${effortTypeDesc})?`

    if (isRestricted) {
        message =
            `Warning: This record uses effort type "${effortTypeDesc}" which is restricted for this course type. ` +
            `If you delete it, you will not be able to add it back with the same effort type. ` +
            `Are you sure you want to delete this effort record?`
    }

    $q.dialog({
        title: isRestricted ? "Delete Restricted Effort Record" : "Delete Effort Record",
        message,
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
            await loadData()
        } else {
            $q.notify({
                type: "negative",
                message: result.error ?? "Failed to delete effort record",
            })
            // Reload data if it was a concurrency conflict
            if (result.isConflict) {
                await loadData()
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
    $q.notify({
        type: "positive",
        message: "Effort record created successfully",
    })
    // Reset pre-selected course after creation
    preSelectedCourseId.value = null
    await loadData()
}

async function onRecordUpdated() {
    $q.notify({
        type: "positive",
        message: "Effort record updated successfully",
    })
    await loadData()
}

async function submitVerification() {
    if (!verifyConfirmed.value) return

    isVerifying.value = true
    try {
        const result = await verificationService.verifyEffort(termCodeNum.value)

        if (result.success) {
            $q.notify({
                type: "positive",
                message: "Your effort has been verified successfully!",
                timeout: 5000,
            })
            // Reload to show updated verification status
            await loadData()
        } else {
            // Handle specific error codes
            let errorMessage = result.errorMessage ?? "Failed to verify effort"

            if (result.errorCode === VerificationErrorCodes.ZERO_EFFORT && result.zeroEffortCourses) {
                errorMessage = `Cannot verify: The following courses have zero effort: ${result.zeroEffortCourses.join(", ")}`
            } else if (result.errorCode === VerificationErrorCodes.ALREADY_VERIFIED) {
                errorMessage = "Your effort has already been verified."
                // Reload anyway to update the UI
                await loadData()
            }

            $q.notify({
                type: "negative",
                message: errorMessage,
                timeout: 8000,
            })
        }
    } catch {
        $q.notify({
            type: "negative",
            message: "An error occurred while verifying. Please try again.",
        })
    } finally {
        isVerifying.value = false
        verifyConfirmed.value = false
    }
}

async function loadData() {
    isLoading.value = true
    loadError.value = null

    try {
        const [effortResult, typesResult] = await Promise.all([
            verificationService.getMyEffort(termCodeNum.value),
            recordService.getEffortTypeOptions(),
        ])
        if (!effortResult) {
            loadError.value = "Failed to load your effort data. Please try again."
            return
        }
        myEffort.value = effortResult
        effortTypes.value = typesResult
    } catch {
        loadError.value = "Failed to load your effort data. Please try again."
    } finally {
        isLoading.value = false
    }
}

onMounted(loadData)
</script>

<style scoped>
.header-title {
    display: inline-flex;
    align-items: center;
}

.help-link {
    margin-left: 8px;
    display: inline-flex;
    align-items: center;
    text-decoration: none;
    font-size: 0.9rem;
    font-weight: normal;
    gap: 4px;
    padding: 6px 12px;
    border-radius: 4px;
    transition: background-color 0.2s ease;
}

.help-link:hover {
    background-color: rgba(0, 95, 153, 0.1);
}
</style>
