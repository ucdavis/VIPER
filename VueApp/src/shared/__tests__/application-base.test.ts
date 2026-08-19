import { applicationBase, stripApplicationBase } from "@/shared/application-base"

// The base is read from import.meta.env at call time, so each test stubs the base it needs.
function withBase(base: string, run: () => void): void {
    vi.stubEnv("VITE_VIPER_HOME", base)
    try {
        run()
    } finally {
        vi.unstubAllEnvs()
    }
}

describe("applicationBase()", () => {
    it.each([
        ["the domain root", "/", ""],
        ["a subpath deployment", "/2/", "/2"],
        ["a base configured without a trailing slash", "/2", "/2"],
        ["a base with duplicate trailing slashes", "/2///", "/2"],
    ])("normalizes %s for concatenation", (_label, configured, expected) => {
        withBase(configured, () => {
            expect(applicationBase()).toBe(expected)
        })
    })
})

describe("stripApplicationBase()", () => {
    it("drops the base so the history layer does not double it", () => {
        withBase("/2/", () => {
            expect(stripApplicationBase("/2/CTS/Home")).toBe("/CTS/Home")
        })
    })

    it("leaves a path that does not carry the base", () => {
        withBase("/2/", () => {
            expect(stripApplicationBase("/CTS/Home")).toBe("/CTS/Home")
        })
    })

    it("maps the base on its own to the application root", () => {
        // "/2" addresses the app root; returning it unchanged would resolve to "/2/2".
        withBase("/2/", () => {
            expect(stripApplicationBase("/2")).toBe("/")
        })
    })

    it("does not strip a path that merely starts with the same characters", () => {
        // "/2fa" is not under the "/2" base; only a full segment match counts.
        withBase("/2/", () => {
            expect(stripApplicationBase("/2fa/setup")).toBe("/2fa/setup")
        })
    })

    it("is a no-op at the domain root", () => {
        withBase("/", () => {
            expect(stripApplicationBase("/CTS/Home")).toBe("/CTS/Home")
        })
    })

    it("round-trips with applicationBase", () => {
        withBase("/2/", () => {
            expect(stripApplicationBase(`${applicationBase()}/CTS/Home`)).toBe("/CTS/Home")
        })
    })
})
