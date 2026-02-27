<template>
    <div class="q-pa-md">
        <h2>Teaching Activity Report - School Summary</h2>

        <ReportFilterForm
            :term-code="termCode"
            :loading="loading"
            :initial-filters="initialFilters"
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
            <template #header>
                <div class="text-h6">{{ report.termName }} - School Summary</div>
            </template>

            <template v-if="report.departments.length === 0">
                <div class="text-grey-6 text-center q-pa-lg">No data found for the selected filters.</div>
            </template>

            <template v-else>
                <div class="dept-section">
                    <table class="report-table">
                        <thead>
                            <tr>
                                <th class="col-department">Department</th>
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
                                v-for="(dept, deptIdx) in report.departments"
                                :key="dept.department"
                            >
                                <!-- Department totals row -->
                                <tr>
                                    <td class="dept-name-cell">{{ dept.department }}</td>
                                    <td
                                        v-for="type in orderedEffortTypes"
                                        :key="type"
                                        :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
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
                                        :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                    >
                                        {{ getAverageValue(dept.averages, type) }}
                                    </td>
                                </tr>
                                <!-- HR divider between departments -->
                                <tr
                                    v-if="deptIdx < report.departments.length - 1"
                                    class="divider-row"
                                >
                                    <td colspan="100%">
                                        <hr />
                                    </td>
                                </tr>
                            </template>

                            <!-- Re-display effort type headers before grand totals -->
                            <tr class="header-repeat">
                                <th></th>
                                <th
                                    v-for="type in orderedEffortTypes"
                                    :key="type"
                                    class="col-effort"
                                    :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                >
                                    {{ type }}
                                </th>
                            </tr>

                            <!-- Grand Total row -->
                            <tr class="grand-totals-row bg-grey-4">
                                <th class="subt">Grand Total</th>
                                <td
                                    v-for="type in orderedEffortTypes"
                                    :key="type"
                                    class="total"
                                    :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
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
                                    :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
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
import type { SchoolSummaryReport } from "../types"

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
} = useReportPage<SchoolSummaryReport>({
    fetchReport: (params) => reportService.getSchoolSummary(params),
    fetchPdf: (params) => reportService.openPdf("teaching/school-summary/pdf", params),
    getEffortTypes: (r) => r.effortTypes,
})
</script>

<style>
@import "../report-tables.css";
</style>

<style scoped>
.dept-section {
    margin-bottom: 2rem;
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
