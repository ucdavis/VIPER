import { ref } from "vue"
import type { Ref } from "vue"
import type { ApiResult } from "../types/api"
import type { AuditLogEntry } from "../types/audit-types"

const LOAD_FAILED = "Failed to load the audit trail"
const LOAD_ERROR = "An error occurred while loading the audit trail"

export interface UseAuditEntries {
    entries: Ref<AuditLogEntry[]>
    isLoading: Ref<boolean>
    error: Ref<string | null>
    load: (request: () => Promise<ApiResult<AuditLogEntry[]>>) => Promise<void>
    /** Clear entries/error so the next load shows a clean loading state, not stale content */
    reset: () => void
}

/**
 * Shared loader for audit-trail surfaces (the full page and the inline per-week
 * popover): owns the entries/loading/error state and applies one consistent
 * success/failure handling, so each caller stays a thin wrapper around its request.
 */
export function useAuditEntries(): UseAuditEntries {
    const entries = ref<AuditLogEntry[]>([])
    const isLoading = ref(false)
    const error = ref<string | null>(null)
    let latestRequestId = 0

    async function load(request: () => Promise<ApiResult<AuditLogEntry[]>>): Promise<void> {
        // Overlapping loads (debounced filter edits, retry) can resolve out of order;
        // only the latest request may commit state, or stale results win the race.
        latestRequestId += 1
        const requestId = latestRequestId
        isLoading.value = true
        error.value = null
        try {
            const response = await request()
            if (requestId !== latestRequestId) {
                return
            }
            if (response.success) {
                entries.value = response.result
            } else {
                entries.value = []
                error.value = response.errors?.join(", ") || LOAD_FAILED
            }
        } catch (err) {
            if (requestId !== latestRequestId) {
                return
            }
            entries.value = []
            error.value = err instanceof Error ? err.message : LOAD_ERROR
        } finally {
            if (requestId === latestRequestId) {
                isLoading.value = false
            }
        }
    }

    // Drop the current result (and invalidate any in-flight load) so a reopened
    // surface shows its loading state, not stale rows.
    function reset(): void {
        latestRequestId += 1
        entries.value = []
        error.value = null
        isLoading.value = false
    }

    return { entries, isLoading, error, load, reset }
}
