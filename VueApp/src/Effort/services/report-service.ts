import { useFetch, postForBlob } from "@/composables/ViperFetch"
import type {
    TeachingActivityReport,
    ReportFilterParams,
    DeptSummaryReport,
    SchoolSummaryReport,
    MeritDetailReport,
    MeritAverageReport,
    MeritSummaryReport,
    ClinicalEffortReport,
    ScheduledCliWeeksReport,
    ZeroEffortReport,
    EvalSummaryReport,
    EvalDetailReport,
    YearStatisticsReport,
    MultiYearReport,
    SabbaticalDto,
} from "../types"

const { get, put } = useFetch()

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
     * Get the department summary report.
     */
    async getDeptSummary(params: ReportFilterParams): Promise<DeptSummaryReport | null> {
        const url = this.buildUrl("teaching/dept-summary", params)
        const response = await get(url)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as DeptSummaryReport
    }

    /**
     * Get the school summary report.
     */
    async getSchoolSummary(params: ReportFilterParams): Promise<SchoolSummaryReport | null> {
        const url = this.buildUrl("teaching/school-summary", params)
        const response = await get(url)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as SchoolSummaryReport
    }

    /**
     * Get the merit detail report.
     */
    async getMeritDetail(params: ReportFilterParams): Promise<MeritDetailReport | null> {
        const url = this.buildUrl("merit/detail", params)
        const response = await get(url)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as MeritDetailReport
    }

    /**
     * Get the merit average report.
     */
    async getMeritAverage(params: ReportFilterParams): Promise<MeritAverageReport | null> {
        const url = this.buildUrl("merit/average", params)
        const response = await get(url)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as MeritAverageReport
    }

    /**
     * Get the merit summary report.
     */
    async getMeritSummary(params: ReportFilterParams): Promise<MeritSummaryReport | null> {
        const url = this.buildUrl("merit/summary", params)
        const response = await get(url)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as MeritSummaryReport
    }

    /**
     * Get the clinical effort report.
     */
    async getClinicalEffort(academicYear: string, clinicalType: number): Promise<ClinicalEffortReport | null> {
        const searchParams = new URLSearchParams()
        searchParams.set("academicYear", academicYear)
        searchParams.set("clinicalType", clinicalType.toString())
        const url = `${this.baseUrl}/merit/clinical?${searchParams.toString()}`
        const response = await get(url)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as ClinicalEffortReport
    }

    /**
     * Get the scheduled CLI weeks report.
     */
    async getScheduledCliWeeks(params: ReportFilterParams): Promise<ScheduledCliWeeksReport | null> {
        const url = this.buildUrl("clinical-schedule", params)
        const response = await get(url)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as ScheduledCliWeeksReport
    }

    /**
     * Get the zero effort report.
     */
    async getZeroEffort(params: ReportFilterParams): Promise<ZeroEffortReport | null> {
        const url = this.buildUrl("zero-effort", params)
        const response = await get(url)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as ZeroEffortReport
    }

    /**
     * Get the evaluation summary report.
     */
    async getEvalSummary(params: ReportFilterParams): Promise<EvalSummaryReport | null> {
        const url = this.buildUrl("eval/summary", params)
        const response = await get(url)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as EvalSummaryReport
    }

    /**
     * Get the evaluation detail report.
     */
    async getEvalDetail(params: ReportFilterParams): Promise<EvalDetailReport | null> {
        const url = this.buildUrl("eval/detail", params)
        const response = await get(url)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as EvalDetailReport
    }

    /**
     * Get the year statistics report ("Lairmore Report").
     */
    async getYearStatistics(academicYear: string): Promise<YearStatisticsReport | null> {
        const searchParams = new URLSearchParams()
        searchParams.set("academicYear", academicYear)
        const url = `${this.baseUrl}/year-stats?${searchParams.toString()}`
        const response = await get(url)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as YearStatisticsReport
    }

    /**
     * Get the multi-year merit + evaluation report for a single instructor.
     */
    async getMultiYear(params: {
        personId: number
        startYear: number
        endYear: number
        excludeClinTerms?: string
        excludeDidTerms?: string
        useAcademicYear?: boolean
    }): Promise<MultiYearReport | null> {
        const searchParams = new URLSearchParams()
        searchParams.set("personId", params.personId.toString())
        searchParams.set("startYear", params.startYear.toString())
        searchParams.set("endYear", params.endYear.toString())
        if (params.excludeClinTerms) {
            searchParams.set("excludeClinTerms", params.excludeClinTerms)
        }
        if (params.excludeDidTerms) {
            searchParams.set("excludeDidTerms", params.excludeDidTerms)
        }
        if (params.useAcademicYear) {
            searchParams.set("useAcademicYear", "true")
        }
        const url = `${this.baseUrl}/merit/multiyear?${searchParams.toString()}`
        const response = await get(url)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as MultiYearReport
    }

    /**
     * Get the min/max calendar years for an instructor's effort data.
     * Used to populate year range dropdowns matching legacy behavior.
     */
    async getMultiYearRange(personId: number): Promise<{ minYear: number; maxYear: number } | null> {
        const url = `${this.baseUrl}/merit/multiyear/year-range?personId=${personId}`
        const response = await get(url)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as { minYear: number; maxYear: number }
    }

    /**
     * Export multi-year report as PDF. Returns false if no data.
     */
    async openMultiYearPdf(params: {
        personId: number
        startYear: number
        endYear: number
        excludeClinTerms?: string
        excludeDidTerms?: string
        useAcademicYear?: boolean
    }): Promise<boolean> {
        const body = {
            personId: params.personId,
            startYear: params.startYear,
            endYear: params.endYear,
            excludeClinicalTerms: params.excludeClinTerms || null,
            excludeDidacticTerms: params.excludeDidTerms || null,
            useAcademicYear: params.useAcademicYear || false,
        }
        const { blob } = await postForBlob(`${this.baseUrl}/merit/multiyear/pdf`, body)
        if (blob.size === 0) {
            return false
        }
        const url = globalThis.URL.createObjectURL(blob)
        globalThis.open(url, "_blank")
        return true
    }

    /**
     * Open a report PDF in a new tab. Returns false if the report has no data.
     */
    async openPdf(endpoint: string, params: ReportFilterParams): Promise<boolean> {
        const { blob } = await postForBlob(`${this.baseUrl}/${endpoint}`, params)
        if (blob.size === 0) {
            return false
        }
        const url = globalThis.URL.createObjectURL(blob)
        globalThis.open(url, "_blank")
        return true
    }

    private buildUrl(endpoint: string, params: ReportFilterParams): string {
        const searchParams = new URLSearchParams()
        if (params.academicYear) {
            searchParams.set("academicYear", params.academicYear)
        } else if (params.termCode) {
            searchParams.set("termCode", params.termCode.toString())
        }
        if (params.department) {
            searchParams.set("department", params.department)
        }
        if (params.personId) {
            searchParams.set("personId", params.personId.toString())
        }
        if (params.role) {
            searchParams.set("role", params.role)
        }
        if (params.title) {
            searchParams.set("title", params.title)
        }
        return `${this.baseUrl}/${endpoint}?${searchParams.toString()}`
    }

    // ============================================
    // Sabbatical/Leave Exclusion
    // ============================================

    /**
     * Get sabbatical/leave exclusion data for a person.
     */
    async getSabbatical(personId: number): Promise<SabbaticalDto | null> {
        const url = `${this.baseUrl}/sabbaticals/${personId}`
        const response = await get(url)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as SabbaticalDto
    }

    /**
     * Save sabbatical/leave exclusion data (admin only).
     */
    async saveSabbatical(
        personId: number,
        excludeClinicalTerms: string | null,
        excludeDidacticTerms: string | null,
    ): Promise<SabbaticalDto | null> {
        const url = `${this.baseUrl}/sabbaticals/${personId}`
        const response = await put(url, {
            excludeClinicalTerms,
            excludeDidacticTerms,
        })
        if (!response.success || !response.result) {
            return null
        }
        return response.result as SabbaticalDto
    }
}

const reportService = new ReportService()
export { reportService }
