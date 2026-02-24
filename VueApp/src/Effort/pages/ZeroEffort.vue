<template>
    <div class="q-pa-md">
        <h2>Zero Effort Check</h2>

        <ReportFilterForm
            :term-code="termCode"
            :loading="loading"
            :initial-filters="initialFilters"
            :visible-fields="['department']"
            @generate="generateReport"
        />

        <!-- Loading state -->
        <div
            v-if="loading"
            class="text-grey q-my-lg"
        >
            Loading report...
        </div>

        <!-- Report content -->
        <template v-else-if="report">
            <template v-if="report.instructors.length === 0">
                <q-banner
                    class="bg-positive text-white q-my-md"
                    rounded
                >
                    All instructors have effort recorded for this term.
                </q-banner>
            </template>

            <template v-else>
                <table class="zero-effort-table q-mt-md">
                    <template
                        v-for="dept in groupedByDepartment"
                        :key="dept.department"
                    >
                        <!-- Department header row -->
                        <thead>
                            <tr class="dept-header-row bg-grey-4">
                                <th colspan="2">{{ dept.department }}</th>
                            </tr>
                            <tr>
                                <th class="col-verified">Verified</th>
                                <th>Instructor</th>
                            </tr>
                        </thead>
                        <tbody>
                            <tr
                                v-for="instructor in dept.instructors"
                                :key="instructor.mothraId"
                            >
                                <td class="text-center">
                                    <q-icon
                                        v-if="instructor.verified"
                                        name="check"
                                        color="positive"
                                        size="sm"
                                    />
                                </td>
                                <td>{{ instructor.instructor }}</td>
                            </tr>
                        </tbody>
                    </template>
                </table>

                <div class="text-caption text-grey-7 q-pa-sm q-mt-sm">
                    {{ report.instructors.length }} instructor{{ report.instructors.length !== 1 ? "s" : "" }}
                    with zero effort
                </div>
            </template>
        </template>

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
import { useReportUrlParams } from "../composables/use-report-url-params"
import ReportFilterForm from "../components/ReportFilterForm.vue"
import type { ZeroEffortReport, ZeroEffortInstructorRow, ReportFilterParams } from "../types"

const route = useRoute()
const { initialFilters, updateUrlParams } = useReportUrlParams()

const termCode = computed(() => {
    const tc = route.params.termCode
    return tc ? parseInt(tc as string, 10) : 0
})

const loading = ref(false)
const report = ref<ZeroEffortReport | null>(null)

const groupedByDepartment = computed(() => {
    if (!report.value) return []
    const grouped = new Map<string, ZeroEffortInstructorRow[]>()
    for (const inst of report.value.instructors) {
        const dept = inst.department || "Unknown"
        if (!grouped.has(dept)) grouped.set(dept, [])
        grouped.get(dept)!.push(inst)
    }
    return Array.from(grouped.entries())
        .sort(([a], [b]) => a.localeCompare(b))
        .map(([department, instructors]) => ({ department, instructors }))
})

async function generateReport(params: ReportFilterParams) {
    updateUrlParams(params)
    loading.value = true
    try {
        report.value = await reportService.getZeroEffort(params)
    } finally {
        loading.value = false
    }
}
</script>

<style scoped>
.zero-effort-table {
    width: 25rem;
    border-collapse: collapse;
    border: 1px solid var(--ucdavis-black-20);
}

.zero-effort-table th,
.zero-effort-table td {
    padding: 0.25rem 0.5rem;
    border: 1px solid var(--ucdavis-black-20);
}

.dept-header-row th {
    text-align: left;
    font-size: 0.9rem;
}

.col-verified {
    width: 4rem;
    text-align: center;
}
</style>
