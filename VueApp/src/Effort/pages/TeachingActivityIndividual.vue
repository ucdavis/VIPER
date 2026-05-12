<template>
    <EffortReportPage
        title="Teaching Activity Report - Individual"
        :term-code="termCode"
        :loading="loading"
        :has-report="report !== null"
        :is-empty="allInstructors.length === 0"
        :initial-filters="initialFilters"
        :on-pdf-export="handlePrint"
        :on-excel-export="handleExcelDownload"
        @generate="generateReport"
    >
        <template
            v-if="report"
            #header
            >{{ report.termName }} - Teaching Activity (Individual)</template
        >

        <template v-if="report">
            <q-card
                v-for="instructor in allInstructors"
                :key="instructor.mothraId"
                flat
                bordered
                class="q-mb-md instructor-card"
            >
                <!-- Instructor header -->
                <q-card-section class="q-pa-sm instructor-card-header bg-grey-2">
                    <div class="text-weight-bold">{{ instructor.instructor }}</div>
                    <div
                        v-if="instructor.department"
                        class="text-caption text-grey-8"
                    >
                        {{ instructor.department }}
                    </div>
                </q-card-section>

                <q-separator />

                <!-- Course rows table -->
                <table class="report-table">
                    <caption class="sr-only">
                        Teaching activity for
                        {{
                            instructor.instructor
                        }}
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
                            <th scope="col">Course</th>
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
                        <tr
                            v-for="course in instructor.courses"
                            :key="`${course.termCode}_${course.courseId}_${course.roleId}`"
                        >
                            <td>{{ course.termCode }}</td>
                            <td>{{ course.roleId }}</td>
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
                                colspan="5"
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
                    </tbody>
                </table>
            </q-card>
        </template>
    </EffortReportPage>
</template>

<script setup lang="ts">
import { computed } from "vue"
import { reportService } from "../services/report-service"
import { useReportPage } from "../composables/use-report-page"
import EffortReportPage from "../components/EffortReportPage.vue"
import type { TeachingActivityReport, TeachingActivityInstructorGroup } from "../types"
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
    title: "Teaching Activity by Instructor",
    fetchReport: (params) => reportService.getTeachingActivityIndividual(params),
    fetchPdf: (params) => reportService.openPdf("teaching/individual/pdf", params),
    fetchExcel: (params) => reportService.downloadExcel("teaching/individual/excel", params),
    getEffortTypes: (r) => r.effortTypes,
    hasData: (r) => r.departments.some((d) => d.instructors.length > 0),
})

function formatDecimal(value: number): string {
    return parseFloat(value.toString()).toString()
}

type InstructorWithDept = TeachingActivityInstructorGroup & { department: string }

const allInstructors = computed<InstructorWithDept[]>(() => {
    if (!report.value) return []
    const result: InstructorWithDept[] = []
    for (const dept of report.value.departments) {
        for (const instructor of dept.instructors) {
            result.push({ ...instructor, department: dept.department })
        }
    }
    return result
})
</script>

<style scoped>
.col-units {
    width: 3.5rem;
}

.col-enroll {
    width: 3.5rem;
}
</style>
