import { isSafeAbsoluteUrl } from "@/shared/url-safety"

// Link collection URLs are always full addresses (they point off to other sites), so these hold
// callers to an absolute URL on top of the shared protocol allowlist. A blank URL is the caller's
// own required-rule to report, so it passes here and renders as "#".

function isSafeUrl(val: string | null | undefined): boolean {
    const normalized = val?.trim()
    return !normalized || isSafeAbsoluteUrl(normalized)
}

function safeHref(url: string | null | undefined): string {
    const normalized = url?.trim()
    return normalized && isSafeAbsoluteUrl(normalized) ? normalized : "#"
}

export { isSafeUrl, safeHref }
