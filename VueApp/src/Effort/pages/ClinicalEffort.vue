<template>
    <div class="q-pa-md">
        <h2>Merit &amp; Promotion Report - Clinical Effort</h2>

        <!-- Custom filter form — academic year + clinical type only -->
        <div class="filter-form q-mb-md">
            <div class="row q-gutter-sm items-end q-mb-sm">
                <q-select
                    v-model="selectedYear"
                    :options="yearOptions"
                    label="Academic Year"
                    dense
                    options-dense
                    outlined
                    emit-value
                    map-options
                    class="col-auto"
                    style="min-width: 10rem"
                />
                <q-select
                    v-model="selectedType"
                    :options="clinicalTypeOptions"
                    label="Clinical Type"
                    dense
                    options-dense
                    outlined
                    emit-value
                    map-options
                    class="col-auto"
                    style="min-width: 8rem"
                />
            </div>
            <div class="row items-center q-mb-md">
                <q-btn
                    color="primary"
                    icon="assessment"
                    label="Generate Report"
                    :loading="loading"
                    :disable="!selectedYear"
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

            <template v-if="report.jobGroups.length === 0">
                <div class="text-grey-6 text-center q-pa-lg">No data found for the selected filters.</div>
            </template>

            <template v-else>
                <div
                    v-for="jobGroup in report.jobGroups"
                    :key="jobGroup.jobGroupDescription"
                    class="job-group-section"
                >
                    <div class="job-group-header">{{ jobGroup.jobGroupDescription }}</div>

                    <table class="report-table">
                        <thead>
                            <tr>
                                <th class="col-instructor">Instructor</th>
                                <th class="col-percent text-center">Clinical %</th>
                                <th class="col-effort text-center">CLI</th>
                                <th class="col-ratio text-center">
                                    CLI Ratio
                                    <div>CLI Weeks / Percent</div>
                                </th>
                                <th
                                    v-for="type in effortTypesWithoutCli"
                                    :key="type"
                                    class="col-effort text-center"
                                >
                                    {{ type }}
                                </th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr
                                v-for="instructor in jobGroup.instructors"
                                :key="instructor.mothraId"
                            >
                                <td>{{ instructor.instructor }}</td>
                                <td class="text-center">{{ formatPercent(instructor.clinicalPercent) }}</td>
                                <td class="text-center">{{ getTotalValue(instructor.effortByType, "CLI") }}</td>
                                <td class="text-center">{{ formatRatio(instructor.cliRatio) }}</td>
                                <td
                                    v-for="type in effortTypesWithoutCli"
                                    :key="type"
                                    class="text-center"
                                >
                                    {{ getTotalValue(instructor.effortByType, type) }}
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </div>
            </template>
        </ReportLayout>

        <!-- No report generated yet -->
        <div
            v-else-if="!loading"
            class="text-grey-6 text-center q-pa-lg"
        >
            Select an academic year and clinical type, then click "Generate Report" to view data.
        </div>
    </div>
</template>

<script setup lang="ts">
import { ref, computed, onMounted } from "vue"
import { useRoute, useRouter } from "vue-router"
import { useQuasar } from "quasar"
import { reportService } from "../services/report-service"
import { postForBlob } from "@/composables/ViperFetch"
import { termService } from "../services/term-service"
import { useEffortTypeColumns } from "../composables/use-effort-type-columns"
import ReportLayout from "../components/ReportLayout.vue"
import type { ClinicalEffortReport } from "../types"

const $q = useQuasar()
const route = useRoute()
const router = useRouter()

const loading = ref(false)
const printLoading = ref(false)
const report = ref<ClinicalEffortReport | null>(null)
const yearOptions = ref<{ label: string; value: string }[]>([])

const clinicalTypeOptions = [
    { label: "VMTH", value: 1 },
    { label: "CAHFS", value: 25 },
]

// Default academic year from route term code
const selectedYear = ref("")
const selectedType = ref(1)

function getAcademicYear(termCode: number): string {
    const year = Math.floor(termCode / 100)
    const term = termCode % 100
    const startYear = term >= 1 && term <= 3 ? year - 1 : year
    return `${startYear}-${startYear + 1}`
}

const effortTypes = computed(() => report.value?.effortTypes ?? [])
const { effortColumns, getTotalValue } = useEffortTypeColumns(effortTypes, {
    showZero: true,
    legacyColumnOrder: true,
})
const orderedEffortTypes = computed(() => effortColumns.value.map((c) => c.label))
const effortTypesWithoutCli = computed(() => orderedEffortTypes.value.filter((t) => t !== "CLI"))

function formatPercent(value: number): string {
    if (value === 0) return ""
    return `${value.toFixed(1)}%`
}

function formatRatio(value: number | null): string {
    if (value === null) return ""
    return value.toFixed(1)
}

async function generateReport() {
    if (!selectedYear.value) return
    router.replace({ query: { academicYear: selectedYear.value, clinicalType: selectedType.value.toString() } })
    loading.value = true
    try {
        report.value = await reportService.getClinicalEffort(selectedYear.value, selectedType.value)
    } finally {
        loading.value = false
    }
}

async function handlePrint() {
    if (!selectedYear.value) return
    printLoading.value = true
    try {
        const baseUrl = `${import.meta.env.VITE_API_URL}effort/reports`
        const { blob } = await postForBlob(`${baseUrl}/merit/clinical/pdf`, {
            academicYear: selectedYear.value,
            clinicalType: selectedType.value,
        })
        if (blob.size === 0) {
            $q.notify({ type: "warning", message: "No data to export for the selected filters." })
        } else {
            const url = globalThis.URL.createObjectURL(blob)
            globalThis.open(url, "_blank")
        }
    } finally {
        printLoading.value = false
    }
}

onMounted(async () => {
    // Build academic year options from available terms
    const terms = await termService.getTerms()
    if (terms) {
        const years = new Set<string>()
        for (const term of terms) {
            years.add(getAcademicYear(term.termCode))
        }
        yearOptions.value = Array.from(years)
            .sort()
            .reverse()
            .map((y) => ({ label: y, value: y }))
    }

    // Restore from URL query params (bookmarkable)
    const q = route.query
    if (q.academicYear) {
        selectedYear.value = q.academicYear as string
        if (q.clinicalType) {
            selectedType.value = parseInt(q.clinicalType as string, 10)
        }
        await generateReport()
        return
    }

    // Default from route
    const tc = route.params.termCode
    if (tc) {
        selectedYear.value = getAcademicYear(parseInt(tc as string, 10))
    }
})
</script>

<style>
@import "../report-tables.css";
</style>

<style scoped>
.job-group-section {
    margin-bottom: 2rem;
}

.job-group-header {
    font-weight: bold;
    font-size: 1rem;
    padding: 0.5rem 0;
    border-bottom: 2px solid var(--ucdavis-black-80);
    margin-bottom: 0.5rem;
}

:deep(.report-table tbody tr) {
    border-bottom: 1px solid var(--ucdavis-black-20);
}

:deep(.report-table th.text-center) {
    text-align: center;
}

.col-percent {
    min-width: 4.5rem;
}

.col-ratio {
    min-width: 4.5rem;
}

.col-instructor {
    min-width: 10rem;
    text-align: left;
}
</style>
