import { useFetch } from "@/composables/ViperFetch"
import type { PercentAssignTypeDto, InstructorsByPercentAssignTypeResponseDto } from "../types"

const { get } = useFetch()

/**
 * Service for Percent Assignment Type operations (read-only).
 */
class PercentAssignTypeService {
    private baseUrl = `${import.meta.env.VITE_API_URL}effort/percent-assign-types`

    /**
     * Get all percent assignment types, optionally filtered by active status.
     */
    async getPercentAssignTypes(activeOnly?: boolean): Promise<PercentAssignTypeDto[]> {
        const params = new URLSearchParams()
        if (activeOnly !== undefined) {
            params.append("activeOnly", String(activeOnly))
        }
        const url = params.toString() ? `${this.baseUrl}?${params.toString()}` : this.baseUrl
        const response = await get(url)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as PercentAssignTypeDto[]
    }

    /**
     * Get all instructors who have a specific percent assignment type assigned.
     */
    async getInstructorsByPercentAssignType(typeId: number): Promise<InstructorsByPercentAssignTypeResponseDto | null> {
        const response = await get(`${this.baseUrl}/${typeId}/instructors`)
        if (!response.success || !response.result) {
            return null
        }
        return response.result as InstructorsByPercentAssignTypeResponseDto
    }
}

const percentAssignTypeService = new PercentAssignTypeService()
export { percentAssignTypeService }
