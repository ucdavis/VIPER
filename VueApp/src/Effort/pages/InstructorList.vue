<template>
    <div class="q-pa-md">
        <h2>Instructor List for {{ currentTermName }}</h2>

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
                <div
                    class="dept-header text-white q-pa-sm row items-center"
                    :class="{
                        'dept-header--no-dept': deptGroup.dept === 'No Department',
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
                    <span class="text-weight-bold">{{ deptGroup.dept }} ({{ deptGroup.instructors.length }})</span>
                    <q-space />
                    <q-btn
                        v-if="getEmailableCount(deptGroup) > 0"
                        icon="mail_outline"
                        :label="`Email ${getEmailableCount(deptGroup)} Unverified ${inflect('Instructor', getEmailableCount(deptGroup))}`"
                        flat
                        size="0.75rem"
                        color="white"
                        no-caps
                        :disable="sendingEmailDepts.has(deptGroup.dept)"
                        :loading="sendingEmailDepts.has(deptGroup.dept)"
                        @click="confirmBulkEmail(deptGroup)"
                    />
                </div>

                <!-- Mobile card view -->
                <div
                    v-if="$q.screen.lt.sm"
                    v-show="!collapsedDepts.has(deptGroup.dept)"
                    class="instructor-cards"
                >
                    <q-card
                        v-for="instructor in deptGroup.instructors"
                        :key="instructor.personId"
                        flat
                        bordered
                        class="instructor-card"
                    >
                        <q-card-section class="q-pa-sm">
                            <div class="row items-center no-wrap q-mb-xs">
                                <!-- Verified status -->
                                <q-icon
                                    v-if="instructor.isVerified && instructor.recordCount > 0"
                                    name="check"
                                    color="positive"
                                    size="sm"
                                    class="q-mr-sm"
                                >
                                    <q-tooltip>Effort verified</q-tooltip>
                                </q-icon>
                                <q-icon
                                    v-else-if="instructor.isVerified && instructor.recordCount === 0"
                                    name="check"
                                    color="info"
                                    size="sm"
                                    class="q-mr-sm"
                                >
                                    <q-tooltip>Verified no effort</q-tooltip>
                                </q-icon>
                                <!-- Name and badges -->
                                <div class="col card-name-badges">
                                    <router-link
                                        :to="{
                                            name: 'InstructorDetail',
                                            params: {
                                                termCode: selectedTermCode,
                                                personId: instructor.personId,
                                            },
                                        }"
                                        class="text-weight-bold instructor-name"
                                    >
                                        {{ instructor.fullName }}
                                    </router-link>
                                    <q-badge
                                        v-if="instructor.recordCount === 0"
                                        color="orange-8"
                                        text-color="white"
                                    >
                                        No Effort
                                    </q-badge>
                                    <q-badge
                                        v-else-if="instructor.hasZeroHourRecords"
                                        color="negative"
                                        text-color="white"
                                    >
                                        0 Hours
                                    </q-badge>
                                    <div
                                        v-if="instructor.title"
                                        class="text-caption text-grey-7 card-title"
                                    >
                                        {{ instructor.title }}
                                    </div>
                                </div>
                                <!-- Actions -->
                                <div class="card-actions">
                                    <q-btn
                                        v-if="instructor.isVerified"
                                        icon="mark_email_read"
                                        color="positive"
                                        flat
                                        round
                                        size="0.75rem"
                                        disable
                                    />
                                    <q-btn
                                        v-else-if="
                                            sendingEmailPersonIds.has(instructor.personId) ||
                                            sendingEmailDepts.has(instructor.effortDept)
                                        "
                                        flat
                                        round
                                        size="0.75rem"
                                        disable
                                    >
                                        <q-spinner
                                            color="primary"
                                            size="sm"
                                        />
                                    </q-btn>
                                    <q-btn
                                        v-else-if="isEmailedPastDeadline(instructor)"
                                        icon="mark_email_unread"
                                        color="negative"
                                        flat
                                        round
                                        size="0.75rem"
                                        @click="confirmResendEmail(instructor)"
                                    />
                                    <q-btn
                                        v-else-if="instructor.lastEmailedDate"
                                        icon="mark_email_unread"
                                        color="warning"
                                        flat
                                        round
                                        size="0.75rem"
                                        @click="confirmResendEmail(instructor)"
                                    />
                                    <q-btn
                                        v-else
                                        icon="mail"
                                        :color="instructor.recordCount === 0 ? 'warning' : 'primary'"
                                        flat
                                        round
                                        size="0.75rem"
                                        @click="sendVerificationEmail(instructor)"
                                    />
                                    <!-- View effort records -->
                                    <q-btn
                                        :icon="getEffortIcon(instructor)"
                                        :color="getEffortIconColor(instructor)"
                                        flat
                                        round
                                        size="0.75rem"
                                        :to="{
                                            name: 'InstructorDetail',
                                            params: {
                                                termCode: selectedTermCode,
                                                personId: instructor.personId,
                                            },
                                        }"
                                    />
                                    <!-- Edit -->
                                    <q-btn
                                        v-if="hasEditInstructor"
                                        icon="edit"
                                        color="primary"
                                        flat
                                        round
                                        size="0.75rem"
                                        :to="{
                                            name: 'InstructorEdit',
                                            params: {
                                                termCode: selectedTermCode?.toString(),
                                                personId: instructor.personId.toString(),
                                            },
                                        }"
                                    />
                                    <!-- Delete -->
                                    <q-btn
                                        v-if="hasDeleteInstructor"
                                        icon="delete"
                                        color="negative"
                                        flat
                                        round
                                        size="0.75rem"
                                        @click="confirmDeleteInstructor(instructor)"
                                    />
                                </div>
                            </div>
                            <!-- Percentage summaries -->
                            <div
                                v-if="
                                    instructor.percentAdminSummary ||
                                    instructor.percentClinicalSummary ||
                                    instructor.percentOtherSummary
                                "
                                class="q-mt-xs text-caption card-percentages"
                            >
                                <div
                                    v-if="instructor.percentAdminSummary"
                                    class="percent-line"
                                >
                                    <span class="text-weight-medium">Admin:</span> {{ instructor.percentAdminSummary }}
                                </div>
                                <div
                                    v-if="instructor.percentClinicalSummary"
                                    class="percent-line"
                                >
                                    <span class="text-weight-medium">Clinical:</span>
                                    {{ instructor.percentClinicalSummary }}
                                </div>
                                <div
                                    v-if="instructor.percentOtherSummary"
                                    class="percent-line"
                                >
                                    <span class="text-weight-medium">Other:</span> {{ instructor.percentOtherSummary }}
                                </div>
                            </div>
                        </q-card-section>
                    </q-card>
                </div>

                <!-- Desktop table view -->
                <q-table
                    v-else
                    v-show="!collapsedDepts.has(deptGroup.dept)"
                    :rows="deptGroup.instructors"
                    :columns="columns"
                    row-key="personId"
                    dense
                    flat
                    bordered
                    hide-pagination
                    :rows-per-page-options="[0]"
                    class="dept-table"
                    :class="{ 'dept-table--no-dept': deptGroup.dept === 'No Department' }"
                >
                    <template #header-cell-actions="props">
                        <q-th :props="props">
                            <span class="actions-header-content">
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
                                        <div class="text-subtitle2 q-mb-sm text-dark">Actions Legend</div>
                                        <div class="legend-section-title">Email Status</div>
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
                                            <span>Emailed within {{ verificationReplyDays }} days</span>
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
                                        <div class="legend-section-title">Effort Records</div>
                                        <div class="legend-item">
                                            <q-icon
                                                name="work_history"
                                                color="primary"
                                                size="xs"
                                            />
                                            <span>Has effort records</span>
                                        </div>
                                        <div class="legend-item">
                                            <q-icon
                                                name="work_alert"
                                                color="warning"
                                                size="xs"
                                            />
                                            <span>Has no effort records (can verify)</span>
                                        </div>
                                        <div class="legend-item">
                                            <q-icon
                                                name="work_alert"
                                                color="negative"
                                                size="xs"
                                            />
                                            <span>Has 0-hour items (cannot verify)</span>
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
                                size="xs"
                            >
                                <q-tooltip>Effort verified</q-tooltip>
                            </q-icon>
                            <!-- Verified no effort -->
                            <q-icon
                                v-else-if="props.row.isVerified && props.row.recordCount === 0"
                                name="check"
                                color="info"
                                size="xs"
                            >
                                <q-tooltip>Verified no effort</q-tooltip>
                            </q-icon>
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
                                <q-badge
                                    v-else-if="props.row.hasZeroHourRecords"
                                    color="negative"
                                    text-color="white"
                                    class="no-effort-badge"
                                >
                                    Item with 0 Hours
                                    <q-tooltip>This instructor has effort items with 0 hours - cannot verify</q-tooltip>
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
                    <template #body-cell-actions="props">
                        <q-td :props="props">
                            <div class="actions-cell">
                                <!-- Email action: Already verified -->
                                <q-btn
                                    v-if="props.row.isVerified"
                                    icon="mark_email_read"
                                    color="positive"
                                    flat
                                    round
                                    size="0.75rem"
                                    disable
                                    :aria-label="`${props.row.fullName} already verified`"
                                >
                                    <q-tooltip>Already verified</q-tooltip>
                                </q-btn>
                                <!-- Email action: Loading spinner while sending -->
                                <q-btn
                                    v-else-if="
                                        sendingEmailPersonIds.has(props.row.personId) ||
                                        sendingEmailDepts.has(props.row.effortDept)
                                    "
                                    flat
                                    round
                                    size="0.75rem"
                                    disable
                                    :aria-label="`Sending email to ${props.row.fullName}`"
                                >
                                    <q-spinner
                                        color="primary"
                                        size="0.75rem"
                                    />
                                </q-btn>
                                <!-- Email action: Emailed past deadline, not verified -->
                                <q-btn
                                    v-else-if="isEmailedPastDeadline(props.row)"
                                    icon="mark_email_unread"
                                    color="negative"
                                    flat
                                    round
                                    size="0.75rem"
                                    :aria-label="`Resend verification email to ${props.row.fullName} (past deadline)`"
                                    @click="confirmResendEmail(props.row)"
                                >
                                    <q-tooltip>{{ getEmailTooltip(props.row) }}</q-tooltip>
                                </q-btn>
                                <!-- Email action: Emailed within deadline, not verified -->
                                <q-btn
                                    v-else-if="props.row.lastEmailedDate"
                                    icon="mark_email_unread"
                                    color="warning"
                                    flat
                                    round
                                    size="0.75rem"
                                    :aria-label="`Resend verification email to ${props.row.fullName}`"
                                    @click="confirmResendEmail(props.row)"
                                >
                                    <q-tooltip>{{ getEmailTooltip(props.row) }}</q-tooltip>
                                </q-btn>
                                <!-- Email action: Never emailed, not verified -->
                                <q-btn
                                    v-else
                                    icon="mail"
                                    :color="props.row.recordCount === 0 ? 'warning' : 'primary'"
                                    flat
                                    round
                                    size="0.75rem"
                                    :aria-label="
                                        props.row.recordCount === 0
                                            ? `Send no-effort verification email to ${props.row.fullName}`
                                            : `Send verification email to ${props.row.fullName}`
                                    "
                                    @click="sendVerificationEmail(props.row)"
                                >
                                    <q-tooltip>
                                        {{
                                            props.row.recordCount === 0
                                                ? "Send no-effort verification email"
                                                : "Send verification email"
                                        }}
                                    </q-tooltip>
                                </q-btn>
                                <!-- View effort records action -->
                                <q-btn
                                    :icon="getEffortIcon(props.row)"
                                    :color="getEffortIconColor(props.row)"
                                    flat
                                    round
                                    size="0.75rem"
                                    :aria-label="`View effort records for ${props.row.fullName}`"
                                    :to="{
                                        name: 'InstructorDetail',
                                        params: {
                                            termCode: selectedTermCode,
                                            personId: props.row.personId,
                                        },
                                    }"
                                >
                                    <q-tooltip>{{ getEffortTooltip(props.row) }}</q-tooltip>
                                </q-btn>
                                <!-- Edit action -->
                                <q-btn
                                    v-if="hasEditInstructor"
                                    icon="edit"
                                    color="primary"
                                    flat
                                    round
                                    size="0.75rem"
                                    :aria-label="`Edit ${props.row.fullName}`"
                                    :to="{
                                        name: 'InstructorEdit',
                                        params: {
                                            termCode: selectedTermCode?.toString(),
                                            personId: props.row.personId.toString(),
                                        },
                                    }"
                                >
                                    <q-tooltip>Edit instructor</q-tooltip>
                                </q-btn>
                                <!-- Delete action -->
                                <q-btn
                                    v-if="hasDeleteInstructor"
                                    icon="delete"
                                    color="negative"
                                    flat
                                    round
                                    size="0.75rem"
                                    :aria-label="`Delete ${props.row.fullName}`"
                                    @click="confirmDeleteInstructor(props.row)"
                                >
                                    <q-tooltip>Delete instructor</q-tooltip>
                                </q-btn>
                            </div>
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
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, onUnmounted, watch } from "vue"
import { useQuasar } from "quasar"
import { useRoute, useRouter, onBeforeRouteLeave } from "vue-router"
import { instructorService } from "../services/instructor-service"
import { termService } from "../services/term-service"
import { verificationService } from "../services/verification-service"
import { useEffortPermissions } from "../composables/use-effort-permissions"
import { useUserStore } from "@/store/UserStore"
import type { PersonDto, TermDto, BulkEmailResult } from "../types"
import type { QTableColumn } from "quasar"
import InstructorAddDialog from "../components/InstructorAddDialog.vue"
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
const collapsedDepts = ref<Set<string>>(new Set())

// Dialogs
const showAddDialog = ref(false)

// Email state - using Sets to allow concurrent sends
const sendingEmailPersonIds = ref<Set<number>>(new Set())
const sendingEmailDepts = ref<Set<string>>(new Set())
const verificationReplyDays = ref(7)

// Warn user if they try to leave while email operations are in progress
function handleBeforeUnload(e: BeforeUnloadEvent) {
    if (sendingEmailDepts.value.size > 0 || sendingEmailPersonIds.value.size > 0) {
        e.preventDefault()
        // Modern browsers ignore custom messages, but returnValue is required
        e.returnValue = "Email sending is in progress. Are you sure you want to leave?"
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
    if (sendingEmailDepts.value.size > 0 || sendingEmailPersonIds.value.size > 0) {
        $q.dialog({
            title: "Email In Progress",
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

// Helper to check if department should be treated as "No Department"
function isNoDept(effortDept: string | null | undefined): boolean {
    return !effortDept || effortDept === "UNK"
}

const deptOptions = computed(() => {
    const hasNoDept = instructors.value.some((i) => isNoDept(i.effortDept))
    const uniqueDepts = [
        ...new Set(instructors.value.map((i) => i.effortDept).filter((d): d is string => !!d && d !== "UNK")),
    ].sort()
    // Add "No Department" option if any instructors lack a department or have UNK
    if (hasNoDept) {
        uniqueDepts.unshift("No Department")
    }
    return ["", ...uniqueDepts]
})

const filteredInstructors = computed(() => {
    let result = instructors.value

    if (selectedDept.value) {
        if (selectedDept.value === "No Department") {
            result = result.filter((i) => isNoDept(i.effortDept))
        } else {
            result = result.filter((i) => i.effortDept === selectedDept.value)
        }
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
        const dept = isNoDept(instructor.effortDept) ? "No Department" : instructor.effortDept!
        const groupArray = groups[dept] ?? (groups[dept] = [])
        groupArray.push(instructor)
    }

    // Sort departments, but put "No Department" first
    return Object.keys(groups)
        .sort((a, b) => {
            if (a === "No Department") return -1
            if (b === "No Department") return 1
            return a.localeCompare(b)
        })
        .map((dept) => ({
            dept,
            instructors: groups[dept] ?? [],
        }))
})

// Check if user can see multiple departments (enables collapsible headers)
const hasMultipleDepts = computed(() => groupedInstructors.value.length > 1)

function toggleDeptCollapse(dept: string) {
    if (collapsedDepts.value.has(dept)) {
        collapsedDepts.value.delete(dept)
    } else {
        collapsedDepts.value.add(dept)
    }
}

// Check if any instructor has "Other %" content to determine column width
const hasOtherPercent = computed(() => {
    return instructors.value.some((i) => i.percentOtherSummary && i.percentOtherSummary.trim() !== "")
})

const columns = computed<QTableColumn[]>(() => [
    {
        name: "isVerified",
        label: "Verified",
        field: "isVerified",
        align: "center",
        sortable: true,
        style: "width: 55px; min-width: 55px; padding-left: 8px",
        headerStyle: "width: 55px; min-width: 55px; padding-left: 8px",
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
        name: "percentAdminSummary",
        label: "Admin %",
        field: "percentAdminSummary",
        align: "left",
        sortable: false,
        style: "white-space: pre-line; font-size: 0.85em",
    },
    {
        name: "percentClinicalSummary",
        label: "Clinical %",
        field: "percentClinicalSummary",
        align: "left",
        sortable: false,
        style: "white-space: pre-line; font-size: 0.85em",
    },
    {
        name: "percentOtherSummary",
        label: "Other %",
        field: "percentOtherSummary",
        align: "left",
        sortable: false,
        // Shrink column width when no content, give space to other columns
        style: hasOtherPercent.value
            ? "white-space: pre-line; font-size: 0.85em"
            : "width: 70px; min-width: 70px; font-size: 0.85em",
        headerStyle: hasOtherPercent.value ? undefined : "width: 70px; min-width: 70px",
    },
    {
        name: "actions",
        label: "Actions",
        field: "actions",
        align: "center",
        style: "width: 185px; min-width: 185px",
        headerStyle: "width: 185px; min-width: 185px",
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
        const result = await instructorService.getInstructors(selectedTermCode.value)

        // Abort if a newer request has been initiated
        if (token !== loadToken) return

        instructors.value = result

        // Apply department filter from query param if present
        // Map "UNK" from Staff Dashboard to "No Department" label used in dropdown
        const deptParam = route.query.dept as string | undefined
        if (deptParam) {
            if (deptParam === "UNK" && instructors.value.some((i) => isNoDept(i.effortDept))) {
                selectedDept.value = "No Department"
            } else if (instructors.value.some((i) => i.effortDept === deptParam)) {
                selectedDept.value = deptParam
            }
        } else {
            // Auto-select department if user only has access to one
            const uniqueDepts = [...new Set(result.map((i) => i.effortDept).filter((d): d is string => !!d))]
            if (uniqueDepts.length === 1 && !selectedDept.value) {
                selectedDept.value = uniqueDepts[0] ?? null
            }
        }
    } finally {
        if (token === loadToken) {
            isLoading.value = false
        }
    }
}

function confirmDeleteInstructor(instructor: PersonDto) {
    $q.dialog({
        title: "Delete Instructor",
        message: `Are you sure you want to delete ${instructor.fullName}? This will also delete all associated effort records for this term.`,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        if (!selectedTermCode.value) return

        const { recordCount } = await instructorService.canDeleteInstructor(instructor.personId, selectedTermCode.value)
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

    const success = await instructorService.deleteInstructor(personId, selectedTermCode.value)
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

// Email methods
type DeptGroup = { dept: string; instructors: PersonDto[] }

function getEmailableCount(deptGroup: DeptGroup): number {
    return deptGroup.instructors.filter((i) => i.canSendVerificationEmail).length
}

function getRecentlyEmailedCount(deptGroup: DeptGroup): number {
    return deptGroup.instructors.filter(
        (i) => i.canSendVerificationEmail && !isEmailedPastDeadline(i) && i.lastEmailedDate !== null,
    ).length
}

function getNotRecentlyEmailedCount(deptGroup: DeptGroup): number {
    return deptGroup.instructors.filter(
        (i) => i.canSendVerificationEmail && (isEmailedPastDeadline(i) || i.lastEmailedDate === null),
    ).length
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

// Effort records icon helpers
function getEffortIcon(instructor: PersonDto): string {
    if (instructor.recordCount === 0) return "work_alert"
    if (instructor.hasZeroHourRecords) return "work_alert"
    return "work_history"
}

function getEffortIconColor(instructor: PersonDto): string {
    // Has records with 0 hours = error (cannot verify)
    if (instructor.hasZeroHourRecords) return "negative"
    // No records = warning (can still verify "no effort")
    if (instructor.recordCount === 0) return "warning"
    return "primary"
}

function getEffortTooltip(instructor: PersonDto): string {
    if (instructor.hasZeroHourRecords) return "View effort records (has 0-hour items, cannot verify)"
    if (instructor.recordCount === 0) return "View effort records (no records, can verify 'no effort')"
    return "View effort records"
}

function confirmResendEmail(instructor: PersonDto) {
    const daysSince = getDaysSinceEmailed(instructor)
    const dateStr = formatDate(instructor.lastEmailedDate!)
    const sender = instructor.lastEmailedBy ?? "Unknown"

    const daysAgoText = daysSince === 0 ? "" : ` (${daysSince} ${inflect("day", daysSince)} ago)`
    $q.dialog({
        title: "Resend Verification Email?",
        message: `${instructor.fullName} was last emailed on ${dateStr} by ${sender}${daysAgoText}. Send another email?`,
        cancel: true,
        persistent: true,
    }).onOk(() => sendVerificationEmail(instructor))
}

async function sendVerificationEmail(instructor: PersonDto) {
    if (!selectedTermCode.value) return

    sendingEmailPersonIds.value.add(instructor.personId)
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
        sendingEmailPersonIds.value.delete(instructor.personId)
    }
}

function confirmBulkEmail(deptGroup: DeptGroup) {
    const notRecentlyEmailedCount = getNotRecentlyEmailedCount(deptGroup)
    const recentlyEmailedCount = getRecentlyEmailedCount(deptGroup)

    if (recentlyEmailedCount === 0) {
        // No recently emailed instructors - simple confirmation
        $q.dialog({
            title: "Send Verification Emails",
            message: `This will send verification emails to ${notRecentlyEmailedCount} ${inflect("instructor", notRecentlyEmailedCount)} in ${deptGroup.dept} who haven't verified. Continue?`,
            cancel: true,
            persistent: true,
        }).onOk(() => sendBulkVerificationEmails(deptGroup.dept, false))
    } else {
        // Some instructors were recently emailed - show checkbox option
        $q.dialog({
            title: "Send Verification Emails",
            message: `This will send verification emails to ${notRecentlyEmailedCount} ${inflect("instructor", notRecentlyEmailedCount)} in ${deptGroup.dept} who haven't verified and haven't been emailed recently.`,
            cancel: true,
            persistent: true,
            class: "bulk-email-dialog",
            options: {
                type: "checkbox",
                model: [],
                items: [
                    {
                        label: `Also email ${recentlyEmailedCount} ${inflect("instructor", recentlyEmailedCount)} who ${recentlyEmailedCount === 1 ? "was" : "were"} recently emailed`,
                        value: "includeRecent",
                    },
                ],
            },
        }).onOk((selected: string[]) => {
            const includeRecentlyEmailed = selected.includes("includeRecent")
            sendBulkVerificationEmails(deptGroup.dept, includeRecentlyEmailed)
        })
    }
}

async function sendBulkVerificationEmails(dept: string, includeRecentlyEmailed: boolean = false) {
    if (!selectedTermCode.value) return

    sendingEmailDepts.value.add(dept)
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
            includeRecentlyEmailed,
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
            // Skip if not in this department, already verified, can't receive email, or failed
            if (
                instructor.effortDept !== dept ||
                instructor.isVerified ||
                !instructor.canSendVerificationEmail ||
                failedPersonIds.has(instructor.personId)
            ) {
                continue
            }

            // Skip recently emailed instructors if not including them
            if (!includeRecentlyEmailed && !isEmailedPastDeadline(instructor) && instructor.lastEmailedDate !== null) {
                continue
            }

            instructor.lastEmailedDate = now
            instructor.lastEmailedBy = senderName
        }
    } catch {
        loadingNotify()
        $q.notify({
            type: "negative",
            message: "An error occurred while sending bulk emails",
        })
    } finally {
        sendingEmailDepts.value.delete(dept)
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

/* Mobile card view */
.instructor-cards {
    display: flex;
    flex-direction: column;
}

.instructor-card {
    border-radius: 0;
    border-top: none;
}

.instructor-card:first-child {
    border-top: 1px solid rgb(0 0 0 / 12%);
}

.card-name-badges {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0 0.5rem;
}

.card-actions {
    display: flex;
    gap: 0.5rem;
    flex-shrink: 0;
}

.card-title {
    flex-basis: 100%;
}

.card-percentages {
    display: flex;
    flex-wrap: wrap;
    gap: 0.25rem 1rem;
}

.percent-line {
    white-space: pre-line;
    line-height: 1.3;
}

.dept-header {
    background-color: #5f6a6a;
    font-weight: bold;
}

.dept-header--collapsible {
    cursor: pointer;
    user-select: none;
}

.dept-header--collapsible:hover,
.dept-header--collapsible:focus {
    background-color: #4a5454;
}

.dept-header--collapsible:focus-visible {
    outline: 2px solid #fff;
    outline-offset: -2px;
}

.dept-header--no-dept {
    background-color: var(--q-negative);
}

.dept-header--no-dept.dept-header--collapsible:hover,
.dept-header--no-dept.dept-header--collapsible:focus {
    background-color: #a83232;
}

.dept-table {
    margin-bottom: 0;
    border-radius: 0;
}

.dept-table :deep(.q-table__container) {
    border-radius: 0;
}

.dept-table--no-dept {
    border: 2px solid var(--q-negative);
    border-top: none;
}

.dept-table :deep(.q-table__middle) {
    overflow-x: hidden;
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
    margin-top: -2px;
}

.instructor-info {
    display: flex;
    flex-wrap: wrap;
    align-items: center;
    gap: 0.125rem 0.5rem;
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

.legend-section-title {
    font-weight: 600;
    font-size: 0.75rem;
    color: #666;
    margin-top: 0.5rem;
    margin-bottom: 0.25rem;
    text-transform: uppercase;
}

.legend-section-title:first-of-type {
    margin-top: 0;
}

.actions-header-content {
    display: inline-flex;
    align-items: center;
    gap: 0.25rem;
}

.actions-cell {
    display: inline-flex;
    align-items: center;
    justify-content: center;
    gap: 0.5rem;
}
</style>

<style>
/* Unscoped style for tooltip (injected into body) */
.email-legend-tooltip {
    border: 1px solid #ccc;
}

/* Remove horizontal dividers and reduce spacing in bulk email dialog checkbox options */
.bulk-email-dialog .q-separator {
    display: none;
}

.bulk-email-dialog .q-card__section.q-card__section--vert.scroll {
    padding-top: 0;
    padding-bottom: 0;
}
</style>
