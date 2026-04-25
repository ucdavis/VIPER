<template>
    <EffortReportPage
        title="Teaching Activity Report - Grouped by Department"
        subtitle="Teaching Activity - Grouped by Department"
        :term-code="termCode"
        :loading="loading"
        :has-report="report !== null"
        :is-empty="report !== null && report.departments.length === 0"
        :initial-filters="initialFilters"
        :on-pdf-export="handlePrint"
        :on-excel-export="handleExcelDownload"
        @generate="generateReport"
    >
        <template v-if="report">
            <ReportDeptTabs :departments="report.departments">
                <template #default="{ dept }">
                    <table class="report-table">
                        <caption class="sr-only">
                            Teaching activity grouped by instructor
                        </caption>
                        <thead>
                            <tr>
                                <th
                                    scope="col"
                                    class="col-qtr"
                                >
                                    Qtr
                                </th>
                                <th
                                    scope="col"
                                    class="col-role"
                                >
                                    Role
                                </th>
                                <th
                                    scope="col"
                                    class="col-instructor"
                                >
                                    Instructor
                                </th>
                                <th
                                    scope="col"
                                    class="col-course"
                                >
                                    Course
                                </th>
                                <th
                                    scope="col"
                                    class="col-units"
                                >
                                    Units
                                </th>
                                <th
                                    scope="col"
                                    class="col-enroll"
                                >
                                    Enrl
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
                                    <td>{{ formatDecimal(course.units) }}</td>
                                    <td>{{ course.enrollment }}</td>
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
                                        scope="row"
                                        colspan="6"
                                        class="subt"
                                    >
                                        {{ instructor.instructor }} Totals:
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
                                <th
                                    scope="col"
                                    colspan="6"
                                ></th>
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

                            <!-- Department totals row -->
                            <tr class="dept-totals-row bg-grey-4">
                                <th
                                    scope="row"
                                    colspan="6"
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
    </EffortReportPage>
</template>

<script setup lang="ts">
import { reportService } from "../services/report-service"
import { useReportPage } from "../composables/use-report-page"
import EffortReportPage from "../components/EffortReportPage.vue"
import ReportDeptTabs from "../components/ReportDeptTabs.vue"
import type { TeachingActivityReport } from "../types"
import "../report-tables.css"

const {
    termCode,
    loading,
    report,
    initialFilters,
    orderedEffortTypes,
    getTotalValue,
    isSpacerColumn,
    getEffortTypeLabel,
    generateReport,
    handlePrint,
    handleExcelDownload,
} = useReportPage<TeachingActivityReport>({
    fetchReport: (params) => reportService.getTeachingActivityGrouped(params),
    fetchPdf: (params) => reportService.openPdf("teaching/grouped/pdf", params),
    fetchExcel: (params) => reportService.downloadExcel("teaching/grouped/excel", params),
    getEffortTypes: (r) => r.effortTypes,
})

function formatDecimal(value: number): string {
    return parseFloat(value.toString()).toString()
}
</script>

<style scoped>
.report-table .col-units {
    width: 3.5rem;
}

.report-table .col-enroll {
    width: 3.5rem;
}
</style>
