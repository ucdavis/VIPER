<template>
    <div class="q-pa-md">
        <h2>Term Management</h2>

        <div
            v-if="isLoading"
            class="text-grey q-my-md"
        >
            Loading terms...
        </div>

        <template v-else>
            <!-- Add Future Term Section -->
            <div
                v-if="availableTerms.length > 0"
                class="q-mb-lg"
            >
                <div class="text-subtitle1 q-mb-sm">Add Future Term</div>
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
                        style="min-width: 250px"
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
                                    v-if="props.row.canRolloverPercent"
                                    icon="event_repeat"
                                    label="Percent Rollover"
                                    color="cyan-8"
                                    text-color="white"
                                    dense
                                    no-caps
                                    size="sm"
                                    padding="2px sm"
                                    class="term-action-btn"
                                    @click="openRolloverDialog(props.row)"
                                    @keyup.enter="openRolloverDialog(props.row)"
                                    @keyup.space="openRolloverDialog(props.row)"
                                >
                                    <q-tooltip>Roll forward percent assignments to the new academic year</q-tooltip>
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
                                v-if="term.canRolloverPercent"
                                icon="event_repeat"
                                label="Percent Rollover"
                                color="cyan-8"
                                text-color="white"
                                dense
                                no-caps
                                size="sm"
                                padding="2px sm"
                                class="term-action-btn"
                                @click="openRolloverDialog(term)"
                                @keyup.enter="openRolloverDialog(term)"
                                @keyup.space="openRolloverDialog(term)"
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
            :term-code="rolloverTermCode"
            :term-name="rolloverTermName"
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
import { ref, onMounted } from "vue"
import { useQuasar } from "quasar"
import { termService } from "../services/term-service"
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
const isLoading = ref(false)

// Harvest dialog state
const harvestDialogOpen = ref(false)
const harvestTermCode = ref<number | null>(null)
const harvestTermName = ref("")

// Percent Rollover dialog state
const rolloverDialogOpen = ref(false)
const rolloverTermCode = ref<number | null>(null)
const rolloverTermName = ref("")

// Clinical Import dialog state
const clinicalDialogOpen = ref(false)
const clinicalTermCode = ref<number | null>(null)
const clinicalTermName = ref("")

const columns: QTableColumn[] = [
    { name: "termName", label: "Term", field: "termName", align: "left" },
    { name: "harvestedDate", label: "Harvested", field: "harvestedDate", align: "left" },
    { name: "openedDate", label: "Opened", field: "openedDate", align: "left" },
    { name: "closedDate", label: "Closed", field: "closedDate", align: "left" },
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
        }
    })
}

function confirmAddTerm() {
    if (!selectedNewTerm.value) return
    const term = selectedNewTerm.value
    confirmTermAction({
        title: "Add Term",
        message: `Add "${term.termName}" to the Effort system?`,
        successMessage: "Term added successfully",
        action: () => termService.createTerm(term.termCode),
    })
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

function openRolloverDialog(term: TermDto) {
    rolloverTermCode.value = term.termCode
    rolloverTermName.value = term.termName
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
</style>
