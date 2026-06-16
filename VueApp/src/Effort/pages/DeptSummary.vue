<template>
    <EffortReportPage
        title="Teaching Activity Report - Department Summary"
        subtitle="Teaching Activity - Department Summary"
        :term-code="termCode"
        :loading="loading"
        :has-report="report !== null"
        :is-empty="report !== null && report.departments.length === 0"
        :initial-filters="initialFilters"
        :on-pdf-export="handlePrint"
        :on-excel-export="handleExcelDownload"
        @generate="generateReport"
    >
        <template v-if="report">
            <ReportDeptTabs :departments="report.departments">
                <template #default="{ dept }">
                    <table class="report-table">
                        <caption class="sr-only">
                            Department summary of effort by instructor
                        </caption>
                        <thead>
                            <tr>
                                <th
                                    scope="col"
                                    class="col-instructor"
                                >
                                    Instructor
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
                            <tr
                                v-for="instructor in dept.instructors"
                                :key="instructor.mothraId"
                            >
                                <td class="instructor-cell">{{ instructor.instructor }}</td>
                                <td
                                    v-for="type in orderedEffortTypes"
                                    :key="type"
                                    :class="{ 'col-spacer': isSpacerColumn(type) }"
                                >
                                    {{ getTotalValue(instructor.effortByType ?? {}, type) }}
                                </td>
                            </tr>

                            <!-- Re-display effort type headers before department totals -->
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

                            <!-- Department totals row -->
                            <tr class="dept-totals-row bg-grey-4">
                                <th
                                    scope="row"
                                    class="subt"
                                >
                                    Department Totals:
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

                            <!-- Faculty w/ CLI + averages row -->
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
        </template>
    </EffortReportPage>
</template>

<script setup lang="ts">
import { reportService } from "../services/report-service"
import { useReportPage } from "../composables/use-report-page"
import EffortReportPage from "../components/EffortReportPage.vue"
import ReportDeptTabs from "../components/ReportDeptTabs.vue"
import type { DeptSummaryReport } from "../types"
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
} = useReportPage<DeptSummaryReport>({
    title: "Department Summary Report",
    fetchReport: (params) => reportService.getDeptSummary(params),
    fetchPdf: (params) => reportService.openPdf("teaching/dept-summary/pdf", params),
    fetchExcel: (params) => reportService.downloadExcel("teaching/dept-summary/excel", params),
    getEffortTypes: (r) => r.effortTypes,
    hasData: (r) => r.departments.length > 0,
})
</script>
