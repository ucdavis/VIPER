import { computed, ref, toValue, watch } from "vue"
import type { MaybeRefOrGetter } from "vue"
import { watchDebounced } from "@vueuse/core"
import { useRoute, useRouter } from "vue-router"
import { hasRequiredValues, initialValues, toQuery, toRequestParameters } from "./report-builder-query"
import { reportBuilderService } from "./report-builder-service"
import type {
    ReportExportFormat,
    ReportParameterValues,
    ReportRunRequest,
    ReportRunResult,
    ReportViewerDefinition,
} from "./report-builder-types"

/** The table's paging and sorting, in Quasar's QTable pagination shape. */
interface ReportPagination {
    page: number
    rowsPerPage: number
    sortBy: string | null
    descending: boolean
    rowsNumber: number
}

interface ReportBuilderOptions {
    /** Values for the form when the URL doesn't give them, such as from an embedding page. */
    params?: MaybeRefOrGetter<Record<string, string | string[]> | undefined>
    /**
     * Whether the form's values are written to and read from the URL query, so the page can be
     * bookmarked. Off for an embedded report, whose page owns its own URL.
     */
    syncQuery?: boolean
}

const ALL_ROWS = 0

// How long the form must be still before a change runs the report, so typing a name doesn't
// send a request per keystroke.
const AUTO_RUN_DELAY_MS = 500

function emptyPagination(): ReportPagination {
    return { page: 1, rowsPerPage: ALL_ROWS, sortBy: null, descending: false, rowsNumber: 0 }
}

/**
 * A row count for the table's pager. A source that can't count its rows still gets a next page
 * when this one came back full.
 */
function rowsNumber(result: ReportRunResult, pagination: ReportPagination): number {
    if (result.totalRows !== null) {
        return result.totalRows
    }
    const before = (result.page - 1) * result.perPage
    const more = pagination.rowsPerPage > 0 && result.rows.length === pagination.rowsPerPage ? 1 : 0
    return before + result.rows.length + more
}

/**
 * State for one admin-built report: loads its definition whenever the slug changes, fills the
 * form from the URL query, the embedding page or the report's defaults, and runs, pages, sorts,
 * searches and downloads it. A report whose required fields are all filled runs straight away,
 * and runs again shortly after the form changes; emptying a required field clears the rows.
 * Only the latest load and the latest run may update the state, so a slow reply never overwrites
 * a newer one.
 */
// oxlint-disable-next-line max-lines-per-function -- one report's state and the actions on it belong together
export function useReportBuilder(slug: MaybeRefOrGetter<string>, options: ReportBuilderOptions = {}) {
    const route = useRoute()
    const router = useRouter()
    const syncQuery = options.syncQuery ?? true

    const definition = ref<ReportViewerDefinition | null>(null)
    const values = ref<ReportParameterValues>({})
    const result = ref<ReportRunResult | null>(null)
    const errors = ref<string[]>([])
    const loading = ref(true)
    const notFound = ref(false)
    const running = ref(false)
    const exporting = ref<ReportExportFormat | null>(null)
    const nothingToExport = ref(false)
    const search = ref("")
    const pagination = ref<ReportPagination>(emptyPagination())

    // The request behind the rows on screen, so a download matches what the user sees even
    // after the form has been edited.
    let shownRequest: ReportRunRequest | null = null
    let loadId = 0
    let runId = 0
    // The form's values as last run (or as loaded), so a change back to them, or the load
    // itself, doesn't run the report again.
    let settledValues = ""

    const canExport = computed(() => result.value !== null && result.value.rows.length > 0)

    /** Whether every required field has a value, so the report can run. */
    const ready = computed(
        () => definition.value !== null && hasRequiredValues(definition.value.parameters, values.value),
    )

    function valuesKey(report: ReportViewerDefinition): string {
        return JSON.stringify(toRequestParameters(report.parameters, values.value))
    }

    function buildRequest(report: ReportViewerDefinition): ReportRunRequest {
        const { page, rowsPerPage, sortBy, descending } = pagination.value
        return {
            parameters: toRequestParameters(report.parameters, values.value),
            page,
            perPage: report.paginate ? rowsPerPage : null,
            sortBy,
            sortDescending: descending,
            search: search.value.trim() === "" ? null : search.value.trim(),
        }
    }

    async function fetchRows(): Promise<void> {
        const report = definition.value
        if (report === null) {
            return
        }
        runId += 1
        const id = runId
        running.value = true
        errors.value = []
        nothingToExport.value = false
        const request = buildRequest(report)
        const outcome = await reportBuilderService.run(report.slug, request)
        if (id !== runId) {
            return
        }
        result.value = outcome.result
        errors.value = outcome.errors
        shownRequest = outcome.result === null ? null : request
        if (outcome.result !== null) {
            pagination.value = { ...pagination.value, rowsNumber: rowsNumber(outcome.result, pagination.value) }
        }
        running.value = false
    }

    /** Runs the report from the first page with the form's values, recording them in the URL. */
    async function run(): Promise<void> {
        const report = definition.value
        if (report === null) {
            return
        }
        settledValues = valuesKey(report)
        pagination.value = { ...pagination.value, page: 1 }
        if (syncQuery) {
            await router.replace({ query: toQuery(report.parameters, values.value) })
        }
        await fetchRows()
    }

    /** Drops the rows on screen (and any run still on its way) when a required field is emptied. */
    async function clearRows(report: ReportViewerDefinition): Promise<void> {
        runId += 1
        settledValues = valuesKey(report)
        running.value = false
        result.value = null
        errors.value = []
        shownRequest = null
        if (syncQuery) {
            await router.replace({ query: toQuery(report.parameters, values.value) })
        }
    }

    /** Runs the report again once the form has settled on new values. */
    async function valuesChanged(): Promise<void> {
        const report = definition.value
        if (report === null || loading.value || valuesKey(report) === settledValues) {
            return
        }
        await (ready.value ? run() : clearRows(report))
    }

    /** A page or sort change from the table. */
    async function changePage(next: Omit<ReportPagination, "rowsNumber">): Promise<void> {
        pagination.value = { ...pagination.value, ...next }
        await fetchRows()
    }

    async function changeSearch(text: string | null): Promise<void> {
        search.value = text ?? ""
        pagination.value = { ...pagination.value, page: 1 }
        await fetchRows()
    }

    /** Downloads the report on screen; does nothing until a run has produced rows. */
    async function exportAs(format: ReportExportFormat): Promise<void> {
        const report = definition.value
        if (report === null || shownRequest === null) {
            return
        }
        exporting.value = format
        const outcome = await reportBuilderService.exportReport(report, format, shownRequest)
        nothingToExport.value = outcome === "empty"
        exporting.value = null
    }

    async function load(reportSlug: string): Promise<void> {
        loadId += 1
        const id = loadId
        runId += 1
        loading.value = true
        result.value = null
        errors.value = []
        shownRequest = null
        search.value = ""
        const report = await reportBuilderService.getDefinition(reportSlug)
        if (id !== loadId) {
            return
        }
        definition.value = report
        notFound.value = report === null
        values.value = report
            ? initialValues(report.parameters, syncQuery ? route.query : {}, toValue(options.params))
            : {}
        pagination.value = report
            ? {
                  ...emptyPagination(),
                  rowsPerPage: report.paginate ? report.pageSize : ALL_ROWS,
                  sortBy: report.sortBy,
                  descending: report.sortDescending,
              }
            : emptyPagination()
        settledValues = report ? valuesKey(report) : ""
        loading.value = false
        if (report && hasRequiredValues(report.parameters, values.value)) {
            await fetchRows()
        }
    }

    watch(() => toValue(slug), load, { immediate: true })
    watchDebounced(values, valuesChanged, { debounce: AUTO_RUN_DELAY_MS, deep: true })

    return {
        definition,
        values,
        result,
        errors,
        loading,
        notFound,
        running,
        exporting,
        nothingToExport,
        search,
        pagination,
        canExport,
        ready,
        run,
        changePage,
        changeSearch,
        exportAs,
    }
}

export type { ReportPagination }
