<template>
    <div class="q-pa-md">
        <h2>Teaching Activity Report - Grouped by Department</h2>

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
                        <q-spinner size="1em" class="q-mr-sm" />
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
                <div
                    v-if="activeFilters"
                    class="text-caption text-grey-7 no-print"
                >
                    Filters: {{ activeFilters }}
                </div>
            </template>

            <template v-if="report.departments.length === 0">
                <div class="text-grey-6 text-center q-pa-lg">No data found for the selected filters.</div>
            </template>

            <template v-else>
                <q-tabs
                    v-if="report.departments.length > 1"
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
                                            <th class="col-qtr">Qtr</th>
                                            <th class="col-role">Role</th>
                                            <th class="col-instructor">Instructor</th>
                                            <th class="col-course">Course</th>
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
                                                    :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                                >
                                                    {{ getTotalValue(course.effortByType ?? {}, type) }}
                                                </td>
                                            </tr>

                                            <!-- Instructor totals row -->
                                            <tr class="totals-row">
                                                <th
                                                    colspan="6"
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
                                        </template>

                                        <!-- Re-display effort type headers before department totals -->
                                        <tr class="header-repeat">
                                            <th colspan="6"></th>
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
                                        <tr class="dept-totals-row">
                                            <th
                                                colspan="6"
                                                class="subt"
                                            >
                                                Department Totals:
                                            </th>
                                            <td
                                                v-for="type in orderedEffortTypes"
                                                :key="type"
                                                class="total"
                                                :class="{ 'col-spacer': type === 'VAR' || type === 'EXM' }"
                                            >
                                                {{ getTotalValue(dept.departmentTotals, type) }}
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
import { ref, computed, watch } from "vue"
import { useQuasar } from "quasar"
import { useRoute } from "vue-router"
import { reportService } from "../services/report-service"
import { useEffortTypeColumns } from "../composables/use-effort-type-columns"
import { useReportUrlParams } from "../composables/use-report-url-params"
import ReportFilterForm from "../components/ReportFilterForm.vue"
import ReportLayout from "../components/ReportLayout.vue"
import type { TeachingActivityReport, ReportFilterParams } from "../types"

const $q = useQuasar()
const route = useRoute()
const { initialFilters, updateUrlParams } = useReportUrlParams()

const termCode = computed(() => {
    const tc = route.params.termCode
    return tc ? parseInt(tc as string, 10) : 0
})

const loading = ref(false)
const report = ref<TeachingActivityReport | null>(null)

const effortTypes = computed(() => report.value?.effortTypes ?? [])
const { effortColumns, getTotalValue } = useEffortTypeColumns(effortTypes, { showZero: true, legacyColumnOrder: true })

/** Strip trailing zeros: 12.00 → "12", 10.50 → "10.5" */
function formatDecimal(value: number): string {
    return parseFloat(value.toString()).toString()
}

const orderedEffortTypes = computed(() => {
    const types = effortColumns.value.map((c) => c.label)
    return types
})

const activeFilters = computed(() => {
    if (!report.value) return ""
    const parts: string[] = []
    if (report.value.filterDepartment) parts.push(`Dept: ${report.value.filterDepartment}`)
    if (report.value.filterPerson) parts.push(`Person: ${report.value.filterPerson}`)
    if (report.value.filterRole) parts.push(`Role: ${report.value.filterRole}`)
    if (report.value.filterTitle) parts.push(`Title: ${report.value.filterTitle}`)
    return parts.join(", ")
})

const activeDept = ref(0)

// Reset to first dept when new report loads
watch(
    () => report.value,
    () => {
        activeDept.value = 0
    },
)

// Track last filter params for PDF generation
const lastParams = ref<ReportFilterParams | null>(null)

const printLoading = ref(false)
async function handlePrint() {
    if (!lastParams.value) return
    printLoading.value = true
    try {
        const opened = await reportService.openGroupedPdf(lastParams.value)
        if (!opened) {
            $q.notify({ type: "warning", message: "No data to export for the selected filters." })
        }
    } finally {
        printLoading.value = false
    }
}

async function generateReport(params: ReportFilterParams) {
    updateUrlParams(params)
    loading.value = true
    lastParams.value = params
    try {
        report.value = await reportService.getTeachingActivityGrouped(params)
    } finally {
        loading.value = false
    }
}
</script>

<style scoped>
.dept-section {
    margin-bottom: 2rem;
}

.report-table {
    width: 100%;
    border-collapse: collapse;
    font-size: 0.8rem;
}

.report-table th {
    text-align: left;
    text-decoration: underline;
    font-size: 0.8em;
    padding: 0.25rem 0.5rem;
}

.report-table td {
    padding: 0.25rem 0.5rem;
    vertical-align: top;
}

.report-table th.subt {
    text-align: right;
    text-decoration: none;
    padding-right: 0.625rem;
    font-style: italic;
}

.report-table td.total {
    font-weight: bold;
}

.report-table .col-effort {
    width: 3.5rem;
    min-width: 3.5rem;
}

.report-table .col-spacer {
    padding-right: 1.25rem;
}

.col-qtr {
    width: 4rem;
}

.col-role {
    width: 3rem;
}

.col-instructor {
    width: 12rem;
}

.report-table .col-units {
    width: 3.5rem;
}

.report-table .col-enroll {
    width: 3.5rem;
}

.instructor-cell {
    vertical-align: top;
    font-weight: 500;
}

.totals-row {
    border-top: 1px solid #ccc;
    background-color: #f8f8f8;
}

.header-repeat {
    border-top: 2px solid #999;
}

.dept-totals-row {
    border-top: 2px solid #666;
    background-color: #e8e8e8;
}

@media print {
    .no-print {
        display: none;
    }

    .report-table th {
        text-decoration: underline;
        text-align: left;
        font-size: 0.8em;
    }

    .report-table th.subt {
        text-align: right;
        text-decoration: none;
        padding-right: 0.625rem;
    }

    .report-table td.total {
        font-weight: bold;
    }

    .col-spacer {
        padding-right: 1.25rem;
    }

    .totals-row,
    .dept-totals-row {
        -webkit-print-color-adjust: exact;
        print-color-adjust: exact;
    }
}
</style>
