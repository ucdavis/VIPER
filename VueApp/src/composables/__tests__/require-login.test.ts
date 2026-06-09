import { buildLoginUrl, isValidInternalPath, resolveSendBackTo } from "@/composables/RequireLogin"

// The app base is read from import.meta.env at call time, so each test stubs the base it needs.
function withBase(base: string, run: () => void): void {
    vi.stubEnv("VITE_VIPER_HOME", base)
    try {
        run()
    } finally {
        vi.unstubAllEnvs()
    }
}

describe("buildLoginUrl endpoint selection", () => {
    it("defaults to the welcome splash", () => {
        withBase("/", () => {
            expect(buildLoginUrl("/Effort")).toBe("/welcome?ReturnUrl=%2FEffort")
        })
    })

    it("goes straight to CAS when the login endpoint is requested", () => {
        // Explicit "Log in" buttons skip the splash so a deliberate click isn't met
        // with a second sign-in screen.
        withBase("/", () => {
            expect(buildLoginUrl("/Effort", "login")).toBe("/login?ReturnUrl=%2FEffort")
        })
    })
})

describe("buildLoginUrl application base handling", () => {
    it("emits root-relative paths when the app is served at the domain root", () => {
        withBase("/", () => {
            expect(buildLoginUrl("/CTS/")).toBe("/welcome?ReturnUrl=%2FCTS%2F")
        })
    })

    it("keeps the /2 base for the subpath deployment", () => {
        // TEST/PROD run VIPER 2 under a "/2" PathBase; dropping it escapes to the legacy site.
        withBase("/2/", () => {
            expect(buildLoginUrl("/2/CTS/")).toBe("/2/welcome?ReturnUrl=%2F2%2FCTS%2F")
            expect(buildLoginUrl("/2/CTS/", "login")).toBe("/2/login?ReturnUrl=%2F2%2FCTS%2F")
        })
    })

    it("inserts the separator when the base is configured without a trailing slash", () => {
        // Guards the "/2welcome" regression: the base is normalized, then joined with "/".
        withBase("/2", () => {
            expect(buildLoginUrl("/2/CTS/")).toBe("/2/welcome?ReturnUrl=%2F2%2FCTS%2F")
        })
    })

    it("collapses a base with duplicate trailing slashes", () => {
        withBase("/2///", () => {
            expect(buildLoginUrl("/2/CTS/")).toBe("/2/welcome?ReturnUrl=%2F2%2FCTS%2F")
        })
    })
})

describe("buildLoginUrl invalid return paths", () => {
    it("falls back to the application root instead of forwarding an off-site path", () => {
        withBase("/", () => {
            expect(buildLoginUrl("https://evil.example/x")).toBe("/welcome?ReturnUrl=%2F")
        })
    })

    it("falls back to the base-prefixed root in the subpath deployment", () => {
        withBase("/2/", () => {
            expect(buildLoginUrl("https://evil.example/x")).toBe("/2/welcome?ReturnUrl=%2F2%2F")
        })
    })
})

describe("buildLoginUrl return paths carrying a query string", () => {
    it("round-trips the query string (the CTS landing forwards location.search)", () => {
        withBase("/2/", () => {
            expect(buildLoginUrl("/2/CTS/?sendBackTo=/cts/epa")).toBe(
                "/2/welcome?ReturnUrl=%2F2%2FCTS%2F%3FsendBackTo%3D%2Fcts%2Fepa",
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
                "/2/welcome?ReturnUrl=%2F2%2FCTS%2F%3FsendBackTo%3D%252Fcts%252Fepa",
            )
        })
    })

    it("keeps a return path whose query carries encoded dots", () => {
        withBase("/2/", () => {
            expect(buildLoginUrl("/2/CTS/?q=%2e%2e")).toBe("/2/welcome?ReturnUrl=%2F2%2FCTS%2F%3Fq%3D%252e%252e")
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

type Route = Parameters<typeof resolveSendBackTo>[0]

function routeWith(sendBackTo?: string | string[]): Route {
    return { query: sendBackTo === undefined ? {} : { sendBackTo } } as unknown as Route
}

describe("resolveSendBackTo - routing", () => {
    it("returns nothing when the query has no sendBackTo", () => {
        expect(resolveSendBackTo(routeWith())).toBeNull()
    })

    it("resolves an internal path", () => {
        expect(resolveSendBackTo(routeWith("/CTS/MyAssessments"))).toStrictEqual({
            path: "/CTS/MyAssessments",
            query: {},
            hash: "",
        })
    })

    it("parses the query string of the deep link into route query params", () => {
        expect(resolveSendBackTo(routeWith("/CTS/StudentAssessments?studentId=42&term=202601"))).toStrictEqual({
            path: "/CTS/StudentAssessments",
            query: { studentId: "42", term: "202601" },
            hash: "",
        })
    })

    it("keeps a second question mark inside the query value", () => {
        expect(resolveSendBackTo(routeWith("/CTS/Home?q=a?b"))?.query).toStrictEqual({ q: "a?b" })
    })
})

describe("resolveSendBackTo - fragments and the app base", () => {
    it("keeps a fragment out of the query and returns it as the hash", () => {
        expect(resolveSendBackTo(routeWith("/CTS/Home?tab=details#history"))).toStrictEqual({
            path: "/CTS/Home",
            query: { tab: "details" },
            hash: "#history",
        })
    })

    it("returns a fragment with no query string", () => {
        expect(resolveSendBackTo(routeWith("/CTS/Home#history"))).toStrictEqual({
            path: "/CTS/Home",
            query: {},
            hash: "#history",
        })
    })

    it("sheds the application base so the history layer does not double it", () => {
        // Routes are declared without the base; "/2/CTS/Home" under a "/2" base would resolve to
        // "/2/2/CTS/Home" and match no route.
        withBase("/2/", () => {
            expect(resolveSendBackTo(routeWith("/2/CTS/Home?tab=details"))?.path).toBe("/CTS/Home")
        })
    })

    it("leaves a path alone when it does not carry the base", () => {
        withBase("/2/", () => {
            expect(resolveSendBackTo(routeWith("/CTS/Home"))?.path).toBe("/CTS/Home")
        })
    })
})

describe("resolveSendBackTo - repeated parameter", () => {
    it("uses the first value instead of splicing repeats into one comma-joined path", () => {
        expect(resolveSendBackTo(routeWith(["/CTS/Home", "/CTS/MyAssessments"]))?.path).toBe("/CTS/Home")
    })

    it("rejects a repeat whose first value is not an internal path", () => {
        expect(resolveSendBackTo(routeWith(["https://evil.example", "/CTS/Home"]))).toBeNull()
    })
})

describe("resolveSendBackTo - rejected targets", () => {
    it.each([
        ["an absolute URL", "https://evil.example/steal"],
        ["a protocol-relative URL", "//evil.example/steal"],
        ["path traversal", "/CTS/../../evil"],
        ["an encoded slash", "%2fevil.example"],
        ["an encoded dot", "/CTS/%2e%2e/evil"],
        ["a relative path outside the allow list", "CTS/Home"],
        ["an empty value", ""],
    ])("refuses to resolve %s", (_label, sendBackTo) => {
        expect(resolveSendBackTo(routeWith(sendBackTo))).toBeNull()
    })
})

describe("resolveSendBackTo - prototype safety", () => {
    it("keeps a __proto__ param as plain data instead of mutating the prototype chain", () => {
        const query = resolveSendBackTo(routeWith("/CTS/Home?__proto__=polluted"))?.query ?? {}

        expect(Object.hasOwn(query, "__proto__")).toBeTruthy()
        expect(Object.getPrototypeOf(query)).toBe(Object.prototype)
        expect(({} as Record<string, unknown>).polluted).toBeUndefined()
    })
})
