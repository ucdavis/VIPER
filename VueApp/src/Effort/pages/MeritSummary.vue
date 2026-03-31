<template>
    <div class="q-pa-md">
        <h1>Merit &amp; Promotion Report - Summary</h1>

        <ReportFilterForm
            :term-code="termCode"
            :loading="loading"
            :initial-filters="initialFilters"
            :visible-fields="['department']"
            :merit-only="true"
            @generate="generateReport"
        />

        <!-- Loading state -->
        <div
            v-if="loading"
            role="status"
            class="text-center q-my-lg"
        >
            <q-spinner-dots
                size="3rem"
                color="primary"
            />
            <div class="q-mt-md text-body1">Loading report...</div>
        </div>

        <!-- Report content -->
        <ReportLayout v-else-if="report">
            <template #header>
                <div class="col text-h6">Merit &amp; Promotion - Summary</div>
                <div class="col-auto no-print">
                    <ExportToolbar
                        :pdf-export="handlePrint"
                        :excel-export="handleExcelDownload"
                    />
                </div>
            </template>

            <template v-if="report.jobGroups.length === 0">
                <div
                    role="status"
                    class="text-grey-7 text-center q-pa-lg"
                >
                    No data found for the selected filters.
                </div>
            </template>

            <template v-else>
                <div
                    v-for="jobGroup in report.jobGroups"
                    :key="jobGroup.jobGroupDescription"
                    class="job-group-section"
                >
                    <h3 class="job-group-header">{{ jobGroup.jobGroupDescription }}</h3>

                    <ReportDeptTabs :departments="jobGroup.departments">
                        <template #default="{ dept }">
                            <table class="report-table">
                                <caption class="sr-only">
                                    Merit summary of effort
                                </caption>
                                <thead>
                                    <tr>
                                        <th
                                            scope="col"
                                            class="col-label"
                                        ></th>
                                        <th
                                            v-for="type in orderedEffortTypes"
                                            :key="type"
                                            scope="col"
                                            class="col-effort"
                                            :class="{ 'col-spacer': isSpacerColumn(type) }"
                                        >
                                            <abbr :title="getEffortTypeLabel(type)">{{ type }}</abbr>
                                        </th>
                                    </tr>
                                </thead>
                                <tbody>
                                    <!-- Department totals row -->
                                    <tr class="totals-row bg-grey-1">
                                        <th
                                            scope="row"
                                            class="subt"
                                        >
                                            Totals:
                                        </th>
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
            class="text-grey-7 text-center q-pa-lg"
        >
            Select filters and click "Generate Report" to view data.
        </div>
    </div>
</template>

<script setup lang="ts">
import { reportService } from "../services/report-service"
import { useReportPage } from "../composables/use-report-page"
import ExportToolbar from "@/components/ExportToolbar.vue"
import ReportFilterForm from "../components/ReportFilterForm.vue"
import ReportLayout from "../components/ReportLayout.vue"
import ReportDeptTabs from "../components/ReportDeptTabs.vue"
import type { MeritSummaryReport } from "../types"

const {
    termCode,
    loading,
    report,
    initialFilters,
    orderedEffortTypes,
    getTotalValue,
    getAverageValue,
    isSpacerColumn,
    getEffortTypeLabel,
    generateReport,
    handlePrint,
    handleExcelDownload,
} = useReportPage<MeritSummaryReport>({
    fetchReport: (params) => reportService.getMeritSummary(params),
    fetchPdf: (params) => reportService.openPdf("merit/summary/pdf", params),
    fetchExcel: (params) => reportService.downloadExcel("merit/summary/excel", params),
    getEffortTypes: (r) => r.effortTypes,
})
</script>

<style>
@import url("../report-tables.css");
</style>

<style scoped></style>
