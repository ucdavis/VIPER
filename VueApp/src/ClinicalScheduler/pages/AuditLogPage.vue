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
                :header-style="$q.screen.gt.sm ? 'display: none' : ''"
                class="q-mb-md"
            >
                <q-card flat>
                    <q-card-section :class="$q.screen.gt.sm ? 'q-pa-none' : ''">
                        <div class="row q-col-gutter-sm">
                            <div :class="$q.screen.gt.sm ? 'col-12 col-md-6 col-lg-3' : 'col-12'">
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
                            <div :class="$q.screen.gt.sm ? 'col-12 col-md-6 col-lg-3' : 'col-12'">
                                <q-select
                                    v-model="selectedTermCode"
                                    :options="termOptions"
                                    label="Term"
                                    :display-value="selectedTermCode == null ? 'All terms' : undefined"
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
                                    v-model="selectedRotationId"
                                    :options="rotationOptions"
                                    label="Rotation"
                                    :display-value="selectedRotationId == null ? 'All rotations' : undefined"
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
                                    :display-value="selectedArea == null ? 'All schedules' : undefined"
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
                            <div :class="$q.screen.gt.sm ? 'col-12 col-md-6 col-lg-3' : 'col-12'">
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

            <!-- Results - Desktop (lg+): full table -->
            <q-table
                v-if="$q.screen.gt.md"
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
                        <StatusBadge :color="getAuditActionColor(props.row.action)">
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

            <!-- Results - Tablet (sm/md): main columns + a stacked Action/Week/Area row -->
            <q-table
                v-else-if="$q.screen.gt.xs"
                row-key="scheduleAuditId"
                :rows="entries"
                :columns="tabletColumns"
                :loading="isLoading"
                v-model:pagination="pagination"
                :rows-per-page-options="[10, 25, 50, 100]"
                binary-state-sort
                dense
                flat
                bordered
            >
                <template #body="props">
                    <q-tr :props="props">
                        <q-td
                            key="timeStamp"
                            :props="props"
                        >
                            {{ formatDateTime(props.row.timeStamp, { dateStyle: "short", timeStyle: "short" }) }}
                        </q-td>
                        <q-td
                            key="modifiedByName"
                            :props="props"
                        >
                            {{ props.row.modifiedByName }}
                        </q-td>
                        <q-td
                            key="personName"
                            :props="props"
                        >
                            {{ props.row.personName }}
                        </q-td>
                        <q-td
                            key="rotationName"
                            :props="props"
                        >
                            {{ props.row.rotationName }}
                        </q-td>
                        <q-td
                            key="term"
                            :props="props"
                        >
                            {{ props.row.term }}
                        </q-td>
                    </q-tr>
                    <q-tr :props="props">
                        <q-td
                            :colspan="tabletColumns.length"
                            class="bg-grey-1 q-py-xs"
                        >
                            <div class="row items-center q-gutter-sm">
                                <StatusBadge :color="getAuditActionColor(props.row.action)">
                                    {{ props.row.action }}
                                </StatusBadge>
                                <span
                                    v-if="formatWeekCell(props.row.weekNum, props.row.weekStart)"
                                    class="text-caption text-grey-8"
                                >
                                    {{ formatWeekCell(props.row.weekNum, props.row.weekStart) }}
                                </span>
                                <span class="text-caption text-grey-7">{{ props.row.area }}</span>
                            </div>
                        </q-td>
                    </q-tr>
                </template>
            </q-table>

            <!-- Results - Mobile (xs): card layout -->
            <q-table
                v-else
                row-key="scheduleAuditId"
                :rows="entries"
                :columns="columns"
                :loading="isLoading"
                v-model:pagination="pagination"
                :rows-per-page-options="[10, 25, 50, 100]"
                grid
                binary-state-sort
                dense
                flat
                bordered
            >
                <template #item="props">
                    <div class="q-pa-xs col-12">
                        <q-card
                            flat
                            bordered
                        >
                            <q-card-section class="q-pa-sm">
                                <div class="row items-center q-mb-xs">
                                    <StatusBadge
                                        :color="getAuditActionColor(props.row.action)"
                                        class="q-mr-sm"
                                    >
                                        {{ props.row.action }}
                                    </StatusBadge>
                                    <span class="text-caption text-grey-7">
                                        {{
                                            formatDateTime(props.row.timeStamp, {
                                                dateStyle: "short",
                                                timeStyle: "short",
                                            })
                                        }}
                                    </span>
                                </div>
                                <div
                                    v-if="props.row.personName"
                                    class="text-subtitle2"
                                >
                                    {{ props.row.personName }}
                                </div>
                                <div class="text-caption text-grey-8">
                                    <span v-if="props.row.rotationName">{{ props.row.rotationName }}</span>
                                    <span v-if="props.row.rotationName && props.row.term"> &bull; </span>
                                    <span v-if="props.row.term">{{ props.row.term }}</span>
                                </div>
                                <div
                                    v-if="formatWeekCell(props.row.weekNum, props.row.weekStart)"
                                    class="text-caption text-grey-8"
                                >
                                    {{ formatWeekCell(props.row.weekNum, props.row.weekStart) }}
                                </div>
                                <div class="text-caption q-mt-xs">
                                    <span class="text-grey-7">by</span> {{ props.row.modifiedByName }}
                                    <span class="text-grey-7"> &middot; </span>{{ props.row.area }}
                                </div>
                            </q-card-section>
                        </q-card>
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
import { getAuditActionColor } from "../utils/audit-actions"
import { useAuditEntries } from "../composables/use-audit-entries"
import type { AuditLogEntry } from "../types/audit-types"

const INPUT_DEBOUNCE_MS = 300

const permissionsStore = usePermissionsStore()
const { isLoadingPermissions, goToHome } = usePermissionChecks()
const $q = useQuasar()
const { formatDate, formatDateTime } = useDateFunctions()

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

function formatWeekCell(weekNum: number, weekStart: string | null): string {
    const date = weekStart ? formatDate(weekStart) : ""
    if (weekNum) {
        return date ? `Wk ${weekNum} · ${date}` : `Wk ${weekNum}`
    }
    return date
}

const columns: QTableColumn<AuditLogEntry>[] = [
    { name: "timeStamp", label: "Date", field: "timeStamp", align: "left", sortable: true },
    { name: "modifiedByName", label: "Modified By", field: "modifiedByName", align: "left", sortable: true },
    { name: "personName", label: "Person", field: "personName", align: "left", sortable: true },
    { name: "area", label: "Area", field: "area", align: "left", sortable: true },
    { name: "rotationName", label: "Rotation", field: "rotationName", align: "left", sortable: true },
    { name: "term", label: "Term", field: "term", align: "left", sortable: true },
    {
        name: "week",
        label: "Week",
        field: "weekNum",
        align: "left",
        sortable: true,
        format: (val: number, row: AuditLogEntry) => formatWeekCell(val, row.weekStart),
    },
    { name: "action", label: "Action", field: "action", align: "left", sortable: true },
]

// Tablet (sm/md): a reduced column set; Action/Week/Area move to a stacked detail row.
const tabletColumns = columns.filter((column) =>
    ["timeStamp", "modifiedByName", "personName", "rotationName", "term"].includes(column.name),
)

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
