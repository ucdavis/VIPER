// The only protocols VIPER puts in an href. Anything else a user can type (javascript:, data:,
// vbscript:, file:) either runs script or reaches the reader's own disk when the link is clicked.
const SAFE_PROTOCOLS = new Set(["http:", "https:", "mailto:", "tel:"])

/**
 * The value's protocol, or null when it carries no scheme at all. `new URL` with no base throws for
 * exactly the schemeless case, which is what separates a relative "/welcome" from "javascript:x".
 * A bare "host:port" ("example.com:8080/x") does parse as a scheme, so it reads as an unsafe
 * protocol rather than a relative path; callers say to type the scheme.
 */
function protocolOf(value: string): string | null {
    try {
        return new URL(value.trim()).protocol
    } catch {
        return null
    }
}

/** True for an absolute URL VIPER is willing to link to. A relative value is not absolute. */
function isSafeAbsoluteUrl(value: string): boolean {
    const protocol = protocolOf(value)
    return protocol !== null && SAFE_PROTOCOLS.has(protocol)
}

/**
 * True for anything safe to put in an href: a relative link (a path, query or fragment, which
 * inherits the page's own scheme) or an absolute one on an allowed protocol. Use this where
 * relative links are legitimate; use isSafeAbsoluteUrl where every link must be a full address.
 */
function isSafeHref(value: string): boolean {
    const protocol = protocolOf(value)
    return protocol === null || SAFE_PROTOCOLS.has(protocol)
}

export { isSafeAbsoluteUrl, isSafeHref }
