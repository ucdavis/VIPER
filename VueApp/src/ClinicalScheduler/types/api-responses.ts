/**
 * Client-side API error representation for Clinical Scheduler.
 *
 * The response DTOs that used to live here were unused: services declare
 * their own local copies (see instructor-schedule-service.ts).
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

export { type ApiError }
