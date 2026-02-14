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
                    @click="handlePrint"
                />
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
                <div
                    v-if="activeFilters"
                    class="text-caption text-grey-7"
                >
                    Filters: {{ activeFilters }}
                </div>
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
                    <q-card-section class="q-pa-sm instructor-card-header">
                        <div class="text-weight-bold">{{ instructor.instructor }}</div>
                        <div
                            v-if="instructor.department || instructor.jobGroupId"
                            class="text-caption text-grey-7"
                        >
                            <span v-if="instructor.department">{{ instructor.department }}</span>
                            <span v-if="instructor.department && instructor.jobGroupId"> - </span>
                            <span v-if="instructor.jobGroupId">{{ instructor.jobGroupId }}</span>
                        </div>
                    </q-card-section>

                    <q-separator />

                    <!-- Course rows table -->
                    <q-table
                        :rows="instructor.courses"
                        :columns="allColumns"
                        :row-key="(row: TeachingActivityCourseRow) => `${row.termCode}_${row.courseId}_${row.roleId}`"
                        dense
                        flat
                        hide-pagination
                        :rows-per-page-options="[0]"
                        class="report-table"
                    >
                        <!-- Instructor totals row -->
                        <template #bottom-row>
                            <q-tr class="totals-row">
                                <q-td
                                    colspan="6"
                                    class="text-right text-weight-bold"
                                >
                                    Total
                                </q-td>
                                <q-td
                                    v-for="type in report.effortTypes"
                                    :key="type"
                                    class="text-right text-weight-bold"
                                >
                                    {{ getTotalValue(instructor.instructorTotals, type) }}
                                </q-td>
                            </q-tr>
                        </template>
                    </q-table>
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
import { ref, computed } from "vue"
import { useRoute } from "vue-router"
import { reportService } from "../services/report-service"
import { useEffortTypeColumns } from "../composables/use-effort-type-columns"
import { useReportUrlParams } from "../composables/use-report-url-params"
import ReportFilterForm from "../components/ReportFilterForm.vue"
import ReportLayout from "../components/ReportLayout.vue"
import type { TeachingActivityReport, TeachingActivityInstructorGroup, TeachingActivityCourseRow, ReportFilterParams } from "../types"
import type { QTableColumn } from "quasar"

const route = useRoute()
const { initialFilters, updateUrlParams } = useReportUrlParams()

const termCode = computed(() => {
    const tc = route.params.termCode
    return tc ? parseInt(tc as string, 10) : 0
})

const loading = ref(false)
const report = ref<TeachingActivityReport | null>(null)

const effortTypes = computed(() => report.value?.effortTypes ?? [])
const { effortColumns, getTotalValue } = useEffortTypeColumns(effortTypes)

type InstructorWithDept = TeachingActivityInstructorGroup & { department: string }

/**
 * Flatten departments to get a flat list of instructors with their department name.
 */
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

const staticColumns: QTableColumn[] = [
    { name: "termCode", label: "Qtr", field: "termCode", align: "left", sortable: false, style: "width: 4rem" },
    { name: "course", label: "Course", field: "course", align: "left", sortable: false },
    { name: "crn", label: "CRN", field: "crn", align: "left", sortable: false, style: "width: 4rem" },
    { name: "units", label: "Units", field: "units", align: "right", sortable: false, style: "width: 3.5rem", format: (val: number) => parseFloat(val.toString()).toString() },
    {
        name: "enrollment",
        label: "Enrl",
        field: "enrollment",
        align: "right",
        sortable: false,
        style: "width: 3.5rem",
    },
    { name: "roleId", label: "Role", field: "roleId", align: "left", sortable: false },
]

const allColumns = computed<QTableColumn[]>(() => [...staticColumns, ...effortColumns.value])

const activeFilters = computed(() => {
    if (!report.value) return ""
    const parts: string[] = []
    if (report.value.filterDepartment) parts.push(`Dept: ${report.value.filterDepartment}`)
    if (report.value.filterPerson) parts.push(`Person: ${report.value.filterPerson}`)
    if (report.value.filterRole) parts.push(`Role: ${report.value.filterRole}`)
    if (report.value.filterTitle) parts.push(`Title: ${report.value.filterTitle}`)
    return parts.join(", ")
})

function handlePrint() {
    window.print()
}

async function generateReport(params: ReportFilterParams) {
    updateUrlParams(params)
    loading.value = true
    try {
        report.value = await reportService.getTeachingActivityIndividual(params)
    } finally {
        loading.value = false
    }
}
</script>

<style scoped>
.instructor-card-header {
    background-color: #f5f5f5;
}

.report-table {
    border-radius: 0;
}

.report-table :deep(.q-table__container) {
    border-radius: 0;
}

.totals-row {
    background-color: #fafafa;
    border-top: 2px solid #ddd;
}

@media print {
    .instructor-card {
        break-inside: avoid;
    }

    .instructor-card-header {
        background-color: #f5f5f5;
        -webkit-print-color-adjust: exact;
        print-color-adjust: exact;
    }
}
</style>
