/**
 * Shared API types for Clinical Scheduler
 */

import type { ApiError } from "./api-responses"

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

export type { ApiResult, TypedApiResult }
