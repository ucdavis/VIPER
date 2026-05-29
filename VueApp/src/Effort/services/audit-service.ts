import { useFetch } from "@/composables/ViperFetch"
import type { EffortAuditRow, ModifierInfo, TermOptionDto } from "../types"

const { get } = useFetch()

/**
 * Service for Effort audit API calls.
 */
class EffortAuditService {
    private baseUrl = `${import.meta.env.VITE_API_URL}effort/audit`

    /**
     * Get paginated audit entries with optional filtering.
     */
    async getAuditEntries(params: URLSearchParams): Promise<{
        result: EffortAuditRow[]
        pagination: { totalRecords: number }
    }> {
        const url = `${this.baseUrl}?${params.toString()}`
        const response = await get(url)
        return {
            result: response.success ? (response.result as EffortAuditRow[]) : [],
            pagination: response.pagination ?? { totalRecords: 0 },
        }
    }

    /**
     * Get distinct audit actions for filter dropdown.
     */
    async getActions(): Promise<string[]> {
        const response = await get(`${this.baseUrl}/actions`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as string[]
    }

    /**
     * Get users who have made audit entries for filter dropdown.
     */
    async getModifiers(): Promise<ModifierInfo[]> {
        const response = await get(`${this.baseUrl}/modifiers`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as ModifierInfo[]
    }

    /**
     * Get instructors who have audit entries for filter dropdown.
     */
    async getInstructors(): Promise<ModifierInfo[]> {
        const response = await get(`${this.baseUrl}/instructors`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as ModifierInfo[]
    }

    /**
     * Get terms that have audit entries for filter dropdown.
     */
    async getTerms(): Promise<TermOptionDto[]> {
        const response = await get(`${this.baseUrl}/terms`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as TermOptionDto[]
    }

    /**
     * Get distinct subject codes for filter dropdown.
     * Optionally filtered by term code and/or course number.
     */
    async getSubjectCodes(termCode?: number | null, courseNumber?: string | null): Promise<string[]> {
        const params = new URLSearchParams()
        if (termCode !== undefined && termCode !== null) {
            params.append("termCode", termCode.toString())
        }
        if (courseNumber) {
            params.append("courseNumber", courseNumber)
        }
        const queryString = params.toString()
        const url = queryString ? `${this.baseUrl}/subject-codes?${queryString}` : `${this.baseUrl}/subject-codes`
        const response = await get(url)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as string[]
    }

    /**
     * Get distinct course numbers for filter dropdown.
     * Optionally filtered by term code and/or subject code.
     */
    async getCourseNumbers(termCode?: number | null, subjectCode?: string | null): Promise<string[]> {
        const params = new URLSearchParams()
        if (termCode !== undefined && termCode !== null) {
            params.append("termCode", termCode.toString())
        }
        if (subjectCode) {
            params.append("subjectCode", subjectCode)
        }
        const queryString = params.toString()
        const url = queryString ? `${this.baseUrl}/course-numbers?${queryString}` : `${this.baseUrl}/course-numbers`
        const response = await get(url)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as string[]
    }
}

const effortAuditService = new EffortAuditService()
export { effortAuditService }
