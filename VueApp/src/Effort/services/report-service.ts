import { useFetch, postForBlob } from "@/composables/ViperFetch"
import type { TeachingActivityReport, ReportFilterParams } from "../types"

const { get } = useFetch()

/**
 * Service for report API calls.
 */
class ReportService {
    private baseUrl = `${import.meta.env.VITE_API_URL}effort/reports`

    /**
     * Get the grouped teaching activity report (by department, then instructor).
     */
    async getTeachingActivityGrouped(params: ReportFilterParams): Promise<TeachingActivityReport | null> {
        const url = this.buildUrl("teaching/grouped", params)
        const response = await get(url)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as TeachingActivityReport
    }

    /**
     * Get the individual teaching activity report (by instructor).
     */
    async getTeachingActivityIndividual(params: ReportFilterParams): Promise<TeachingActivityReport | null> {
        const url = this.buildUrl("teaching/individual", params)
        const response = await get(url)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as TeachingActivityReport
    }

    /**
     * Open the grouped teaching activity report as a PDF in a new tab.
     */
    async openGroupedPdf(params: ReportFilterParams): Promise<boolean> {
        const { blob } = await postForBlob(
            `${this.baseUrl}/teaching/grouped/pdf`,
            params
        )
        if (blob.size === 0) return false
        const url = globalThis.URL.createObjectURL(blob)
        globalThis.open(url, '_blank')
        return true
    }

    private buildUrl(endpoint: string, params: ReportFilterParams): string {
        const searchParams = new URLSearchParams()
        if (params.academicYear) {
            searchParams.set("academicYear", params.academicYear)
        } else if (params.termCode) {
            searchParams.set("termCode", params.termCode.toString())
        }
        if (params.department) searchParams.set("department", params.department)
        if (params.personId) searchParams.set("personId", params.personId.toString())
        if (params.role) searchParams.set("role", params.role)
        if (params.title) searchParams.set("title", params.title)
        return `${this.baseUrl}/${endpoint}?${searchParams.toString()}`
    }
}

const reportService = new ReportService()
export { reportService }
