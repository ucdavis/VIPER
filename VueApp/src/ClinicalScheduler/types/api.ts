/**
 * Shared API types for Clinical Scheduler
 */

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

// Standard API result wrapper used throughout the clinical scheduler
interface ApiResult<T> {
    result: T
    success: boolean
    errors: string[]
}

// Enhanced API result with typed errors for gradual migration
interface TypedApiResult<T> {
    result: T
    success: boolean
    error?: ApiError
    errors: string[] // Keep for backward compatibility
}

export type { ApiError, ApiResult, TypedApiResult }
