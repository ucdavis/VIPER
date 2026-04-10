<template>
    <div class="q-pa-md">
        <h2>Teaching Activity Report - School Summary</h2>

        <ReportFilterForm
            :term-code="termCode"
            :loading="loading"
            :initial-filters="initialFilters"
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
                <div class="col text-h6">{{ report.termName }} - School Summary</div>
                <div class="col-auto no-print">
                    <ExportToolbar
                        :pdf-export="handlePrint"
                        :excel-export="handleExcelDownload"
                    />
                </div>
            </template>

            <template v-if="report.departments.length === 0">
                <div
                    role="status"
                    class="text-grey-7 text-center q-pa-lg"
                >
                    No data found for the selected filters.
                </div>
            </template>

            <template v-else>
                <div class="dept-section">
                    <table class="report-table">
                        <caption class="sr-only">
                            School-wide summary of effort by department
                        </caption>
                        <thead>
                            <tr>
                                <th
                                    scope="col"
                                    class="col-department"
                                >
                                    Department
                                </th>
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
                            <template
                                v-for="(dept, deptIdx) in report.departments"
                                :key="dept.department"
                            >
                                <!-- Department totals row -->
                                <tr>
                                    <td class="dept-name-cell">{{ dept.department }}</td>
                                    <td
                                        v-for="type in orderedEffortTypes"
                                        :key="type"
                                        :class="{ 'col-spacer': isSpacerColumn(type) }"
                                    >
                                        {{ getTotalValue(dept.effortTotals, type) }}
                                    </td>
                                </tr>
                                <!-- # Faculty row -->
                                <tr class="faculty-row">
                                    <td>
                                        <span class="faculty-label"># Faculty</span>
                                        <span class="faculty-count">{{ dept.facultyCount }}</span>
                                    </td>
                                </tr>
                                <!-- # Faculty with CLI + averages row -->
                                <tr class="faculty-row bg-grey-3">
                                    <td>
                                        <span class="faculty-label"># Faculty with CLI</span>
                                        <span class="faculty-count">{{ dept.facultyWithCliCount }}</span>
                                        <span class="avg-text">Average</span>
                                    </td>
                                    <td
                                        v-for="type in orderedEffortTypes"
                                        :key="type"
                                        class="avg-value"
                                        :class="{ 'col-spacer': isSpacerColumn(type) }"
                                    >
                                        {{ getAverageValue(dept.averages, type) }}
                                    </td>
                                </tr>
                                <!-- HR divider between departments -->
                                <tr
                                    v-if="deptIdx < report.departments.length - 1"
                                    class="divider-row"
                                    aria-hidden="true"
                                >
                                    <td colspan="100%">
                                        <hr />
                                    </td>
                                </tr>
                            </template>

                            <!-- Re-display effort type headers before grand totals -->
                            <tr class="header-repeat">
                                <th scope="col"></th>
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

                            <!-- Grand Total row -->
                            <tr class="grand-totals-row bg-grey-4">
                                <th
                                    scope="row"
                                    class="subt"
                                >
                                    Grand Total
                                </th>
                                <td
                                    v-for="type in orderedEffortTypes"
                                    :key="type"
                                    class="total"
                                    :class="{ 'col-spacer': isSpacerColumn(type) }"
                                >
                                    {{ getTotalValue(report.grandTotals.effortTotals, type) }}
                                </td>
                            </tr>

                            <!-- # Faculty row (grand) -->
                            <tr class="faculty-row">
                                <td>
                                    <span class="faculty-label"># Faculty</span>
                                    <span class="faculty-count">{{ report.grandTotals.facultyCount }}</span>
                                </td>
                            </tr>

                            <!-- # Faculty with CLI + grand averages row -->
                            <tr class="faculty-row bg-grey-3">
                                <td>
                                    <span class="faculty-label"># Faculty with CLI</span>
                                    <span class="faculty-count">{{ report.grandTotals.facultyWithCliCount }}</span>
                                    <span class="avg-text">Average</span>
                                </td>
                                <td
                                    v-for="type in orderedEffortTypes"
                                    :key="type"
                                    class="total avg-value"
                                    :class="{ 'col-spacer': isSpacerColumn(type) }"
                                >
                                    {{ getAverageValue(report.grandTotals.averages, type) }}
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
import type { SchoolSummaryReport } from "../types"

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
} = useReportPage<SchoolSummaryReport>({
    fetchReport: (params) => reportService.getSchoolSummary(params),
    fetchPdf: (params) => reportService.openPdf("teaching/school-summary/pdf", params),
    fetchExcel: (params) => reportService.downloadExcel("teaching/school-summary/excel", params),
    getEffortTypes: (r) => r.effortTypes,
})
</script>

<style>
@import url("../report-tables.css");
</style>

<style scoped>
.dept-section {
    margin-bottom: 2rem;
    overflow-x: auto;
}

.col-department {
    min-width: 16rem;
}

.dept-name-cell {
    font-weight: 500;
}

.avg-value {
    font-weight: 600;
    font-style: italic;
}

.divider-row td {
    padding: 0;
}

.divider-row hr {
    border: none;
    border-top: 1px solid var(--ucdavis-black-20);
    margin: 0.5rem 0;
}

.grand-totals-row {
    border-top: 2px solid var(--ucdavis-black-60);
}
</style>
