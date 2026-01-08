import { useFetch } from "@/composables/ViperFetch"
import type { SessionTypeDto, CreateSessionTypeRequest, UpdateSessionTypeRequest } from "../types"

const { get, post, put, del } = useFetch()

/**
 * Service for Session Type management API calls.
 */
class SessionTypeService {
    private baseUrl = `${import.meta.env.VITE_API_URL}effort/session-types`

    /**
     * Get all session types with usage counts.
     */
    async getSessionTypes(activeOnly?: boolean): Promise<SessionTypeDto[]> {
        const params = new URLSearchParams()
        if (activeOnly !== undefined) {
            params.append("activeOnly", String(activeOnly))
        }
        const url = params.toString() ? `${this.baseUrl}?${params.toString()}` : this.baseUrl
        const response = await get(url)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as SessionTypeDto[]
    }

    /**
     * Get a specific session type by ID.
     */
    async getSessionType(id: string): Promise<SessionTypeDto | null> {
        const response = await get(`${this.baseUrl}/${encodeURIComponent(id)}`)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as SessionTypeDto
    }

    /**
     * Create a new session type.
     */
    async createSessionType(
        request: CreateSessionTypeRequest,
    ): Promise<{ success: boolean; sessionType?: SessionTypeDto; error?: string }> {
        const response = await post(this.baseUrl, request)
        if (!response.success) {
            return {
                success: false,
                error: response.errors?.[0] ?? "Failed to create session type",
            }
        }
        return { success: true, sessionType: response.result as SessionTypeDto }
    }

    /**
     * Update an existing session type.
     */
    async updateSessionType(
        id: string,
        request: UpdateSessionTypeRequest,
    ): Promise<{ success: boolean; sessionType?: SessionTypeDto; error?: string }> {
        const response = await put(`${this.baseUrl}/${encodeURIComponent(id)}`, request)
        if (!response.success) {
            return {
                success: false,
                error: response.errors?.[0] ?? "Failed to update session type",
            }
        }
        return { success: true, sessionType: response.result as SessionTypeDto }
    }

    /**
     * Delete a session type. Only succeeds if no records reference it.
     */
    async deleteSessionType(id: string): Promise<boolean> {
        const response = await del(`${this.baseUrl}/${encodeURIComponent(id)}`)
        return response.success
    }
}

const sessionTypeService = new SessionTypeService()
export { sessionTypeService }
