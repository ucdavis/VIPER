import { downloadBlob, postForBlob, useFetch } from "@/composables/ViperFetch"
import type {
    ReportCatalogItem,
    ReportDefinitionMetadata,
    ReportExportFormat,
    ReportParameterValues,
    ReportResult,
} from "./report-types"

const { get, post } = useFetch()

const RUN_FAILED = "The report could not be run."

/**
 * A finished run has a result and no errors; a refused or failed run has messages instead.
 * Validation messages name the parameter they concern, so they read well as a list.
 */
type ReportRunOutcome = { result: ReportResult; errors: [] } | { result: null; errors: string[] }

/**
 * Client for the site-wide reports API.
 */
class ReportService {
    private baseUrl = `${import.meta.env.VITE_API_URL}reports`

    /**
     * The reports in one area that the signed-in user may run. The server filters by permission,
     * so the list needs no checks here. Returns null when the request fails, so the caller can
     * tell a failed load apart from an area with no reports for this user.
     */
    async getCatalog(area: string): Promise<ReportCatalogItem[] | null> {
        const r = await get(`${this.baseUrl}?area=${encodeURIComponent(area)}`)
        if (!r.success || !Array.isArray(r.result)) {
            return null
        }
        return r.result as ReportCatalogItem[]
    }

    /**
     * A report's parameters and columns. Null when the report doesn't exist, is only planned,
     * or the user may not run it; the API doesn't say which, and the page treats all three alike.
     */
    async getDefinition(key: string): Promise<ReportDefinitionMetadata | null> {
        const r = await get(this.reportUrl(key))
        if (!r.success || !r.result) {
            return null
        }
        return r.result as ReportDefinitionMetadata
    }

    async run(key: string, parameters: ReportParameterValues): Promise<ReportRunOutcome> {
        const r = await post(`${this.reportUrl(key)}/run`, parameters)
        if (r.success && r.result) {
            return { result: r.result as ReportResult, errors: [] }
        }
        const messages = (r.errors as string[]).filter((message) => message !== "")
        return { result: null, errors: messages.length > 0 ? messages : [RUN_FAILED] }
    }

    /**
     * Runs the report on the server and downloads it in the chosen format. Returns false if the
     * export failed; the shared error handler has already told the user why.
     */
    async exportReport(key: string, format: ReportExportFormat, parameters: ReportParameterValues): Promise<boolean> {
        try {
            const { blob, filename } = await postForBlob(`${this.reportUrl(key)}/export/${format}`, parameters)
            downloadBlob(blob, filename ?? `${key}.${format}`)
            return true
        } catch {
            return false
        }
    }

    private reportUrl(key: string): string {
        return `${this.baseUrl}/${encodeURIComponent(key)}`
    }
}

const reportService = new ReportService()
export { reportService }
export type { ReportRunOutcome }
