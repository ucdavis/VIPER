/**
 * API Response DTOs for Clinical Scheduler
 */

import type { ApiResult } from "./api"

interface ErrorResponse {
    code: string
    userMessage: string
    correlationId: string
    timestamp: string
}

interface InstructorScheduleResponse {
    instructorScheduleId: number
    mothraId: string
    rotationId: number
    weekId: number
    isPrimaryEvaluator: boolean
    canRemove: boolean
}

interface AddInstructorResponse {
    schedules: InstructorScheduleResponse[]
    scheduleIds: number[]
    warningMessage?: string
}

interface ScheduleConflictResponse {
    conflicts: InstructorScheduleResponse[]
    message: string
    hasConflicts: boolean
}

interface SuccessResponse {
    message: string
}

// Specific typed results
type AddInstructorApiResult = ApiResult<AddInstructorResponse>
type ConflictCheckApiResult = ApiResult<ScheduleConflictResponse>
type RemoveInstructorApiResult = ApiResult<void> // 204 No Content

export {
    type ErrorResponse,
    type InstructorScheduleResponse,
    type AddInstructorResponse,
    type ScheduleConflictResponse,
    type SuccessResponse,
    type AddInstructorApiResult,
    type ConflictCheckApiResult,
    type RemoveInstructorApiResult,
}
