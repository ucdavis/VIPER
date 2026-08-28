import type { Result } from "@/composables/ViperFetch"

/**
 * Builds a full ViperFetch Result for mocking a service call.
 *
 * The service layer resolves to the whole Result, not just the interesting fields, so a mock
 * that supplies only `success`/`result`/`errors` fails to type-check against the real return
 * type. This fills in the plumbing fields the tests never assert on.
 */
function apiResult(overrides: Partial<Result> = {}): Result {
    return {
        result: null,
        errors: [],
        success: true,
        pagination: null,
        status: 200,
        ...overrides,
    }
}

/** A failed call carrying server-supplied error messages. */
function apiError(errors: string[]): Result {
    return apiResult({ success: false, errors, status: 400 })
}

export { apiResult, apiError }
