/**
 * API Response DTOs for Clinical Scheduler
 *
 * These types mirror backend contract responses and are consumed by
 * services and components in the Clinical Scheduler module.
 */

import type { ApiResult } from "./api"

/**
 * Standard error payload returned by backend endpoints (when present).
 */
interface ErrorResponse {
    /** Machine-readable error code (e.g., "ValidationFailed", "Forbidden"). */
    code: string
    /** Message safe for display to end users. */
    userMessage: string
    /** Correlates client logs with server logs. */
    correlationId: string
    /** ISO timestamp when the error occurred on the server. */
    timestamp: string
}

/**
 * Discriminated client-side API error representation used in UI flows.
 */
type ApiError =
    | { kind: "NetworkError"; message: string }
    | { kind: "HttpError"; status: number; message: string }
    | { kind: "ValidationError"; message: string; details?: Record<string, string[]> }
    | { kind: "PermissionError"; message: string }
    | { kind: "NotFoundError"; message: string }
    | { kind: "ConflictError"; message: string }
    | { kind: "UnknownError"; message: string }

/**
 * Instructor schedule item returned from create/list APIs.
 */
interface InstructorScheduleResponse {
    instructorScheduleId: number
    mothraId: string
    rotationId: number
    weekId: number
    isPrimaryEvaluator: boolean
    /** Whether the current user has permission to remove this schedule. */
    canRemove: boolean
}

/**
 * Response of add-instructor endpoint, including created schedules
 * and optional warning when conflicts were detected.
 */
interface AddInstructorResponse {
    schedules: InstructorScheduleResponse[]
    scheduleIds: number[]
    /** Optional informational warning shown in the UI. */
    warningMessage?: string
}

/**
 * Conflict check result for proposed instructor-week assignments.
 */
interface ScheduleConflictResponse {
    conflicts: InstructorScheduleResponse[]
    message: string
    hasConflicts: boolean
}

/** Generic success envelope with message. */
interface SuccessResponse {
    message: string
}

// Specific typed results
type AddInstructorApiResult = ApiResult<AddInstructorResponse>
type ConflictCheckApiResult = ApiResult<ScheduleConflictResponse>
// 204 No Content
type RemoveInstructorApiResult = ApiResult<void>

export {
    type ErrorResponse,
    type ApiError,
    type InstructorScheduleResponse,
    type AddInstructorResponse,
    type ScheduleConflictResponse,
    type SuccessResponse,
    type AddInstructorApiResult,
    type ConflictCheckApiResult,
    type RemoveInstructorApiResult,
}
