<template>
    <div class="q-pa-md">
        <h2>Teaching Activity Report - Department Summary</h2>

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
            <template #header />

            <template v-if="report.departments.length === 0">
                <div class="text-grey-6 text-center q-pa-lg">No data found for the selected filters.</div>
            </template>

            <template v-else>
                <q-tabs
                    v-if="report.departments.length > 0"
                    v-model="activeDept"
                    dense
                    align="left"
                    class="text-grey"
                    active-color="primary"
                    indicator-color="primary"
                    narrow-indicator
                >
                    <q-tab
                        v-for="(dept, idx) in report.departments"
                        :key="dept.department"
                        :name="idx"
                        :label="dept.department"
                    />
                </q-tabs>

                <q-separator />

                <q-tab-panels
                    v-model="activeDept"
                    animated
                >
                    <q-tab-panel
                        v-for="(dept, idx) in report.departments"
                        :key="dept.department"
                        :name="idx"
                        class="q-px-none"
                    >
                        <!-- Only render table content for active panel to avoid browser freeze -->
                        <template v-if="idx === activeDept">
                            <div class="dept-section">
                                <table class="report-table">
                                    <thead>
                                        <tr>
                                            <th class="col-instructor">Instructor</th>
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
                                            v-for="instructor in dept.instructors"
                                            :key="instructor.mothraId"
                                        >
                                            <td class="instructor-cell">{{ instructor.instructor }}</td>
                                            <td
                                                v-for="type in orderedEffortTypes"
                                                :key="type"
                                                :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                            >
                                                {{ getTotalValue(instructor.effortByType ?? {}, type) }}
                                            </td>
                                        </tr>

                                        <!-- Re-display effort type headers before department totals -->
                                        <tr class="header-repeat">
                                            <th></th>
                                            <th
                                                v-for="type in orderedEffortTypes"
                                                :key="type"
                                                class="col-effort"
                                                :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                            >
                                                {{ type }}
                                            </th>
                                        </tr>

                                        <!-- Department totals row -->
                                        <tr class="dept-totals-row bg-grey-4">
                                            <th class="subt">Department Totals:</th>
                                            <td
                                                v-for="type in orderedEffortTypes"
                                                :key="type"
                                                class="total"
                                                :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                            >
                                                {{ getTotalValue(dept.departmentTotals, type) }}
                                            </td>
                                        </tr>

                                        <!-- Number Faculty row -->
                                        <tr class="faculty-row">
                                            <td>
                                                <span class="faculty-label">Number Faculty:</span>
                                                <span class="faculty-count">{{ dept.facultyCount }}</span>
                                            </td>
                                        </tr>

                                        <!-- Faculty w/ CLI + averages row -->
                                        <tr class="faculty-row bg-grey-3">
                                            <td>
                                                <span class="faculty-label">Faculty w/ CLI assigned:</span>
                                                <span class="faculty-count">{{ dept.facultyWithCliCount }}</span>
                                                <span class="avg-text">Average</span>
                                            </td>
                                            <td
                                                v-for="type in orderedEffortTypes"
                                                :key="type"
                                                class="total"
                                                :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                            >
                                                {{ getAverageValue(dept.departmentAverages, type) }}
                                            </td>
                                        </tr>
                                    </tbody>
                                </table>
                            </div>
                        </template>
                    </q-tab-panel>
                </q-tab-panels>
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
import { ref, watch } from "vue"
import { reportService } from "../services/report-service"
import { useReportPage } from "../composables/use-report-page"
import ReportFilterForm from "../components/ReportFilterForm.vue"
import ReportLayout from "../components/ReportLayout.vue"
import type { DeptSummaryReport } from "../types"

const {
    termCode,
    loading,
    report,
    printLoading,
    initialFilters,
    orderedEffortTypes,
    getTotalValue,
    getAverageValue,
    generateReport,
    handlePrint,
} = useReportPage<DeptSummaryReport>({
    fetchReport: (params) => reportService.getDeptSummary(params),
    fetchPdf: (params) => reportService.openPdf("teaching/dept-summary/pdf", params),
    getEffortTypes: (r) => r.effortTypes,
})

const activeDept = ref(0)

watch(
    () => report.value,
    () => {
        activeDept.value = 0
    },
)
</script>

<style>
@import "../report-tables.css";
</style>

<style scoped>
.dept-section {
    margin-bottom: 2rem;
}
</style>
