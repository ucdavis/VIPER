<template>
    <div class="q-pa-md">
        <h2>Merit &amp; Promotion Report - Multi-Year</h2>

        <!-- Step 1: Select Instructor (shown when no instructor selected) -->
        <div
            v-if="!selectedPersonId"
            class="filter-form q-mb-md"
        >
            <div class="row q-col-gutter-sm items-end">
                <div class="col-12 col-sm-6">
                    <q-select
                        v-model="selectedPersonId"
                        :options="displayedPersonOptions"
                        option-label="fullName"
                        option-value="personId"
                        emit-value
                        map-options
                        label="Select Instructor"
                        dense
                        options-dense
                        outlined
                        use-input
                        fill-input
                        hide-selected
                        input-debounce="200"
                        @filter="filterPersons"
                        @update:model-value="onInstructorSelected"
                        @input-value="(v: string) => (personInput = v)"
                        @popup-hide="
                            () => {
                                if (!personInput) selectedPersonId = null
                            }
                        "
                    >
                        <template #no-option>
                            <q-item>
                                <q-item-section class="text-grey">No results</q-item-section>
                            </q-item>
                        </template>
                    </q-select>
                </div>
            </div>
            <div class="text-grey-6 text-center q-pa-lg">Select an instructor to configure the report.</div>
            <q-banner
                class="bg-blue-1 text-caption q-mt-md"
                rounded
            >
                <template #avatar>
                    <q-icon
                        name="info"
                        color="primary"
                    />
                </template>
                <p class="q-mb-xs">
                    Faculty count for department averages is a sum of all faculty for the time period specified.
                </p>
                <p class="q-mb-none">
                    The "Department" used for that average is based on the LAST entry in the review period for the
                    reviewee.
                </p>
            </q-banner>
        </div>

        <!-- Step 2: Configure & Generate (after instructor selected) -->
        <div v-else>
            <div class="q-mb-sm">
                <q-btn
                    flat
                    dense
                    icon="arrow_back"
                    label="Change Instructor"
                    @click="clearInstructor"
                />
            </div>

            <div class="text-subtitle1 text-weight-medium q-mb-md">
                Selected Instructor &mdash; {{ selectedPersonName }}
            </div>

            <!-- Year range selectors -->
            <div class="filter-form q-mb-md">
                <div class="row q-col-gutter-sm items-start q-mb-sm">
                    <div class="col-6 col-sm-3">
                        <q-select
                            v-model="selectedStartYear"
                            :options="yearOptions"
                            label="Start Year"
                            dense
                            options-dense
                            outlined
                            emit-value
                            map-options
                        />
                        <div class="text-caption text-grey-7 q-mt-xs">
                            Teaching review period starts on January 1st of this year
                        </div>
                    </div>
                    <div class="col-6 col-sm-3">
                        <q-select
                            v-model="selectedEndYear"
                            :options="endYearOptions"
                            label="End Year"
                            dense
                            options-dense
                            outlined
                            emit-value
                            map-options
                        />
                        <div class="text-caption text-grey-7 q-mt-xs">
                            Teaching review period ends on December 31st of the previous year
                        </div>
                    </div>
                </div>

                <!-- Sabbatical/Leave section -->
                <div class="sabbatical-section q-mb-sm q-pa-sm bg-grey-1 rounded-borders">
                    <div
                        v-if="sabbaticalLoading"
                        class="text-grey"
                    >
                        Loading leave data...
                    </div>
                    <template v-else-if="!hasSabbaticalData">
                        <p class="text-body2 q-mb-none">
                            If you need to view a Multi-Year report with excluded terms, please contact the VMDO
                            Personnel office.
                        </p>
                    </template>
                    <template v-else>
                        <p class="text-body2 q-mb-xs">Due to leave, this report will exclude:</p>
                        <ul class="q-ml-md q-mb-xs">
                            <li v-if="sabbaticalClinTermNames.length > 0">
                                Clinical Terms: {{ sabbaticalClinTermNames.join(", ") }}
                            </li>
                            <li v-if="sabbaticalDidTermNames.length > 0">
                                Didactic Terms: {{ sabbaticalDidTermNames.join(", ") }}
                            </li>
                        </ul>
                        <p class="text-caption text-grey-7 q-mb-none">
                            If you feel these leave parameters are incorrect, please contact the VMDO Personnel office.
                        </p>
                    </template>
                    <q-btn
                        v-if="isAdmin"
                        flat
                        dense
                        color="primary"
                        icon="edit"
                        label="Edit Leave Data"
                        class="q-mt-xs"
                        @click="showLeaveEditor = true"
                    />
                </div>

                <!-- Calendar/Academic year toggle -->
                <div class="row items-center q-mb-sm">
                    <q-option-group
                        v-model="useAcademicYear"
                        :options="yearTypeOptions"
                        type="radio"
                        inline
                        dense
                    />
                </div>

                <!-- Generate + Print buttons -->
                <div class="row items-center q-mb-md">
                    <span>
                        <q-btn
                            color="primary"
                            icon="assessment"
                            label="Generate Report"
                            :loading="loading"
                            :disable="generateDisabled"
                            @click="generateReport"
                        >
                            <template #loading>
                                <q-spinner
                                    size="1em"
                                    class="q-mr-sm"
                                />
                                Generate Report
                            </template>
                        </q-btn>
                        <q-tooltip v-if="generateDisabled">Select year range to generate the report</q-tooltip>
                    </span>
                    <q-space />
                    <q-btn
                        v-if="report"
                        outline
                        dense
                        icon="print"
                        label="Print/PDF"
                        :loading="printLoading"
                        @click="handlePrint"
                    >
                        <template #loading>
                            <q-spinner
                                size="1em"
                                class="q-mr-sm"
                            />
                            Print/PDF
                        </template>
                    </q-btn>
                </div>
            </div>

            <!-- Bottom notes -->
            <q-banner
                v-if="!report && !loading"
                class="bg-blue-1 text-caption q-mt-sm"
                rounded
            >
                <template #avatar>
                    <q-icon
                        name="info"
                        color="primary"
                    />
                </template>
                <p class="q-mb-xs">
                    Faculty count for department averages is a sum of all faculty for the time period specified.
                </p>
                <p class="q-mb-none">
                    The "Department" used for that average is based on the LAST entry in the review period for the
                    reviewee.
                </p>
            </q-banner>
        </div>

        <!-- Loading state -->
        <div
            v-if="loading"
            class="text-grey q-my-lg"
        >
            Loading report...
        </div>

        <!-- Report content -->
        <ReportLayout v-else-if="report">
            <template #header />

            <template v-if="hasMeritData || hasEvalData">
                <!-- Section 1: Merit Activity -->
                <template v-if="hasMeritData">
                    <h3 class="report-section-title">
                        Merit &amp; Promotion Multi-Year Report &mdash; {{ report.instructor }}
                    </h3>
                    <div class="report-section-subtitle">
                        {{ report.startYear }} &ndash; {{ report.endYear }} ({{
                            report.useAcademicYear ? "Academic" : "Calendar"
                        }}
                        Year)
                    </div>
                    <div
                        v-if="report.excludedClinicalTerms.length > 0 || report.excludedDidacticTerms.length > 0"
                        class="q-mb-sm"
                    >
                        <p
                            v-if="report.excludedClinicalTerms.length > 0"
                            class="q-mb-xs text-bold"
                        >
                            Excluding Clinical Terms: {{ report.excludedClinicalTerms.join(", ") }}
                        </p>
                        <p
                            v-if="report.excludedDidacticTerms.length > 0"
                            class="q-mb-xs text-bold"
                        >
                            Excluding Didactic Terms: {{ report.excludedDidacticTerms.join(", ") }}
                        </p>
                    </div>

                    <table class="report-table q-mb-lg">
                        <colgroup>
                            <col style="width: 4rem" />
                            <col style="width: 3rem" />
                            <col style="width: 4rem" />
                            <col style="width: 3rem" />
                            <col style="min-width: 12rem" />
                            <col
                                v-for="type in orderedEffortTypes"
                                :key="`col_${type}`"
                            />
                        </colgroup>
                        <tbody>
                            <template
                                v-for="meritYear in report.meritSection.years"
                                :key="meritYear.year"
                            >
                                <!-- Year header -->
                                <tr class="year-header-row">
                                    <th
                                        :colspan="5 + orderedEffortTypes.length"
                                        class="text-left"
                                    >
                                        Year {{ meritYear.yearLabel }}
                                    </th>
                                </tr>
                                <!-- Column headers (repeated per year, matching legacy) -->
                                <tr>
                                    <th class="col-qtr">Qtr</th>
                                    <th class="col-role">Role</th>
                                    <th class="col-enrl">Enrolled</th>
                                    <th class="col-units">Units</th>
                                    <th class="col-course">Course</th>
                                    <th
                                        v-for="type in orderedEffortTypes"
                                        :key="type"
                                        class="col-effort"
                                        :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                    >
                                        {{ type }}
                                    </th>
                                </tr>
                                <!-- Course rows -->
                                <tr
                                    v-for="(course, idx) in meritYear.courses"
                                    :key="`${meritYear.year}_${idx}`"
                                >
                                    <td>{{ course.termCode }}</td>
                                    <td>{{ course.role }}</td>
                                    <td>{{ course.enrollment || "" }}</td>
                                    <td>{{ formatUnits(course.units) }}</td>
                                    <td>{{ course.course }}</td>
                                    <td
                                        v-for="type in orderedEffortTypes"
                                        :key="type"
                                        :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                    >
                                        {{ getTotalValue(course.efforts, type) }}
                                    </td>
                                </tr>
                                <!-- Year subtotal -->
                                <tr class="totals-row bg-grey-1">
                                    <td></td>
                                    <td></td>
                                    <td></td>
                                    <td></td>
                                    <th class="subt">Total for {{ meritYear.yearLabel }}</th>
                                    <td
                                        v-for="type in orderedEffortTypes"
                                        :key="type"
                                        class="total"
                                        :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                    >
                                        {{ getTotalValue(meritYear.yearTotals, type) }}
                                    </td>
                                </tr>
                            </template>
                            <!-- Instructor total -->
                            <tr class="totals-row bg-grey-2">
                                <td></td>
                                <td></td>
                                <td></td>
                                <td></td>
                                <th class="subt">{{ report.instructor }} Total</th>
                                <td
                                    v-for="type in orderedEffortTypes"
                                    :key="type"
                                    class="total"
                                    :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                >
                                    {{ getTotalValue(report.meritSection.grandTotals, type) }}
                                </td>
                            </tr>
                            <!-- Yearly average -->
                            <tr class="totals-row">
                                <th
                                    class="subt"
                                    colspan="5"
                                >
                                    * Yearly didactic average (actual clinical) over
                                    {{ report.startYear }} - {{ report.endYear }}
                                </th>
                                <td
                                    v-for="type in orderedEffortTypes"
                                    :key="type"
                                    class="total"
                                    :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                >
                                    {{ getAverageValue(report.meritSection.yearlyAverages, type) }}
                                </td>
                            </tr>
                            <!-- Department average -->
                            <tr class="totals-row bg-grey-1">
                                <th
                                    class="subt"
                                    colspan="5"
                                >
                                    {{ report.department }} Department Average over {{ report.startYear }} -
                                    {{ report.endYear }}
                                    <template v-if="report.meritSection.departmentFacultyCount > 0">
                                        with {{ report.meritSection.departmentFacultyCount }} Faculty
                                    </template>
                                </th>
                                <td
                                    v-for="type in orderedEffortTypes"
                                    :key="type"
                                    class="total"
                                    :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                >
                                    {{
                                        type in report.meritSection.departmentAverages
                                            ? getAverageValue(report.meritSection.departmentAverages, type)
                                            : ""
                                    }}
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </template>

                <!-- Section 2: Evaluation Multi-Year -->
                <template v-if="hasEvalData">
                    <h3 class="report-section-title q-mt-lg">Evaluation Multi-Year &mdash; {{ report.instructor }}</h3>
                    <div class="text-bold q-mb-sm">Instructor</div>

                    <table class="report-table">
                        <tbody>
                            <template
                                v-for="evalYear in report.evalSection.years"
                                :key="evalYear.year"
                            >
                                <!-- Year header -->
                                <tr class="year-header-row">
                                    <td colspan="7">
                                        <strong>{{
                                            report.useAcademicYear
                                                ? `Academic Year ${evalYear.yearLabel}`
                                                : evalYear.yearLabel
                                        }}</strong>
                                    </td>
                                </tr>
                                <!-- Column headers (repeated per year, matching legacy) -->
                                <tr>
                                    <th>Term</th>
                                    <th class="col-role">Role</th>
                                    <th class="col-count"># Enrolled</th>
                                    <th class="col-count">% Response</th>
                                    <th class="col-course">Course</th>
                                    <th class="col-avg">Average</th>
                                    <th class="col-avg">Median</th>
                                </tr>
                                <!-- Course rows -->
                                <tr
                                    v-for="(course, idx) in evalYear.courses"
                                    :key="`eval_${evalYear.year}_${idx}`"
                                >
                                    <td>{{ course.termCode }}</td>
                                    <td>{{ course.role }}</td>
                                    <td>{{ course.numEnrolled }}</td>
                                    <td>
                                        {{ formatResponsePercent(course.numResponses, course.numEnrolled) }}
                                    </td>
                                    <td>{{ course.course }}</td>
                                    <td>{{ formatDecimal(course.average) }}</td>
                                    <td>{{ formatMedianInt(course.median) }}</td>
                                </tr>
                                <!-- Year average -->
                                <tr class="totals-row bg-grey-1">
                                    <th
                                        class="subt"
                                        colspan="5"
                                    >
                                        Average for {{ evalYear.yearLabel }}
                                    </th>
                                    <td class="total">{{ formatDecimal(evalYear.yearAverage) }}</td>
                                    <td></td>
                                </tr>
                            </template>
                            <!-- Overall average -->
                            <tr class="totals-row bg-grey-2">
                                <th
                                    class="subt"
                                    colspan="5"
                                >
                                    Overall Average:
                                </th>
                                <td class="total">{{ formatDecimal(report.evalSection.overallAverage) }}</td>
                                <td></td>
                            </tr>
                            <!-- Department average -->
                            <tr class="totals-row bg-grey-1">
                                <th
                                    class="subt"
                                    colspan="5"
                                >
                                    {{ report.department }} Department Average for {{ report.startYear }} -
                                    {{ report.endYear }}
                                </th>
                                <td class="total">
                                    {{ formatDecimal(report.evalSection.departmentAverage ?? 0) }}
                                </td>
                                <td></td>
                            </tr>
                        </tbody>
                    </table>
                </template>

                <!-- Eval summary (always shown, even when no per-course eval data) -->
                <template v-else>
                    <h3 class="report-section-title q-mt-lg">Evaluation Multi-Year &mdash; {{ report.instructor }}</h3>
                    <div>
                        <div class="text-bold q-mb-sm">Instructor</div>
                        <div class="eval-summary">
                            <span class="eval-summary-label">Overall Average:</span>
                            <span class="eval-summary-value">{{
                                formatDecimal(report.evalSection.overallAverage)
                            }}</span>
                            <span class="eval-summary-label">
                                {{ report.department }} Department Average for {{ report.startYear }} -
                                {{ report.endYear }}:
                            </span>
                            <span class="eval-summary-value">
                                {{ formatDecimal(report.evalSection.departmentAverage ?? 0) }}
                            </span>
                        </div>
                    </div>
                </template>
            </template>

            <template v-else>
                <div class="text-grey-6 text-center q-pa-lg">No data found for the selected filters.</div>
            </template>
        </ReportLayout>

        <!-- Leave Editor Modal -->
        <LeaveEditorModal
            v-if="selectedPersonId"
            v-model="showLeaveEditor"
            :person-id="selectedPersonId"
            :all-terms="allTerms"
            :sabbatical-data="sabbaticalData"
            @saved="onLeaveDataSaved"
        />
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useQuasar } from "quasar"
import { reportService } from "../services/report-service"
import { instructorService } from "../services/instructor-service"
import { termService } from "../services/term-service"
import { useEffortTypeColumns } from "../composables/use-effort-type-columns"
import { useEffortPermissions } from "../composables/use-effort-permissions"
import ReportLayout from "../components/ReportLayout.vue"
import LeaveEditorModal from "../components/LeaveEditorModal.vue"
import type { MultiYearReport, PersonDto, TermDto, SabbaticalDto } from "../types"

const $q = useQuasar()
const route = useRoute()
const router = useRouter()
const { isAdmin } = useEffortPermissions()

const loading = ref(false)
const printLoading = ref(false)
const report = ref<MultiYearReport | null>(null)

// Filter state
const selectedPersonId = ref<number | null>(null)
const selectedStartYear = ref<number | null>(null)
const selectedEndYear = ref<number | null>(null)
const useAcademicYear = ref(false)
const personInput = ref("")

// Data sources
const personOptions = ref<PersonDto[]>([])
const displayedPersonOptions = ref<{ fullName: string; personId: number }[]>([])
const allTerms = ref<TermDto[]>([])

// Instructor year range (loaded per instructor, matching legacy behavior)
const instructorMinYear = ref<number | null>(null)
const instructorMaxYear = ref<number | null>(null)

// Sabbatical state
const sabbaticalLoading = ref(false)
const sabbaticalData = ref<SabbaticalDto | null>(null)
const showLeaveEditor = ref(false)

const yearTypeOptions = [
    { label: "Calendar Year", value: false },
    { label: "Academic Year", value: true },
]

// Build start year options from instructor's year range
const yearOptions = computed(() => {
    if (instructorMinYear.value === null || instructorMaxYear.value === null) return []
    const options: { label: string; value: number }[] = []
    for (let y = instructorMaxYear.value; y >= instructorMinYear.value; y--) {
        options.push({ label: String(y), value: y })
    }
    return options
})

// End year options: instructor range + 2 (matching legacy), must be >= start year, max 10 year range
const endYearOptions = computed(() => {
    if (instructorMinYear.value === null || instructorMaxYear.value === null) return []
    const maxEnd = instructorMaxYear.value + 2
    const options: { label: string; value: number }[] = []
    for (let y = maxEnd; y >= instructorMinYear.value; y--) {
        options.push({ label: String(y), value: y })
    }
    if (!selectedStartYear.value) return options
    const start = selectedStartYear.value
    return options.filter((o) => o.value >= start && o.value <= start + 9)
})

// Effort type columns for the merit section
const effortTypes = computed(() => report.value?.effortTypes ?? [])
const { effortColumns, getTotalValue, getAverageValue } = useEffortTypeColumns(effortTypes, {
    showZero: true,
    legacyColumnOrder: true,
})
const orderedEffortTypes = computed(() => effortColumns.value.map((c) => c.label))

const generateDisabled = computed(() => !selectedStartYear.value || !selectedEndYear.value)
const hasMeritData = computed(() => (report.value?.meritSection.years.length ?? 0) > 0)
const hasEvalData = computed(() => (report.value?.evalSection.years.length ?? 0) > 0)

// Sabbatical computed properties
const hasSabbaticalData = computed(() => {
    if (!sabbaticalData.value) return false
    return !!(sabbaticalData.value.excludeClinicalTerms || sabbaticalData.value.excludeDidacticTerms)
})

// Build term code -> term name lookup
const termNameByCode = computed(() => {
    const map = new Map<string, string>()
    for (const t of allTerms.value) {
        map.set(String(t.termCode), t.termName)
    }
    return map
})

function termCodesToNames(csv: string | null | undefined): string[] {
    if (!csv) return []
    return csv
        .split(",")
        .map((code) => code.trim())
        .filter(Boolean)
        .map((code) => termNameByCode.value.get(code) ?? code)
}

const sabbaticalClinTermNames = computed(() => termCodesToNames(sabbaticalData.value?.excludeClinicalTerms))
const sabbaticalDidTermNames = computed(() => termCodesToNames(sabbaticalData.value?.excludeDidacticTerms))

const selectedPersonName = computed(() => {
    if (!selectedPersonId.value) return ""
    const person = personOptions.value.find((p) => p.personId === selectedPersonId.value)
    return person?.fullName ?? ""
})

function formatUnits(val: number): string {
    if (val === 0) return ""
    return val % 1 === 0 ? val.toString() : val.toFixed(1)
}

function formatDecimal(val: number): string {
    return val.toFixed(2)
}

function formatResponsePercent(responses: number, enrolled: number): string {
    if (enrolled === 0) return "0.0%"
    return ((responses / enrolled) * 100).toFixed(1) + "%"
}

function formatMedianInt(val: number | null): string {
    if (val === null) return ""
    return Math.round(val).toString()
}

function filterPersons(val: string, update: (fn: () => void) => void) {
    update(() => {
        const source = personOptions.value.map((p) => ({
            fullName: p.fullName,
            personId: p.personId,
        }))

        if (!val) {
            displayedPersonOptions.value = source
        } else {
            const needle = val.toLowerCase()
            displayedPersonOptions.value = source.filter((p) => p.fullName.toLowerCase().includes(needle))
        }
    })
}

async function onInstructorSelected(personId: number | null) {
    if (!personId) return
    sabbaticalLoading.value = true
    try {
        const [sabbatical, yearRange] = await Promise.all([
            reportService.getSabbatical(personId),
            reportService.getMultiYearRange(personId),
        ])
        sabbaticalData.value = sabbatical
        instructorMinYear.value = yearRange?.minYear ?? null
        instructorMaxYear.value = yearRange?.maxYear ?? null

        // Reset year selections when instructor changes
        selectedStartYear.value = null
        selectedEndYear.value = null
    } finally {
        sabbaticalLoading.value = false
    }
}

function clearInstructor() {
    selectedPersonId.value = null
    report.value = null
    sabbaticalData.value = null
    instructorMinYear.value = null
    instructorMaxYear.value = null
    selectedStartYear.value = null
    selectedEndYear.value = null
}

function onLeaveDataSaved(data: SabbaticalDto) {
    sabbaticalData.value = data
}

function buildQueryParams() {
    const query: Record<string, string> = {}
    if (selectedPersonId.value) query.personId = selectedPersonId.value.toString()
    if (selectedStartYear.value) query.startYear = selectedStartYear.value.toString()
    if (selectedEndYear.value) query.endYear = selectedEndYear.value.toString()
    if (useAcademicYear.value) query.useAcademicYear = "true"
    return query
}

function buildApiParams() {
    // Use sabbatical data for exclusion terms (auto-loaded from DB)
    const clinTerms = sabbaticalData.value?.excludeClinicalTerms || undefined
    const didTerms = sabbaticalData.value?.excludeDidacticTerms || undefined

    return {
        personId: selectedPersonId.value!,
        startYear: selectedStartYear.value!,
        endYear: selectedEndYear.value!,
        excludeClinTerms: clinTerms,
        excludeDidTerms: didTerms,
        useAcademicYear: useAcademicYear.value || undefined,
    }
}

async function generateReport() {
    if (selectedPersonId.value === null || !selectedStartYear.value || !selectedEndYear.value) return
    router.replace({ query: buildQueryParams() })
    loading.value = true
    try {
        report.value = await reportService.getMultiYear(buildApiParams())
    } finally {
        loading.value = false
    }
}

async function handlePrint() {
    if (selectedPersonId.value === null || !selectedStartYear.value || !selectedEndYear.value) return
    printLoading.value = true
    try {
        const opened = await reportService.openMultiYearPdf(buildApiParams())
        if (!opened) {
            $q.notify({ type: "warning", message: "No data to export for the selected filters." })
        }
    } finally {
        printLoading.value = false
    }
}

async function loadInstructors(termCode: number) {
    const persons = await instructorService.getInstructors(termCode, undefined, true)
    personOptions.value = persons
    displayedPersonOptions.value = persons.map((p) => ({
        fullName: p.fullName,
        personId: p.personId,
    }))
}

onMounted(async () => {
    const terms = await termService.getTerms()
    allTerms.value = terms

    // Load all merit-eligible instructors across all terms (matching legacy behavior).
    // termCode=0 tells the API to query all terms and deduplicate by person.
    await loadInstructors(0)

    // Restore from URL query params (bookmarkable)
    const q = route.query
    if (q.personId) {
        selectedPersonId.value = parseInt(q.personId as string, 10)
        if (q.useAcademicYear === "true") useAcademicYear.value = true

        // Load sabbatical + year range for the restored instructor
        await onInstructorSelected(selectedPersonId.value)

        // Restore year selections after year range is loaded (onInstructorSelected resets them)
        if (q.startYear) selectedStartYear.value = parseInt(q.startYear as string, 10)
        if (q.endYear) selectedEndYear.value = parseInt(q.endYear as string, 10)
        await generateReport()
    }
})
</script>

<style>
@import url("../report-tables.css");
</style>

<style scoped>
.report-section-title {
    font-size: 1.1rem;
    font-weight: bold;
    margin-bottom: 0.25rem;
}

.report-section-subtitle {
    font-size: 0.9rem;
    color: var(--ucdavis-black-60);
    margin-bottom: 0.5rem;
}

.sabbatical-notice {
    font-size: 0.85rem;
    font-style: italic;
    color: var(--ucdavis-black-60);
}

.sabbatical-section {
    border: 1px solid var(--ucdavis-black-20, #ddd);
}

.year-header-row th,
.year-header-row td {
    padding-top: 0.75rem;
    border-bottom: none;
    text-decoration: none;
    font-size: 0.9rem;
}

.col-course {
    min-width: 12rem;
}

.col-units {
    min-width: 3rem;
}

.col-enrl {
    min-width: 3rem;
}

.col-avg {
    min-width: 4rem;
}

.col-count {
    min-width: 4rem;
}

.eval-summary {
    display: inline-grid;
    grid-template-columns: auto auto;
    column-gap: 0.5rem;
    font-size: 0.85rem;
}

.eval-summary-label {
    text-align: right;
    font-weight: bold;
    font-style: italic;
}

.eval-summary-value {
    font-weight: bold;
}
</style>
