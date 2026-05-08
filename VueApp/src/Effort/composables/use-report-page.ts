import { ref, computed, unref } from "vue"
import type { Ref, MaybeRef } from "vue"
import { useQuasar } from "quasar"
import { useRoute } from "vue-router"
import { useTitle } from "@vueuse/core"
import {
    useEffortTypeColumns,
    SPACING_COLUMNS,
    getEffortTypeLabel,
    loadEffortTypeLabels,
} from "./use-effort-type-columns"
import { useReportUrlParams } from "./use-report-url-params"
import type { ReportFilterParams } from "../types"

// Wires document.title to "{base} - {term} | VIPER" once the report loads.
// `getSuffix` lets callers override the default `academicYear || termName`
// extraction for reports whose DTOs don't follow the common shape.
function useReportDocumentTitle<T>(
    base: MaybeRef<string>,
    report: Ref<T | null>,
    getSuffix?: (report: T) => string | null | undefined,
) {
    const suffix = computed(() => {
        if (!report.value) {
            return ""
        }
        if (getSuffix) {
            return getSuffix(report.value) ?? ""
        }
        // Default: prefer academicYear, fall back to termName. Both fields
        // are present on every report DTO under VueApp/src/Effort/types/;
        // the cast is safe because pages with different shapes override
        // via the getSuffix argument.
        const r = report.value as { academicYear?: string | null; termName?: string | null }
        return r.academicYear || r.termName || ""
    })
    useTitle(
        computed(() => {
            const baseTitle = unref(base)
            return suffix.value ? `${baseTitle} - ${suffix.value} | VIPER` : `${baseTitle} | VIPER`
        }),
    )
}

/**
 * Shared composable for effort report pages.
 * Handles term code extraction, loading states, effort type columns,
 * URL param sync, PDF export, and document title — the pattern common to
 * all report pages. The `title` option is the human-readable report name
 * (e.g. "Merit Detail Report"); the document title is rendered as
 * "{title} - {term} | VIPER" once a report is loaded, where {term} is
 * the report's `academicYear` if set, otherwise its `termName`. Pages
 * with non-standard shapes can override via `getTitleSuffix`.
 */
export function useReportPage<T>(options: {
    title: MaybeRef<string>
    fetchReport: (params: ReportFilterParams) => Promise<T | null>
    fetchPdf?: (params: ReportFilterParams) => void
    fetchExcel?: (params: ReportFilterParams) => Promise<boolean>
    getEffortTypes?: (report: T) => string[]
    hasData?: (report: T) => boolean
    getTitleSuffix?: (report: T) => string | null | undefined
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
    const excelLoading = ref(false)

    useReportDocumentTitle(options.title, report, options.getTitleSuffix)

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
            const [data] = await Promise.all([options.fetchReport(params), loadEffortTypeLabels()])
            report.value = data
        } finally {
            loading.value = false
        }
    }

    function handlePrint() {
        if (!lastParams.value || !options.fetchPdf) {
            return
        }
        // PDFs open in a new tab via GET, so the empty-data preflight has to happen here
        // — otherwise the controller's 204 response paints a blank tab.
        if (options.hasData && (!report.value || !options.hasData(report.value))) {
            $q.notify({ type: "warning", message: "No data to export for the selected filters." })
            return
        }
        options.fetchPdf(lastParams.value)
    }

    async function handleExcelDownload() {
        if (!lastParams.value || !options.fetchExcel) {
            return
        }
        excelLoading.value = true
        try {
            const success = await options.fetchExcel(lastParams.value)
            if (success) {
                $q.notify({ type: "positive", message: "Excel report downloaded." })
            } else {
                $q.notify({ type: "warning", message: "No data to export for the selected filters." })
            }
        } finally {
            excelLoading.value = false
        }
    }

    function isSpacerColumn(type: string): boolean {
        return SPACING_COLUMNS.has(type)
    }

    return {
        termCode,
        loading,
        report,
        lastParams,
        excelLoading,
        initialFilters,
        effortTypes,
        orderedEffortTypes,
        getTotalValue,
        getAverageValue,
        isSpacerColumn,
        getEffortTypeLabel,
        generateReport,
        handlePrint,
        handleExcelDownload,
    }
}
