import { ref, computed } from "vue"
import type { Ref } from "vue"
import { useQuasar } from "quasar"
import { useRoute } from "vue-router"
import { useEffortTypeColumns } from "./use-effort-type-columns"
import { useReportUrlParams } from "./use-report-url-params"
import type { ReportFilterParams } from "../types"

/**
 * Shared composable for effort report pages.
 * Handles term code extraction, loading states, effort type columns,
 * URL param sync, and PDF export — the pattern common to all report pages.
 */
export function useReportPage<T>(options: {
    fetchReport: (params: ReportFilterParams) => Promise<T | null>
    fetchPdf?: (params: ReportFilterParams) => Promise<boolean>
    getEffortTypes?: (report: T) => string[]
}) {
    const $q = useQuasar()
    const route = useRoute()
    const { initialFilters, updateUrlParams } = useReportUrlParams()

    const termCode = computed(() => {
        const tc = route.params.termCode
        return tc ? Number.parseInt(tc as string, 10) : 0
    })

    const loading = ref(false)
    const report = ref<T | null>(null) as Ref<T | null>
    const lastParams = ref<ReportFilterParams | null>(null)
    const printLoading = ref(false)

    // Effort type columns (only set up if getEffortTypes is provided)
    const effortTypes = computed(() => {
        if (!options.getEffortTypes || !report.value) {
            return []
        }
        return options.getEffortTypes(report.value)
    })
    const { effortColumns, getTotalValue, getAverageValue } = useEffortTypeColumns(effortTypes, {
        showZero: true,
        legacyColumnOrder: true,
    })
    const orderedEffortTypes = computed(() => effortColumns.value.map((c) => c.label))

    async function generateReport(params: ReportFilterParams) {
        updateUrlParams(params)
        loading.value = true
        lastParams.value = params
        try {
            report.value = await options.fetchReport(params)
        } finally {
            loading.value = false
        }
    }

    async function handlePrint() {
        if (!lastParams.value || !options.fetchPdf) {
            return
        }
        printLoading.value = true
        try {
            const opened = await options.fetchPdf(lastParams.value)
            if (!opened) {
                $q.notify({ type: "warning", message: "No data to export for the selected filters." })
            }
        } finally {
            printLoading.value = false
        }
    }

    return {
        termCode,
        loading,
        report,
        lastParams,
        printLoading,
        initialFilters,
        effortTypes,
        orderedEffortTypes,
        getTotalValue,
        getAverageValue,
        generateReport,
        handlePrint,
    }
}
