import { useFetch } from "@/composables/ViperFetch"
import type { DashboardStatsDto, DepartmentVerificationDto, EffortChangeAlertDto, RecentChangeDto } from "../types"

const { get, post } = useFetch()

const DEFAULT_VERIFICATION_THRESHOLD = 80
const DEFAULT_RECENT_CHANGES_LIMIT = 10

/**
 * Service for dashboard API calls.
 */
class DashboardService {
    private baseUrl = `${import.meta.env.VITE_API_URL}effort/dashboard`

    /**
     * Get dashboard statistics for a term.
     */
    async getStats(termCode: number): Promise<DashboardStatsDto | null> {
        const response = await get(`${this.baseUrl}/${termCode}/stats`)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as DashboardStatsDto
    }

    /**
     * Get department verification breakdown.
     */
    async getDepartmentVerification(
        termCode: number,
        threshold = DEFAULT_VERIFICATION_THRESHOLD,
    ): Promise<DepartmentVerificationDto[]> {
        const response = await get(`${this.baseUrl}/${termCode}/departments?threshold=${threshold}`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as DepartmentVerificationDto[]
    }

    /**
     * Get all data hygiene alerts.
     */
    async getDataHygieneAlerts(termCode: number): Promise<EffortChangeAlertDto[]> {
        const response = await get(`${this.baseUrl}/${termCode}/hygiene`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as EffortChangeAlertDto[]
    }

    /**
     * Get all alerts (combined effort changes + hygiene).
     */
    async getAllAlerts(termCode: number, includeIgnored = false): Promise<EffortChangeAlertDto[]> {
        const url = `${this.baseUrl}/${termCode}/alerts?includeIgnored=${includeIgnored}`
        const response = await get(url)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as EffortChangeAlertDto[]
    }

    /**
     * Ignore an alert (mark as not requiring action).
     */
    async ignoreAlert(termCode: number, alertType: string, entityId: string): Promise<boolean> {
        const response = await post(`${this.baseUrl}/${termCode}/alerts/ignore`, { alertType, entityId })
        return response.success
    }

    /**
     * Get recent changes for a term from the audit log.
     */
    async getRecentChanges(termCode: number, limit = DEFAULT_RECENT_CHANGES_LIMIT): Promise<RecentChangeDto[]> {
        const response = await get(`${this.baseUrl}/${termCode}/recent-changes?limit=${limit}`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as RecentChangeDto[]
    }
}

const dashboardService = new DashboardService()
export { dashboardService }
