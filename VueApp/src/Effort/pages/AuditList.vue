<template>
    <div class="q-pa-md">
        <h2>Audit Trail</h2>

        <!-- Filter Panel - Collapsible on mobile, always visible on desktop -->
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
                                v-model="filter.termCode"
                                :options="terms"
                                option-label="termName"
                                option-value="termCode"
                                label="Term"
                                dense
                                options-dense
                                outlined
                                clearable
                                emit-value
                                map-options
                                @update:model-value="onTermChange"
                            />
                        </div>
                        <div :class="$q.screen.gt.sm ? 'col-12 col-md-6 col-lg-3' : 'col-12'">
                            <q-select
                                v-model="filter.action"
                                :options="actions"
                                label="Action"
                                dense
                                options-dense
                                outlined
                                clearable
                            />
                        </div>
                        <div :class="$q.screen.gt.sm ? 'col-12 col-md-6 col-lg-3' : 'col-12'">
                            <q-select
                                v-model="filter.modifiedByPersonId"
                                :options="filteredModifiers"
                                option-label="fullName"
                                option-value="personId"
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
                                v-model="filter.instructorPersonId"
                                :options="filteredInstructors"
                                option-label="fullName"
                                option-value="personId"
                                label="Instructor"
                                dense
                                options-dense
                                outlined
                                clearable
                                emit-value
                                map-options
                                use-input
                                :input-debounce="INPUT_DEBOUNCE_MS"
                                @filter="filterInstructors"
                            />
                        </div>
                        <div :class="$q.screen.gt.sm ? 'col-12 col-md-6 col-lg-3' : 'col-12'">
                            <q-input
                                v-model="filter.searchText"
                                label="Search Changes"
                                dense
                                outlined
                                clearable
                            />
                        </div>
                        <div :class="$q.screen.gt.sm ? 'col-12 col-md-6 col-lg-3' : 'col-6'">
                            <q-input
                                v-model="filter.dateFrom"
                                label="Date From"
                                type="date"
                                dense
                                outlined
                                clearable
                            />
                        </div>
                        <div :class="$q.screen.gt.sm ? 'col-12 col-md-6 col-lg-3' : 'col-6'">
                            <q-input
                                v-model="filter.dateTo"
                                label="Date To"
                                type="date"
                                dense
                                outlined
                                clearable
                            />
                        </div>
                        <div :class="$q.screen.gt.sm ? 'col-12 col-md-6 col-lg-3' : 'col-6'">
                            <q-select
                                v-model="filter.subjectCode"
                                :options="subjectCodes"
                                label="Subject Code"
                                dense
                                options-dense
                                outlined
                                clearable
                                use-input
                                :input-debounce="INPUT_DEBOUNCE_MS"
                            />
                        </div>
                        <div :class="$q.screen.gt.sm ? 'col-12 col-md-6 col-lg-3' : 'col-6'">
                            <q-select
                                v-model="filter.courseNumber"
                                :options="courseNumbers"
                                label="Course Number"
                                dense
                                options-dense
                                outlined
                                clearable
                                use-input
                                :input-debounce="INPUT_DEBOUNCE_MS"
                            />
                        </div>
                    </div>
                    <div class="row q-mt-sm">
                        <div class="col-12 text-right">
                            <q-btn
                                label="Clear Filters"
                                color="grey"
                                dense
                                flat
                                @click="clearFilter"
                            />
                        </div>
                    </div>
                </q-card-section>
            </q-card>
        </q-expansion-item>

        <!-- Results Table - Desktop view (lg and up) -->
        <q-table
            v-if="$q.screen.gt.md"
            row-key="id"
            :rows="auditRows"
            :columns="columns"
            :loading="loading"
            v-model:pagination="pagination"
            :rows-per-page-options="[10, 25, 50, 100]"
            binary-state-sort
            dense
            flat
            bordered
            @request="loadAuditRows"
        >
            <template #body-cell-changedDate="props">
                <q-td :props="props">
                    {{ formatDateTime(props.row.changedDate, { dateStyle: "short", timeStyle: "short" }) }}
                </q-td>
            </template>
            <template #body-cell-action="props">
                <q-td :props="props">
                    <q-badge :color="getActionColor(props.row.action)">
                        {{ props.row.action }}
                    </q-badge>
                </q-td>
            </template>
            <template #body-cell-changes="props">
                <q-td :props="props">
                    <template v-if="props.row.changesDetail">
                        <div
                            v-for="(detail, key) in props.row.changesDetail"
                            :key="key"
                            class="text-caption"
                        >
                            <strong>{{ key }}:</strong>
                            <span
                                v-if="detail.oldValue !== null"
                                class="text-negative"
                                >{{ detail.oldValue }}</span
                            >
                            <span v-if="detail.oldValue !== null && detail.newValue !== null"> &rarr; </span>
                            <span
                                v-if="detail.newValue !== null"
                                class="text-positive"
                                >{{ detail.newValue }}</span
                            >
                        </div>
                    </template>
                    <template v-else-if="props.row.changes">
                        <span class="text-grey-7">{{ props.row.changes }}</span>
                    </template>
                </q-td>
            </template>
        </q-table>

        <!-- Results Table - Tablet view (sm and md) with stacked Action/Changes -->
        <q-table
            v-else-if="$q.screen.gt.xs"
            row-key="id"
            :rows="auditRows"
            :columns="tabletColumns"
            :loading="loading"
            v-model:pagination="pagination"
            :rows-per-page-options="[10, 25, 50, 100]"
            binary-state-sort
            dense
            flat
            bordered
            @request="loadAuditRows"
        >
            <template #body="props">
                <q-tr :props="props">
                    <q-td
                        key="changedDate"
                        :props="props"
                    >
                        {{ formatDateTime(props.row.changedDate, { dateStyle: "short", timeStyle: "short" }) }}
                    </q-td>
                    <q-td
                        key="changedByName"
                        :props="props"
                    >
                        {{ props.row.changedByName }}
                    </q-td>
                    <q-td
                        key="instructorName"
                        :props="props"
                    >
                        {{ props.row.instructorName }}
                    </q-td>
                    <q-td
                        key="termName"
                        :props="props"
                    >
                        {{ props.row.termName }}
                    </q-td>
                    <q-td
                        key="courseCode"
                        :props="props"
                    >
                        {{ props.row.courseCode }}
                    </q-td>
                </q-tr>
                <!-- Second row for Action and Changes -->
                <q-tr
                    :props="props"
                    class="audit-detail-row"
                >
                    <q-td
                        colspan="5"
                        class="bg-grey-1 q-py-xs"
                    >
                        <div class="row items-start q-gutter-sm">
                            <q-badge
                                :color="getActionColor(props.row.action)"
                                class="q-mr-sm"
                            >
                                {{ props.row.action }}
                            </q-badge>
                            <div
                                v-if="props.row.changesDetail"
                                class="text-caption"
                            >
                                <span
                                    v-for="([key, detail], idx) in Object.entries(props.row.changesDetail) as [
                                        string,
                                        ChangeDetail,
                                    ][]"
                                    :key="key"
                                >
                                    <span v-if="idx > 0"> &bull; </span>
                                    <strong>{{ key }}:</strong>
                                    <span
                                        v-if="detail.oldValue !== null"
                                        class="text-negative"
                                        >{{ detail.oldValue }}</span
                                    >
                                    <span v-if="detail.oldValue !== null && detail.newValue !== null"> &rarr; </span>
                                    <span
                                        v-if="detail.newValue !== null"
                                        class="text-positive"
                                        >{{ detail.newValue }}</span
                                    >
                                </span>
                            </div>
                            <span
                                v-else-if="props.row.changes"
                                class="text-caption text-grey-7"
                                >{{ props.row.changes }}</span
                            >
                        </div>
                    </q-td>
                </q-tr>
            </template>
        </q-table>

        <!-- Results Table - Mobile view (xs) with card layout -->
        <q-table
            v-else
            row-key="id"
            :rows="auditRows"
            :columns="columns"
            :loading="loading"
            v-model:pagination="pagination"
            :rows-per-page-options="[10, 25, 50, 100]"
            grid
            binary-state-sort
            dense
            flat
            bordered
            @request="loadAuditRows"
        >
            <template #item="props">
                <div class="q-pa-xs col-12">
                    <q-card
                        flat
                        bordered
                    >
                        <q-card-section class="q-pa-sm">
                            <div class="row items-center q-mb-xs">
                                <q-badge
                                    :color="getActionColor(props.row.action)"
                                    class="q-mr-sm"
                                >
                                    {{ props.row.action }}
                                </q-badge>
                                <span class="text-caption text-grey-7">
                                    {{
                                        formatDateTime(props.row.changedDate, {
                                            dateStyle: "short",
                                            timeStyle: "short",
                                        })
                                    }}
                                </span>
                            </div>
                            <div
                                v-if="props.row.instructorName"
                                class="text-subtitle2"
                            >
                                {{ props.row.instructorName }}
                            </div>
                            <div class="text-caption text-grey-8">
                                <span v-if="props.row.termName">{{ props.row.termName }}</span>
                                <span v-if="props.row.termName && props.row.courseCode"> &bull; </span>
                                <span v-if="props.row.courseCode">{{ props.row.courseCode }}</span>
                            </div>
                            <div class="text-caption q-mt-xs">
                                <span class="text-grey-7">by</span> {{ props.row.changedByName }}
                            </div>
                            <q-separator
                                v-if="props.row.changesDetail || props.row.changes"
                                class="q-my-xs"
                            />
                            <div
                                v-if="props.row.changesDetail"
                                class="text-caption"
                            >
                                <div
                                    v-for="(detail, key) in props.row.changesDetail"
                                    :key="key"
                                >
                                    <strong>{{ key }}:</strong>
                                    <span
                                        v-if="detail.oldValue !== null"
                                        class="text-negative"
                                        >{{ detail.oldValue }}</span
                                    >
                                    <span v-if="detail.oldValue !== null && detail.newValue !== null"> &rarr; </span>
                                    <span
                                        v-if="detail.newValue !== null"
                                        class="text-positive"
                                        >{{ detail.newValue }}</span
                                    >
                                </div>
                            </div>
                            <div
                                v-else-if="props.row.changes"
                                class="text-caption text-grey-7"
                            >
                                {{ props.row.changes }}
                            </div>
                        </q-card-section>
                    </q-card>
                </div>
            </template>
        </q-table>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted, watch } from "vue"
import type { Ref } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useDebounceFn } from "@vueuse/core"
import { useQuasar } from "quasar"
import type { QTableColumn, QTableProps } from "quasar"
import { effortAuditService } from "../services/audit-service"
import { effortService } from "../services/effort-service"
import { useDateFunctions } from "@/composables/DateFunctions"
import type { ChangeDetail, EffortAuditRow, ModifierInfo, TermDto } from "../types"

// Constants
const INPUT_DEBOUNCE_MS = 300

const route = useRoute()
const router = useRouter()
const $q = useQuasar()
const { formatDateTime } = useDateFunctions()

// Filter state
const filtersExpanded = ref(false)
const filtersExpandedComputed = computed({
    get: () => ($q.screen.gt.sm ? true : filtersExpanded.value),
    set: (val: boolean) => {
        filtersExpanded.value = val
    },
})
const isInitializing = ref(true)
const filter = ref({
    termCode: null as number | null,
    action: null as string | null,
    modifiedByPersonId: null as number | null,
    instructorPersonId: null as number | null,
    searchText: null as string | null,
    dateFrom: null as string | null,
    dateTo: null as string | null,
    subjectCode: null as string | null,
    courseNumber: null as string | null,
})

// Filter options
const terms = ref<TermDto[]>([])
const actions = ref<string[]>([])
const modifiers = ref<ModifierInfo[]>([])
const filteredModifiers = ref<ModifierInfo[]>([])
const instructors = ref<ModifierInfo[]>([])
const filteredInstructors = ref<ModifierInfo[]>([])
const subjectCodes = ref<string[]>([])
const courseNumbers = ref<string[]>([])
const allSubjectCodes = ref<string[]>([])
const allCourseNumbers = ref<string[]>([])

// Table state
const auditRows = ref<EffortAuditRow[]>([])
const loading = ref(false)
const pagination = ref({
    page: 1,
    rowsPerPage: 25,
    rowsNumber: 0,
    sortBy: "changedDate",
    descending: true,
}) as Ref<QTableProps["pagination"]>

const columns: QTableColumn[] = [
    {
        name: "changedDate",
        label: "Date",
        field: "changedDate",
        align: "left",
        sortable: true,
    },
    {
        name: "changedByName",
        label: "Modified By",
        field: "changedByName",
        align: "left",
        sortable: true,
    },
    {
        name: "instructorName",
        label: "Instructor",
        field: "instructorName",
        align: "left",
        sortable: true,
    },
    {
        name: "termName",
        label: "Term",
        field: "termName",
        align: "left",
        sortable: true,
    },
    {
        name: "courseCode",
        label: "Course",
        field: "courseCode",
        align: "left",
        sortable: true,
    },
    {
        name: "action",
        label: "Action",
        field: "action",
        align: "left",
        sortable: true,
    },
    {
        name: "changes",
        label: "Changes",
        field: "changes",
        align: "left",
        sortable: false,
    },
]

// Tablet columns - same as desktop but without Action and Changes (they appear in a second row)
const tabletColumns: QTableColumn[] = columns.slice(0, 5)

function getActionColor(action: string): string {
    if (action.startsWith("Create")) return "positive"
    if (action.startsWith("Update")) return "primary"
    if (action.startsWith("Delete")) return "negative"
    if (action.startsWith("Open") || action.startsWith("Reopen")) return "secondary"
    if (action.startsWith("Close")) return "warning"
    if (action.startsWith("Import")) return "info"
    return "grey"
}

async function loadFilterOptions() {
    const [termsResult, actionsResult, modifiersResult, instructorsResult, subjectCodesResult, courseNumbersResult] =
        await Promise.all([
            effortService.getTerms(),
            effortAuditService.getActions(),
            effortAuditService.getModifiers(),
            effortAuditService.getInstructors(),
            effortAuditService.getSubjectCodes(filter.value.termCode),
            effortAuditService.getCourseNumbers(filter.value.termCode),
        ])
    terms.value = termsResult
    actions.value = actionsResult
    modifiers.value = modifiersResult
    filteredModifiers.value = modifiersResult
    instructors.value = instructorsResult
    filteredInstructors.value = instructorsResult
    subjectCodes.value = subjectCodesResult
    courseNumbers.value = courseNumbersResult
    allSubjectCodes.value = subjectCodesResult
    allCourseNumbers.value = courseNumbersResult
}

function filterModifiers(val: string, update: (fn: () => void) => void) {
    update(() => {
        if (!val) {
            filteredModifiers.value = modifiers.value
        } else {
            const needle = val.toLowerCase()
            filteredModifiers.value = modifiers.value.filter((m) => m.fullName.toLowerCase().includes(needle))
        }
    })
}

function filterInstructors(val: string, update: (fn: () => void) => void) {
    update(() => {
        if (!val) {
            filteredInstructors.value = instructors.value
        } else {
            const needle = val.toLowerCase()
            filteredInstructors.value = instructors.value.filter((i) => i.fullName.toLowerCase().includes(needle))
        }
    })
}

async function updateSubjectCodesForCourseNumber() {
    if (filter.value.courseNumber) {
        subjectCodes.value = await effortAuditService.getSubjectCodes(filter.value.termCode, filter.value.courseNumber)
    } else {
        subjectCodes.value = allSubjectCodes.value
    }
}

async function updateCourseNumbersForSubjectCode() {
    if (filter.value.subjectCode) {
        courseNumbers.value = await effortAuditService.getCourseNumbers(filter.value.termCode, filter.value.subjectCode)
    } else {
        courseNumbers.value = allCourseNumbers.value
    }
}

async function refreshCourseDropdownsForTerm() {
    // Fetch subject codes and course numbers filtered by the current term
    // Also pass any existing subjectCode/courseNumber filters for bidirectional filtering
    const [newSubjectCodes, newCourseNumbers] = await Promise.all([
        effortAuditService.getSubjectCodes(filter.value.termCode, filter.value.courseNumber),
        effortAuditService.getCourseNumbers(filter.value.termCode, filter.value.subjectCode),
    ])

    // Update "all" options for the current term (used when other filter is cleared)
    const [allCodesForTerm, allNumbersForTerm] = await Promise.all([
        effortAuditService.getSubjectCodes(filter.value.termCode, null),
        effortAuditService.getCourseNumbers(filter.value.termCode, null),
    ])
    allSubjectCodes.value = allCodesForTerm
    allCourseNumbers.value = allNumbersForTerm

    // Clear invalid selections if they don't exist in the new term's data
    if (filter.value.subjectCode && !newSubjectCodes.includes(filter.value.subjectCode)) {
        filter.value.subjectCode = null
    }
    if (filter.value.courseNumber && !newCourseNumbers.includes(filter.value.courseNumber)) {
        filter.value.courseNumber = null
    }

    // Update the dropdown options
    subjectCodes.value = newSubjectCodes
    courseNumbers.value = newCourseNumbers
}

async function loadAuditRows(props: { pagination: QTableProps["pagination"] }) {
    if (!props.pagination) return

    const { page, rowsPerPage, sortBy, descending } = props.pagination
    loading.value = true

    try {
        const params = buildSearchParams(page ?? 1, rowsPerPage ?? 25, sortBy ?? "changedDate", descending ?? true)
        const { result, pagination: resultPagination } = await effortAuditService.getAuditEntries(params)

        auditRows.value = result
        pagination.value = {
            ...pagination.value,
            page: page ?? 1,
            rowsPerPage: rowsPerPage ?? 25,
            sortBy: sortBy ?? "changedDate",
            descending: descending ?? true,
            rowsNumber: resultPagination?.totalRecords ?? 0,
        }
    } finally {
        loading.value = false
    }
}

function updateUrlParams() {
    const query: Record<string, string> = {}

    if (filter.value.termCode !== null) {
        query.termCode = filter.value.termCode.toString()
    }
    if (filter.value.action !== null) {
        query.action = filter.value.action
    }
    if (filter.value.modifiedByPersonId !== null) {
        query.modifiedByPersonId = filter.value.modifiedByPersonId.toString()
    }
    if (filter.value.instructorPersonId !== null) {
        query.instructorPersonId = filter.value.instructorPersonId.toString()
    }
    if (filter.value.searchText !== null && filter.value.searchText.trim() !== "") {
        query.searchText = filter.value.searchText
    }
    if (filter.value.dateFrom !== null) {
        query.dateFrom = filter.value.dateFrom
    }
    if (filter.value.dateTo !== null) {
        query.dateTo = filter.value.dateTo
    }
    if (filter.value.subjectCode !== null && filter.value.subjectCode.trim() !== "") {
        query.subjectCode = filter.value.subjectCode
    }
    if (filter.value.courseNumber !== null && filter.value.courseNumber.trim() !== "") {
        query.courseNumber = filter.value.courseNumber
    }

    router.replace({ query })
}

function buildSearchParams(page: number, perPage: number, sortBy: string, descending: boolean): URLSearchParams {
    const params = new URLSearchParams()

    params.append("page", page.toString())
    params.append("perPage", perPage.toString())
    params.append("sortBy", sortBy)
    params.append("descending", descending.toString())

    if (filter.value.termCode !== null) {
        params.append("termCode", filter.value.termCode.toString())
    }
    if (filter.value.action !== null) {
        params.append("action", filter.value.action)
    }
    if (filter.value.modifiedByPersonId !== null) {
        params.append("modifiedByPersonId", filter.value.modifiedByPersonId.toString())
    }
    if (filter.value.instructorPersonId !== null) {
        params.append("instructorPersonId", filter.value.instructorPersonId.toString())
    }
    if (filter.value.searchText !== null && filter.value.searchText.trim() !== "") {
        params.append("searchText", filter.value.searchText)
    }
    if (filter.value.dateFrom !== null) {
        params.append("dateFrom", filter.value.dateFrom)
    }
    if (filter.value.dateTo !== null) {
        params.append("dateTo", filter.value.dateTo)
    }
    if (filter.value.subjectCode !== null && filter.value.subjectCode.trim() !== "") {
        params.append("subjectCode", filter.value.subjectCode)
    }
    if (filter.value.courseNumber !== null && filter.value.courseNumber.trim() !== "") {
        params.append("courseNumber", filter.value.courseNumber)
    }

    return params
}

async function clearFilter() {
    filter.value = {
        termCode: null,
        action: null,
        modifiedByPersonId: null,
        instructorPersonId: null,
        searchText: null,
        dateFrom: null,
        dateTo: null,
        subjectCode: null,
        courseNumber: null,
    }
    // Reload unfiltered dropdown options
    const [newSubjectCodes, newCourseNumbers] = await Promise.all([
        effortAuditService.getSubjectCodes(null, null),
        effortAuditService.getCourseNumbers(null, null),
    ])
    subjectCodes.value = newSubjectCodes
    courseNumbers.value = newCourseNumbers
    allSubjectCodes.value = newSubjectCodes
    allCourseNumbers.value = newCourseNumbers
    router.replace({ query: {} })
    await applyFilters()
}

async function applyFilters() {
    updateUrlParams()
    await loadAuditRows({
        pagination: {
            ...pagination.value,
            page: 1,
        },
    })
}

const debouncedApplyFilters = useDebounceFn(applyFilters, 400)

/**
 * Handle term selection change - refreshes subject code and course number dropdowns
 * to show only options available for the selected term.
 * @param newTermCode The newly selected term code (passed from @update:model-value)
 */
async function onTermChange(newTermCode: number | null) {
    if (isInitializing.value) return
    // The @update:model-value event fires with the new value, but v-model may not be updated yet
    // Set the filter value explicitly to ensure it's available for API calls
    filter.value.termCode = newTermCode
    await refreshCourseDropdownsForTerm()
    await applyFilters()
}

// Watch for subject code changes
watch(
    () => filter.value.subjectCode,
    async (newSubjectCode, oldSubjectCode) => {
        if (isInitializing.value) return
        if (newSubjectCode !== oldSubjectCode) {
            await updateCourseNumbersForSubjectCode()
            await applyFilters()
        }
    },
)

// Watch for course number changes
watch(
    () => filter.value.courseNumber,
    async (newCourseNumber, oldCourseNumber) => {
        if (isInitializing.value) return
        if (newCourseNumber !== oldCourseNumber) {
            await updateSubjectCodesForCourseNumber()
            await applyFilters()
        }
    },
)

// Watch for search text changes with debounce
watch(
    () => filter.value.searchText,
    (newSearchText, oldSearchText) => {
        if (isInitializing.value) return
        if (newSearchText !== oldSearchText) {
            debouncedApplyFilters()
        }
    },
)

// Watch for other filter changes and auto-apply
watch(
    [
        () => filter.value.action,
        () => filter.value.modifiedByPersonId,
        () => filter.value.instructorPersonId,
        () => filter.value.dateFrom,
        () => filter.value.dateTo,
    ],
    async () => {
        if (isInitializing.value) return
        await applyFilters()
    },
)

onMounted(async () => {
    // Restore filter state from URL query parameters
    const q = route.query
    if (q.termCode) {
        filter.value.termCode = parseInt(q.termCode as string, 10)
    }
    if (q.action) {
        filter.value.action = q.action as string
    }
    if (q.modifiedByPersonId) {
        filter.value.modifiedByPersonId = parseInt(q.modifiedByPersonId as string, 10)
    }
    if (q.instructorPersonId) {
        filter.value.instructorPersonId = parseInt(q.instructorPersonId as string, 10)
    }
    if (q.searchText) {
        filter.value.searchText = q.searchText as string
    }
    if (q.dateFrom) {
        filter.value.dateFrom = q.dateFrom as string
    }
    if (q.dateTo) {
        filter.value.dateTo = q.dateTo as string
    }
    if (q.subjectCode) {
        filter.value.subjectCode = q.subjectCode as string
    }
    if (q.courseNumber) {
        filter.value.courseNumber = q.courseNumber as string
    }

    await loadFilterOptions()

    // Update bidirectional filter options based on URL params
    if (filter.value.subjectCode) {
        await updateCourseNumbersForSubjectCode()
    }
    if (filter.value.courseNumber) {
        await updateSubjectCodesForCourseNumber()
    }

    await loadAuditRows({
        pagination: pagination.value,
    })

    isInitializing.value = false
})
</script>
