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

    async function load(request: () => Promise<ApiResult<AuditLogEntry[]>>): Promise<void> {
        isLoading.value = true
        error.value = null
        try {
            const response = await request()
            if (response.success) {
                entries.value = response.result
            } else {
                error.value = response.errors?.join(", ") || LOAD_FAILED
            }
        } catch (err) {
            error.value = err instanceof Error ? err.message : LOAD_ERROR
        } finally {
            isLoading.value = false
        }
    }

    return { entries, isLoading, error, load }
}
