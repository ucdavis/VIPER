import { ref, toValue, watch } from "vue"
import type { MaybeRefOrGetter } from "vue"
import { useRoute, useRouter } from "vue-router"
import { hasQueryParameters, initialValues, toQuery, toRequestBody } from "./report-query"
import { reportService } from "./report-service"
import type { ReportDefinitionMetadata, ReportExportFormat, ReportParameterValues, ReportResult } from "./report-types"

/**
 * State for a report page: loads the report's definition whenever the key changes, fills the
 * form from the URL query or the report's defaults, and runs or exports the report. Running
 * writes the parameters back to the query so the page can be bookmarked; opening a URL that
 * carries parameters, or a report that has none, runs it straight away.
 */
export function useReport(key: MaybeRefOrGetter<string>) {
    const route = useRoute()
    const router = useRouter()

    const definition = ref<ReportDefinitionMetadata | null>(null)
    const values = ref<ReportParameterValues>({})
    const result = ref<ReportResult | null>(null)
    const errors = ref<string[]>([])
    const loading = ref(true)
    const notFound = ref(false)
    const running = ref(false)
    const exporting = ref<ReportExportFormat | null>(null)
    // The body of the run on screen, so an export matches what the user sees even after the
    // form has been edited.
    let shownBody: ReportParameterValues = {}

    // Only the latest load may update the page, if the key changes while one is in flight.
    let loadId = 0

    async function run(): Promise<void> {
        const report = definition.value
        if (report === null) {
            return
        }
        running.value = true
        errors.value = []
        await router.replace({ query: toQuery(report.parameters, values.value) })
        const body = toRequestBody(report.parameters, values.value)
        const outcome = await reportService.run(report.key, body)
        result.value = outcome.result
        shownBody = body
        errors.value = outcome.errors
        running.value = false
    }

    /** Exports the report on screen; does nothing until a run has produced one. */
    async function exportAs(format: ReportExportFormat): Promise<void> {
        const report = definition.value
        if (report === null || result.value === null) {
            return
        }
        exporting.value = format
        await reportService.exportReport(report.key, format, shownBody)
        exporting.value = null
    }

    async function load(reportKey: string): Promise<void> {
        loadId += 1
        const id = loadId
        loading.value = true
        result.value = null
        errors.value = []
        const report = await reportService.getDefinition(reportKey)
        if (id !== loadId) {
            return
        }
        definition.value = report
        notFound.value = report === null
        values.value = report ? initialValues(report.parameters, route.query) : {}
        loading.value = false
        if (report && (report.parameters.length === 0 || hasQueryParameters(report.parameters, route.query))) {
            await run()
        }
    }

    watch(() => toValue(key), load, { immediate: true })

    return { definition, values, result, errors, loading, notFound, running, exporting, run, exportAs }
}
