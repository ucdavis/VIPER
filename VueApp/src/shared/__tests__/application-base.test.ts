import { applicationBase } from "../application-base"

// The base is read from import.meta.env at call time, so each test stubs the value it needs.
function withBase(base: string, run: () => void): void {
    vi.stubEnv("VITE_VIPER_HOME", base)
    try {
        run()
    } finally {
        vi.unstubAllEnvs()
    }
}

describe("applicationBase()", () => {
    it("is the empty string at the domain root, so callers supply their own separator", () => {
        withBase("/", () => {
            expect(applicationBase()).toBe("")
        })
    })

    it("keeps the subpath base without its trailing slash", () => {
        withBase("/2/", () => {
            expect(applicationBase()).toBe("/2")
        })
    })

    it("leaves a base already free of trailing slashes unchanged", () => {
        withBase("/2", () => {
            expect(applicationBase()).toBe("/2")
        })
    })

    it("collapses a base misconfigured with duplicate trailing slashes", () => {
        withBase("/2///", () => {
            expect(applicationBase()).toBe("/2")
        })
    })
})
