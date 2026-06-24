<template>
    <div class="clinical-scheduler-container position-relative">
        <h1 class="sr-only">Clinical Scheduler Audit Trail</h1>
        <SchedulerNavigation />

        <!-- Loading permissions state -->
        <div
            v-if="isLoadingPermissions"
            class="text-center q-my-lg"
        >
            <q-spinner-dots
                size="3rem"
                color="primary"
            />
            <div class="q-mt-md text-body1">Loading...</div>
        </div>

        <!-- Access denied for users without manage permission -->
        <AccessDeniedCard
            v-else-if="!permissionsStore.hasManagePermission"
            :message="ACCESS_DENIED_MESSAGES.AUDIT_LOG"
            :subtitle="ACCESS_DENIED_SUBTITLES.AUDIT_LOG"
        >
            <template #actions>
                <q-btn
                    color="primary"
                    label="Return to Home"
                    @click="goToHome"
                />
            </template>
        </AccessDeniedCard>

        <!-- Main content -->
        <div v-else>
            <h2 class="text-h5 q-mb-md">Audit Trail</h2>

            <StatusBanner
                v-if="error"
                type="error"
            >
                <strong>Error: </strong>{{ error }}
                <template #action>
                    <q-btn
                        flat
                        color="negative"
                        label="Retry"
                        :loading="isLoading"
                        @click="loadAuditTrail"
                    />
                </template>
            </StatusBanner>

            <!-- Filters - always visible on desktop, collapsible on mobile -->
            <q-expansion-item
                v-model="filtersExpandedComputed"
                icon="filter_list"
                label="Filters"
                header-class="bg-grey-2 lt-md"
                class="q-mb-md"
            >
                <q-card flat>
                    <q-card-section :class="$q.screen.gt.sm ? 'q-pa-none' : ''">
                        <div class="row q-col-gutter-sm">
                            <div :class="$q.screen.gt.sm ? 'col-12 col-md-6 col-lg-3' : 'col-12'">
                                <q-select
                                    v-model="currentYear"
                                    :options="yearOptions"
                                    label="Year"
                                    dense
                                    options-dense
                                    outlined
                                    @update:model-value="onYearChange"
                                />
                            </div>
                            <div :class="$q.screen.gt.sm ? 'col-12 col-md-6 col-lg-3' : 'col-12'">
                                <q-select
                                    v-model="selectedRotationId"
                                    :options="rotationOptions"
                                    label="Rotation"
                                    dense
                                    options-dense
                                    outlined
                                    clearable
                                    emit-value
                                    map-options
                                />
                            </div>
                            <div :class="$q.screen.gt.sm ? 'col-12 col-md-6 col-lg-3' : 'col-12'">
                                <q-select
                                    v-model="selectedArea"
                                    :options="areaOptions"
                                    label="Area"
                                    dense
                                    options-dense
                                    outlined
                                    emit-value
                                    map-options
                                />
                            </div>
                            <div :class="$q.screen.gt.sm ? 'col-12 col-md-6 col-lg-3' : 'col-12'">
                                <q-select
                                    v-model="selectedModifier"
                                    :options="filteredModifierOptions"
                                    label="Modified By"
                                    dense
                                    options-dense
                                    outlined
                                    clearable
                                    emit-value
                                    map-options
                                    use-input
                                    :input-debounce="INPUT_DEBOUNCE_MS"
                                    @filter="filterModifiers"
                                />
                            </div>
                            <div :class="$q.screen.gt.sm ? 'col-12 col-md-6 col-lg-3' : 'col-12'">
                                <q-select
                                    v-model="selectedPerson"
                                    :options="filteredPersonOptions"
                                    label="Person"
                                    dense
                                    options-dense
                                    outlined
                                    clearable
                                    emit-value
                                    map-options
                                    use-input
                                    :input-debounce="INPUT_DEBOUNCE_MS"
                                    @filter="filterPersons"
                                />
                            </div>
                            <div :class="$q.screen.gt.sm ? 'col-12 col-md-6 col-lg-3' : 'col-6'">
                                <q-input
                                    v-model="fromDate"
                                    type="date"
                                    label="Date From"
                                    dense
                                    outlined
                                    clearable
                                />
                            </div>
                            <div :class="$q.screen.gt.sm ? 'col-12 col-md-6 col-lg-3' : 'col-6'">
                                <q-input
                                    v-model="toDate"
                                    type="date"
                                    label="Date To"
                                    dense
                                    outlined
                                    clearable
                                />
                            </div>
                        </div>
                        <div class="row q-mt-sm">
                            <div class="col-12 text-right">
                                <q-btn
                                    label="Clear Filters"
                                    color="secondary"
                                    dense
                                    flat
                                    @click="clearFilters"
                                />
                            </div>
                        </div>
                    </q-card-section>
                </q-card>
            </q-expansion-item>

            <!-- Results -->
            <q-table
                row-key="scheduleAuditId"
                :rows="entries"
                :columns="columns"
                :loading="isLoading"
                v-model:pagination="pagination"
                :rows-per-page-options="[10, 25, 50, 100]"
                binary-state-sort
                dense
                flat
                bordered
            >
                <template #body-cell-timeStamp="props">
                    <q-td :props="props">
                        {{ formatDateTime(props.row.timeStamp, { dateStyle: "short", timeStyle: "short" }) }}
                    </q-td>
                </template>
                <template #body-cell-action="props">
                    <q-td :props="props">
                        <StatusBadge :color="getActionColor(props.row.action)">
                            {{ props.row.action }}
                        </StatusBadge>
                    </q-td>
                </template>
                <template #no-data>
                    <div class="full-width text-center q-pa-md text-grey-7">
                        No audit entries found matching this filter.
                    </div>
                </template>
            </q-table>
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from "vue"
import type { Ref } from "vue"
import { useQuasar } from "quasar"
import type { QTableColumn, QTableProps } from "quasar"
import { useDebounceFn } from "@vueuse/core"
import { usePermissionsStore } from "../stores/permissions"
import { usePermissionChecks } from "../composables/use-permission-checks"
import { AuditLogService } from "../services/audit-log-service"
import { RotationService } from "../services/rotation-service"
import { PageDataService } from "../services/page-data-service"
import { useDateFunctions } from "@/composables/DateFunctions"
import StatusBanner from "@/components/StatusBanner.vue"
import StatusBadge from "@/components/StatusBadge.vue"
import SchedulerNavigation from "../components/SchedulerNavigation.vue"
import AccessDeniedCard from "../components/AccessDeniedCard.vue"
import { ACCESS_DENIED_MESSAGES, ACCESS_DENIED_SUBTITLES } from "../constants/permission-messages"
import type { AuditLogEntry } from "../types/audit-types"

const INPUT_DEBOUNCE_MS = 300

const permissionsStore = usePermissionsStore()
const { isLoadingPermissions, goToHome } = usePermissionChecks()
const $q = useQuasar()
const { formatDate, formatDateTime } = useDateFunctions()

const isLoading = ref(false)
const error = ref<string | null>(null)
const ready = ref(false)
const entries = ref<AuditLogEntry[]>([])

// Filters
const currentYear = ref<number | null>(null)
const selectedRotationId = ref<number | null>(null)
const selectedArea = ref<string | null>(null)
const selectedModifier = ref<string | null>(null)
const selectedPerson = ref<string | null>(null)
const fromDate = ref<string>("")
const toDate = ref<string>("")

// Filter dropdown options
const yearOptions = ref<number[]>([])
const rotationOptions = ref<{ label: string; value: number }[]>([])
const modifierOptions = ref<{ label: string; value: string }[]>([])
const filteredModifierOptions = ref<{ label: string; value: string }[]>([])
const personOptions = ref<{ label: string; value: string }[]>([])
const filteredPersonOptions = ref<{ label: string; value: string }[]>([])

const areaOptions: { label: string; value: string | null }[] = [
    { label: "All Schedules", value: null },
    { label: "Clinician Schedules", value: "Clinicians" },
    { label: "Student Schedules", value: "Students" },
]

// Filters panel: always open on desktop, collapsible on mobile (mirrors the Effort audit trail).
const filtersExpanded = ref(false)
const filtersExpandedComputed = computed({
    get: () => ($q.screen.gt.sm ? true : filtersExpanded.value),
    set: (val: boolean) => {
        filtersExpanded.value = val
    },
})

const pagination = ref({
    sortBy: "timeStamp",
    descending: true,
    rowsPerPage: 25,
}) as Ref<QTableProps["pagination"]>

const columns: QTableColumn<AuditLogEntry>[] = [
    { name: "timeStamp", label: "Date", field: "timeStamp", align: "left", sortable: true },
    { name: "modifiedByName", label: "Modified By", field: "modifiedByName", align: "left", sortable: true },
    { name: "personName", label: "Person", field: "personName", align: "left", sortable: true },
    { name: "area", label: "Area", field: "area", align: "left", sortable: true },
    { name: "rotationName", label: "Rotation", field: "rotationName", align: "left", sortable: true },
    {
        name: "week",
        label: "Week",
        field: "weekNum",
        align: "left",
        sortable: true,
        format: (val: number, row: AuditLogEntry) => {
            const date = row.weekStart ? formatDate(row.weekStart) : ""
            if (val) {
                return date ? `Wk ${val} · ${date}` : `Wk ${val}`
            }
            return date
        },
    },
    { name: "action", label: "Action", field: "action", align: "left", sortable: true },
]

// Map each audit action to a semantic Quasar color, matching the Effort audit trail's badge styling.
function getActionColor(action: string): string {
    switch (action) {
        case "Added to rotation":
            return "positive"
        case "Removed from rotation":
            return "negative"
        case "Made primary evaluator":
            return "primary"
        case "Primary evaluator flag removed":
            return "warning"
        default:
            return "grey-8"
    }
}

function filterModifiers(val: string, update: (fn: () => void) => void) {
    update(() => {
        if (!val) {
            filteredModifierOptions.value = modifierOptions.value
        } else {
            const needle = val.toLowerCase()
            filteredModifierOptions.value = modifierOptions.value.filter((m) => m.label.toLowerCase().includes(needle))
        }
    })
}

function filterPersons(val: string, update: (fn: () => void) => void) {
    update(() => {
        if (!val) {
            filteredPersonOptions.value = personOptions.value
        } else {
            const needle = val.toLowerCase()
            filteredPersonOptions.value = personOptions.value.filter((p) => p.label.toLowerCase().includes(needle))
        }
    })
}

async function loadAuditTrail() {
    if (!permissionsStore.hasManagePermission) {
        return
    }
    isLoading.value = true
    error.value = null
    try {
        const response = await AuditLogService.getAuditLog({
            year: currentYear.value ?? undefined,
            rotationId: selectedRotationId.value ?? undefined,
            area: selectedArea.value ?? undefined,
            modifiedBy: selectedModifier.value ?? undefined,
            person: selectedPerson.value ?? undefined,
            from: fromDate.value || undefined,
            to: toDate.value || undefined,
        })
        if (response.success) {
            entries.value = response.result
        } else {
            error.value = response.errors?.join(", ") || "Failed to load the audit trail"
        }
    } catch (err) {
        error.value = err instanceof Error ? err.message : "An error occurred while loading the audit trail"
    } finally {
        isLoading.value = false
    }
}

const debouncedLoad = useDebounceFn(() => {
    void loadAuditTrail()
}, INPUT_DEBOUNCE_MS)

async function loadPageData() {
    try {
        const pageData = await PageDataService.getPageData()
        yearOptions.value = pageData.availableGradYears
        if (currentYear.value === null) {
            currentYear.value = pageData.currentGradYear
        }
    } catch (err) {
        error.value = err instanceof Error ? err.message : "Failed to load the available years"
    }
}

async function loadFilterOptions() {
    const [rotations, modifiers, persons] = await Promise.all([
        RotationService.getRotations(),
        AuditLogService.getModifiers(),
        AuditLogService.getPersons(),
    ])
    if (rotations.success) {
        rotationOptions.value = rotations.result
            .map((rotation) => ({ label: rotation.name, value: rotation.rotId }))
            .sort((a, b) => a.label.localeCompare(b.label))
    }
    if (modifiers.success) {
        modifierOptions.value = modifiers.result.map((modifier) => ({
            label: modifier.displayName,
            value: modifier.mothraId,
        }))
        filteredModifierOptions.value = modifierOptions.value
    }
    if (persons.success) {
        personOptions.value = persons.result.map((person) => ({
            label: person.displayName,
            value: person.mothraId,
        }))
        filteredPersonOptions.value = personOptions.value
    }
}

function onYearChange() {
    void loadAuditTrail()
}

function clearFilters() {
    selectedRotationId.value = null
    selectedArea.value = null
    selectedModifier.value = null
    selectedPerson.value = null
    fromDate.value = ""
    toDate.value = ""
}

// Auto-apply when any filter other than the year changes (the year loads immediately via onYearChange).
watch([selectedRotationId, selectedArea, selectedModifier, selectedPerson, fromDate, toDate], () => {
    if (!ready.value) {
        return
    }
    debouncedLoad()
})

async function initialize() {
    if (!permissionsStore.userPermissions) {
        await permissionsStore.initialize()
    }
    if (permissionsStore.hasManagePermission) {
        await Promise.all([loadPageData(), loadFilterOptions()])
        await loadAuditTrail()
    }
    ready.value = true
}

onMounted(() => {
    void initialize()
})
</script>
