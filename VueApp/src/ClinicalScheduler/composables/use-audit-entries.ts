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
                error.value = response.errors?.join(", ") || LOAD_FAILED
            }
        } catch (err) {
            if (requestId !== latestRequestId) {
                return
            }
            error.value = err instanceof Error ? err.message : LOAD_ERROR
        } finally {
            if (requestId === latestRequestId) {
                isLoading.value = false
            }
        }
    }

    return { entries, isLoading, error, load }
}
