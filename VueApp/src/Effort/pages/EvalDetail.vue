<template>
    <div class="q-pa-md">
        <h1>Evaluation Report - Detail</h1>

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
                <div class="col text-h6">Evaluation - Detail</div>
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
                                Evaluation detail by instructor and course
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
                                        scope="col"
                                        class="col-role"
                                    >
                                        Role
                                    </th>
                                    <th
                                        scope="col"
                                        class="col-term"
                                    >
                                        Term
                                    </th>
                                    <th scope="col">Course</th>
                                    <th
                                        scope="col"
                                        class="col-numeric"
                                    >
                                        Average
                                    </th>
                                    <th
                                        scope="col"
                                        class="col-numeric"
                                    >
                                        Median
                                    </th>
                                </tr>
                            </thead>
                            <tbody>
                                <template
                                    v-for="instructor in dept.instructors"
                                    :key="instructor.mothraId"
                                >
                                    <!-- Course rows with instructor name rowspanned -->
                                    <tr
                                        v-for="(course, courseIdx) in instructor.courses"
                                        :key="`${instructor.mothraId}_${course.crn}_${course.termCode}`"
                                    >
                                        <td
                                            v-if="courseIdx === 0"
                                            :rowspan="instructor.courses.length"
                                            class="instructor-cell"
                                        >
                                            {{ instructor.instructor }}
                                        </td>
                                        <td class="col-role">{{ course.role }}</td>
                                        <td class="col-term">{{ course.termName }}</td>
                                        <td>{{ course.course }}</td>
                                        <td class="col-numeric">{{ formatDecimal(course.average) }}</td>
                                        <td class="col-numeric">{{ formatNullableDecimal(course.median) }}</td>
                                    </tr>

                                    <!-- Instructor subtotal row -->
                                    <tr class="totals-row bg-grey-1">
                                        <th
                                            scope="row"
                                            colspan="4"
                                            class="subt"
                                        >
                                            Instructor Average
                                        </th>
                                        <td class="col-numeric total">
                                            {{ formatDecimal(instructor.instructorAverage) }}
                                        </td>
                                        <td></td>
                                    </tr>
                                </template>

                                <!-- Department average row -->
                                <tr class="dept-totals-row bg-grey-4">
                                    <th
                                        scope="row"
                                        colspan="4"
                                        class="subt"
                                    >
                                        Department Average
                                    </th>
                                    <td class="col-numeric total">{{ formatDecimal(dept.departmentAverage) }}</td>
                                    <td></td>
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
import type { EvalDetailReport } from "../types"
import "../report-tables.css"

const { termCode, loading, report, initialFilters, generateReport, handlePrint, handleExcelDownload } =
    useReportPage<EvalDetailReport>({
        title: "Evaluation Detail Report",
        fetchReport: (params) => reportService.getEvalDetail(params),
        fetchPdf: (params) => reportService.openPdf("eval/detail/pdf", params),
        fetchExcel: (params) => reportService.downloadExcel("eval/detail/excel", params),
        hasData: (r) => r.departments.length > 0,
    })

function formatDecimal(value: number): string {
    return value.toFixed(2)
}

function formatNullableDecimal(value: number | null): string {
    return value !== null ? value.toFixed(2) : ""
}
</script>

<style scoped>
.col-numeric {
    text-align: right;
    white-space: nowrap;
    width: 5rem;
}

.col-term {
    white-space: nowrap;
}

.col-role {
    white-space: nowrap;
}
</style>
