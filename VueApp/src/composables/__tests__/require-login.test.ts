import { buildLoginUrl, isValidInternalPath } from "@/composables/RequireLogin"

// The app base is read from import.meta.env at call time, so each test stubs the base it needs.
function withBase(base: string, run: () => void): void {
    vi.stubEnv("VITE_VIPER_HOME", base)
    try {
        run()
    } finally {
        vi.unstubAllEnvs()
    }
}

describe("buildLoginUrl application base handling", () => {
    it("emits root-relative paths when the app is served at the domain root", () => {
        withBase("/", () => {
            expect(buildLoginUrl("/CTS/")).toBe("/login?ReturnUrl=%2FCTS%2F")
        })
    })

    it("keeps the /2 base for the subpath deployment", () => {
        // TEST/PROD run VIPER 2 under a "/2" PathBase; dropping it escapes to the legacy site.
        withBase("/2/", () => {
            expect(buildLoginUrl("/2/CTS/")).toBe("/2/login?ReturnUrl=%2F2%2FCTS%2F")
        })
    })

    it("inserts the separator when the base is configured without a trailing slash", () => {
        // Guards the "/2login" regression: the base is normalized, then joined with "/".
        withBase("/2", () => {
            expect(buildLoginUrl("/2/CTS/")).toBe("/2/login?ReturnUrl=%2F2%2FCTS%2F")
        })
    })

    it("collapses a base with duplicate trailing slashes", () => {
        withBase("/2///", () => {
            expect(buildLoginUrl("/2/CTS/")).toBe("/2/login?ReturnUrl=%2F2%2FCTS%2F")
        })
    })
})

describe("buildLoginUrl invalid return paths", () => {
    it("falls back to the application root instead of forwarding an off-site path", () => {
        withBase("/", () => {
            expect(buildLoginUrl("https://evil.example/x")).toBe("/login?ReturnUrl=%2F")
        })
    })

    it("falls back to the base-prefixed root in the subpath deployment", () => {
        withBase("/2/", () => {
            expect(buildLoginUrl("https://evil.example/x")).toBe("/2/login?ReturnUrl=%2F2%2F")
        })
    })
})

describe("buildLoginUrl return paths carrying a query string", () => {
    it("round-trips the query string (the CTS landing forwards location.search)", () => {
        withBase("/2/", () => {
            expect(buildLoginUrl("/2/CTS/?sendBackTo=/cts/epa")).toBe(
                "/2/login?ReturnUrl=%2F2%2FCTS%2F%3FsendBackTo%3D%2Fcts%2Fepa",
            )
        })
    })

    it("keeps a return path whose query carries encoded slashes", () => {
        // The encoded-bypass checks are scoped to the path, so an ordinary percent-encoded query
        // value survives. CtsHome forwards location.search verbatim, so this is the real shape of a
        // sendBackTo round trip; screening the whole string used to drop it and strand the user on
        // the app root after sign-in.
        withBase("/2/", () => {
            expect(buildLoginUrl("/2/CTS/?sendBackTo=%2Fcts%2Fepa")).toBe(
                "/2/login?ReturnUrl=%2F2%2FCTS%2F%3FsendBackTo%3D%252Fcts%252Fepa",
            )
        })
    })

    it("keeps a return path whose query carries encoded dots", () => {
        withBase("/2/", () => {
            expect(buildLoginUrl("/2/CTS/?q=%2e%2e")).toBe("/2/login?ReturnUrl=%2F2%2FCTS%2F%3Fq%3D%252e%252e")
        })
    })
})

describe("isValidInternalPath open-redirect guard", () => {
    // Encoded characters in a query value, and an encoded dot inside a larger segment, are ordinary.
    // A dot-segment only counts as traversal when it is the whole segment.
    it.each([
        ["a plain internal path", "/Effort"],
        ["a base-prefixed path", "/2/Effort"],
        ["a vue path", "/vue/app"],
        ["encoded slashes in the query", "/Effort/Reports?sendBackTo=%2Fcts%2Fepa"],
        ["encoded dots in the query", "/Effort/Reports?q=%2e%2e"],
        ["an encoded dot inside a longer segment", "/Effort/%2ename/x"],
        ["a literal dot inside a longer segment", "/Effort/..name/x"],
    ])("accepts %s", (_label, path) => {
        expect(isValidInternalPath(path)).toBeTruthy()
    })

    it.each([
        ["an absolute http URL", "https://evil.example/x"],
        ["a protocol-relative URL", "//evil.example/x"],
        ["a path traversal", "/Effort/../api/secret"],
        ["a trailing traversal segment", "/Effort/.."],
        ["a single-dot segment", "/./api/secret"],
        ["an encoded slash", "/Effort%2f..%2fapi"],
        ["an encoded dot", "/Effort/%2e%2e/api"],
        ["a mixed-encoding traversal", "/Effort/.%2e/api/secret"],
        ["an uppercase encoded traversal", "/Effort/%2E%2E/api/secret"],
        ["a traversal before the query", "/Effort/../api?tab=1"],
        ["a backslash escape", String.raw`/\evil.example`],
        ["a path with no leading slash", "Effort"],
        ["an empty path", ""],
    ])("rejects %s", (_label, path) => {
        expect(isValidInternalPath(path)).toBeFalsy()
    })
})
