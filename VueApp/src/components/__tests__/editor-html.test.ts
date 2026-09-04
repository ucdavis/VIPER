import {
    buildImageHtml,
    buildLinkHtml,
    buildTableHtml,
    escapeHtml,
    isViperUrl,
    normalizeHref,
    parseLinkHref,
    toRelativeViperUrl,
} from "@/components/editor/editor-html"

const ORIGIN = "https://viper.example.edu"

test("escapeHtml escapes all five characters", () => {
    expect(escapeHtml(`&<>"'`)).toBe("&amp;&lt;&gt;&quot;&#39;")
})

test("normalizeHref prepends https:// for a bare domain", () => {
    expect(normalizeHref("example.com/x")).toBe("https://example.com/x")
})

test("normalizeHref leaves relative and already-schemed values untouched", () => {
    expect(normalizeHref("/welcome")).toBe("/welcome")
    expect(normalizeHref("./a")).toBe("./a")
    expect(normalizeHref("../a")).toBe("../a")
    expect(normalizeHref("?page=2")).toBe("?page=2")
    expect(normalizeHref("#s1")).toBe("#s1")
    expect(normalizeHref("http://x")).toBe("http://x")
    expect(normalizeHref("https://x")).toBe("https://x")
    expect(normalizeHref("HTTPS://x")).toBe("HTTPS://x")
    expect(normalizeHref("mailto:a@b")).toBe("mailto:a@b")
})

test("normalizeHref trims and handles empty/whitespace", () => {
    expect(normalizeHref("  example.com  ")).toBe("https://example.com")
    expect(normalizeHref("   ")).toBe("")
    expect(normalizeHref("")).toBe("")
})

test("toRelativeViperUrl strips a same-origin scheme+host", () => {
    expect(toRelativeViperUrl("https://viper.example.edu/2/CMS/Files?id=x", ORIGIN)).toBe("/2/CMS/Files?id=x")
})

test("toRelativeViperUrl strips a same-host protocol-relative URL", () => {
    expect(toRelativeViperUrl("//viper.example.edu/2/CMS/Files?id=x", ORIGIN)).toBe("/2/CMS/Files?id=x")
})

test("toRelativeViperUrl leaves an already-relative value alone", () => {
    expect(toRelativeViperUrl("/2/CMS/Files?id=abc", ORIGIN)).toBe("/2/CMS/Files?id=abc")
})

test("toRelativeViperUrl returns an offsite URL unchanged, keeping query and hash", () => {
    expect(toRelativeViperUrl("https://example.com/x.jpg?a=1#frag", ORIGIN)).toBe("https://example.com/x.jpg?a=1#frag")
})

test("isViperUrl is true for relative and same-origin values", () => {
    expect(isViperUrl("/2/CMS/Files?id=x", ORIGIN)).toBeTruthy()
    expect(isViperUrl("https://viper.example.edu/2/CMS/Files?id=x", ORIGIN)).toBeTruthy()
})

test("isViperUrl is false for offsite and non-http(s) schemes", () => {
    expect(isViperUrl("https://example.com/x", ORIGIN)).toBeFalsy()
    expect(isViperUrl("//example.com/x", ORIGIN)).toBeFalsy()
    expect(isViperUrl("data:image/png;base64,x", ORIGIN)).toBeFalsy()
    expect(isViperUrl("javascript:alert(1)", ORIGIN)).toBeFalsy()
    // Browsers read a backslash as a slash in http URLs, so this resolves to example.com.
    expect(isViperUrl("\\\\example.com/x", ORIGIN)).toBeFalsy()
})

test("parseLinkHref round-trips mailto, tel, and plain URLs", () => {
    expect(parseLinkHref("mailto:a@b.com")).toStrictEqual({ kind: "email", address: "a@b.com" })
    expect(parseLinkHref("tel:5305551212")).toStrictEqual({ kind: "phone", address: "5305551212" })
    expect(parseLinkHref("MAILTO:A@B.com")).toStrictEqual({ kind: "email", address: "A@B.com" })
    expect(parseLinkHref("https://example.com/x")).toStrictEqual({ kind: "url", address: "https://example.com/x" })
})

test("buildLinkHtml adds target/rel only when newWindow is true", () => {
    expect(buildLinkHtml({ kind: "url", address: "https://x", newWindow: false })).toBe(
        `<a href="https://x">https://x</a>`,
    )
    expect(buildLinkHtml({ kind: "url", address: "https://x", newWindow: true })).toBe(
        `<a href="https://x" target="_blank" rel="noopener">https://x</a>`,
    )
})

test("buildLinkHtml builds mailto: and tel: hrefs", () => {
    expect(buildLinkHtml({ kind: "email", address: "a@b.com", newWindow: false })).toBe(
        `<a href="mailto:a@b.com">a@b.com</a>`,
    )
    expect(buildLinkHtml({ kind: "phone", address: "530", newWindow: false })).toBe(`<a href="tel:530">530</a>`)
})

test("buildLinkHtml escapes text, uses innerHtml verbatim, and falls back to the address", () => {
    expect(buildLinkHtml({ kind: "url", address: "https://x", text: "<b>go</b>", newWindow: false })).toBe(
        `<a href="https://x">&lt;b&gt;go&lt;/b&gt;</a>`,
    )
    expect(buildLinkHtml({ kind: "url", address: "https://x", innerHtml: "<b>go</b>", newWindow: false })).toBe(
        `<a href="https://x"><b>go</b></a>`,
    )
    expect(buildLinkHtml({ kind: "url", address: "https://x", newWindow: false })).toBe(
        `<a href="https://x">https://x</a>`,
    )
})

test("buildLinkHtml escapes a quote in the address so it can't break the attribute", () => {
    expect(buildLinkHtml({ kind: "url", address: `https://x/"><script>`, newWindow: false })).toBe(
        `<a href="https://x/&quot;&gt;&lt;script&gt;">https://x/&quot;&gt;&lt;script&gt;</a>`,
    )
})

test("buildImageHtml makes src relative and escapes alt, emitting alt even when empty", () => {
    expect(buildImageHtml({ src: "https://viper.example.edu/2/CMS/Files?id=x", alt: 'a "cat"', origin: ORIGIN })).toBe(
        `<img src="/2/CMS/Files?id=x" alt="a &quot;cat&quot;">`,
    )
    expect(buildImageHtml({ src: "/2/CMS/Files?id=x", alt: "", origin: ORIGIN })).toBe(
        `<img src="/2/CMS/Files?id=x" alt="">`,
    )
})

test("buildTableHtml with a header splits header row from body rows", () => {
    const html = buildTableHtml({ rows: 3, cols: 3, header: true })
    expect(html).toBe(
        "<table><thead><tr><th>&nbsp;</th><th>&nbsp;</th><th>&nbsp;</th></tr></thead>" +
            "<tbody><tr><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td></tr>" +
            "<tr><td>&nbsp;</td><td>&nbsp;</td><td>&nbsp;</td></tr></tbody></table><p><br></p>",
    )
})

test("buildTableHtml with rows=1 and header=true has no tbody at all", () => {
    const html = buildTableHtml({ rows: 1, cols: 2, header: true })
    expect(html).toContain("<thead>")
    expect(html).not.toContain("<tbody")
})

test("buildTableHtml with rows=1 and header=false has a single body row and no thead", () => {
    const html = buildTableHtml({ rows: 1, cols: 2, header: false })
    expect(html).toBe("<table><tbody><tr><td>&nbsp;</td><td>&nbsp;</td></tr></tbody></table><p><br></p>")
    expect(html).not.toContain("<thead")
})

test("buildTableHtml clamps cols to 20", () => {
    const html = buildTableHtml({ rows: 1, cols: 99, header: false })
    expect(html.match(/<td>/gu) ?? []).toHaveLength(20)
})
