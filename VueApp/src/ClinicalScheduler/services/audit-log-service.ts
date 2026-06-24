import { useFetch } from "../../composables/ViperFetch"
import type { ApiResult } from "../types/api"
import type { AuditLogEntry, AuditModifier } from "../types/audit-types"

interface AuditLogFilters {
    year?: number
    rotationId?: number
    person?: string
    modifiedBy?: string
    area?: string
    from?: string
    to?: string
}

class AuditLogService {
    private static readonly BASE_URL = `${import.meta.env.VITE_API_URL}clinicalscheduler/audit`

    private static buildUrl(baseUrl: string, params: Record<string, string | number | undefined>): string {
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
}

export { AuditLogService }
export type { AuditLogFilters }
