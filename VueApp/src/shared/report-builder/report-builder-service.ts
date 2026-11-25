import { downloadBlob, postForBlob, useFetch } from "@/composables/ViperFetch"
import type {
    ReportCatalogItem,
    ReportExportFormat,
    ReportRunRequest,
    ReportRunResult,
    ReportViewerDefinition,
} from "./report-builder-types"

const { get, post } = useFetch()

const RUN_FAILED = "The report couldn't be run. Please try again later."

/** A finished run has a result and no errors; a refused or failed run has messages instead. */
type ReportRunOutcome = { result: ReportRunResult; errors: [] } | { result: null; errors: string[] }

/**
 * What became of a download: saved, nothing to save (the report had no rows), or failed, in
 * which case the shared error handler has already told the user why.
 */
type ReportExportOutcome = "downloaded" | "empty" | "failed"

/** Client for the Report Builder viewer API. */
class ReportBuilderService {
    private baseUrl = `${import.meta.env.VITE_API_URL}report-builder`

    /**
     * The reports the signed-in user may view; the server filters by permission. Null when the
     * request fails, so the page can tell a failed load apart from having no reports.
     */
    async getCatalog(): Promise<ReportCatalogItem[] | null> {
        const r = await get(this.baseUrl)
        return r.success && Array.isArray(r.result) ? (r.result as ReportCatalogItem[]) : null
    }

    /**
     * A report's form fields and columns. Null when the report doesn't exist, was deleted, or the
     * user may not view it; the page treats all three alike.
     */
    async getDefinition(slug: string): Promise<ReportViewerDefinition | null> {
        const r = await get(this.reportUrl(slug))
        return r.success && r.result ? (r.result as ReportViewerDefinition) : null
    }

    async run(slug: string, request: ReportRunRequest): Promise<ReportRunOutcome> {
        const r = await post(`${this.reportUrl(slug)}/run`, request)
        if (r.success && r.result) {
            return { result: r.result as ReportRunResult, errors: [] }
        }
        const messages = ((r.errors ?? []) as string[]).filter((message) => message !== "")
        return { result: null, errors: messages.length > 0 ? messages : [RUN_FAILED] }
    }

    /** Runs the report on the server and saves it in the chosen format. */
    async exportReport(
        report: Pick<ReportViewerDefinition, "slug" | "title">,
        format: ReportExportFormat,
        request: ReportRunRequest,
    ): Promise<ReportExportOutcome> {
        try {
            const { blob, filename } = await postForBlob(`${this.reportUrl(report.slug)}/export/${format}`, request)
            // The server answers 204 No Content when there are no rows to download.
            if (blob.size === 0) {
                return "empty"
            }
            downloadBlob(blob, filename ?? `${report.title}.${format}`)
            return "downloaded"
        } catch {
            return "failed"
        }
    }

    private reportUrl(slug: string): string {
        return `${this.baseUrl}/${encodeURIComponent(slug)}`
    }
}

const reportBuilderService = new ReportBuilderService()
export { reportBuilderService }
export type { ReportRunOutcome }
