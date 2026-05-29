import { useFetch } from "@/composables/ViperFetch"
import type { UnitDto, CreateUnitRequest, UpdateUnitRequest } from "../types"

const { get, post, put, del } = useFetch()

/**
 * Service for Unit management API calls.
 */
class UnitService {
    private baseUrl = `${import.meta.env.VITE_API_URL}effort/units`

    /**
     * Get all units with usage counts.
     */
    async getUnits(activeOnly?: boolean): Promise<UnitDto[]> {
        const params = new URLSearchParams()
        if (activeOnly !== undefined) {
            params.append("activeOnly", String(activeOnly))
        }
        const url = params.toString() ? `${this.baseUrl}?${params.toString()}` : this.baseUrl
        const response = await get(url)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as UnitDto[]
    }

    /**
     * Get a specific unit by ID.
     */
    async getUnit(id: number): Promise<UnitDto | null> {
        const response = await get(`${this.baseUrl}/${id}`)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as UnitDto
    }

    /**
     * Create a new unit.
     */
    async createUnit(request: CreateUnitRequest): Promise<{ success: boolean; unit?: UnitDto; error?: string }> {
        const response = await post(this.baseUrl, request)
        if (!response.success) {
            return {
                success: false,
                error: response.errors?.[0] ?? "Failed to create unit",
            }
        }
        return { success: true, unit: response.result as UnitDto }
    }

    /**
     * Update an existing unit.
     */
    async updateUnit(
        id: number,
        request: UpdateUnitRequest,
    ): Promise<{ success: boolean; unit?: UnitDto; error?: string }> {
        const response = await put(`${this.baseUrl}/${id}`, request)
        if (!response.success) {
            return {
                success: false,
                error: response.errors?.[0] ?? "Failed to update unit",
            }
        }
        return { success: true, unit: response.result as UnitDto }
    }

    /**
     * Delete a unit. Only succeeds if no percentages reference it.
     */
    async deleteUnit(id: number): Promise<boolean> {
        const response = await del(`${this.baseUrl}/${id}`)
        return response.success
    }

    /**
     * Check if a unit can be deleted and get usage count.
     */
    async canDeleteUnit(id: number): Promise<{ canDelete: boolean; usageCount: number }> {
        const response = await get(`${this.baseUrl}/${id}/can-delete`)
        if (!response.success || !response.result) {
            return { canDelete: false, usageCount: 0 }
        }
        return response.result as { canDelete: boolean; usageCount: number }
    }
}

const unitService = new UnitService()
export { unitService }
