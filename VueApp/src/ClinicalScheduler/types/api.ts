/**
 * Shared API types for Clinical Scheduler
 */

// Standard API result wrapper used throughout the clinical scheduler
interface ApiResult<T> {
    result: T
    success: boolean
    errors: string[]
}

export type { ApiResult }
