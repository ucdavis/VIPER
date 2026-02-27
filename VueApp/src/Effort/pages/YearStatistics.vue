<template>
    <div class="q-pa-md">
        <h2>Year Statistics Report</h2>

        <!-- Custom filter form — academic year only -->
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

            <q-tabs
                v-model="activeTab"
                dense
                align="left"
                class="text-grey"
                active-color="primary"
                indicator-color="primary"
                narrow-indicator
            >
                <q-tab
                    name="svm"
                    label="SVM"
                />
                <q-tab
                    name="dvm"
                    label="DVM"
                />
                <q-tab
                    name="resident"
                    label="Resident"
                />
                <q-tab
                    name="undergradGrad"
                    label="Undergrad/Grad"
                />
            </q-tabs>

            <q-separator />

            <q-tab-panels
                v-model="activeTab"
                animated
            >
                <q-tab-panel
                    v-for="tab in tabs"
                    :key="tab.key"
                    :name="tab.key"
                    class="q-px-none"
                >
                    <template v-if="tab.key === activeTab">
                        <YearStatsPanel
                            :sub-report="tab.subReport"
                            :effort-types="orderedEffortTypes"
                            :get-value="getValue"
                            :get-average="getAverage"
                            :show-groupings="tab.key === 'svm' || tab.key === 'dvm'"
                        />
                    </template>
                </q-tab-panel>
            </q-tab-panels>
        </ReportLayout>

        <!-- No report generated yet -->
        <div
            v-else-if="!loading"
            class="text-grey-6 text-center q-pa-lg"
        >
            Select an academic year and click "Generate Report" to view data.
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
import YearStatsPanel from "../components/YearStatsPanel.vue"
import type { YearStatisticsReport } from "../types"

const $q = useQuasar()
const route = useRoute()
const router = useRouter()

const loading = ref(false)
const printLoading = ref(false)
const report = ref<YearStatisticsReport | null>(null)
const yearOptions = ref<{ label: string; value: string }[]>([])
const selectedYear = ref("")
const activeTab = ref("svm")

function getAcademicYear(termCode: number): string {
    const year = Math.floor(termCode / 100)
    const term = termCode % 100
    const startYear = term >= 1 && term <= 3 ? year - 1 : year
    return `${startYear}-${startYear + 1}`
}

const effortTypes = computed(() => report.value?.effortTypes ?? [])
const { effortColumns, getTotalValue, getAverageValue } = useEffortTypeColumns(effortTypes, {
    showZero: true,
    legacyColumnOrder: true,
})
const orderedEffortTypes = computed(() => effortColumns.value.map((c) => c.label))

function getValue(totals: Record<string, number>, type: string): string {
    return getTotalValue(totals, type)
}

function getAverage(averages: Record<string, number>, type: string): string {
    return getAverageValue(averages, type)
}

const tabs = computed(() => {
    if (!report.value) return []
    return [
        { key: "svm" as const, label: "SVM", subReport: report.value.svm },
        { key: "dvm" as const, label: "DVM", subReport: report.value.dvm },
        { key: "resident" as const, label: "Resident", subReport: report.value.resident },
        { key: "undergradGrad" as const, label: "Undergrad/Grad", subReport: report.value.undergradGrad },
    ]
})

async function generateReport() {
    if (!selectedYear.value) return
    router.replace({ query: { academicYear: selectedYear.value } })
    loading.value = true
    try {
        report.value = await reportService.getYearStatistics(selectedYear.value)
        activeTab.value = "svm"
    } finally {
        loading.value = false
    }
}

async function handlePrint() {
    if (!selectedYear.value) return
    printLoading.value = true
    try {
        const baseUrl = `${import.meta.env.VITE_API_URL}effort/reports`
        const { blob } = await postForBlob(`${baseUrl}/year-stats/pdf`, {
            academicYear: selectedYear.value,
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
