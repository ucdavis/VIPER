import { useFetch } from '../../composables/ViperFetch'

const { get, post, del, put } = useFetch()

export interface InstructorScheduleRequest {
    MothraId: string
    RotationId: number
    WeekIds: number[]
    GradYear: number
    IsPrimaryEvaluator?: boolean
}

export interface InstructorScheduleResponse {
    scheduleIds: number[]
    schedules?: any[]
    message?: string
    warningMessage?: string
}

export interface ScheduleConflict {
    weekId: number
    weekNumber: number
    rotationId: number
    rotationName: string
    dateStart: string
    dateEnd: string
    isAlreadyScheduled: boolean
}

export interface SetPrimaryEvaluatorRequest {
    IsPrimary: boolean
}

export interface AuditEntry {
    auditId: number
    action: string
    details: string
    modifiedBy: string
    modifiedDate: string
    mothraId?: string
    instructorName?: string
}

export interface ApiResult<T> {
    result: T
    success: boolean
    errors: string[]
}

export class InstructorScheduleService {
    private static readonly BASE_URL = import.meta.env.VITE_API_URL + 'clinicalscheduler/instructor-schedules'

    /**
     * Add an instructor to one or more weeks of a rotation
     */
    static async addInstructor(request: InstructorScheduleRequest): Promise<ApiResult<InstructorScheduleResponse>> {
        try {
            // Trim MothraId to prevent whitespace issues
            const trimmedRequest = { ...request, MothraId: request.MothraId.trim() }
            const response = await post(this.BASE_URL, trimmedRequest)
            return response as ApiResult<InstructorScheduleResponse>
        } catch (error) {
            console.error('Error adding instructor to schedule:', error)
            
            // Extract meaningful error message from the response
            let errorMessage = 'Failed to add instructor to schedule'
            if (error instanceof Error) {
                // Check if error has validation errors property (from ViperFetch ValidationError)
                if ((error as any).errors && Array.isArray((error as any).errors)) {
                    const validationErrors = (error as any).errors
                    if (validationErrors.length > 0) {
                        errorMessage = validationErrors.join(', ')
                    }
                } else {
                    errorMessage = error.message
                }
            }
            
            return {
                result: { scheduleIds: [] },
                success: false,
                errors: [errorMessage]
            }
        }
    }

    /**
     * Remove an instructor from a scheduled week
     */
    static async removeInstructor(scheduleId: number): Promise<ApiResult<boolean>> {
        try {
            const response = await del(`${this.BASE_URL}/${scheduleId}`)
            
            // Check if the response indicates success
            if (response && (response.success !== false)) {
                return {
                    result: true,
                    success: true,
                    errors: []
                }
            } else {
                return {
                    result: false,
                    success: false,
                    errors: response?.errors || ['Failed to remove instructor from schedule']
                }
            }
        } catch (error) {
            console.error('Error removing instructor from schedule:', error)
            
            // Try to extract meaningful error message from HTTP response
            let errorMessage = 'Failed to remove instructor from schedule'
            if (error instanceof Error) {
                errorMessage = error.message
                
                // If it's an HTTP error, try to get more details
                if (error.message.includes('500')) {
                    errorMessage = 'Server error occurred. The instructor may be a primary evaluator or there may be a permission issue.'
                } else if (error.message.includes('403')) {
                    errorMessage = 'You do not have permission to remove this instructor.'
                } else if (error.message.includes('404')) {
                    errorMessage = 'Instructor schedule not found.'
                }
            }
            
            return {
                result: null,
                success: false,
                errors: [errorMessage]
            }
        }
    }

    /**
     * Set or unset an instructor as the primary evaluator for a rotation/week
     */
    static async setPrimaryEvaluator(scheduleId: number, isPrimary: boolean): Promise<ApiResult<{ isPrimaryEvaluator: boolean; previousPrimaryName?: string | null }>> {
        try {
            const request: SetPrimaryEvaluatorRequest = { IsPrimary: isPrimary }
            const response = await put(`${this.BASE_URL}/${scheduleId}/primary`, request)
            
            // Check if the response indicates success
            if (response && (response.success !== false)) {
                return {
                    result: response,
                    success: true,
                    errors: []
                }
            } else {
                return {
                    result: null,
                    success: false,
                    errors: response?.errors || ['Failed to set primary evaluator status']
                }
            }
        } catch (error) {
            console.error('Error setting primary evaluator:', error)
            
            // Try to extract meaningful error message from HTTP response
            let errorMessage = 'Failed to set primary evaluator status'
            if (error instanceof Error) {
                errorMessage = error.message
                
                // If it's an HTTP error, try to get more details
                if (error.message.includes('500')) {
                    errorMessage = 'Server error occurred while updating primary evaluator status.'
                } else if (error.message.includes('403')) {
                    errorMessage = 'You do not have permission to modify primary evaluator status.'
                } else if (error.message.includes('404')) {
                    errorMessage = 'Instructor schedule not found.'
                }
            }
            
            return {
                result: null,
                success: false,
                errors: [errorMessage]
            }
        }
    }

    /**
     * Check for scheduling conflicts before adding an instructor
     */
    static async checkConflicts(
        mothraId: string,
        rotationId: number,
        weekIds: number[],
        gradYear: number
    ): Promise<ApiResult<ScheduleConflict[]>> {
        try {
            const params = new URLSearchParams({
                mothraId: mothraId.trim(),
                excludeRotationId: rotationId.toString(),
                weekIds: weekIds.join(','),
                gradYear: gradYear.toString()
            })
            
            const url = `${this.BASE_URL}/other-assignments?${params.toString()}`
            const response = await get(url)
            return response as ApiResult<ScheduleConflict[]>
        } catch (error) {
            console.error('Error checking schedule conflicts:', error)
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : 'Failed to check schedule conflicts']
            }
        }
    }

    /**
     * Get the audit history for a specific schedule entry
     */
    static async getAuditHistory(scheduleId: number): Promise<ApiResult<AuditEntry[]>> {
        try {
            const url = `${this.BASE_URL}/${scheduleId}/audit`
            const response = await get(url)
            return response as ApiResult<AuditEntry[]>
        } catch (error) {
            console.error('Error fetching audit history:', error)
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : 'Failed to fetch audit history']
            }
        }
    }

    /**
     * Get audit history for a rotation/week combination
     */
    static async getRotationWeekAudit(
        rotationId: number,
        weekId: number
    ): Promise<ApiResult<AuditEntry[]>> {
        try {
            const params = new URLSearchParams({
                rotationId: rotationId.toString(),
                weekId: weekId.toString()
            })
            
            const url = `${this.BASE_URL}/audit?${params.toString()}`
            const response = await get(url)
            return response as ApiResult<AuditEntry[]>
        } catch (error) {
            console.error('Error fetching rotation/week audit history:', error)
            return {
                result: [],
                success: false,
                errors: [error instanceof Error ? error.message : 'Failed to fetch audit history']
            }
        }
    }
}

export default InstructorScheduleService