<template>
    <div class="q-pa-md">
        <h1>Merit &amp; Promotion Report - Average</h1>

        <ReportFilterForm
            :term-code="termCode"
            :loading="loading"
            :initial-filters="initialFilters"
            :visible-fields="['department', 'faculty']"
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
                <div class="col text-h6">Merit &amp; Promotion - Average</div>
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
                    <h2 class="job-group-header">{{ jobGroup.jobGroupDescription }}</h2>

                    <ReportDeptTabs :departments="jobGroup.departments">
                        <template #default="{ dept }">
                            <table class="report-table">
                                <caption class="sr-only">
                                    Merit average of effort by instructor
                                </caption>
                                <thead>
                                    <tr>
                                        <th
                                            scope="col"
                                            class="col-instructor"
                                            colspan="2"
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
                                    <template
                                        v-for="instructor in dept.instructors"
                                        :key="instructor.mothraId"
                                    >
                                        <!-- Per-term rows with instructor name rowspanned -->
                                        <tr
                                            v-for="(term, termIdx) in instructor.terms"
                                            :key="`${instructor.mothraId}_${term.termCode}`"
                                        >
                                            <td
                                                v-if="termIdx === 0"
                                                :rowspan="instructor.terms.length"
                                                class="instructor-cell"
                                            >
                                                {{ instructor.instructor }}
                                            </td>
                                            <td class="term-cell">{{ term.termName }}</td>
                                            <td
                                                v-for="type in orderedEffortTypes"
                                                :key="type"
                                                :class="{ 'col-spacer': isSpacerColumn(type) }"
                                            >
                                                {{ getTotalValue(term.effortByType ?? {}, type) }}
                                            </td>
                                        </tr>

                                        <!-- Instructor totals row -->
                                        <tr class="totals-row bg-grey-1">
                                            <th
                                                scope="row"
                                                colspan="2"
                                                class="subt"
                                            >
                                                INSTRUCTOR TOTALS:
                                            </th>
                                            <td
                                                v-for="type in orderedEffortTypes"
                                                :key="type"
                                                class="total"
                                                :class="{ 'col-spacer': isSpacerColumn(type) }"
                                            >
                                                {{ getTotalValue(instructor.effortByType ?? {}, type) }}
                                            </td>
                                        </tr>
                                    </template>

                                    <!-- Faculty Count Total row -->
                                    <tr class="counts-row">
                                        <td colspan="100%">Faculty Count Total: {{ dept.facultyCount }}</td>
                                    </tr>

                                    <!-- Faculty with assigned CLI row -->
                                    <tr class="counts-row">
                                        <td colspan="100%">
                                            Faculty with assigned CLI: {{ dept.facultyWithCliCount }}
                                        </td>
                                    </tr>

                                    <!-- Group totals row -->
                                    <tr class="dept-totals-row bg-grey-4">
                                        <th
                                            scope="row"
                                            colspan="2"
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
                                            {{ getTotalValue(dept.groupTotals, type) }}
                                        </td>
                                    </tr>

                                    <!-- Group averages row -->
                                    <tr class="dept-averages-row bg-grey-3">
                                        <th
                                            scope="row"
                                            colspan="2"
                                            class="subt"
                                        >
                                            Averages:
                                        </th>
                                        <td
                                            v-for="type in orderedEffortTypes"
                                            :key="type"
                                            class="total"
                                            :class="{ 'col-spacer': isSpacerColumn(type) }"
                                        >
                                            {{ getAverageValue(dept.groupAverages, type) }}
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
import type { MeritAverageReport } from "../types"

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
} = useReportPage<MeritAverageReport>({
    title: "Merit & Promotion Report - Average",
    fetchReport: (params) => reportService.getMeritAverage(params),
    fetchPdf: (params) => reportService.openPdf("merit/average/pdf", params),
    fetchExcel: (params) => reportService.downloadExcel("merit/average/excel", params),
    getEffortTypes: (r) => r.effortTypes,
    hasData: (r) => r.jobGroups.length > 0,
})
</script>

<style>
@import url("../report-tables.css");
</style>

<style scoped>
.term-cell {
    font-size: 0.85em;
    color: var(--ucdavis-black-70);
}
</style>
