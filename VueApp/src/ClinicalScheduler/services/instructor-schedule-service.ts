import { useFetch } from "../../composables/ViperFetch"
import type { ApiResult } from "../types/api"
import { SCHEDULE_OPERATION_ERRORS } from "../constants/permission-messages"

class InstructorScheduleService {
    private static readonly BASE_URL = `${import.meta.env.VITE_API_URL}clinicalscheduler/instructor-schedules`

    /**
     * Add an instructor to one or more weeks of a rotation
     */
    static async addInstructor(request: InstructorScheduleRequest): Promise<ApiResult<InstructorScheduleResponse>> {
        try {
            // Trim MothraId to prevent whitespace issues
            const trimmedRequest = { ...request, MothraId: request.MothraId.trim() }
            const { post } = useFetch()
            const response = await post(this.BASE_URL, trimmedRequest)
            return response as ApiResult<InstructorScheduleResponse>
        } catch (error) {
            // Extract meaningful error message from the response
            let errorMessage = "Failed to add instructor to schedule"
            if (error instanceof Error) {
                // Check if error has validation errors property (from ViperFetch ValidationError)
                const errorWithDetails = error as Error & { errors?: string[] }
                if (errorWithDetails.errors && Array.isArray(errorWithDetails.errors)) {
                    const validationErrors = errorWithDetails.errors
                    if (validationErrors.length > 0) {
                        errorMessage = validationErrors.join(", ")
                    }
                } else {
                    errorMessage = error.message
                }
            }

            return {
                result: { scheduleIds: [] },
                success: false,
                errors: [errorMessage],
            }
        }
    }

    /**
     * Remove an instructor from a scheduled week
     */
    static async removeInstructor(scheduleId: number): Promise<ApiResult<boolean>> {
        try {
            const { del } = useFetch()
            const response = await del(`${this.BASE_URL}/${scheduleId}`)

            // Check if the response indicates success
            if (response && response.success !== false) {
                return {
                    result: true,
                    success: true,
                    errors: [],
                }
            }
            return {
                result: false,
                success: false,
                errors: response?.errors || ["Failed to remove instructor from schedule"],
            }
        } catch (error) {
            // Try to extract meaningful error message from HTTP response
            let errorMessage = "Failed to remove instructor from schedule"
            if (error instanceof Error) {
                errorMessage = error.message

                // If it's an HTTP error, try to get more details
                if (error.message.includes("500")) {
                    errorMessage =
                        "Server error occurred. The instructor may be a primary evaluator or there may be a permission issue."
                } else if (error.message.includes("403")) {
                    errorMessage = SCHEDULE_OPERATION_ERRORS.NO_PERMISSION_REMOVE_INSTRUCTOR
                } else if (error.message.includes("404")) {
                    errorMessage = "Instructor schedule not found."
                }
            }

            return {
                result: false,
                success: false,
                errors: [errorMessage],
            }
        }
    }

    /**
     * Set or unset an instructor as the primary evaluator for a rotation/week
     */
    static async setPrimaryEvaluator(
        scheduleId: number,
        isPrimary: boolean,
    ): Promise<ApiResult<{ isPrimaryEvaluator: boolean; previousPrimaryName?: string | undefined }>> {
        try {
            const request: SetPrimaryEvaluatorRequest = { IsPrimary: isPrimary }
            const { put } = useFetch()
            const response = await put(`${this.BASE_URL}/${scheduleId}/primary`, request)

            // Check if the response indicates success
            if (response && response.success !== false) {
                return {
                    result: {
                        isPrimaryEvaluator: response.result?.isPrimaryEvaluator ?? isPrimary,
                        previousPrimaryName: response.result?.previousPrimaryName,
                    },
                    success: true,
                    errors: [],
                }
            }
            return {
                result: { isPrimaryEvaluator: false },
                success: false,
                errors: response?.errors || ["Failed to set primary evaluator status"],
            }
        } catch (error) {
            // Try to extract meaningful error message from HTTP response
            let errorMessage = "Failed to set primary evaluator status"
            if (error instanceof Error) {
                errorMessage = error.message

                // If it's an HTTP error, try to get more details
                if (error.message.includes("500")) {
                    errorMessage = "Server error occurred while updating primary evaluator status."
                } else if (error.message.includes("403")) {
                    errorMessage = SCHEDULE_OPERATION_ERRORS.NO_PERMISSION_MODIFY_PRIMARY_EVALUATOR
                } else if (error.message.includes("404")) {
                    errorMessage = "Instructor schedule not found."
                }
            }

            return {
                result: { isPrimaryEvaluator: false },
                success: false,
                errors: [errorMessage],
            }
        }
    }

    /**
     * Update primary evaluator status (alias for setPrimaryEvaluator for compatibility)
     */
    static updatePrimaryEvaluator(
        scheduleId: number,
        isPrimary: boolean,
    ): Promise<ApiResult<{ isPrimaryEvaluator: boolean; previousPrimaryName?: string | undefined }>> {
        return this.setPrimaryEvaluator(scheduleId, isPrimary)
    }

    /**
     * Check for scheduling conflicts before adding an instructor
     */
    static async checkConflicts(options: {
        mothraId: string
        rotationId: number
        weekIds: number[]
        gradYear: number
    }): Promise<ApiResult<ScheduleConflict[]>> {
        try {
            const { mothraId, rotationId, weekIds, gradYear } = options
            const params = new URLSearchParams({
                mothraId: mothraId.trim(),
                excludeRotationId: rotationId.toString(),
                weekIds: weekIds.join(","),
                gradYear: gradYear.toString(),
            })

            const url = `${this.BASE_URL}/other-assignments?${params.toString()}`
            const { get } = useFetch()
            const response = await get(url)
            return response as ApiResult<ScheduleConflict[]>
        } catch (error) {
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : "Failed to check schedule conflicts"],
            }
        }
    }

    /**
     * Get the audit history for a specific schedule entry
     */
    static async getAuditHistory(scheduleId: number): Promise<ApiResult<AuditEntry[]>> {
        try {
            const url = `${this.BASE_URL}/${scheduleId}/audit`
            const { get } = useFetch()
            const response = await get(url)
            return response as ApiResult<AuditEntry[]>
        } catch (error) {
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : "Failed to fetch audit history"],
            }
        }
    }

    /**
     * Get audit history for a rotation/week combination
     */
    static async getRotationWeekAudit(rotationId: number, weekId: number): Promise<ApiResult<AuditEntry[]>> {
        try {
            const params = new URLSearchParams({
                rotationId: rotationId.toString(),
                weekId: weekId.toString(),
            })

            const url = `${this.BASE_URL}/audit?${params.toString()}`
            const { get } = useFetch()
            const response = await get(url)
            return response as ApiResult<AuditEntry[]>
        } catch (error) {
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : "Failed to fetch audit history"],
            }
        }
    }
}

interface InstructorScheduleRequest {
    MothraId: string
    RotationId: number
    WeekIds: number[]
    GradYear: number
    IsPrimaryEvaluator?: boolean
}

interface InstructorScheduleResponse {
    scheduleIds: number[]
    schedules?: Array<{
        instructorScheduleId: number
        mothraId: string
        rotationId: number
        weekId: number
        isPrimaryEvaluator: boolean
        canRemove: boolean
    }>
    message?: string
    warningMessage?: string
}

interface ScheduleConflict {
    weekId: number
    weekNumber: number
    rotationId: number
    name: string
    dateStart: string
    dateEnd: string
    isAlreadyScheduled: boolean
}

interface SetPrimaryEvaluatorRequest {
    IsPrimary: boolean
}

interface AuditEntry {
    auditId: number
    action: string
    details: string
    modifiedBy: string
    modifiedDate: string
    mothraId?: string
    instructorName?: string
}

// ApiResult moved to shared types/api.ts

export {
    InstructorScheduleService,
    type InstructorScheduleRequest,
    type InstructorScheduleResponse,
    type ScheduleConflict,
    type SetPrimaryEvaluatorRequest,
    type AuditEntry,
    type ApiResult,
}
