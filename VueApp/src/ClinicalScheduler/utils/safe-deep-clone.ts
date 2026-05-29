/**
 * Deep clone helper that handles Vue reactive objects and circular references
 */
export function safeDeepClone<T>(obj: T): T {
    try {
        // First try structuredClone if available and works
        if (typeof structuredClone === "function") {
            return structuredClone(obj)
        }
    } catch {
        // StructuredClone failed, fall back to JSON method
    }

    // Fallback to JSON parse/stringify - handles most Vue reactive objects
    try {
        // eslint-disable-next-line prefer-structured-clone -- This is a fallback when structuredClone fails
        return JSON.parse(JSON.stringify(obj))
    } catch {
        // JSON fallback failed -> abort and surface error to caller so rollback can be handled safely.
        // Remove UI side-effects from this helper.
        throw new Error("safeDeepClone failed: unable to deep clone object")
    }
}
