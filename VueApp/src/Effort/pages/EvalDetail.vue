<template>
    <div class="q-pa-md">
        <h2>Evaluation Report - Detail</h2>

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
                <div
                    v-for="dept in report.departments"
                    :key="dept.department"
                    class="dept-section"
                >
                    <div class="dept-subheader bg-grey-3">{{ dept.department }}</div>

                    <table class="report-table">
                        <thead>
                            <tr>
                                <th class="col-instructor">Instructor</th>
                                <th class="col-role">Role</th>
                                <th class="col-term">Term</th>
                                <th>Course</th>
                                <th class="col-numeric">Average</th>
                                <th class="col-numeric">Median</th>
                                <th class="col-numeric">Responses</th>
                                <th class="col-numeric">Enrolled</th>
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
                                    <td class="col-term">{{ course.termCode }}</td>
                                    <td>{{ course.course }}</td>
                                    <td class="col-numeric">{{ formatDecimal(course.average) }}</td>
                                    <td class="col-numeric">{{ formatNullableDecimal(course.median) }}</td>
                                    <td class="col-numeric">{{ course.numResponses }}</td>
                                    <td class="col-numeric">{{ course.numEnrolled }}</td>
                                </tr>

                                <!-- Instructor subtotal row -->
                                <tr class="totals-row bg-grey-1">
                                    <th class="subt">Instructor Average:</th>
                                    <td></td>
                                    <td></td>
                                    <td></td>
                                    <td class="col-numeric total">{{ formatDecimal(instructor.instructorAverage) }}</td>
                                    <td class="col-numeric total">
                                        {{ formatNullableDecimal(instructor.instructorMedian) }}
                                    </td>
                                    <td></td>
                                    <td></td>
                                </tr>
                            </template>

                            <!-- Department average row -->
                            <tr class="dept-totals-row bg-grey-4">
                                <th class="subt">Department Average:</th>
                                <td></td>
                                <td></td>
                                <td></td>
                                <td class="col-numeric total">{{ formatDecimal(dept.departmentAverage) }}</td>
                                <td></td>
                                <td></td>
                                <td></td>
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
import type { EvalDetailReport } from "../types"

const { termCode, loading, report, printLoading, initialFilters, generateReport, handlePrint } =
    useReportPage<EvalDetailReport>({
        fetchReport: (params) => reportService.getEvalDetail(params),
        fetchPdf: (params) => reportService.openPdf("eval/detail/pdf", params),
    })

function formatDecimal(value: number): string {
    return value.toFixed(2)
}

function formatNullableDecimal(value: number | null): string {
    return value !== null ? value.toFixed(2) : ""
}
</script>

<style>
@import url("../report-tables.css");
</style>

<style scoped>
.dept-section {
    margin-bottom: 1.5rem;
}

.col-numeric {
    text-align: right;
    white-space: nowrap;
    width: 5rem;
}

.col-term {
    white-space: nowrap;
    width: 4rem;
}

.col-role {
    white-space: nowrap;
}
</style>
