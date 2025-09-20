import type { ApiError } from "../types/api-responses"

/**
 * Transforms various error types into strongly-typed ApiError objects
 * for consistent error handling across the application.
 */

// HTTP Status Code Constants
const HTTP_STATUS = {
    FORBIDDEN: 403,
    NOT_FOUND: 404,
    CONFLICT: 409,
    INTERNAL_SERVER_ERROR: 500,
} as const

/**
 * Handle HTTP status-based error transformation
 */
function handleHttpStatusError(status: number, error: any): ApiError | null {
    if (status === HTTP_STATUS.FORBIDDEN) {
        return {
            kind: "PermissionError",
            message: extractMessageOr(error?.message || "", "You don't have permission to perform this action"),
        }
    }

    if (status === HTTP_STATUS.NOT_FOUND) {
        return {
            kind: "NotFoundError",
            message: extractMessageOr(error?.message || "", "The requested resource was not found"),
        }
    }

    if (status === HTTP_STATUS.CONFLICT) {
        return {
            kind: "ConflictError",
            message: extractMessageOr(error?.message || "", "This operation conflicts with existing data"),
        }
    }

    if (status >= HTTP_STATUS.INTERNAL_SERVER_ERROR) {
        return { kind: "HttpError", status, message: "An internal server error occurred. Please try again later." }
    }

    return null
}

/**
 * Handle Error object-based error transformation
 */
function handleErrorObject(error: Error): ApiError {
    const errorMessage = error.message.toLowerCase()

    // Fallback to text parsing for non-HTTP errors
    if (errorMessage.includes("forbidden") || errorMessage.includes("403")) {
        return {
            kind: "PermissionError",
            message: extractMessageOr(error.message, "You don't have permission to perform this action"),
        }
    }

    if (errorMessage.includes("404") || errorMessage.includes("not found")) {
        return {
            kind: "NotFoundError",
            message: extractMessageOr(error.message, "The requested resource was not found"),
        }
    }

    if (errorMessage.includes("409") || errorMessage.includes("conflict")) {
        return {
            kind: "ConflictError",
            message: extractMessageOr(error.message, "This operation conflicts with existing data"),
        }
    }

    if (errorMessage.includes("400") || errorMessage.includes("validation")) {
        return handleValidationError(error)
    }

    if (errorMessage.includes("500") || errorMessage.includes("internal server error")) {
        return {
            kind: "HttpError",
            status: HTTP_STATUS.INTERNAL_SERVER_ERROR,
            message: "An internal server error occurred. Please try again later.",
        }
    }

    // Generic HTTP error with status code extraction
    const statusMatch = errorMessage.match(/\b([4-5]\d{2})\b/)
    if (statusMatch?.[1]) {
        return {
            kind: "HttpError",
            status: Number.parseInt(statusMatch[1], 10),
            message: extractMessageOr(error.message ?? "", "An error occurred while processing your request"),
        }
    }

    // Default to unknown error with original message
    return {
        kind: "UnknownError",
        message: error.message ?? "An unexpected error occurred",
    }
}

/**
 * Handle validation error transformation
 */
function handleValidationError(error: Error): ApiError {
    const errorWithDetails = error as Error & { errors?: string[] | Record<string, string[]> }

    if (errorWithDetails.errors) {
        // Handle array of validation errors
        if (Array.isArray(errorWithDetails.errors)) {
            return {
                kind: "ValidationError",
                message: errorWithDetails.errors.join(", ") || "Validation failed",
                details: undefined,
            }
        }
        // Handle validation details object
        if (typeof errorWithDetails.errors === "object") {
            return {
                kind: "ValidationError",
                message: "Validation failed. Please check the form fields.",
                details: errorWithDetails.errors,
            }
        }
    }

    return {
        kind: "ValidationError",
        message: extractMessageOr(error.message, "The provided data is invalid"),
    }
}

/**
 * Transform an unknown error into a typed ApiError
 */
function transformError(error: unknown): ApiError {
    // Handle network errors (no response from server)
    if (error instanceof TypeError && error.message.includes("fetch")) {
        return {
            kind: "NetworkError",
            message: "Unable to connect to the server. Please check your internet connection.",
        }
    }

    // Prefer structured HTTP status if available (fetch wrapper or axios-style errors)
    const maybeResponse = (error as any)?.response || (error as any)
    const status = typeof maybeResponse?.status === "number" ? maybeResponse.status : undefined

    if (status) {
        const statusError = handleHttpStatusError(status, error)
        if (statusError) {
            return statusError
        }
    }

    if (error instanceof Error) {
        return handleErrorObject(error)
    }

    // Handle non-Error objects
    return {
        kind: "UnknownError",
        message: (error as any)?.message ?? "An unexpected error occurred",
    }
}

/**
 * Extract a clean message from an error string or use default
 */
function extractMessageOr(errorMessage: string, defaultMessage: string): string {
    // Try to extract a clean message without status codes
    const cleanMessage = errorMessage
        .replaceAll(/\b\d{3}\b/g, "") // Remove status codes
        .replaceAll("error:", "") // Remove "error:" prefix
        .trim()

    return cleanMessage || defaultMessage
}

/**
 * Get user-friendly message based on error type
 */
function getErrorMessage(error: ApiError): string {
    switch (error.kind) {
        case "NetworkError": {
            return error.message
        }
        case "PermissionError": {
            return error.message
        }
        case "NotFoundError": {
            return error.message
        }
        case "ValidationError": {
            if (error.details) {
                const messages = Object.values(error.details).flat()
                return messages.length > 0 ? messages.join(", ") : error.message
            }
            return error.message
        }
        case "ConflictError": {
            return error.message
        }
        case "HttpError": {
            return `${error.message} (Error ${error.status})`
        }
        case "UnknownError": {
            return error.message
        }
        default: {
            // This should never be reached due to exhaustive checking above
            const _exhaustiveCheck: never = error
            return "An unexpected error occurred"
        }
    }
}

/**
 * Check if an error is of a specific kind
 */
function isErrorKind<K extends ApiError["kind"]>(error: ApiError, kind: K): error is Extract<ApiError, { kind: K }> {
    return error.kind === kind
}

/**
 * Transform error and get specific context-aware messages for schedule operations
 */
function getScheduleErrorMessage(error: unknown, operation: "add" | "remove" | "toggle"): string {
    const apiError = transformError(error)

    // Provide operation-specific messages
    if (isErrorKind(apiError, "PermissionError")) {
        switch (operation) {
            case "add": {
                return "You don't have permission to add instructors to this schedule"
            }
            case "remove": {
                return "You don't have permission to remove this instructor"
            }
            case "toggle": {
                return "You don't have permission to modify primary evaluator status"
            }
            default: {
                return "You don't have permission to perform this action"
            }
        }
    }

    if (isErrorKind(apiError, "ConflictError")) {
        switch (operation) {
            case "add": {
                return "This instructor is already scheduled for this week"
            }
            case "remove": {
                return "Cannot remove: This instructor may be the primary evaluator"
            }
            case "toggle": {
                return "Cannot change primary status: Another operation is in progress"
            }
            default: {
                return "This operation conflicts with existing data"
            }
        }
    }

    if (isErrorKind(apiError, "NotFoundError")) {
        switch (operation) {
            case "add": {
                return "The rotation or week was not found"
            }
            case "remove":
            case "toggle": {
                return "The schedule entry was not found. It may have been already removed."
            }
            default: {
                return "The requested resource was not found"
            }
        }
    }

    // Default to generic message from error
    return getErrorMessage(apiError)
}

// Export all functions together at the end
export { transformError, getErrorMessage, isErrorKind, getScheduleErrorMessage }
