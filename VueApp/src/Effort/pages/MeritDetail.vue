<template>
    <div class="q-pa-md">
        <h2>Merit &amp; Promotion Report - Detail</h2>

        <ReportFilterForm
            :term-code="termCode"
            :loading="loading"
            :initial-filters="initialFilters"
            :visible-fields="['department', 'faculty', 'role']"
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

            <template v-if="report.departments.length === 0">
                <div class="text-grey-6 text-center q-pa-lg">No data found for the selected filters.</div>
            </template>

            <template v-else>
                <ReportDeptTabs :departments="report.departments">
                    <template #default="{ dept }">
                        <table class="report-table">
                            <thead>
                                <tr>
                                    <th class="col-qtr">Qtr</th>
                                    <th class="col-role">Role</th>
                                    <th class="col-instructor">Instructor</th>
                                    <th class="col-course">Course</th>
                                    <th
                                        v-for="type in orderedEffortTypes"
                                        :key="type"
                                        class="col-effort"
                                        :class="{ 'col-spacer': isSpacerColumn(type) }"
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
                                    <!-- Course rows with instructor rowspan on first row -->
                                    <tr
                                        v-for="(course, courseIdx) in instructor.courses"
                                        :key="`${course.termCode}_${course.courseId}_${course.roleId}`"
                                    >
                                        <td>{{ course.termCode }}</td>
                                        <td>{{ course.roleId }}</td>
                                        <td
                                            v-if="courseIdx === 0"
                                            :rowspan="instructor.courses.length"
                                            class="instructor-cell"
                                        >
                                            {{ instructor.instructor }}
                                        </td>
                                        <td>{{ course.course }}</td>
                                        <td
                                            v-for="type in orderedEffortTypes"
                                            :key="type"
                                            :class="{ 'col-spacer': isSpacerColumn(type) }"
                                        >
                                            {{ getTotalValue(course.effortByType ?? {}, type) }}
                                        </td>
                                    </tr>

                                    <!-- Instructor totals row -->
                                    <tr class="totals-row bg-grey-1">
                                        <th
                                            colspan="4"
                                            class="subt"
                                        >
                                            Instructor Totals:
                                        </th>
                                        <td
                                            v-for="type in orderedEffortTypes"
                                            :key="type"
                                            class="total"
                                            :class="{ 'col-spacer': isSpacerColumn(type) }"
                                        >
                                            {{ getTotalValue(instructor.instructorTotals, type) }}
                                        </td>
                                    </tr>
                                </template>

                                <!-- Re-display effort type headers before department totals -->
                                <tr class="header-repeat">
                                    <th colspan="4"></th>
                                    <th
                                        v-for="type in orderedEffortTypes"
                                        :key="type"
                                        class="col-effort"
                                        :class="{ 'col-spacer': isSpacerColumn(type) }"
                                    >
                                        {{ type }}
                                    </th>
                                </tr>

                                <!-- Department totals row -->
                                <tr class="dept-totals-row bg-grey-4">
                                    <th
                                        colspan="4"
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
import type { MeritDetailReport } from "../types"

const {
    termCode,
    loading,
    report,
    printLoading,
    initialFilters,
    orderedEffortTypes,
    getTotalValue,
    isSpacerColumn,
    generateReport,
    handlePrint,
} = useReportPage<MeritDetailReport>({
    fetchReport: (params) => reportService.getMeritDetail(params),
    fetchPdf: (params) => reportService.openPdf("merit/detail/pdf", params),
    getEffortTypes: (r) => r.effortTypes,
})
</script>

<style>
@import "../report-tables.css";
</style>

<style scoped></style>
