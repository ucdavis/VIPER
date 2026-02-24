<template>
    <div class="q-pa-md">
        <h2>Teaching Activity Report - Individual</h2>

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
                <div class="text-h6">{{ report.termName }} - Teaching Activity (Individual)</div>
            </template>

            <template v-if="allInstructors.length === 0">
                <div class="text-grey-6 text-center q-pa-lg">No data found for the selected filters.</div>
            </template>

            <template v-else>
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
                            class="text-caption text-grey-7"
                        >
                            {{ instructor.department }}
                        </div>
                    </q-card-section>

                    <q-separator />

                    <!-- Course rows table -->
                    <table class="report-table">
                        <thead>
                            <tr>
                                <th class="col-qtr">Qtr</th>
                                <th class="col-role">Role</th>
                                <th>Course</th>
                                <th class="col-units">Units</th>
                                <th class="col-enroll">Enrl</th>
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
                                    :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                >
                                    {{ getTotalValue(course.effortByType ?? {}, type) }}
                                </td>
                            </tr>

                            <!-- Instructor totals row -->
                            <tr class="totals-row bg-grey-1">
                                <th
                                    colspan="5"
                                    class="subt"
                                >
                                    {{ instructor.instructor }} Totals:
                                </th>
                                <td
                                    v-for="type in orderedEffortTypes"
                                    :key="type"
                                    class="total"
                                    :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                >
                                    {{ getTotalValue(instructor.instructorTotals, type) }}
                                </td>
                            </tr>
                        </tbody>
                    </table>
                </q-card>
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
import { computed } from "vue"
import { reportService } from "../services/report-service"
import { useReportPage } from "../composables/use-report-page"
import ReportFilterForm from "../components/ReportFilterForm.vue"
import ReportLayout from "../components/ReportLayout.vue"
import type { TeachingActivityReport, TeachingActivityInstructorGroup } from "../types"

const {
    termCode,
    loading,
    report,
    printLoading,
    initialFilters,
    orderedEffortTypes,
    getTotalValue,
    generateReport,
    handlePrint,
} = useReportPage<TeachingActivityReport>({
    fetchReport: (params) => reportService.getTeachingActivityIndividual(params),
    fetchPdf: (params) => reportService.openPdf("teaching/individual/pdf", params),
    getEffortTypes: (r) => r.effortTypes,
})

/** Strip trailing zeros: 12.00 -> "12", 10.50 -> "10.5" */
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

<style>
@import "../report-tables.css";
</style>

<style scoped>
.col-units {
    width: 3.5rem;
}

.col-enroll {
    width: 3.5rem;
}
</style>
