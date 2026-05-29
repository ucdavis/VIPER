import { useFetch, HTTP_STATUS } from "@/composables/ViperFetch"
import { useErrorStore } from "@/store/ErrorStore"
import type {
    AvailableCoursesDto,
    EffortTypeOptionDto,
    RoleOptionDto,
    CreateEffortRecordRequest,
    UpdateEffortRecordRequest,
    EffortRecordResult,
} from "../types"

const { get, post, put, del } = useFetch()

class RecordService {
    private baseUrl = `${import.meta.env.VITE_API_URL}effort`

    /**
     * Get available courses for creating effort records.
     * Returns courses grouped by those with existing effort and all available courses.
     */
    async getAvailableCourses(personId: number, termCode: number): Promise<AvailableCoursesDto> {
        const response = await get(
            `${this.baseUrl}/records/available-courses?personId=${personId}&termCode=${termCode}`,
        )
        if (!response.success || !response.result) {
            return { existingCourses: [], allCourses: [] }
        }
        return response.result as AvailableCoursesDto
    }

    /**
     * Get active effort types for dropdown.
     */
    async getEffortTypeOptions(): Promise<EffortTypeOptionDto[]> {
        const response = await get(`${this.baseUrl}/records/effort-types`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as EffortTypeOptionDto[]
    }

    /**
     * Get active roles for dropdown.
     */
    async getRoleOptions(): Promise<RoleOptionDto[]> {
        const response = await get(`${this.baseUrl}/records/roles`)
        if (!response.success || !Array.isArray(response.result)) {
            return []
        }
        return response.result as RoleOptionDto[]
    }

    /**
     * Check if the current user can edit effort for the given term.
     */
    async canEditTerm(termCode: number): Promise<boolean> {
        const response = await get(`${this.baseUrl}/records/can-edit-term?termCode=${termCode}`)
        if (!response.success) {
            return false
        }
        return response.result as boolean
    }

    /**
     * Create a new effort record.
     */
    async createEffortRecord(
        request: CreateEffortRecordRequest,
    ): Promise<{ success: boolean; result?: EffortRecordResult; error?: string }> {
        const response = await post(`${this.baseUrl}/records`, request)
        if (!response.success) {
            // Clear global error - we handle errors in the dialog
            useErrorStore().clearError()
            return {
                success: false,
                error: response.errors?.[0] ?? "Failed to create effort record",
            }
        }
        return { success: true, result: response.result as EffortRecordResult }
    }

    /**
     * Update an existing effort record.
     */
    async updateEffortRecord(
        recordId: number,
        request: UpdateEffortRecordRequest,
    ): Promise<{ success: boolean; result?: EffortRecordResult; error?: string; isConflict?: boolean }> {
        const response = await put(`${this.baseUrl}/records/${recordId}`, request)
        if (!response.success) {
            // Clear global error - we handle errors in the dialog
            useErrorStore().clearError()
            return {
                success: false,
                error: response.errors?.[0] ?? "Failed to update effort record",
                isConflict: response.status === HTTP_STATUS.CONFLICT,
            }
        }
        return { success: true, result: response.result as EffortRecordResult }
    }

    /**
     * Delete an effort record.
     * @param recordId The record ID to delete
     * @param originalModifiedDate The ModifiedDate from when record was loaded (for concurrency check)
     */
    async deleteEffortRecord(
        recordId: number,
        originalModifiedDate?: string | null,
    ): Promise<{ success: boolean; error?: string; isConflict?: boolean }> {
        const params = new URLSearchParams()
        if (originalModifiedDate !== null && originalModifiedDate !== undefined) {
            params.append("originalModifiedDate", originalModifiedDate)
        }
        const url = params.toString()
            ? `${this.baseUrl}/records/${recordId}?${params.toString()}`
            : `${this.baseUrl}/records/${recordId}`
        const response = await del(url)
        if (!response.success) {
            // Clear global error - we handle errors in the component
            useErrorStore().clearError()
            return {
                success: false,
                error: response.errors?.[0] ?? "Failed to delete effort record",
                isConflict: response.status === HTTP_STATUS.CONFLICT,
            }
        }
        return { success: true }
    }
}

const recordService = new RecordService()
export { recordService }
