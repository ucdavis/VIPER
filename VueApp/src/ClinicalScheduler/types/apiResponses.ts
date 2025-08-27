/**
 * API Response DTOs for Clinical Scheduler
 */

export interface ErrorResponse {
  code: string
  userMessage: string
  correlationId: string
  timestamp: string
}

export interface InstructorScheduleResponse {
  instructorScheduleId: number
  mothraId: string
  rotationId: number
  weekId: number
  isPrimaryEvaluator: boolean
  canRemove: boolean
}

export interface AddInstructorResponse {
  schedules: InstructorScheduleResponse[]
  scheduleIds: number[]
  warningMessage?: string
}

export interface ScheduleConflictResponse {
  conflicts: InstructorScheduleResponse[]
  message: string
  hasConflicts: boolean
}

export interface SuccessResponse {
  message: string
}

// API Result wrapper for service responses
export interface ApiResult<T> {
  success: boolean
  result?: T
  error?: Error
}

// Specific typed results
export type AddInstructorApiResult = ApiResult<AddInstructorResponse>
export type ConflictCheckApiResult = ApiResult<ScheduleConflictResponse>
export type RemoveInstructorApiResult = ApiResult<void> // 204 No Content