<template>
    <div class="q-pa-md">
        <h2>Merit &amp; Promotion Report - Summary</h2>

        <ReportFilterForm
            :term-code="termCode"
            :loading="loading"
            :initial-filters="initialFilters"
            :visible-fields="['department']"
            :merit-only="true"
            @generate="generateReport"
        >
            <template #actions>
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
            </template>
        </ReportFilterForm>

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

                    <ReportDeptTabs :departments="jobGroup.departments">
                        <template #default="{ dept }">
                            <table class="report-table">
                                <thead>
                                    <tr>
                                        <th class="col-label"></th>
                                        <th
                                            v-for="type in orderedEffortTypes"
                                            :key="type"
                                            class="col-effort"
                                            :class="{ 'col-spacer': isSpacerColumn(type) }"
                                        >
                                            {{ type }}
                                        </th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <!-- Department totals row -->
                                    <tr class="totals-row bg-grey-1">
                                        <th class="subt">Totals:</th>
                                        <td
                                            v-for="type in orderedEffortTypes"
                                            :key="type"
                                            class="total"
                                            :class="{ 'col-spacer': isSpacerColumn(type) }"
                                        >
                                            {{ getTotalValue(dept.departmentTotals, type) }}
                                        </td>
                                    </tr>

                                    <!-- Number Faculty row -->
                                    <tr class="faculty-row">
                                        <td>
                                            <span class="faculty-label">Number Faculty:</span>
                                            <span class="faculty-count">{{ dept.facultyCount }}</span>
                                        </td>
                                    </tr>

                                    <!-- Faculty w/ CLI assigned + averages row -->
                                    <tr class="faculty-row bg-grey-3">
                                        <td>
                                            <span class="faculty-label">Faculty w/ CLI assigned:</span>
                                            <span class="faculty-count">{{ dept.facultyWithCliCount }}</span>
                                            <span class="avg-text">Average</span>
                                        </td>
                                        <td
                                            v-for="type in orderedEffortTypes"
                                            :key="type"
                                            class="total"
                                            :class="{ 'col-spacer': isSpacerColumn(type) }"
                                        >
                                            {{ getAverageValue(dept.departmentAverages, type) }}
                                        </td>
                                    </tr>
                                </tbody>
                            </table>
                        </template>
                    </ReportDeptTabs>
                </div>
            </template>
        </ReportLayout>

        <!-- No report generated yet -->
        <div
            v-else-if="!loading"
            class="text-grey-6 text-center q-pa-lg"
        >
            Select filters and click "Generate Report" to view data.
        </div>
    </div>
</template>

<script setup lang="ts">
import { reportService } from "../services/report-service"
import { useReportPage } from "../composables/use-report-page"
import ReportFilterForm from "../components/ReportFilterForm.vue"
import ReportLayout from "../components/ReportLayout.vue"
import ReportDeptTabs from "../components/ReportDeptTabs.vue"
import type { MeritSummaryReport } from "../types"

const {
    termCode,
    loading,
    report,
    printLoading,
    initialFilters,
    orderedEffortTypes,
    getTotalValue,
    getAverageValue,
    isSpacerColumn,
    generateReport,
    handlePrint,
} = useReportPage<MeritSummaryReport>({
    fetchReport: (params) => reportService.getMeritSummary(params),
    fetchPdf: (params) => reportService.openPdf("merit/summary/pdf", params),
    getEffortTypes: (r) => r.effortTypes,
})
</script>

<style>
@import "../report-tables.css";
</style>

<style scoped></style>
