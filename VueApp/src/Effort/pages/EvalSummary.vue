<template>
    <div class="q-pa-md">
        <h1>Evaluation Report - Summary</h1>

        <ReportFilterForm
            :term-code="termCode"
            :loading="loading"
            :initial-filters="initialFilters"
            :visible-fields="['department', 'faculty', 'role']"
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
                <div class="col text-h6">Evaluation - Summary</div>
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
                                Evaluation summary by instructor
                            </caption>
                            <thead>
                                <tr>
                                    <th scope="col">Instructor</th>
                                    <th
                                        scope="col"
                                        class="col-numeric"
                                    >
                                        Average
                                    </th>
                                </tr>
                            </thead>
                            <tbody>
                                <tr
                                    v-for="instructor in dept.instructors"
                                    :key="instructor.mothraId"
                                >
                                    <td>{{ instructor.instructor }}</td>
                                    <td class="col-numeric">{{ formatDecimal(instructor.weightedAverage) }}</td>
                                </tr>

                                <!-- Department average row -->
                                <tr class="dept-totals-row bg-grey-4">
                                    <th
                                        scope="row"
                                        class="subt"
                                    >
                                        Department Average
                                    </th>
                                    <td class="col-numeric total">{{ formatDecimal(dept.departmentAverage) }}</td>
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
import type { EvalSummaryReport } from "../types"

const { termCode, loading, report, initialFilters, generateReport, handlePrint, handleExcelDownload } =
    useReportPage<EvalSummaryReport>({
        title: "Evaluation Summary Report",
        fetchReport: (params) => reportService.getEvalSummary(params),
        fetchPdf: (params) => reportService.openPdf("eval/summary/pdf", params),
        fetchExcel: (params) => reportService.downloadExcel("eval/summary/excel", params),
        hasData: (r) => r.departments.length > 0,
    })

function formatDecimal(value: number): string {
    return value.toFixed(2)
}
</script>

<style>
@import url("../report-tables.css");
</style>

<style scoped>
.report-table {
    width: 50%;
    min-width: 20rem;
}

.col-numeric {
    text-align: right;
    white-space: nowrap;
    padding-left: 3rem;
}
</style>
