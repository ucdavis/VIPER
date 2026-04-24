<template>
    <EffortReportPage
        title="Merit &amp; Promotion Report - Summary"
        subtitle="Merit &amp; Promotion - Summary"
        :term-code="termCode"
        :loading="loading"
        :has-report="report !== null"
        :is-empty="report !== null && report.jobGroups.length === 0"
        :initial-filters="initialFilters"
        :visible-fields="['department']"
        :merit-only="true"
        :on-pdf-export="handlePrint"
        :on-excel-export="handleExcelDownload"
        @generate="generateReport"
    >
        <template v-if="report">
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
    </EffortReportPage>
</template>

<script setup lang="ts">
import { reportService } from "../services/report-service"
import { useReportPage } from "../composables/use-report-page"
import EffortReportPage from "../components/EffortReportPage.vue"
import ReportDeptTabs from "../components/ReportDeptTabs.vue"
import type { MeritSummaryReport } from "../types"
import "../report-tables.css"

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
