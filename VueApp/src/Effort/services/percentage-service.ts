import { useFetch } from "@/composables/ViperFetch"
import type {
    PercentageDto,
    CreatePercentageRequest,
    UpdatePercentageRequest,
    PercentageValidationResult,
    AveragePercentByTypeDto,
} from "../types"

const { get, post, put, del } = useFetch()

/**
 * Service for Percentage assignment API calls.
 */
class PercentageService {
    private baseUrl = `${import.meta.env.VITE_API_URL}effort/percentages`

    /**
     * Get all percentages for a person.
     */
    async getPercentagesForPerson(personId: number): Promise<PercentageDto[]> {
        const response = await get(`${this.baseUrl}/person/${personId}`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as PercentageDto[]
    }

    /**
     * Get a single percentage by ID.
     */
    async getPercentage(id: number): Promise<PercentageDto | null> {
        const response = await get(`${this.baseUrl}/${id}`)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as PercentageDto
    }

    /**
     * Create a new percentage assignment.
     * Returns the created percentage and any warnings.
     */
    async createPercentage(request: CreatePercentageRequest): Promise<{
        result: PercentageDto
        warnings: string[]
    }> {
        const response = await post(this.baseUrl, request)
        if (!response.success) {
            throw new Error(response.errors?.[0] ?? "Failed to create percentage")
        }
        // The API returns { result: PercentageDto, warnings: string[] }
        const data = response.result as { result: PercentageDto; warnings: string[] }
        return {
            result: data.result ?? (data as unknown as PercentageDto),
            warnings: data.warnings ?? [],
        }
    }

    /**
     * Validate a percentage assignment without creating it.
     */
    async validatePercentage(request: CreatePercentageRequest): Promise<PercentageValidationResult> {
        const response = await post(`${this.baseUrl}/validate`, request)
        if (!response.success || !response.result) {
            return {
                isValid: false,
                errors: response.errors ?? ["Validation failed"],
                warnings: [],
                totalActivePercent: 0,
            }
        }
        return response.result as PercentageValidationResult
    }

    /**
     * Update an existing percentage assignment.
     * Returns the updated percentage and any warnings, or error if failed.
     */
    async updatePercentage(
        id: number,
        request: UpdatePercentageRequest,
    ): Promise<{
        success: boolean
        result?: PercentageDto
        warnings: string[]
        error?: string
    }> {
        const response = await put(`${this.baseUrl}/${id}`, request)
        if (!response.success) {
            return {
                success: false,
                warnings: [],
                error: response.errors?.[0] ?? "Failed to update percentage assignment",
            }
        }
        // The API returns { result: PercentageDto, warnings: string[] }
        const data = response.result as { result: PercentageDto; warnings: string[] }
        return {
            success: true,
            result: data.result ?? (data as unknown as PercentageDto),
            warnings: data.warnings ?? [],
        }
    }

    /**
     * Delete a percentage assignment.
     */
    async deletePercentage(id: number): Promise<boolean> {
        const response = await del(`${this.baseUrl}/${id}`)
        return response.success
    }

    /**
     * Get averaged percentages by type for a person within a date range.
     * Returns a record keyed by typeClass with arrays of averaged percentages.
     */
    async getAveragesByType(
        personId: number,
        start: string,
        end: string,
    ): Promise<Record<string, AveragePercentByTypeDto[]>> {
        const params = new URLSearchParams({ start, end })
        const response = await get(`${this.baseUrl}/person/${personId}/averages?${params.toString()}`)
        if (!response.success || !response.result) {
            return {}
        }
        return response.result as Record<string, AveragePercentByTypeDto[]>
    }
}

const percentageService = new PercentageService()
export { percentageService }
