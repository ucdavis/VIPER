<template>
    <EffortReportPage
        title="Teaching Activity Report - School Summary"
        :term-code="termCode"
        :loading="loading"
        :has-report="report !== null"
        :is-empty="report !== null && report.departments.length === 0"
        :initial-filters="initialFilters"
        :on-pdf-export="handlePrint"
        :on-excel-export="handleExcelDownload"
        @generate="generateReport"
    >
        <template
            v-if="report"
            #header
            >{{ report.termName }} - School Summary</template
        >

        <template v-if="report">
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
    </EffortReportPage>
</template>

<script setup lang="ts">
import { reportService } from "../services/report-service"
import { useReportPage } from "../composables/use-report-page"
import EffortReportPage from "../components/EffortReportPage.vue"
import type { SchoolSummaryReport } from "../types"
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
} = useReportPage<SchoolSummaryReport>({
    fetchReport: (params) => reportService.getSchoolSummary(params),
    fetchPdf: (params) => reportService.openPdf("teaching/school-summary/pdf", params),
    fetchExcel: (params) => reportService.downloadExcel("teaching/school-summary/excel", params),
    getEffortTypes: (r) => r.effortTypes,
})
</script>

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
