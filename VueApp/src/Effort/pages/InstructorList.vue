<template>
    <div class="q-pa-md">
        <h2>Instructor List for {{ currentTermName }}</h2>

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
                <q-btn
                    v-if="hasImportInstructor"
                    color="primary"
                    label="Add Instructor"
                    icon="add"
                    dense
                    class="full-width-xs"
                    @click="showAddDialog = true"
                />
            </div>
        </div>

        <!-- Loading state -->
        <div
            v-if="isLoading"
            class="text-grey q-my-md"
        >
            Loading instructors...
        </div>

        <!-- Grouped instructors by department -->
        <template v-else-if="groupedInstructors.length > 0">
            <div
                v-for="deptGroup in groupedInstructors"
                :key="deptGroup.dept"
                class="q-mb-sm"
            >
                <!-- Department header with bulk email button -->
                <div class="dept-header text-white q-pa-sm row items-center">
                    <span class="text-weight-bold">{{ deptGroup.dept }}</span>
                    <q-space />
                    <q-btn
                        v-if="getEmailableCount(deptGroup) > 0"
                        icon="mail_outline"
                        :label="`Email ${getEmailableCount(deptGroup)} Unverified ${inflect('Instructor', getEmailableCount(deptGroup))}`"
                        dense
                        flat
                        size="sm"
                        color="white"
                        no-caps
                        :disable="bulkEmailDept === deptGroup.dept"
                        :loading="bulkEmailDept === deptGroup.dept"
                        @click="confirmBulkEmail(deptGroup)"
                    />
                </div>

                <!-- Instructors table for this department -->
                <q-table
                    :rows="deptGroup.instructors"
                    :columns="columns"
                    row-key="personId"
                    dense
                    flat
                    bordered
                    hide-pagination
                    :rows-per-page-options="[0]"
                    class="dept-table"
                >
                    <template #header-cell-email="props">
                        <q-th :props="props">
                            <span class="email-header-content">
                                {{ props.col.label }}
                                <q-icon
                                    name="help_outline"
                                    size="xs"
                                    class="cursor-pointer"
                                >
                                    <q-tooltip
                                        class="email-legend-tooltip bg-white text-dark shadow-4"
                                        anchor="bottom middle"
                                        self="top middle"
                                    >
                                        <div class="text-subtitle2 q-mb-sm text-dark">Email Status Legend</div>
                                        <div class="legend-item">
                                            <q-icon
                                                name="mail"
                                                color="primary"
                                                size="xs"
                                            />
                                            <span>Never emailed (has effort)</span>
                                        </div>
                                        <div class="legend-item">
                                            <q-icon
                                                name="mail"
                                                color="warning"
                                                size="xs"
                                            />
                                            <span>Never emailed (no effort)</span>
                                        </div>
                                        <div class="legend-item">
                                            <q-icon
                                                name="mark_email_unread"
                                                color="warning"
                                                size="xs"
                                            />
                                            <span>Emailed within 7 days</span>
                                        </div>
                                        <div class="legend-item">
                                            <q-icon
                                                name="mark_email_unread"
                                                color="negative"
                                                size="xs"
                                            />
                                            <span>Emailed, past deadline</span>
                                        </div>
                                        <div class="legend-item">
                                            <q-icon
                                                name="mark_email_read"
                                                color="positive"
                                                size="xs"
                                            />
                                            <span>Already verified</span>
                                        </div>
                                    </q-tooltip>
                                </q-icon>
                            </span>
                        </q-th>
                    </template>
                    <template #body-cell-isVerified="props">
                        <q-td :props="props">
                            <!-- Verified with effort records -->
                            <q-icon
                                v-if="props.row.isVerified && props.row.recordCount > 0"
                                name="check"
                                color="positive"
                                size="sm"
                            >
                                <q-tooltip>Effort verified</q-tooltip>
                            </q-icon>
                            <!-- Verified no effort -->
                            <q-icon
                                v-else-if="props.row.isVerified && props.row.recordCount === 0"
                                name="check"
                                color="info"
                                size="sm"
                            >
                                <q-tooltip>Verified no effort</q-tooltip>
                            </q-icon>
                            <!-- Unverified with no records - show dash -->
                            <span
                                v-else-if="props.row.recordCount === 0"
                                class="text-grey-6"
                            >
                                --
                                <q-tooltip>No effort records - can verify "no effort"</q-tooltip>
                            </span>
                            <!-- Unverified with records - no icon needed -->
                        </q-td>
                    </template>
                    <template #body-cell-fullName="props">
                        <q-td :props="props">
                            <div class="instructor-info">
                                <router-link
                                    :to="{
                                        name: 'InstructorDetail',
                                        params: {
                                            termCode: selectedTermCode,
                                            personId: props.row.personId,
                                        },
                                    }"
                                    class="text-weight-medium instructor-name"
                                >
                                    {{ props.row.fullName }}
                                </router-link>
                                <q-badge
                                    v-if="props.row.recordCount === 0"
                                    color="orange-8"
                                    text-color="white"
                                    class="no-effort-badge"
                                >
                                    No Effort
                                    <q-tooltip>This instructor has no effort records for this term</q-tooltip>
                                </q-badge>
                            </div>
                            <div
                                v-if="props.row.title"
                                class="text-caption title-code"
                            >
                                {{ props.row.title }}
                            </div>
                        </q-td>
                    </template>
                    <template #body-cell-email="props">
                        <q-td :props="props">
                            <!-- Already verified -->
                            <q-icon
                                v-if="props.row.isVerified"
                                name="mark_email_read"
                                color="positive"
                                size="xs"
                            >
                                <q-tooltip>Already verified</q-tooltip>
                            </q-icon>
                            <!-- Loading spinner while sending (individual or bulk for this dept) -->
                            <q-spinner
                                v-else-if="
                                    sendingEmailFor === props.row.personId || bulkEmailDept === props.row.effortDept
                                "
                                color="primary"
                                size="xs"
                            />
                            <!-- Emailed past deadline, not verified -->
                            <q-icon
                                v-else-if="isEmailedPastDeadline(props.row)"
                                name="mark_email_unread"
                                color="negative"
                                size="xs"
                                class="cursor-pointer"
                                tabindex="0"
                                role="button"
                                :aria-label="`Resend verification email to ${props.row.fullName} (past deadline)`"
                                @click="confirmResendEmail(props.row)"
                                @keydown.enter.prevent="confirmResendEmail(props.row)"
                                @keydown.space.prevent="confirmResendEmail(props.row)"
                            >
                                <q-tooltip>{{ getEmailTooltip(props.row) }}</q-tooltip>
                            </q-icon>
                            <!-- Emailed within deadline, not verified -->
                            <q-icon
                                v-else-if="props.row.lastEmailedDate"
                                name="mark_email_unread"
                                color="warning"
                                size="xs"
                                class="cursor-pointer"
                                tabindex="0"
                                role="button"
                                :aria-label="`Resend verification email to ${props.row.fullName}`"
                                @click="confirmResendEmail(props.row)"
                                @keydown.enter.prevent="confirmResendEmail(props.row)"
                                @keydown.space.prevent="confirmResendEmail(props.row)"
                            >
                                <q-tooltip>{{ getEmailTooltip(props.row) }}</q-tooltip>
                            </q-icon>
                            <!-- Never emailed, not verified -->
                            <q-icon
                                v-else
                                name="mail"
                                :color="props.row.recordCount === 0 ? 'warning' : 'primary'"
                                size="xs"
                                class="cursor-pointer"
                                tabindex="0"
                                role="button"
                                :aria-label="
                                    props.row.recordCount === 0
                                        ? `Send no-effort verification email to ${props.row.fullName}`
                                        : `Send verification email to ${props.row.fullName}`
                                "
                                @click="sendVerificationEmail(props.row)"
                                @keydown.enter.prevent="sendVerificationEmail(props.row)"
                                @keydown.space.prevent="sendVerificationEmail(props.row)"
                            >
                                <q-tooltip>
                                    {{
                                        props.row.recordCount === 0
                                            ? "Send no-effort verification email"
                                            : "Send verification email"
                                    }}
                                </q-tooltip>
                            </q-icon>
                        </q-td>
                    </template>
                    <template #body-cell-actions="props">
                        <q-td :props="props">
                            <q-btn
                                v-if="hasEditInstructor"
                                icon="edit"
                                color="primary"
                                dense
                                flat
                                round
                                size="sm"
                                :aria-label="`Edit ${props.row.fullName}`"
                                @click="openEditDialog(props.row)"
                            >
                                <q-tooltip>Edit instructor</q-tooltip>
                            </q-btn>
                            <q-btn
                                v-if="hasDeleteInstructor"
                                icon="delete"
                                color="negative"
                                dense
                                flat
                                round
                                size="sm"
                                :aria-label="`Delete ${props.row.fullName}`"
                                @click="confirmDeleteInstructor(props.row)"
                            >
                                <q-tooltip>Delete instructor</q-tooltip>
                            </q-btn>
                        </q-td>
                    </template>
                </q-table>
            </div>
        </template>

        <!-- No data state -->
        <div
            v-else
            class="full-width row flex-center text-grey q-gutter-sm q-py-lg"
        >
            <q-icon
                name="person"
                size="2em"
            />
            <span>No instructors found for this term</span>
        </div>

        <!-- Add Dialog -->
        <InstructorAddDialog
            v-model="showAddDialog"
            :term-code="selectedTermCode"
            @created="onInstructorCreated"
        />

        <!-- Edit Dialog -->
        <InstructorEditDialog
            v-model="showEditDialog"
            :instructor="selectedInstructor"
            :term-code="selectedTermCode"
            @updated="onInstructorUpdated"
        />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from "vue"
import { useQuasar } from "quasar"
import { useRoute, useRouter, onBeforeRouteLeave } from "vue-router"
import { effortService } from "../services/effort-service"
import { termService } from "../services/term-service"
import { verificationService } from "../services/verification-service"
import { useEffortPermissions } from "../composables/use-effort-permissions"
import { useUserStore } from "@/store/UserStore"
import type { PersonDto, TermDto, BulkEmailResult } from "../types"
import type { QTableColumn } from "quasar"
import InstructorAddDialog from "../components/InstructorAddDialog.vue"
import InstructorEditDialog from "../components/InstructorEditDialog.vue"
import { inflect } from "inflection"

const $q = useQuasar()
const userStore = useUserStore()
const route = useRoute()
const router = useRouter()
const { hasImportInstructor, hasEditInstructor, hasDeleteInstructor } = useEffortPermissions()

// State
const terms = ref<TermDto[]>([])
const selectedTermCode = ref<number | null>(null)
const instructors = ref<PersonDto[]>([])
const selectedDept = ref<string | null>(null)
const searchText = ref("")
const isLoading = ref(false)

// Dialogs
const showAddDialog = ref(false)
const showEditDialog = ref(false)
const selectedInstructor = ref<PersonDto | null>(null)

// Email state
const sendingEmailFor = ref<number | null>(null)
const bulkEmailDept = ref<string | null>(null)
const verificationReplyDays = ref(7)

// Warn user if they try to leave while bulk email is in progress
function handleBeforeUnload(e: BeforeUnloadEvent) {
    if (bulkEmailDept.value) {
        e.preventDefault()
        // Modern browsers ignore custom messages, but returnValue is required
        e.returnValue = "Bulk email is in progress. Are you sure you want to leave?"
        return e.returnValue
    }
}

onMounted(() => {
    window.addEventListener("beforeunload", handleBeforeUnload)
})

onUnmounted(() => {
    window.removeEventListener("beforeunload", handleBeforeUnload)
})

onBeforeRouteLeave((_to, _from, next) => {
    if (bulkEmailDept.value) {
        $q.dialog({
            title: "Bulk Email In Progress",
            message:
                "Verification emails are still being sent. Leaving this page may interrupt the process. Are you sure you want to leave?",
            cancel: true,
            persistent: true,
        })
            .onOk(() => next())
            .onCancel(() => next(false))
    } else {
        next()
    }
})

// Computed
const currentTermName = computed(() => {
    if (!selectedTermCode.value) return ""
    const term = terms.value.find((t) => t.termCode === selectedTermCode.value)
    return term?.termName ?? ""
})

const deptOptions = computed(() => {
    const uniqueDepts = [...new Set(instructors.value.map((i) => i.effortDept))].sort()
    return ["", ...uniqueDepts]
})

const filteredInstructors = computed(() => {
    let result = instructors.value

    if (selectedDept.value) {
        result = result.filter((i) => i.effortDept === selectedDept.value)
    }

    if (searchText.value) {
        const search = searchText.value.toLowerCase()
        result = result.filter(
            (i) =>
                i.fullName.toLowerCase().includes(search) ||
                i.firstName.toLowerCase().includes(search) ||
                i.lastName.toLowerCase().includes(search),
        )
    }

    return result
})

// Group instructors by department
const groupedInstructors = computed(() => {
    const groups: Record<string, PersonDto[]> = {}

    for (const instructor of filteredInstructors.value) {
        const dept = instructor.effortDept || "UNK"
        const groupArray = groups[dept] ?? (groups[dept] = [])
        groupArray.push(instructor)
    }

    // Sort departments and return as array with guaranteed instructor arrays
    return Object.keys(groups)
        .sort()
        .map((dept) => ({
            dept,
            instructors: groups[dept] ?? [],
        }))
})

const columns = computed<QTableColumn[]>(() => [
    {
        name: "isVerified",
        label: "Verified",
        field: "isVerified",
        align: "center",
        sortable: true,
        style: "width: 60px; min-width: 60px",
        headerStyle: "width: 60px; min-width: 60px",
    },
    {
        name: "fullName",
        label: "Instructor",
        field: "fullName",
        align: "left",
        sortable: true,
        classes: "instructor-cell",
    },
    {
        name: "email",
        label: "Email",
        field: "email",
        align: "center",
        style: "width: 50px; min-width: 50px",
        headerStyle: "width: 50px; min-width: 50px",
    },
    {
        name: "actions",
        label: "Actions",
        field: "actions",
        align: "center",
        style: "width: 70px; min-width: 70px",
        headerStyle: "width: 70px; min-width: 70px",
    },
])

// Race condition protection for async loads
let loadToken = 0

// Methods
async function loadTerms() {
    const paramTermCode = route.params.termCode ? parseInt(route.params.termCode as string, 10) : null

    const [termsResult, settings] = await Promise.all([termService.getTerms(), verificationService.getSettings()])
    terms.value = termsResult
    verificationReplyDays.value = settings.verificationReplyDays

    if (paramTermCode && terms.value.some((t) => t.termCode === paramTermCode)) {
        selectedTermCode.value = paramTermCode
        await loadInstructors()
    } else {
        router.replace({ name: "EffortHome" })
    }
}

watch(selectedTermCode, (newTermCode, oldTermCode) => {
    if (newTermCode && oldTermCode !== null) {
        router.replace({ name: "InstructorList", params: { termCode: newTermCode.toString() } })
        loadInstructors()
    }
})

watch(
    () => route.params.termCode,
    (newTermCode) => {
        const parsed = newTermCode ? parseInt(newTermCode as string, 10) : null
        if (parsed && parsed !== selectedTermCode.value && terms.value.some((t) => t.termCode === parsed)) {
            selectedTermCode.value = parsed
        }
    },
)

async function loadInstructors() {
    if (!selectedTermCode.value) return

    const token = ++loadToken
    isLoading.value = true

    try {
        const result = await effortService.getInstructors(selectedTermCode.value)

        // Abort if a newer request has been initiated
        if (token !== loadToken) return

        instructors.value = result
    } finally {
        if (token === loadToken) {
            isLoading.value = false
        }
    }
}

function openEditDialog(instructor: PersonDto) {
    selectedInstructor.value = instructor
    showEditDialog.value = true
}

function confirmDeleteInstructor(instructor: PersonDto) {
    $q.dialog({
        title: "Delete Instructor",
        message: `Are you sure you want to delete ${instructor.fullName}? This will also delete all associated effort records for this term.`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        if (!selectedTermCode.value) return

        const { recordCount } = await effortService.canDeleteInstructor(instructor.personId, selectedTermCode.value)
        if (recordCount > 0) {
            $q.dialog({
                title: "Confirm Delete",
                message: `This instructor has ${recordCount} effort ${inflect("record", recordCount)} that will also be deleted. Are you sure you want to proceed?`,
                cancel: true,
                persistent: true,
            }).onOk(() => deleteInstructor(instructor.personId))
        } else {
            await deleteInstructor(instructor.personId)
        }
    })
}

async function deleteInstructor(personId: number) {
    if (!selectedTermCode.value) return

    const success = await effortService.deleteInstructor(personId, selectedTermCode.value)
    if (success) {
        $q.notify({ type: "positive", message: "Instructor deleted successfully" })
        await loadInstructors()
    } else {
        $q.notify({ type: "negative", message: "Failed to delete instructor" })
    }
}

function onInstructorCreated() {
    $q.notify({ type: "positive", message: "Instructor added successfully" })
    loadInstructors()
}

function onInstructorUpdated() {
    $q.notify({ type: "positive", message: "Instructor updated successfully" })
    loadInstructors()
}

// Email methods
type DeptGroup = { dept: string; instructors: PersonDto[] }

function getEmailableCount(deptGroup: DeptGroup): number {
    return deptGroup.instructors.filter((i) => i.canSendVerificationEmail).length
}

function getDaysSinceEmailed(instructor: PersonDto): number {
    if (!instructor.lastEmailedDate) return -1
    const lastEmailed = new Date(instructor.lastEmailedDate)
    const now = new Date()
    return Math.floor((now.getTime() - lastEmailed.getTime()) / (1000 * 60 * 60 * 24))
}

function isEmailedPastDeadline(instructor: PersonDto): boolean {
    const daysSince = getDaysSinceEmailed(instructor)
    return daysSince > verificationReplyDays.value
}

function formatDate(dateStr: string): string {
    return new Date(dateStr).toLocaleDateString()
}

function getEmailTooltip(instructor: PersonDto): string {
    if (!instructor.lastEmailedDate) return ""

    const daysSince = getDaysSinceEmailed(instructor)
    const dateStr = formatDate(instructor.lastEmailedDate)
    const sender = instructor.lastEmailedBy ?? "Unknown"

    if (daysSince > verificationReplyDays.value) {
        const daysPast = daysSince - verificationReplyDays.value
        return `Emailed ${dateStr} by ${sender} - ${daysPast} ${inflect("day", daysPast)} past deadline`
    }
    return `Emailed ${dateStr} by ${sender}`
}

function confirmResendEmail(instructor: PersonDto) {
    const daysSince = getDaysSinceEmailed(instructor)
    const dateStr = formatDate(instructor.lastEmailedDate!)
    const sender = instructor.lastEmailedBy ?? "Unknown"

    $q.dialog({
        title: "Resend Verification Email?",
        message: `${instructor.fullName} was last emailed on ${dateStr} by ${sender} (${daysSince} ${inflect("day", daysSince)} ago). Send another email?`,
        cancel: true,
        persistent: true,
    }).onOk(() => sendVerificationEmail(instructor))
}

async function sendVerificationEmail(instructor: PersonDto) {
    if (!selectedTermCode.value) return

    sendingEmailFor.value = instructor.personId
    try {
        const result = await verificationService.sendVerificationEmail(instructor.personId, selectedTermCode.value)
        if (result.success) {
            $q.notify({
                type: "positive",
                message: `Verification email sent to ${instructor.fullName}`,
            })
            // Update the instructor locally instead of reloading all instructors
            instructor.lastEmailedDate = new Date().toISOString()
            instructor.lastEmailedBy = `${userStore.userInfo.firstName} ${userStore.userInfo.lastName}`
        } else {
            $q.notify({
                type: "negative",
                message: result.error ?? "Failed to send email",
            })
        }
    } catch {
        $q.notify({
            type: "negative",
            message: "An error occurred while sending the email",
        })
    } finally {
        sendingEmailFor.value = null
    }
}

function confirmBulkEmail(deptGroup: DeptGroup) {
    const emailableCount = getEmailableCount(deptGroup)
    $q.dialog({
        title: "Send Verification Emails",
        message: `This will send verification emails to ${emailableCount} ${inflect("instructor", emailableCount)} in ${deptGroup.dept} who haven't verified. Continue?`,
        cancel: true,
        persistent: true,
    }).onOk(() => sendBulkVerificationEmails(deptGroup.dept))
}

async function sendBulkVerificationEmails(dept: string) {
    if (!selectedTermCode.value) return

    bulkEmailDept.value = dept
    const loadingNotify = $q.notify({
        type: "ongoing",
        message: `Sending verification emails to ${dept}...`,
        spinner: true,
        timeout: 0,
    })

    try {
        const result: BulkEmailResult = await verificationService.sendBulkVerificationEmails(
            dept,
            selectedTermCode.value,
        )

        loadingNotify()

        if (result.emailsFailed === 0) {
            $q.notify({
                type: "positive",
                message: `Successfully sent ${result.emailsSent} verification ${inflect("email", result.emailsSent)}`,
                timeout: 5000,
            })
        } else {
            $q.notify({
                type: "warning",
                message: `Sent ${result.emailsSent} ${inflect("email", result.emailsSent)}, ${result.emailsFailed} failed`,
                timeout: 8000,
            })
            // Show details of failures
            if (result.failures.length > 0) {
                const failureList = result.failures.map((f) => `${f.instructorName}: ${f.reason}`).join("\n")
                $q.dialog({
                    title: "Email Failures",
                    message: `The following emails could not be sent:\n\n${failureList}`,
                })
            }
        }

        // Update instructors locally instead of reloading
        // Find all unverified instructors in this department that weren't in the failure list
        const failedPersonIds = new Set(result.failures.map((f) => f.personId))
        const now = new Date().toISOString()
        const senderName = `${userStore.userInfo.firstName} ${userStore.userInfo.lastName}`

        for (const instructor of instructors.value) {
            if (
                instructor.effortDept === dept &&
                !instructor.isVerified &&
                instructor.canSendVerificationEmail &&
                !failedPersonIds.has(instructor.personId)
            ) {
                instructor.lastEmailedDate = now
                instructor.lastEmailedBy = senderName
            }
        }
    } catch {
        loadingNotify()
        $q.notify({
            type: "negative",
            message: "An error occurred while sending bulk emails",
        })
    } finally {
        bulkEmailDept.value = null
    }
}

onMounted(loadTerms)
</script>

<style scoped>
@media (width <= 599px) {
    .full-width-xs {
        width: 100%;
    }
}

.dept-header {
    background-color: #5f6a6a;
    font-weight: bold;
}

.dept-table {
    margin-bottom: 0;
}

.dept-table :deep(table) {
    table-layout: fixed;
    width: 100%;
}

.dept-table :deep(.instructor-cell) {
    overflow: hidden;
    text-overflow: ellipsis;
    white-space: nowrap;
    max-width: 0;
}

.instructor-name {
    color: #800000;
    overflow: hidden;
    text-overflow: ellipsis;
    display: block;
}

.title-code {
    color: #666;
    font-size: 0.8em;
    text-transform: uppercase;
}

.instructor-info {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.5rem;
}

.no-effort-badge {
    flex-shrink: 0;
}

.legend-item {
    display: flex;
    align-items: center;
    gap: 0.5rem;
    padding: 0.25rem 0;
}

.email-header-content {
    display: inline-flex;
    align-items: center;
    gap: 0.125rem;
    margin-right: auto;
}
</style>

<style>
/* Unscoped style for tooltip (injected into body) */
.email-legend-tooltip {
    border: 1px solid #ccc;
}
</style>
