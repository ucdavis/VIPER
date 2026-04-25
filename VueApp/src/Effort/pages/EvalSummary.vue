<template>
    <EffortReportPage
        title="Evaluation Report - Summary"
        subtitle="Evaluation - Summary"
        :term-code="termCode"
        :loading="loading"
        :has-report="report !== null"
        :is-empty="report !== null && report.departments.length === 0"
        :initial-filters="initialFilters"
        :visible-fields="['department', 'faculty', 'role']"
        :on-pdf-export="handlePrint"
        :on-excel-export="handleExcelDownload"
        @generate="generateReport"
    >
        <template v-if="report">
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
    </EffortReportPage>
</template>

<script setup lang="ts">
import { reportService } from "../services/report-service"
import { useReportPage } from "../composables/use-report-page"
import EffortReportPage from "../components/EffortReportPage.vue"
import ReportDeptTabs from "../components/ReportDeptTabs.vue"
import type { EvalSummaryReport } from "../types"
import "../report-tables.css"

const { termCode, loading, report, initialFilters, generateReport, handlePrint, handleExcelDownload } =
    useReportPage<EvalSummaryReport>({
        fetchReport: (params) => reportService.getEvalSummary(params),
        fetchPdf: (params) => reportService.openPdf("eval/summary/pdf", params),
        fetchExcel: (params) => reportService.downloadExcel("eval/summary/excel", params),
    })

function formatDecimal(value: number): string {
    return value.toFixed(2)
}
</script>

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
