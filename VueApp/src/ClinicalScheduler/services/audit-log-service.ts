import { useFetch } from "../../composables/ViperFetch"
import type { ApiResult } from "../types/api"
import type { AuditLogEntry, AuditModifier, AuditTerm } from "../types/audit-types"

interface AuditLogFilters {
    year?: number | null
    rotationId?: number | null
    termCode?: number | null
    person?: string | null
    modifiedBy?: string | null
    area?: string | null
    from?: string | null
    to?: string | null
}

class AuditLogService {
    private static readonly BASE_URL = `${import.meta.env.VITE_API_URL}clinicalscheduler/audit`

    private static buildUrl(baseUrl: string, params: Record<string, string | number | null | undefined>): string {
        const search = new URLSearchParams()
        for (const [key, value] of Object.entries(params)) {
            if (value !== undefined && value !== null && `${value}`.length > 0) {
                search.set(key, value.toString())
            }
        }
        const queryString = search.toString()
        return queryString ? `${baseUrl}?${queryString}` : baseUrl
    }

    /**
     * Get the filtered audit log. An empty/omitted year defaults to the current grad year server-side.
     */
    static async getAuditLog(filters: AuditLogFilters = {}): Promise<ApiResult<AuditLogEntry[]>> {
        try {
            const url = AuditLogService.buildUrl(AuditLogService.BASE_URL, { ...filters })
            const { get } = useFetch()
            return await get(url)
        } catch (error) {
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : "Unknown error occurred"],
            }
        }
    }

    /**
     * Get the distinct users who have made audited changes, for the "Modified By" filter.
     */
    static async getModifiers(): Promise<ApiResult<AuditModifier[]>> {
        try {
            const { get } = useFetch()
            return await get(`${AuditLogService.BASE_URL}/modifiers`)
        } catch (error) {
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : "Unknown error occurred"],
            }
        }
    }

    /**
     * Get the terms (semesters) within a grad year, for the "Term" filter.
     */
    static async getTerms(year?: number): Promise<ApiResult<AuditTerm[]>> {
        try {
            const url = AuditLogService.buildUrl(`${AuditLogService.BASE_URL}/terms`, { year })
            const { get } = useFetch()
            return await get(url)
        } catch (error) {
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : "Unknown error occurred"],
            }
        }
    }

    /**
     * Get the distinct affected students/clinicians in the audit trail, for the "Person" filter.
     */
    static async getPersons(): Promise<ApiResult<AuditModifier[]>> {
        try {
            const { get } = useFetch()
            return await get(`${AuditLogService.BASE_URL}/persons`)
        } catch (error) {
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : "Unknown error occurred"],
            }
        }
    }

    /**
     * Get the change history for a single rotation + week (Schedule-by-Rotation inline history).
     */
    static async getRotationWeekHistory(rotationId: number, weekId: number): Promise<ApiResult<AuditLogEntry[]>> {
        try {
            const url = AuditLogService.buildUrl(`${AuditLogService.BASE_URL}/rotation-week`, { rotationId, weekId })
            const { get } = useFetch()
            return await get(url)
        } catch (error) {
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : "Unknown error occurred"],
            }
        }
    }

    /**
     * Get the change history for a single clinician + week (Schedule-by-Clinician inline history).
     */
    static async getClinicianWeekHistory(mothraId: string, weekId: number): Promise<ApiResult<AuditLogEntry[]>> {
        try {
            const url = AuditLogService.buildUrl(`${AuditLogService.BASE_URL}/clinician-week`, { mothraId, weekId })
            const { get } = useFetch()
            return await get(url)
        } catch (error) {
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : "Unknown error occurred"],
            }
        }
    }
}

export { AuditLogService }
export type { AuditLogFilters }
