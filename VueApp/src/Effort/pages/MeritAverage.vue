<template>
    <div class="q-pa-md">
        <h2>Merit &amp; Promotion Report - Average</h2>

        <ReportFilterForm
            :term-code="termCode"
            :loading="loading"
            :initial-filters="initialFilters"
            :visible-fields="['department', 'faculty']"
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

                    <div
                        v-for="dept in jobGroup.departments"
                        :key="`${jobGroup.jobGroupDescription}_${dept.department}`"
                        class="dept-section"
                    >
                        <div class="dept-subheader bg-grey-3">{{ dept.department }}</div>

                        <table class="report-table">
                            <thead>
                                <tr>
                                    <th
                                        class="col-instructor"
                                        colspan="2"
                                    >
                                        Instructor
                                    </th>
                                    <th
                                        v-for="type in orderedEffortTypes"
                                        :key="type"
                                        class="col-effort"
                                        :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                    >
                                        {{ type }}
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
                                            :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                        >
                                            {{ getTotalValue(term.effortByType ?? {}, type) }}
                                        </td>
                                    </tr>

                                    <!-- Instructor totals row -->
                                    <tr class="totals-row bg-grey-1">
                                        <th
                                            colspan="2"
                                            class="subt"
                                        >
                                            INSTRUCTOR TOTALS:
                                        </th>
                                        <td
                                            v-for="type in orderedEffortTypes"
                                            :key="type"
                                            class="total"
                                            :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                        >
                                            {{ getTotalValue(instructor.effortByType ?? {}, type) }}
                                        </td>
                                    </tr>
                                </template>

                                <!-- Re-display effort type headers before group totals -->
                                <tr class="header-repeat">
                                    <th colspan="2"></th>
                                    <th
                                        v-for="type in orderedEffortTypes"
                                        :key="type"
                                        class="col-effort"
                                        :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                    >
                                        {{ type }}
                                    </th>
                                </tr>

                                <!-- Faculty Count Total row -->
                                <tr class="counts-row">
                                    <td colspan="100%">Faculty Count Total: {{ dept.facultyCount }}</td>
                                </tr>

                                <!-- Faculty with assigned CLI row -->
                                <tr class="counts-row">
                                    <td colspan="100%">Faculty with assigned CLI: {{ dept.facultyWithCliCount }}</td>
                                </tr>

                                <!-- Group totals row -->
                                <tr class="dept-totals-row bg-grey-4">
                                    <th
                                        colspan="2"
                                        class="subt"
                                    >
                                        Totals:
                                    </th>
                                    <td
                                        v-for="type in orderedEffortTypes"
                                        :key="type"
                                        class="total"
                                        :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                    >
                                        {{ getTotalValue(dept.groupTotals, type) }}
                                    </td>
                                </tr>

                                <!-- Group averages row -->
                                <tr class="dept-averages-row bg-grey-3">
                                    <th
                                        colspan="2"
                                        class="subt"
                                    >
                                        Averages:
                                    </th>
                                    <td
                                        v-for="type in orderedEffortTypes"
                                        :key="type"
                                        class="total"
                                        :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                    >
                                        {{ getAverageValue(dept.groupAverages, type) }}
                                    </td>
                                </tr>
                            </tbody>
                        </table>
                    </div>
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
import type { MeritAverageReport } from "../types"

const {
    termCode,
    loading,
    report,
    printLoading,
    initialFilters,
    orderedEffortTypes,
    getTotalValue,
    getAverageValue,
    generateReport,
    handlePrint,
} = useReportPage<MeritAverageReport>({
    fetchReport: (params) => reportService.getMeritAverage(params),
    fetchPdf: (params) => reportService.openPdf("merit/average/pdf", params),
    getEffortTypes: (r) => r.effortTypes,
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

.dept-section {
    margin-bottom: 1.5rem;
}

.term-cell {
    font-size: 0.85em;
    color: var(--ucdavis-black-70);
}
</style>
