import { useFetch } from "@/composables/ViperFetch"
import type { ReportRunOutcome } from "@/shared/report-builder/report-builder-service"
import type { ReportRunRequest, ReportRunResult } from "@/shared/report-builder/report-builder-types"
import type {
    ReportAdminDefinition,
    ReportAdminListItem,
    ReportCheckResult,
    ReportEditRequest,
    ReportFailure,
    ReportHistoryItem,
    ReportHistoryVersion,
    ReportPermissionOption,
    ReportSourceDetail,
    ReportSourceSummary,
} from "./report-admin-types"

const { get, post, put, del } = useFetch()

const HTTP_CONFLICT = 409
const SAVE_FAILED = "The report couldn't be saved. Please try again later."
const PREVIEW_FAILED = "The preview couldn't be run. Please try again later."

/**
 * A save's result: the saved report, or the server's messages. `conflict` is true when someone
 * else saved the report after this editor loaded it.
 */
type ReportSaveOutcome =
    | { definition: ReportAdminDefinition; errors: []; conflict: false }
    | { definition: null; errors: string[]; conflict: boolean }

function messages(errors: unknown, fallback: string): string[] {
    const list = (Array.isArray(errors) ? errors : []).filter(
        (message): message is string => typeof message === "string" && message !== "",
    )
    return list.length > 0 ? list : [fallback]
}

/** Client for the Report Builder admin API. Reads return null when the request fails. */
class ReportAdminService {
    private baseUrl = `${import.meta.env.VITE_API_URL}report-builder/admin`

    async getSources(): Promise<ReportSourceSummary[] | null> {
        const r = await get(`${this.baseUrl}/sources`)
        return r.success && Array.isArray(r.result) ? (r.result as ReportSourceSummary[]) : null
    }

    async getSource(name: string): Promise<ReportSourceDetail | null> {
        const r = await get(`${this.baseUrl}/sources/${encodeURIComponent(name)}`)
        return r.success && r.result ? (r.result as ReportSourceDetail) : null
    }

    /** RAPS permissions whose name contains the text; none when the request fails. */
    async searchPermissions(text: string): Promise<ReportPermissionOption[]> {
        const r = await get(`${this.baseUrl}/permissions?search=${encodeURIComponent(text)}`)
        return r.success && Array.isArray(r.result) ? (r.result as ReportPermissionOption[]) : []
    }

    async list(includeDeleted: boolean): Promise<ReportAdminListItem[] | null> {
        const r = await get(`${this.baseUrl}/definitions?includeDeleted=${includeDeleted}`)
        return r.success && Array.isArray(r.result) ? (r.result as ReportAdminListItem[]) : null
    }

    async get(id: number): Promise<ReportAdminDefinition | null> {
        const r = await get(this.definitionUrl(id))
        return r.success && r.result ? (r.result as ReportAdminDefinition) : null
    }

    /** Creates the report when `id` is null, otherwise saves over it. */
    async save(id: number | null, report: ReportEditRequest): Promise<ReportSaveOutcome> {
        const r =
            id === null ? await post(`${this.baseUrl}/definitions`, report) : await put(this.definitionUrl(id), report)
        if (r.success && r.result) {
            return { definition: r.result as ReportAdminDefinition, errors: [], conflict: false }
        }
        return { definition: null, errors: messages(r.errors, SAVE_FAILED), conflict: r.status === HTTP_CONFLICT }
    }

    async remove(id: number): Promise<ReportAdminDefinition | null> {
        const r = await del(this.definitionUrl(id))
        return r.success && r.result ? (r.result as ReportAdminDefinition) : null
    }

    async restore(id: number): Promise<ReportAdminDefinition | null> {
        const r = await post(`${this.definitionUrl(id)}/restore`)
        return r.success && r.result ? (r.result as ReportAdminDefinition) : null
    }

    async getHistory(id: number): Promise<ReportHistoryItem[] | null> {
        const r = await get(`${this.definitionUrl(id)}/history`)
        return r.success && Array.isArray(r.result) ? (r.result as ReportHistoryItem[]) : null
    }

    async getVersion(id: number, historyId: number): Promise<ReportHistoryVersion | null> {
        const r = await get(`${this.definitionUrl(id)}/history/${historyId}`)
        return r.success && r.result ? (r.result as ReportHistoryVersion) : null
    }

    /** A page of the report as the form now stands, without saving it. */
    async preview(report: ReportEditRequest, run: ReportRunRequest): Promise<ReportRunOutcome> {
        const r = await post(`${this.baseUrl}/preview`, { report, run })
        if (r.success && r.result) {
            return { result: r.result as ReportRunResult, errors: [] }
        }
        return { result: null, errors: messages(r.errors, PREVIEW_FAILED) }
    }

    /** Runs that failed in the last `days` days, newest first. */
    async getFailures(days: number): Promise<ReportFailure[] | null> {
        const r = await get(`${this.baseUrl}/failures?days=${days}`)
        return r.success && Array.isArray(r.result) ? (r.result as ReportFailure[]) : null
    }

    /** Checks every live report as a save would. */
    async check(): Promise<ReportCheckResult | null> {
        const r = await get(`${this.baseUrl}/check`)
        return r.success && r.result ? (r.result as ReportCheckResult) : null
    }

    private definitionUrl(id: number): string {
        return `${this.baseUrl}/definitions/${id}`
    }
}

const reportAdminService = new ReportAdminService()
export { reportAdminService }
