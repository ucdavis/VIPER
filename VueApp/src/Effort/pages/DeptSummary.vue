<template>
    <div class="q-pa-md">
        <h1>Teaching Activity Report - Department Summary</h1>

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
                <div class="col text-h6">Teaching Activity - Department Summary</div>
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
