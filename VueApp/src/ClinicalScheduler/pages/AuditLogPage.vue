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
                        @click="initialize"
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
                    <q-card-section :class="filterSectionClass">
                        <div class="row q-col-gutter-sm">
                            <div :class="filterColClass">
                                <q-select
                                    v-model="currentYear"
                                    :options="yearOptions"
                                    label="Grad Year"
                                    dense
                                    options-dense
                                    outlined
                                    @update:model-value="onYearChange"
                                />
                            </div>
                            <div :class="filterColClass">
                                <q-select
                                    v-model="selectedTermCode"
                                    :options="termOptions"
                                    label="Term"
                                    :display-value="termDisplayValue"
                                    dense
                                    options-dense
                                    outlined
                                    clearable
                                    emit-value
                                    map-options
                                />
                            </div>
                            <div :class="filterColClass">
                                <q-select
                                    v-model="selectedRotationId"
                                    :options="rotationOptions"
                                    label="Rotation"
                                    :display-value="rotationDisplayValue"
                                    dense
                                    options-dense
                                    outlined
                                    clearable
                                    emit-value
                                    map-options
                                />
                            </div>
                            <div :class="filterColClass">
                                <q-select
                                    v-model="selectedArea"
                                    :options="areaOptions"
                                    label="Area"
                                    :display-value="areaDisplayValue"
                                    dense
                                    options-dense
                                    outlined
                                    clearable
                                    emit-value
                                    map-options
                                />
                            </div>
                            <div :class="filterColClass">
                                <q-select
                                    v-model="selectedModifier"
                                    :options="filteredModifierOptions"
                                    label="Modified By"
                                    stack-label
                                    placeholder="All users"
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
                            <div :class="filterColClass">
                                <q-select
                                    v-model="selectedPerson"
                                    :options="filteredPersonOptions"
                                    label="Person"
                                    stack-label
                                    placeholder="All people"
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
                            <div :class="dateColClass">
                                <q-input
                                    v-model="fromDate"
                                    type="date"
                                    label="Date From"
                                    dense
                                    outlined
                                    clearable
                                />
                            </div>
                            <div :class="dateColClass">
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
            <AuditLogResultsTable
                :entries="entries"
                :is-loading="isLoading"
            />
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from "vue"
import { useQuasar } from "quasar"
import { useDebounceFn } from "@vueuse/core"
import { usePermissionsStore } from "../stores/permissions"
import { usePermissionChecks } from "../composables/use-permission-checks"
import { AuditLogService } from "../services/audit-log-service"
import { RotationService } from "../services/rotation-service"
import { PageDataService } from "../services/page-data-service"
import StatusBanner from "@/components/StatusBanner.vue"
import SchedulerNavigation from "../components/SchedulerNavigation.vue"
import AccessDeniedCard from "../components/AccessDeniedCard.vue"
import AuditLogResultsTable from "../components/AuditLogResultsTable.vue"
import { ACCESS_DENIED_MESSAGES, ACCESS_DENIED_SUBTITLES } from "../constants/permission-messages"
import { useAuditEntries } from "../composables/use-audit-entries"

const INPUT_DEBOUNCE_MS = 300

const permissionsStore = usePermissionsStore()
const { isLoadingPermissions, goToHome } = usePermissionChecks()
const $q = useQuasar()

const { entries, isLoading, error, load } = useAuditEntries()
const ready = ref(false)

// Filters
const currentYear = ref<number | null>(null)
const selectedTermCode = ref<number | null>(null)
const selectedRotationId = ref<number | null>(null)
const selectedArea = ref<string | null>(null)
const selectedModifier = ref<string | null>(null)
const selectedPerson = ref<string | null>(null)
const fromDate = ref<string>("")
const toDate = ref<string>("")

// Filter dropdown options
const yearOptions = ref<number[]>([])
const termOptions = ref<{ label: string; value: number }[]>([])
const rotationOptions = ref<{ label: string; value: number }[]>([])
const modifierOptions = ref<{ label: string; value: string }[]>([])
const filteredModifierOptions = ref<{ label: string; value: string }[]>([])
const personOptions = ref<{ label: string; value: string }[]>([])
const filteredPersonOptions = ref<{ label: string; value: string }[]>([])

const areaOptions: { label: string; value: string }[] = [
    { label: "Clinician Schedules", value: "Clinicians" },
    { label: "Student Schedules", value: "Students" },
]

// Responsive layout classes, computed once so the template stays branch-free.
const filterColClass = computed(() => ($q.screen.gt.sm ? "col-12 col-md-6 col-lg-3" : "col-12"))
const dateColClass = computed(() => ($q.screen.gt.sm ? "col-12 col-md-6 col-lg-3" : "col-6"))
const filterSectionClass = computed(() => ($q.screen.gt.sm ? "q-pa-none" : ""))

// "All ..." placeholder text while a clearable filter is empty.
const termDisplayValue = computed(() => (selectedTermCode.value === null ? "All terms" : undefined))
const rotationDisplayValue = computed(() => (selectedRotationId.value === null ? "All rotations" : undefined))
const areaDisplayValue = computed(() => (selectedArea.value === null ? "All schedules" : undefined))

// Filters panel: always open on desktop, collapsible on mobile (mirrors the Effort audit trail).
const filtersExpanded = ref(false)
const filtersExpandedComputed = computed({
    get: () => ($q.screen.gt.sm ? true : filtersExpanded.value),
    set: (val: boolean) => {
        filtersExpanded.value = val
    },
})

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
    await load(() =>
        AuditLogService.getAuditLog({
            year: currentYear.value,
            termCode: selectedTermCode.value,
            rotationId: selectedRotationId.value,
            area: selectedArea.value,
            modifiedBy: selectedModifier.value,
            person: selectedPerson.value,
            from: fromDate.value,
            to: toDate.value,
        }),
    )
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

async function loadTermOptions() {
    const response = await AuditLogService.getTerms(currentYear.value ?? undefined)
    if (response.success) {
        termOptions.value = response.result.map((term) => ({ label: term.term, value: term.termCode }))
    }
}

async function onYearChange() {
    // Terms are grad-year specific, so reset the term filter and reload its options.
    selectedTermCode.value = null
    await loadTermOptions()
    void loadAuditTrail()
}

function clearFilters() {
    selectedTermCode.value = null
    selectedRotationId.value = null
    selectedArea.value = null
    selectedModifier.value = null
    selectedPerson.value = null
    fromDate.value = ""
    toDate.value = ""
}

// Auto-apply when any filter other than the year changes (the year loads immediately via onYearChange).
watch([selectedRotationId, selectedTermCode, selectedArea, selectedModifier, selectedPerson, fromDate, toDate], () => {
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
        await loadTermOptions()
        await loadAuditTrail()
    }
    ready.value = true
}

onMounted(() => {
    void initialize()
})
</script>
