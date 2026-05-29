const SAFE_PROTOCOLS = new Set(["http:", "https:", "mailto:", "tel:"])

function isSafeUrl(val: string | null | undefined): boolean {
    const normalized = val?.trim()
    if (!normalized) {
        return true
    }
    try {
        return SAFE_PROTOCOLS.has(new URL(normalized).protocol)
    } catch {
        return false
    }
}

function safeHref(url: string | null | undefined): string {
    const normalized = url?.trim()
    if (!normalized) {
        return "#"
    }
    try {
        return SAFE_PROTOCOLS.has(new URL(normalized).protocol) ? normalized : "#"
    } catch {
        return "#"
    }
}

export { isSafeUrl, safeHref }
