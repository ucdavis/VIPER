<template>
    <section aria-labelledby="report-failed-runs">
        <h2 id="report-failed-runs">Failed runs</h2>
        <p>
            Runs and downloads that failed because of the report or its source, with SQL Server's reason. Mistakes in
            the values a user entered aren't included.
        </p>
        <div class="row q-col-gutter-sm q-mb-md compact-form">
            <div class="col-12 col-sm-4 col-md-3">
                <q-select
                    v-model="days"
                    :options="DAY_OPTIONS"
                    label="Period"
                    emit-value
                    map-options
                    outlined
                    dense
                    options-dense
                />
            </div>
            <div class="col-12 col-sm-8 col-md-5">
                <q-select
                    v-model="reportId"
                    :options="reportOptions"
                    label="Report"
                    emit-value
                    map-options
                    clearable
                    outlined
                    dense
                    options-dense
                    :disable="reportOptions.length === 0"
                />
            </div>
        </div>
        <StatusBanner
            v-if="failed"
            type="error"
        >
            The failed runs couldn't be loaded. Please try again later.
        </StatusBanner>
        <template v-else>
            <p
                v-if="!loading && failures.length > 0"
                class="text-caption"
            >
                {{ summary }}
            </p>
            <q-table
                :rows="shown"
                :columns="COLUMNS"
                :loading="loading"
                :pagination="{ rowsPerPage: 25 }"
                row-key="id"
                flat
                bordered
                dense
                wrap-cells
                no-data-label="No failed runs in this period."
            >
                <template #body-cell-report="cell">
                    <q-td :props="cell">
                        <router-link :to="{ name: 'ReportAdminEdit', params: { id: cell.row.reportId } }">{{
                            cell.row.reportTitle
                        }}</router-link>
                        <StatusBadge
                            v-if="cell.row.reportDeleted"
                            color="grey-7"
                            label="Deleted"
                            class="q-ml-sm"
                        />
                    </q-td>
                </template>
                <template #body-cell-error="cell">
                    <q-td :props="cell">
                        <div>{{ cell.value }}</div>
                        <div
                            v-if="cell.row.parameters"
                            class="text-caption text-grey-8"
                        >
                            Values: {{ cell.row.parameters }}
                        </div>
                    </q-td>
                </template>
            </q-table>
        </template>
    </section>
</template>

<script setup lang="ts">
import { computed, ref, watch } from "vue"
import type { QTableColumn } from "quasar"
import StatusBadge from "@/components/StatusBadge.vue"
import StatusBanner from "@/components/StatusBanner.vue"
import { formatCell } from "@/shared/report-builder/report-builder-format"
import { reportAdminService } from "../admin/report-admin-service"
import type { ReportFailure, ReportRunAction } from "../admin/report-admin-types"

const DAY_OPTIONS = [
    { label: "Last 7 days", value: 7 },
    { label: "Last 30 days", value: 30 },
    { label: "Last 90 days", value: 90 },
    { label: "Last year", value: 365 },
]

const ACTIONS: Record<ReportRunAction, string> = { Run: "On screen", ExportExcel: "Excel", ExportPdf: "PDF" }

function describeError(failure: ReportFailure): string {
    return failure.errorNumber === null
        ? failure.errorMessage
        : `SQL error ${failure.errorNumber}: ${failure.errorMessage}`
}

const COLUMNS: QTableColumn<ReportFailure>[] = [
    {
        name: "ranOn",
        label: "When",
        field: "ranOn",
        format: (value: string | null) => formatCell(value, "DateTime"),
        align: "left",
        sortable: true,
    },
    { name: "report", label: "Report", field: "reportTitle", align: "left", sortable: true },
    {
        name: "action",
        label: "As",
        field: "action",
        format: (value: ReportRunAction | null) => (value === null ? "" : ACTIONS[value]),
        align: "left",
    },
    { name: "loginId", label: "User", field: "loginId", align: "left", sortable: true },
    { name: "error", label: "Reason", field: describeError, align: "left" },
]

const days = ref(30)
const reportId = ref<number | null>(null)
const failures = ref<ReportFailure[]>([])
const loading = ref(true)
const failed = ref(false)

/** The reports that failed in the period, for the filter. */
const reportOptions = computed(() => {
    const titles = new Map<number, string>()
    for (const failure of failures.value) {
        titles.set(failure.reportId, failure.reportTitle)
    }
    return [...titles].map(([value, label]) => ({ value, label })).toSorted((a, b) => a.label.localeCompare(b.label))
})

const shown = computed(() =>
    reportId.value === null ? failures.value : failures.value.filter((failure) => failure.reportId === reportId.value),
)

const summary = computed(() => {
    const runs = failures.value.length === 1 ? "1 failed run" : `${failures.value.length} failed runs`
    const reports = reportOptions.value.length === 1 ? "1 report" : `${reportOptions.value.length} reports`
    return `${runs} across ${reports}.`
})

// Changing the period again before the last list arrives must not let the older reply win.
let latestLoad = 0

async function load(period: number): Promise<void> {
    latestLoad += 1
    const thisLoad = latestLoad
    loading.value = true
    const found = await reportAdminService.getFailures(period)
    if (thisLoad !== latestLoad) {
        return
    }
    failed.value = found === null
    failures.value = found ?? []
    if (reportId.value !== null && !failures.value.some((failure) => failure.reportId === reportId.value)) {
        reportId.value = null
    }
    loading.value = false
}

watch(days, load, { immediate: true })
</script>
