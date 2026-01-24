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
            <div class="q-mb-md">
                <h2 class="q-my-none q-mb-sm">
                    My Effort<template v-if="myEffort.termName"> for {{ myEffort.termName }}</template>
                </h2>
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
                    <strong>You have courses with ZERO effort.</strong>
                    You will not be able to verify your effort until these items have been updated to document
                    hours/weeks or are removed. Please work with your departmental Effort admin to resolve these issues.
                </div>
            </q-banner>

            <!-- Clinical effort note -->
            <p
                v-if="hasClinicalEffort"
                class="text-grey-8"
            >
                Enter clinical effort as weeks. Enter all other effort as hours.
            </p>

            <!-- No records message -->
            <div
                v-if="myEffort.effortRecords.length === 0"
                class="text-center text-grey q-py-lg q-mb-md"
            >
                <q-icon
                    name="school"
                    size="2em"
                    class="q-mb-sm"
                />
                <div>No effort records found for you in this term.</div>
            </div>

            <!-- Mobile Card View -->
            <div
                v-if="myEffort.effortRecords.length > 0"
                class="lt-sm q-mb-lg"
            >
                <q-card
                    v-for="record in myEffort.effortRecords"
                    :key="record.id"
                    flat
                    bordered
                    class="q-mb-sm"
                    :class="{ 'zero-effort-card': isZeroEffortRecord(record.id) }"
                >
                    <q-card-section class="q-py-sm">
                        <div class="row items-center justify-between q-mb-xs">
                            <span class="text-primary text-weight-bold">
                                {{ record.course.subjCode }}
                                {{ record.course.crseNumb.trim() }}-{{ record.course.seqNumb }}
                            </span>
                            <div>
                                <q-btn
                                    v-if="myEffort.canEdit"
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
                                    v-if="myEffort.canEdit"
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
                            <span :class="{ 'text-warning text-weight-bold': isZeroEffortRecord(record.id) }">
                                {{ record.effortValue ?? 0 }}
                                {{ record.effortLabel === "weeks" ? "Weeks" : "Hours" }}
                                <template v-if="isZeroEffortRecord(record.id)"> *** ZERO EFFORT *** </template>
                            </span>
                        </div>
                    </q-card-section>
                </q-card>
            </div>

            <!-- Effort Records Table (desktop) -->
            <q-table
                v-if="myEffort.effortRecords.length > 0"
                :rows="myEffort.effortRecords"
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
                        {{ props.row.course.subjCode }}
                        {{ props.row.course.crseNumb.trim() }}-{{ props.row.course.seqNumb }}
                    </q-td>
                </template>
                <template #body-cell-effort="props">
                    <q-td
                        :props="props"
                        :class="{ 'zero-effort': isZeroEffortRecord(props.row.id) }"
                    >
                        {{ props.row.effortValue ?? 0 }}
                        {{ props.row.effortLabel === "weeks" ? "Weeks" : "Hours" }}
                        <span
                            v-if="isZeroEffortRecord(props.row.id)"
                            class="text-weight-bold"
                        >
                            *** ZERO ***
                        </span>
                    </q-td>
                </template>
                <template #body-cell-actions="props">
                    <q-td :props="props">
                        <q-btn
                            v-if="myEffort?.canEdit"
                            flat
                            dense
                            round
                            icon="edit"
                            color="primary"
                            size="sm"
                            aria-label="Edit effort record"
                            @click="openEditDialog(props.row)"
                        >
                            <q-tooltip>Edit</q-tooltip>
                        </q-btn>
                        <q-btn
                            v-if="myEffort?.canEdit"
                            flat
                            dense
                            round
                            icon="delete"
                            color="negative"
                            size="sm"
                            aria-label="Delete effort record"
                            @click="confirmDelete(props.row)"
                        >
                            <q-tooltip>Delete</q-tooltip>
                        </q-btn>
                    </q-td>
                </template>
            </q-table>

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
                v-if="!myEffort.instructor.isVerified && myEffort.effortRecords.length > 0"
                class="q-mt-lg"
            >
                <q-separator class="q-mb-md" />
                <h3 class="q-my-none q-mb-md">Verify Effort</h3>

                <div
                    v-if="myEffort.canVerify"
                    class="q-gutter-sm"
                >
                    <q-checkbox
                        v-model="verifyConfirmed"
                        label="I verify that the information above accurately represents my effort"
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

        <!-- Edit Effort Dialog -->
        <EffortRecordEditDialog
            v-model="showEditDialog"
            :record="selectedRecord"
            :term-code="termCodeNum"
            @updated="onRecordUpdated"
        />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRoute } from "vue-router"
import { useQuasar, type QTableColumn } from "quasar"
import { verificationService } from "../services/verification-service"
import { effortService } from "../services/effort-service"
import type { MyEffortDto, InstructorEffortRecordDto, ChildCourseDto } from "../types"
import { VerificationErrorCodes } from "../types"
import EffortRecordEditDialog from "../components/EffortRecordEditDialog.vue"

const route = useRoute()
const $q = useQuasar()

// Route params
const termCode = computed(() => route.params.termCode as string)
const termCodeNum = computed(() => parseInt(termCode.value, 10))

// State
const myEffort = ref<MyEffortDto | null>(null)
const isLoading = ref(true)
const loadError = ref<string | null>(null)
const verifyConfirmed = ref(false)
const isVerifying = ref(false)

// Dialog state
const showEditDialog = ref(false)
const selectedRecord = ref<InstructorEffortRecordDto | null>(null)

// Computed
const hasClinicalEffort = computed(() => {
    return myEffort.value?.effortRecords.some((r) => r.effortType === "CLI") ?? false
})

const columns = computed<QTableColumn[]>(() => {
    const cols: QTableColumn[] = [
        {
            name: "course",
            label: "Course",
            field: (row: InstructorEffortRecordDto) =>
                `${row.course.subjCode} ${row.course.crseNumb.trim()}-${row.course.seqNumb}`,
            align: "left",
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

    // Add actions column if user can edit
    if (myEffort.value?.canEdit) {
        cols.push({
            name: "actions",
            label: "Actions",
            field: "id",
            align: "center",
        })
    }

    return cols
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
function isZeroEffortRecord(recordId: number): boolean {
    return myEffort.value?.zeroEffortRecordIds.includes(recordId) ?? false
}

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

function openEditDialog(record: InstructorEffortRecordDto) {
    selectedRecord.value = record
    showEditDialog.value = true
}

function confirmDelete(record: InstructorEffortRecordDto) {
    $q.dialog({
        title: "Delete Effort Record",
        message: `Are you sure you want to delete the effort record for ${record.course.subjCode} ${record.course.crseNumb.trim()} (${record.effortType})?`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        await deleteRecord(record.id)
    })
}

async function deleteRecord(recordId: number) {
    try {
        const success = await effortService.deleteEffortRecord(recordId)
        if (success) {
            $q.notify({
                type: "positive",
                message: "Effort record deleted successfully",
            })
            await loadData()
        } else {
            $q.notify({
                type: "negative",
                message: "Failed to delete effort record",
            })
        }
    } catch {
        $q.notify({
            type: "negative",
            message: "An error occurred while deleting the record",
        })
    }
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
        const result = await verificationService.getMyEffort(termCodeNum.value)
        if (!result) {
            loadError.value = "Failed to load your effort data. Please try again."
            return
        }
        myEffort.value = result
    } catch {
        loadError.value = "Failed to load your effort data. Please try again."
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
</style>
