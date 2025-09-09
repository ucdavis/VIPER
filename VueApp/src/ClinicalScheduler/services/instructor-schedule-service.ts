import { useFetch } from "../../composables/ViperFetch"
import type { ApiResult, TypedApiResult } from "../types/api"
import { transformError, getScheduleErrorMessage } from "./error-transformer"

class InstructorScheduleService {
    private static readonly BASE_URL = `${import.meta.env.VITE_API_URL}clinicalscheduler/instructor-schedules`

    /**
     * Add an instructor to one or more weeks of a rotation
     * Returns TypedApiResult with structured error information
     */
    static async addInstructor(
        request: InstructorScheduleRequest,
    ): Promise<TypedApiResult<InstructorScheduleResponse>> {
        try {
            // Trim MothraId to prevent whitespace issues
            const trimmedRequest = { ...request, MothraId: request.MothraId.trim() }
            const { post } = useFetch()
            const response = await post(this.BASE_URL, trimmedRequest)
            return response as ApiResult<InstructorScheduleResponse>
        } catch (error) {
            const apiError = transformError(error)
            const errorMessage = getScheduleErrorMessage(error, "add")

            return {
                result: { scheduleIds: [] },
                success: false,
                error: apiError,
                errors: [errorMessage], // Keep for backward compatibility
            }
        }
    }

    /**
     * Remove an instructor from a scheduled week
     * Returns TypedApiResult with structured error information
     */
    static async removeInstructor(scheduleId: number): Promise<TypedApiResult<boolean>> {
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
            const apiError = transformError(error)
            const errorMessage = getScheduleErrorMessage(error, "remove")

            return {
                result: false,
                success: false,
                error: apiError,
                errors: [errorMessage], // Keep for backward compatibility
            }
        }
    }

    /**
     * Set or unset an instructor as the primary evaluator for a rotation/week
     */
    static async setPrimaryEvaluator(
        scheduleId: number,
        isPrimary: boolean,
    ): Promise<TypedApiResult<{ isPrimaryEvaluator: boolean; previousPrimaryName?: string | undefined }>> {
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
            const apiError = transformError(error)
            const errorMessage = getScheduleErrorMessage(error, "toggle")

            return {
                result: { isPrimaryEvaluator: false },
                success: false,
                error: apiError,
                errors: [errorMessage], // Keep for backward compatibility
            }
        }
    }

    /**
     * Update primary evaluator status (alias for setPrimaryEvaluator for compatibility)
     */
    static updatePrimaryEvaluator(
        scheduleId: number,
        isPrimary: boolean,
    ): Promise<TypedApiResult<{ isPrimaryEvaluator: boolean; previousPrimaryName?: string | undefined }>> {
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
