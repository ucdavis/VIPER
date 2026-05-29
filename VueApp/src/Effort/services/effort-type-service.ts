import { useFetch } from "@/composables/ViperFetch"
import type { EffortTypeDto, CreateEffortTypeRequest, UpdateEffortTypeRequest } from "../types"

const { get, post, put, del } = useFetch()

/**
 * Service for Effort Type management API calls.
 */
class EffortTypeService {
    private baseUrl = `${import.meta.env.VITE_API_URL}effort/effort-types`

    /**
     * Get all effort types with usage counts.
     */
    async getEffortTypes(activeOnly?: boolean): Promise<EffortTypeDto[]> {
        const params = new URLSearchParams()
        if (activeOnly !== undefined) {
            params.append("activeOnly", String(activeOnly))
        }
        const url = params.toString() ? `${this.baseUrl}?${params.toString()}` : this.baseUrl
        const response = await get(url)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as EffortTypeDto[]
    }

    /**
     * Get a specific effort type by ID.
     * Uses query parameter because IDs can contain "/" (e.g., "D/L", "L/D").
     */
    async getEffortType(id: string): Promise<EffortTypeDto | null> {
        const response = await get(`${this.baseUrl}/by-id?id=${encodeURIComponent(id)}`)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as EffortTypeDto
    }

    /**
     * Create a new effort type.
     */
    async createEffortType(
        request: CreateEffortTypeRequest,
    ): Promise<{ success: boolean; effortType?: EffortTypeDto; error?: string }> {
        const response = await post(this.baseUrl, request)
        if (!response.success) {
            return {
                success: false,
                error: response.errors?.[0] ?? "Failed to create effort type",
            }
        }
        return { success: true, effortType: response.result as EffortTypeDto }
    }

    /**
     * Update an existing effort type.
     * Uses query parameter because IDs can contain "/" (e.g., "D/L", "L/D").
     */
    async updateEffortType(
        id: string,
        request: UpdateEffortTypeRequest,
    ): Promise<{ success: boolean; effortType?: EffortTypeDto; error?: string }> {
        const response = await put(`${this.baseUrl}/by-id?id=${encodeURIComponent(id)}`, request)
        if (!response.success) {
            return {
                success: false,
                error: response.errors?.[0] ?? "Failed to update effort type",
            }
        }
        return { success: true, effortType: response.result as EffortTypeDto }
    }

    /**
     * Delete an effort type. Only succeeds if no records reference it.
     * Uses query parameter because IDs can contain "/" (e.g., "D/L", "L/D").
     */
    async deleteEffortType(id: string): Promise<boolean> {
        const response = await del(`${this.baseUrl}/by-id?id=${encodeURIComponent(id)}`)
        return response.success
    }
}

const effortTypeService = new EffortTypeService()
export { effortTypeService }
