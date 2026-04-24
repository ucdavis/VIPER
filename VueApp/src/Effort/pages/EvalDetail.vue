<template>
    <EffortReportPage
        title="Evaluation Report - Detail"
        subtitle="Evaluation - Detail"
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
    </EffortReportPage>
</template>

<script setup lang="ts">
import { reportService } from "../services/report-service"
import { useReportPage } from "../composables/use-report-page"
import EffortReportPage from "../components/EffortReportPage.vue"
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
