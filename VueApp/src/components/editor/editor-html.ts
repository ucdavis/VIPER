// HTML-building helpers for the rich text editor's link/image/table dialogs. Pure functions only,
// no DOM mutation, so the dialogs can build a string and hand it to the editor's insertHtml call.

export type LinkKind = "url" | "email" | "phone"

/** Escape &, <, >, ", ' for use in text and attribute values. */
export function escapeHtml(value: string): string {
    return value
        .replaceAll("&", "&amp;")
        .replaceAll("<", "&lt;")
        .replaceAll(">", "&gt;")
        .replaceAll('"', "&quot;")
        .replaceAll("'", "&#39;")
}

const HREF_PASSTHROUGH_PREFIXES = ["http:", "https:", "mailto:", "tel:", "/", "./", "../", "?", "#"]

/**
 * Trim; prepend "https://" only when the value starts with none of the recognized schemes or
 * relative-path markers. Internal links like "/2/CMS/Files?id=x" or "#s1" pass through untouched.
 */
export function normalizeHref(value: string): string {
    const trimmed = value.trim()
    if (!trimmed) {
        return ""
    }
    const lower = trimmed.toLowerCase()
    if (HREF_PASSTHROUGH_PREFIXES.some((prefix) => lower.startsWith(prefix))) {
        return trimmed
    }
    return `https://${trimmed}`
}

/**
 * Make a VIPER file URL relative. An absolute (http/https) or protocol-relative ("//host/...") URL
 * whose host matches `origin` is reduced to path + search + hash. Already-relative values and
 * offsite URLs pass through unchanged (trimmed). Never throws on junk input.
 */
export function toRelativeViperUrl(value: string, origin: string = globalThis.location.origin): string {
    const trimmed = value.trim()
    const isAbsolute = /^https?:/iu.test(trimmed)
    const isProtocolRelative = trimmed.startsWith("//")
    if (!isAbsolute && !isProtocolRelative) {
        return trimmed
    }

    try {
        const url = new URL(trimmed, origin)
        if (url.host !== new URL(origin).host) {
            return trimmed
        }
        return url.pathname + url.search + url.hash
    } catch {
        return trimmed
    }
}

/**
 * True when the value resolves to http(s) on the same host as `origin`: relative paths, and absolute
 * or protocol-relative URLs pointing back at VIPER. False for offsite hosts, backslash hosts
 * ("\\host\..." resolves offsite in browsers) and any other scheme ("data:", "javascript:", ...).
 */
export function isViperUrl(value: string, origin: string = globalThis.location.origin): boolean {
    try {
        const url = new URL(value.trim(), origin)
        return /^https?:$/u.test(url.protocol) && url.host === new URL(origin).host
    } catch {
        return false
    }
}

/** Parse an existing href back into the dialog's fields. */
export function parseLinkHref(href: string): { kind: LinkKind; address: string } {
    const lower = href.toLowerCase()
    if (lower.startsWith("mailto:")) {
        return { kind: "email", address: href.slice("mailto:".length) }
    }
    if (lower.startsWith("tel:")) {
        return { kind: "phone", address: href.slice("tel:".length) }
    }
    return { kind: "url", address: href }
}

/** Build an <a> element string. */
export function buildLinkHtml(opts: {
    kind: LinkKind
    address: string
    text?: string
    innerHtml?: string
    newWindow: boolean
}): string {
    const address = opts.address.trim()
    let href = normalizeHref(address)
    if (opts.kind === "email") {
        href = `mailto:${address}`
    } else if (opts.kind === "phone") {
        href = `tel:${address}`
    }
    const inner = opts.innerHtml ?? escapeHtml(opts.text?.trim() || address)
    const attrs = opts.newWindow ? ` target="_blank" rel="noopener"` : ""
    return `<a href="${escapeHtml(href)}"${attrs}>${inner}</a>`
}

/** Build an <img> element string, with src rewritten to a relative VIPER path when possible. */
export function buildImageHtml(opts: { src: string; alt: string; origin?: string }): string {
    const src = escapeHtml(toRelativeViperUrl(opts.src, opts.origin))
    return `<img src="${src}" alt="${escapeHtml(opts.alt)}">`
}

function clamp(value: number, min: number, max: number): number {
    return Math.min(max, Math.max(min, Number.isNaN(value) ? min : value))
}

// A body with zero rows renders as no <tbody> element at all (used for the header-only, rows=1 case).
function tbody(rowCount: number, bodyRow: string): string {
    return rowCount > 0 ? `<tbody>${bodyRow.repeat(rowCount)}</tbody>` : ""
}

const MAX_TABLE_ROWS = 50
const MAX_TABLE_COLS = 20

/**
 * Build a <table> skeleton. `rows` is the total row count including the header row when `header`
 * is true. Every cell holds &nbsp; so the caret can enter it in contenteditable. A trailing
 * `<p><br></p>` is appended so the user can type below the table.
 */
export function buildTableHtml(opts: { rows: number; cols: number; header: boolean }): string {
    const rows = clamp(opts.rows, 1, MAX_TABLE_ROWS)
    const cols = clamp(opts.cols, 1, MAX_TABLE_COLS)

    const headerRow = `<tr>${"<th>&nbsp;</th>".repeat(cols)}</tr>`
    const bodyRow = `<tr>${"<td>&nbsp;</td>".repeat(cols)}</tr>`

    const table = opts.header
        ? `<table><thead>${headerRow}</thead>${tbody(rows - 1, bodyRow)}</table>`
        : `<table>${tbody(rows, bodyRow)}</table>`
    return `${table}<p><br></p>`
}
