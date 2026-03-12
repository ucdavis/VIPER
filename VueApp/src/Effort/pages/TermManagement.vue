<template>
    <div class="q-pa-md">
        <h2>Term Management</h2>

        <div
            v-if="isLoading"
            class="text-center q-my-lg"
        >
            <q-spinner-dots
                size="3rem"
                color="primary"
            />
            <div class="q-mt-md text-body1">Loading terms...</div>
        </div>

        <template v-else>
            <!-- Action Buttons -->
            <div class="row q-gutter-sm q-mb-md">
                <q-btn
                    v-if="availableTerms.length > 0"
                    icon="add"
                    label="Add Future Term"
                    color="primary"
                    dense
                    no-caps
                    :outline="showAddTermForm"
                    @click="toggleAddTermForm"
                    @keyup.enter="toggleAddTermForm"
                    @keyup.space="toggleAddTermForm"
                />
                <q-btn
                    icon="event_repeat"
                    label="Percent Rollover"
                    color="cyan-8"
                    :text-color="showRolloverForm ? undefined : 'white'"
                    dense
                    no-caps
                    :outline="showRolloverForm"
                    @click="toggleRolloverForm"
                    @keyup.enter="toggleRolloverForm"
                    @keyup.space="toggleRolloverForm"
                />
            </div>

            <!-- Percent Assignment Rollover (inline expand) -->
            <q-slide-transition>
                <div
                    v-show="showRolloverForm"
                    class="q-mb-md q-pa-sm bg-grey-1 rounded-borders"
                >
                    <div class="row items-center q-gutter-sm">
                        <template v-if="!rolloverYearOverride">
                            <span class="text-body2"
                                >Year: <strong>{{ rolloverYear }}</strong></span
                            >
                            <q-btn
                                label="Change"
                                flat
                                dense
                                no-caps
                                size="sm"
                                color="grey-7"
                                @click="rolloverYearOverride = true"
                            />
                        </template>
                        <template v-else>
                            <q-input
                                v-model.number="rolloverYear"
                                type="number"
                                label="Boundary Year"
                                dense
                                outlined
                                style="max-width: 150px"
                                :rules="[yearRule]"
                            />
                            <q-btn
                                label="Reset"
                                flat
                                dense
                                no-caps
                                size="sm"
                                color="grey-7"
                                @click="resetRolloverYear"
                            />
                        </template>
                        <q-btn
                            icon="event_repeat"
                            label="Preview Rollover"
                            color="cyan-8"
                            text-color="white"
                            dense
                            no-caps
                            :disable="!rolloverYear || rolloverYear < 2020 || rolloverYear > currentYear"
                            @click="openRolloverDialog"
                            @keyup.enter="openRolloverDialog"
                            @keyup.space="openRolloverDialog"
                        />
                    </div>
                    <div
                        v-if="rolloverYearOverride"
                        class="text-caption text-orange-9 q-mt-xs"
                    >
                        Changing the year from the default is unusual. Only do this if you need to run a past rollover.
                    </div>
                </div>
            </q-slide-transition>

            <!-- Add Future Term (inline expand) -->
            <q-slide-transition>
                <div
                    v-show="showAddTermForm && availableTerms.length > 0"
                    class="q-mb-md q-pa-sm bg-grey-1 rounded-borders"
                >
                    <div class="row items-center q-gutter-sm">
                        <q-select
                            v-model="selectedNewTerm"
                            :options="availableTerms"
                            option-label="termName"
                            option-value="termCode"
                            label="Select a term to add"
                            dense
                            options-dense
                            outlined
                            class="term-select-input"
                        />
                        <q-input
                            v-model="newTermExpectedCloseDate"
                            type="date"
                            label="Expected Close Date"
                            dense
                            outlined
                            clearable
                            reactive-rules
                            :rules="[createCloseDateRule]"
                            :hint="createCloseDateHint"
                            class="expected-close-input"
                        />
                        <q-btn
                            label="Add Term"
                            color="primary"
                            dense
                            :disable="!selectedNewTerm"
                            @click="confirmAddTerm"
                        />
                    </div>
                </div>
            </q-slide-transition>
            <!-- Desktop: Table view -->
            <q-table
                :rows="terms"
                :columns="columns"
                row-key="termCode"
                dense
                flat
                bordered
                :pagination="{ rowsPerPage: 0 }"
                hide-pagination
                class="gt-xs"
            >
                <template #body-cell-termName="props">
                    <q-td :props="props">
                        <div class="row items-center no-wrap">
                            <router-link
                                :to="`/Effort/${props.row.termCode}`"
                                class="text-primary"
                            >
                                {{ props.row.termName }}
                            </router-link>
                            <q-btn
                                v-if="props.row.canDelete"
                                icon="delete"
                                color="negative"
                                dense
                                flat
                                round
                                size="sm"
                                class="q-ml-sm"
                                @click="confirmDeleteTerm(props.row)"
                                @keyup.enter="confirmDeleteTerm(props.row)"
                                @keyup.space="confirmDeleteTerm(props.row)"
                            >
                                <q-tooltip>Delete term</q-tooltip>
                            </q-btn>
                        </div>
                    </q-td>
                </template>
                <template #body-cell-harvestedDate="props">
                    <q-td :props="props">
                        <div class="row items-center">
                            <span class="q-mr-sm">{{ formatDate(props.row.harvestedDate) }}</span>
                            <div class="row q-gutter-xs wrap">
                                <q-btn
                                    v-if="props.row.canHarvest"
                                    icon="cloud_download"
                                    :label="props.row.harvestedDate ? 'Re-Harvest' : 'Harvest'"
                                    color="info"
                                    text-color="white"
                                    dense
                                    no-caps
                                    size="sm"
                                    padding="2px sm"
                                    class="term-action-btn"
                                    @click="openHarvestDialog(props.row)"
                                    @keyup.enter="openHarvestDialog(props.row)"
                                    @keyup.space="openHarvestDialog(props.row)"
                                >
                                    <q-tooltip
                                        >Import instructors and courses from CREST and Clinical Scheduler</q-tooltip
                                    >
                                </q-btn>
                                <q-btn
                                    v-if="props.row.canImportClinical"
                                    icon="medical_services"
                                    label="Import Clinical"
                                    color="teal-8"
                                    text-color="white"
                                    dense
                                    no-caps
                                    size="sm"
                                    padding="2px sm"
                                    class="term-action-btn"
                                    @click="openClinicalDialog(props.row)"
                                    @keyup.enter="openClinicalDialog(props.row)"
                                    @keyup.space="openClinicalDialog(props.row)"
                                >
                                    <q-tooltip>Import clinical rotation assignments from Clinical Scheduler</q-tooltip>
                                </q-btn>
                            </div>
                        </div>
                    </q-td>
                </template>
                <template #body-cell-openedDate="props">
                    <q-td :props="props">
                        <div class="row items-center">
                            <span class="q-mr-sm">{{ formatDate(props.row.openedDate) }}</span>
                            <div class="row q-gutter-xs wrap">
                                <q-btn
                                    v-if="props.row.canOpen"
                                    icon="add_circle"
                                    label="Open"
                                    color="positive"
                                    text-color="white"
                                    dense
                                    no-caps
                                    size="sm"
                                    padding="2px sm"
                                    class="term-action-btn"
                                    @click="confirmOpenTerm(props.row)"
                                    @keyup.enter="confirmOpenTerm(props.row)"
                                    @keyup.space="confirmOpenTerm(props.row)"
                                />
                                <q-btn
                                    v-if="props.row.canUnopen"
                                    icon="history"
                                    label="Unopen"
                                    color="warning"
                                    text-color="dark"
                                    dense
                                    no-caps
                                    size="sm"
                                    padding="2px sm"
                                    class="term-action-btn"
                                    @click="confirmUnopenTerm(props.row)"
                                    @keyup.enter="confirmUnopenTerm(props.row)"
                                    @keyup.space="confirmUnopenTerm(props.row)"
                                />
                            </div>
                        </div>
                    </q-td>
                </template>
                <template #body-cell-closedDate="props">
                    <q-td :props="props">
                        <div class="row items-center">
                            <span class="q-mr-sm">{{ formatDate(props.row.closedDate) }}</span>
                            <div class="row q-gutter-xs wrap">
                                <q-btn
                                    v-if="props.row.canClose"
                                    icon="lock"
                                    label="Close"
                                    color="negative"
                                    text-color="white"
                                    dense
                                    no-caps
                                    size="sm"
                                    padding="2px sm"
                                    class="term-action-btn"
                                    @click="confirmCloseTerm(props.row)"
                                    @keyup.enter="confirmCloseTerm(props.row)"
                                    @keyup.space="confirmCloseTerm(props.row)"
                                />
                                <q-btn
                                    v-if="props.row.canReopen"
                                    icon="lock_open"
                                    label="Reopen"
                                    color="secondary"
                                    text-color="white"
                                    dense
                                    no-caps
                                    size="sm"
                                    padding="2px sm"
                                    class="term-action-btn"
                                    @click="confirmReopenTerm(props.row)"
                                    @keyup.enter="confirmReopenTerm(props.row)"
                                    @keyup.space="confirmReopenTerm(props.row)"
                                />
                            </div>
                        </div>
                    </q-td>
                </template>
                <template #body-cell-expectedCloseDate="props">
                    <q-td :props="props">
                        <div class="row items-center no-wrap">
                            <template v-if="editingExpectedCloseTermCode === props.row.termCode">
                                <q-input
                                    v-model="editingExpectedCloseDate"
                                    type="date"
                                    dense
                                    outlined
                                    clearable
                                    hide-bottom-space
                                    :rules="[editCloseDateRule]"
                                    class="expected-close-edit-input"
                                    @keyup.enter="saveExpectedCloseDate(props.row)"
                                    @keyup.escape="cancelEditExpectedCloseDate"
                                />
                                <q-btn
                                    icon="check"
                                    color="positive"
                                    dense
                                    flat
                                    round
                                    size="sm"
                                    class="q-ml-xs"
                                    @click="saveExpectedCloseDate(props.row)"
                                >
                                    <q-tooltip>Save</q-tooltip>
                                </q-btn>
                                <q-btn
                                    icon="close"
                                    color="negative"
                                    dense
                                    flat
                                    round
                                    size="sm"
                                    @click="cancelEditExpectedCloseDate"
                                >
                                    <q-tooltip>Cancel</q-tooltip>
                                </q-btn>
                            </template>
                            <template v-else>
                                <span class="q-mr-sm">{{ formatDate(props.row.expectedCloseDate) }}</span>
                                <q-btn
                                    v-if="props.row.canEditExpectedCloseDate"
                                    icon="edit"
                                    dense
                                    flat
                                    round
                                    size="sm"
                                    color="grey-7"
                                    @click="startEditExpectedCloseDate(props.row)"
                                    @keyup.enter="startEditExpectedCloseDate(props.row)"
                                    @keyup.space="startEditExpectedCloseDate(props.row)"
                                >
                                    <q-tooltip>Edit expected close date</q-tooltip>
                                </q-btn>
                            </template>
                        </div>
                    </q-td>
                </template>
            </q-table>

            <!-- Mobile: Card view -->
            <div class="lt-sm">
                <q-card
                    v-for="term in terms"
                    :key="term.termCode"
                    flat
                    bordered
                    class="q-mb-sm"
                >
                    <q-card-section class="q-py-sm">
                        <div class="text-subtitle1">
                            <router-link
                                :to="`/Effort/${term.termCode}`"
                                class="text-primary"
                            >
                                {{ term.termName }}
                            </router-link>
                        </div>
                        <div class="text-caption text-grey-7 q-mt-xs">
                            <span v-if="term.harvestedDate">Harvested: {{ formatDate(term.harvestedDate) }}</span>
                            <span
                                v-if="term.openedDate"
                                class="q-ml-md"
                                >Opened: {{ formatDate(term.openedDate) }}</span
                            >
                            <span
                                v-if="term.closedDate"
                                class="q-ml-md"
                                >Closed: {{ formatDate(term.closedDate) }}</span
                            >
                            <span
                                v-if="term.expectedCloseDate && editingExpectedCloseTermCode !== term.termCode"
                                class="q-ml-md"
                                >Expected Close: {{ formatDate(term.expectedCloseDate) }}</span
                            >
                        </div>
                        <!-- Mobile: Expected close date editing -->
                        <div
                            v-if="editingExpectedCloseTermCode === term.termCode"
                            class="row items-center q-gutter-xs q-mt-sm"
                        >
                            <q-input
                                v-model="editingExpectedCloseDate"
                                type="date"
                                label="Expected Close Date"
                                dense
                                outlined
                                clearable
                                hide-bottom-space
                                :rules="[editCloseDateRule]"
                                style="min-width: 170px"
                            />
                            <q-btn
                                icon="check"
                                color="positive"
                                dense
                                flat
                                round
                                size="sm"
                                @click="saveExpectedCloseDate(term)"
                            >
                                <q-tooltip>Save</q-tooltip>
                            </q-btn>
                            <q-btn
                                icon="close"
                                color="negative"
                                dense
                                flat
                                round
                                size="sm"
                                @click="cancelEditExpectedCloseDate"
                            >
                                <q-tooltip>Cancel</q-tooltip>
                            </q-btn>
                        </div>
                        <div class="row q-gutter-xs q-mt-sm wrap">
                            <q-btn
                                v-if="term.canHarvest"
                                icon="cloud_download"
                                :label="term.harvestedDate ? 'Re-Harvest' : 'Harvest'"
                                color="info"
                                text-color="white"
                                dense
                                no-caps
                                size="sm"
                                padding="2px sm"
                                class="term-action-btn"
                                @click="openHarvestDialog(term)"
                                @keyup.enter="openHarvestDialog(term)"
                                @keyup.space="openHarvestDialog(term)"
                            />
                            <q-btn
                                v-if="term.canImportClinical"
                                icon="medical_services"
                                label="Import Clinical"
                                color="teal-8"
                                text-color="white"
                                dense
                                no-caps
                                size="sm"
                                padding="2px sm"
                                class="term-action-btn"
                                @click="openClinicalDialog(term)"
                                @keyup.enter="openClinicalDialog(term)"
                                @keyup.space="openClinicalDialog(term)"
                            />
                            <q-btn
                                v-if="term.canOpen"
                                icon="add_circle"
                                label="Open"
                                color="positive"
                                text-color="white"
                                dense
                                no-caps
                                size="sm"
                                padding="2px sm"
                                class="term-action-btn"
                                @click="confirmOpenTerm(term)"
                                @keyup.enter="confirmOpenTerm(term)"
                                @keyup.space="confirmOpenTerm(term)"
                            />
                            <q-btn
                                v-if="term.canUnopen"
                                icon="history"
                                label="Unopen"
                                color="warning"
                                text-color="dark"
                                dense
                                no-caps
                                size="sm"
                                padding="2px sm"
                                class="term-action-btn"
                                @click="confirmUnopenTerm(term)"
                                @keyup.enter="confirmUnopenTerm(term)"
                                @keyup.space="confirmUnopenTerm(term)"
                            />
                            <q-btn
                                v-if="term.canClose"
                                icon="lock"
                                label="Close"
                                color="negative"
                                text-color="white"
                                dense
                                no-caps
                                size="sm"
                                padding="2px sm"
                                class="term-action-btn"
                                @click="confirmCloseTerm(term)"
                                @keyup.enter="confirmCloseTerm(term)"
                                @keyup.space="confirmCloseTerm(term)"
                            />
                            <q-btn
                                v-if="term.canReopen"
                                icon="lock_open"
                                label="Reopen"
                                color="secondary"
                                text-color="white"
                                dense
                                no-caps
                                size="sm"
                                padding="2px sm"
                                class="term-action-btn"
                                @click="confirmReopenTerm(term)"
                                @keyup.enter="confirmReopenTerm(term)"
                                @keyup.space="confirmReopenTerm(term)"
                            />
                            <q-btn
                                v-if="term.canEditExpectedCloseDate && editingExpectedCloseTermCode !== term.termCode"
                                icon="edit_calendar"
                                label="Expected Close"
                                color="grey-7"
                                text-color="white"
                                dense
                                no-caps
                                size="sm"
                                padding="2px sm"
                                class="term-action-btn"
                                @click="startEditExpectedCloseDate(term)"
                                @keyup.enter="startEditExpectedCloseDate(term)"
                                @keyup.space="startEditExpectedCloseDate(term)"
                            />
                            <q-btn
                                v-if="term.canDelete"
                                icon="delete"
                                label="Delete"
                                color="negative"
                                text-color="white"
                                dense
                                no-caps
                                size="sm"
                                padding="2px sm"
                                class="term-action-btn"
                                @click="confirmDeleteTerm(term)"
                                @keyup.enter="confirmDeleteTerm(term)"
                                @keyup.space="confirmDeleteTerm(term)"
                            />
                        </div>
                    </q-card-section>
                </q-card>
            </div>
        </template>

        <!-- Harvest Dialog -->
        <HarvestDialog
            v-model="harvestDialogOpen"
            :term-code="harvestTermCode"
            :term-name="harvestTermName"
            @harvested="onHarvested"
        />

        <!-- Percent Rollover Dialog -->
        <PercentRolloverDialog
            v-model="rolloverDialogOpen"
            :year="rolloverYear"
            @completed="onRolloverCompleted"
        />

        <!-- Clinical Import Dialog -->
        <ClinicalImportDialog
            v-model="clinicalDialogOpen"
            :term-code="clinicalTermCode"
            :term-name="clinicalTermName"
            @completed="onClinicalCompleted"
        />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useQuasar } from "quasar"
import { termService } from "../services/term-service"
import { rolloverService } from "../services/rollover-service"
import { useDateFunctions } from "@/composables/DateFunctions"
import HarvestDialog from "../components/HarvestDialog.vue"
import PercentRolloverDialog from "../components/PercentRolloverDialog.vue"
import ClinicalImportDialog from "../components/ClinicalImportDialog.vue"
import type { TermDto, AvailableTermDto } from "../types"
import type { QTableColumn } from "quasar"

const $q = useQuasar()
const { formatDate } = useDateFunctions()

const terms = ref<TermDto[]>([])
const availableTerms = ref<AvailableTermDto[]>([])
const selectedNewTerm = ref<AvailableTermDto | null>(null)
const newTermExpectedCloseDate = ref<string | null>(null)
const isLoading = ref(false)
const showRolloverForm = ref(false)
const showAddTermForm = ref(false)

// Expected close date inline editing
const editingExpectedCloseTermCode = ref<number | null>(null)
const editingExpectedCloseDate = ref<string | null>(null)
const editingTermEndDate = ref<string | null>(null)

// Expected close date validation — must be after term end and within 1 year
function toYmd(isoDate: string): string {
    return isoDate.split("T")[0] ?? isoDate
}

function closeDateRule(endDateIso: string | null | undefined) {
    return (val: string | null | undefined) => {
        if (!val || !endDateIso) return true
        const endYmd = toYmd(endDateIso)
        if (val <= endYmd) return "Must be after term end date"
        const maxYmd = `${parseInt(endYmd.slice(0, 4)) + 1}${endYmd.slice(4)}`
        if (val > maxYmd) return "Cannot exceed 1 year after term end"
        return true
    }
}

function createCloseDateRule(val: string | null | undefined) {
    return closeDateRule(selectedNewTerm.value?.endDate)(val)
}

function editCloseDateRule(val: string | null | undefined) {
    return closeDateRule(editingTermEndDate.value)(val)
}

const createCloseDateHint = computed(() => {
    if (!selectedNewTerm.value?.endDate) return ""
    const endYmd = toYmd(selectedNewTerm.value.endDate)
    const year = parseInt(endYmd.substring(0, 4))
    const maxYmd = `${year + 1}${endYmd.substring(4)}`
    return `Between ${formatDate(endYmd)} and ${formatDate(maxYmd)}`
})

// Harvest dialog state
const harvestDialogOpen = ref(false)
const harvestTermCode = ref<number | null>(null)
const harvestTermName = ref("")

// Percent Rollover state
const rolloverDialogOpen = ref(false)
const currentYear = new Date().getFullYear()
const rolloverYear = ref(rolloverService.getDefaultYear())
const rolloverYearOverride = ref(false)

function yearRule(val: number) {
    if (!val || val < 2020) return "Year must be 2020 or later"
    if (val > currentYear) return `Year cannot be in the future (max: ${currentYear})`
    return true
}

function resetRolloverYear() {
    rolloverYear.value = rolloverService.getDefaultYear()
    rolloverYearOverride.value = false
}

function toggleRolloverForm() {
    showRolloverForm.value = !showRolloverForm.value
    showAddTermForm.value = false
}

function toggleAddTermForm() {
    showAddTermForm.value = !showAddTermForm.value
    showRolloverForm.value = false
    if (!showAddTermForm.value) {
        newTermExpectedCloseDate.value = null
    }
}

// Clinical Import dialog state
const clinicalDialogOpen = ref(false)
const clinicalTermCode = ref<number | null>(null)
const clinicalTermName = ref("")

const columns: QTableColumn[] = [
    { name: "termName", label: "Term", field: "termName", align: "left" },
    { name: "harvestedDate", label: "Harvested", field: "harvestedDate", align: "left" },
    { name: "openedDate", label: "Opened", field: "openedDate", align: "left" },
    { name: "closedDate", label: "Closed", field: "closedDate", align: "left" },
    { name: "expectedCloseDate", label: "Expected Close", field: "expectedCloseDate", align: "left" },
]

async function loadTerms(options?: { background?: boolean }) {
    if (!options?.background) {
        isLoading.value = true
    }
    try {
        const [termsResult, availableResult] = await Promise.all([
            termService.getTerms(),
            termService.getAvailableTerms(),
        ])
        terms.value = termsResult
        availableTerms.value = availableResult
        selectedNewTerm.value = null
    } finally {
        isLoading.value = false
    }
}

interface TermActionConfig {
    title: string
    message: string
    successMessage: string
    action: () => Promise<unknown>
}

function confirmTermAction(config: TermActionConfig) {
    $q.dialog({
        title: config.title,
        message: config.message,
        cancel: true,
        persistent: true,
    }).onOk(async () => {
        const result = await config.action()
        if (result && (typeof result !== "object" || (result as { success?: boolean }).success !== false)) {
            $q.notify({ type: "positive", message: config.successMessage })
            await loadTerms({ background: true })
        } else {
            $q.notify({ type: "negative", message: `${config.title} failed. Please try again.` })
        }
    })
}

function confirmAddTerm() {
    if (!selectedNewTerm.value) return
    const term = selectedNewTerm.value
    const expectedClose = newTermExpectedCloseDate.value || null

    if (expectedClose) {
        const ruleResult = createCloseDateRule(expectedClose)
        if (ruleResult !== true) {
            $q.notify({ type: "negative", message: ruleResult })
            return
        }
    }

    confirmTermAction({
        title: "Add Term",
        message: `Add "${term.termName}" to the Effort system?`,
        successMessage: "Term added successfully",
        action: () => termService.createTerm(term.termCode, expectedClose),
    })
}

function startEditExpectedCloseDate(term: TermDto) {
    editingExpectedCloseTermCode.value = term.termCode
    // Convert ISO date to yyyy-MM-dd for the date input
    editingExpectedCloseDate.value = term.expectedCloseDate
        ? (term.expectedCloseDate.split("T")[0] ?? null)
        : null
    editingTermEndDate.value = term.termEndDate ?? null
}

function cancelEditExpectedCloseDate() {
    editingExpectedCloseTermCode.value = null
    editingExpectedCloseDate.value = null
    editingTermEndDate.value = null
}

async function saveExpectedCloseDate(term: TermDto) {
    // Skip API call if nothing changed
    const originalYmd = term.expectedCloseDate ? (term.expectedCloseDate.split("T")[0] ?? null) : null
    if ((editingExpectedCloseDate.value || null) === originalYmd) {
        cancelEditExpectedCloseDate()
        return
    }

    const ruleResult = editCloseDateRule(editingExpectedCloseDate.value)
    if (ruleResult !== true) {
        $q.notify({ type: "negative", message: ruleResult })
        return
    }

    const { result, error } = await termService.updateExpectedCloseDate(
        term.termCode,
        editingExpectedCloseDate.value || null,
    )
    if (result) {
        $q.notify({ type: "positive", message: "Expected close date updated" })
        cancelEditExpectedCloseDate()
        await loadTerms({ background: true })
    } else {
        $q.notify({ type: "negative", message: error ?? "Failed to update expected close date" })
    }
}

function confirmOpenTerm(term: TermDto) {
    confirmTermAction({
        title: "Open Term",
        message: `Are you sure you want to open "${term.termName}" for effort entry?`,
        successMessage: "Term opened successfully",
        action: () => termService.openTerm(term.termCode),
    })
}

function confirmCloseTerm(term: TermDto) {
    confirmTermAction({
        title: "Close Term",
        message: `Are you sure you want to close "${term.termName}"?`,
        successMessage: "Term closed successfully",
        action: () => termService.closeTerm(term.termCode),
    })
}

function confirmReopenTerm(term: TermDto) {
    confirmTermAction({
        title: "Reopen Term",
        message: `Are you sure you want to reopen "${term.termName}"?`,
        successMessage: "Term reopened successfully",
        action: () => termService.reopenTerm(term.termCode),
    })
}

function confirmUnopenTerm(term: TermDto) {
    confirmTermAction({
        title: "Revert Term",
        message: `Are you sure you want to revert "${term.termName}" to unopened status?`,
        successMessage: "Term reverted to unopened status",
        action: () => termService.unopenTerm(term.termCode),
    })
}

function confirmDeleteTerm(term: TermDto) {
    confirmTermAction({
        title: "Delete Term",
        message: `Are you sure you want to delete "${term.termName}"? This cannot be undone.`,
        successMessage: "Term deleted successfully",
        action: () => termService.deleteTerm(term.termCode),
    })
}

function openHarvestDialog(term: TermDto) {
    harvestTermCode.value = term.termCode
    harvestTermName.value = term.termName
    harvestDialogOpen.value = true
}

function onHarvested() {
    loadTerms({ background: true })
}

function openRolloverDialog() {
    rolloverDialogOpen.value = true
}

function onRolloverCompleted() {
    loadTerms()
}

function openClinicalDialog(term: TermDto) {
    clinicalTermCode.value = term.termCode
    clinicalTermName.value = term.termName
    clinicalDialogOpen.value = true
}

function onClinicalCompleted() {
    loadTerms()
}

onMounted(loadTerms)
</script>

<style scoped>
.term-action-btn {
    min-width: 130px;
}

.term-select-input {
    min-width: 250px;
}

.expected-close-input {
    min-width: 200px;
}

.expected-close-edit-input {
    min-width: 170px;
}
</style>
