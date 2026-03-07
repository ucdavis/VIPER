<template>
    <div class="q-pa-md">
        <h2>Evaluation Report - Summary</h2>

        <ReportFilterForm
            :term-code="termCode"
            :loading="loading"
            :initial-filters="initialFilters"
            :visible-fields="['department', 'faculty', 'role']"
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

            <template v-if="report.departments.length === 0">
                <div class="text-grey-6 text-center q-pa-lg">No data found for the selected filters.</div>
            </template>

            <template v-else>
                <ReportDeptTabs :departments="report.departments">
                    <template #default="{ dept }">
                        <table class="report-table">
                            <thead>
                                <tr>
                                    <th>Instructor</th>
                                    <th class="col-numeric">Average</th>
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
                                    <th class="subt">Department Average</th>
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
import ReportDeptTabs from "../components/ReportDeptTabs.vue"
import type { EvalSummaryReport } from "../types"

const { termCode, loading, report, printLoading, initialFilters, generateReport, handlePrint } =
    useReportPage<EvalSummaryReport>({
        fetchReport: (params) => reportService.getEvalSummary(params),
        fetchPdf: (params) => reportService.openPdf("eval/summary/pdf", params),
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
