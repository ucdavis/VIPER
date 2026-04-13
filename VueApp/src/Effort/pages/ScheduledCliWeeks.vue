<template>
    <div class="q-pa-md">
        <h1>Merit &amp; Promotion Report - Scheduled Clinical Weeks</h1>

        <ReportFilterForm
            :term-code="termCode"
            :loading="loading"
            :initial-filters="initialFilters"
            :visible-fields="[]"
            @generate="generateReport"
        />

        <!-- Loading state -->
        <div
            v-if="loading"
            role="status"
            class="text-center q-my-lg"
        >
            <q-spinner-dots
                size="3rem"
                color="primary"
            />
            <div class="q-mt-md text-body1">Loading report...</div>
        </div>

        <!-- Report content -->
        <template v-else-if="report">
            <StatusBanner type="info">
                The following data is based on the instructor schedule pulled live from the Clinical Scheduler. It does
                not contain the verified effort entered by faculty and departments.
            </StatusBanner>
            <div class="row items-center q-pa-sm q-mb-md bg-grey-2 rounded-borders">
                <div class="col text-h6">Scheduled Clinical Weeks</div>
                <div class="col-auto no-print">
                    <ExportToolbar
                        :pdf-export="handlePrint"
                        :excel-export="handleExcelDownload"
                    />
                </div>
            </div>

            <template v-if="report.instructors.length === 0">
                <div
                    role="status"
                    class="text-grey-6 text-center q-pa-lg"
                >
                    No scheduled clinical weeks found.
                </div>
            </template>

            <template v-else>
                <q-table
                    :rows="tableRows"
                    :columns="tableColumns"
                    row-key="mothraId"
                    flat
                    bordered
                    dense
                    hide-pagination
                    hide-bottom
                    :rows-per-page-options="[0]"
                    :pagination="{ rowsPerPage: 0 }"
                />
            </template>
        </template>

        <!-- No report generated yet -->
        <div
            v-else-if="!loading"
            class="text-grey-7 text-center q-pa-lg"
        >
            Select a term or academic year and click "Generate Report" to view data.
        </div>
    </div>
</template>

<script setup lang="ts">
import { computed } from "vue"
import StatusBanner from "@/components/StatusBanner.vue"
import { reportService } from "../services/report-service"
import { useReportPage } from "../composables/use-report-page"
import ExportToolbar from "@/components/ExportToolbar.vue"
import ReportFilterForm from "../components/ReportFilterForm.vue"
import type { ScheduledCliWeeksReport } from "../types"
import type { QTableColumn } from "quasar"

const { termCode, loading, report, initialFilters, generateReport, handlePrint, handleExcelDownload } =
    useReportPage<ScheduledCliWeeksReport>({
        fetchReport: (params) => reportService.getScheduledCliWeeks(params),
        fetchPdf: (params) => reportService.openPdf("clinical-schedule/pdf", params),
        fetchExcel: (params) => reportService.downloadExcel("clinical-schedule/excel", params),
    })

type CliWeeksRow = {
    mothraId: string
    instructor: string
    [termName: string]: string
}

const tableColumns = computed<QTableColumn[]>(() => {
    if (!report.value) return []
    const cols: QTableColumn[] = [
        { name: "instructor", label: "Instructor", field: "instructor", align: "left", sortable: true },
    ]
    for (const termName of report.value.termNames) {
        cols.push({
            name: termName,
            label: termName,
            field: termName,
            align: "left",
            sortable: false,
            style: "white-space: pre-line",
        })
    }
    if (report.value.termNames.length > 1) {
        cols.push({
            name: "academicYearTotal",
            label: "AY Total",
            field: "academicYearTotal",
            align: "center",
            sortable: true,
        })
    }
    return cols
})

const tableRows = computed<CliWeeksRow[]>(() => {
    if (!report.value) return []
    return report.value.instructors.map((inst) => {
        const row: CliWeeksRow = { mothraId: inst.mothraId, instructor: inst.instructor }
        for (const termName of report.value!.termNames) {
            const term = inst.terms.find((t) => t.termName === termName)
            if (term) {
                row[termName] = Object.entries(term.weeksByService)
                    .filter(([, weeks]) => weeks > 0)
                    .map(([service, weeks]) => `${service} - ${weeks}`)
                    .join("\n")
            } else {
                row[termName] = ""
            }
        }
        row.academicYearTotal = String(inst.totalWeeks)
        return row
    })
})
</script>
