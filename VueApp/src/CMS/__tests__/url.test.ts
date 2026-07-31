// oxlint-disable no-script-url -- this suite exists to prove javascript: URLs are blocked, so the literals are intentional
import { isSafeUrl, safeHref } from "@/CMS/utils/url"

/**
 * Exhaustive coverage of the CMS URL safety helpers. These guard every user-supplied
 * link rendered as an href, so the allowed/blocked scheme split and the null/empty
 * asymmetry are the security-critical contract.
 */

describe("CMS url safety - isSafeUrl", () => {
    describe("allowed schemes pass", () => {
        it.each([
            ["http", "http://example.com/page"],
            ["https", "https://example.com/page"],
            ["mailto", "mailto:person@example.com"],
            ["tel", "tel:+15555551234"],
        ])("treats %s as safe", (_scheme, url) => {
            expect(isSafeUrl(url)).toBeTruthy()
        })

        it("is case-insensitive about the scheme (URL normalizes it)", () => {
            expect(isSafeUrl("HTTPS://example.com")).toBeTruthy()
            expect(isSafeUrl("MailTo:person@example.com")).toBeTruthy()
        })
    })

    describe("dangerous schemes are blocked", () => {
        it.each([
            ["javascript", "javascript:alert(1)"],
            ["data", "data:text/html,<script>alert(1)</script>"],
            ["file", "file:///etc/passwd"],
            ["vbscript", "vbscript:msgbox(1)"],
        ])("rejects %s: URLs", (_scheme, url) => {
            expect(isSafeUrl(url)).toBeFalsy()
        })

        it("blocks a javascript: URL regardless of casing/whitespace padding", () => {
            expect(isSafeUrl("  JavaScript:alert(1)  ")).toBeFalsy()
        })
    })

    describe("null / empty / whitespace are treated as safe (asymmetry with safeHref)", () => {
        // IsSafeUrl returns true for absent values: an empty link is not a dangerous link,
        // so it controls the "render as text vs link" choice. safeHref still returns "#"
        // for the same inputs because there is no usable destination.
        it.each([
            ["null", null],
            ["undefined", undefined],
            ["empty string", ""],
            ["whitespace only", "   "],
        ])("returns true for %s", (_label, value) => {
            expect(isSafeUrl(value)).toBeTruthy()
        })
    })

    describe("malformed URLs are rejected", () => {
        it.each([
            ["scheme with no host", "http://"],
            ["colons only", "::::"],
            ["bare word", "not a url"],
            ["protocol-relative (no scheme for URL())", "//example.com/foo"],
        ])("returns false for %s", (_label, url) => {
            expect(isSafeUrl(url)).toBeFalsy()
        })
    })
})

describe("CMS url safety - safeHref", () => {
    it("returns the original URL unchanged for allowed schemes", () => {
        expect(safeHref("https://example.com/a?b=c#d")).toBe("https://example.com/a?b=c#d")
        expect(safeHref("mailto:person@example.com")).toBe("mailto:person@example.com")
        expect(safeHref("tel:+15555551234")).toBe("tel:+15555551234")
    })

    it("preserves surrounding text but the original (untrimmed) string is returned", () => {
        // Trimming is only used for the safety check; the returned value is the normalized
        // (trimmed) form for valid URLs.
        expect(safeHref("  https://example.com  ")).toBe("https://example.com")
    })

    it.each([
        ["javascript", "javascript:alert(1)"],
        ["data", "data:text/html,x"],
        ["file", "file:///etc/passwd"],
        ["vbscript", "vbscript:msgbox(1)"],
    ])("returns '#' for blocked %s scheme", (_scheme, url) => {
        expect(safeHref(url)).toBe("#")
    })

    it.each([
        ["null", null],
        ["undefined", undefined],
        ["empty string", ""],
        ["whitespace only", "   "],
    ])("returns '#' for %s (asymmetry: isSafeUrl says true here)", (_label, value) => {
        expect(safeHref(value)).toBe("#")
        expect(isSafeUrl(value)).toBeTruthy()
    })

    it.each([
        ["scheme with no host", "http://"],
        ["colons only", "::::"],
        ["bare word", "not a url"],
    ])("returns '#' for malformed input %s", (_label, url) => {
        expect(safeHref(url)).toBe("#")
    })
})
