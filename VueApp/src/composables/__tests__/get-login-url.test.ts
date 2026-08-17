import { reactive } from "vue"
import { getLoginUrl } from "@/composables/RequireLogin"

// getLoginUrl reads the current route, so stand in a reactive one: the point of the composable is
// that the URL recomputes as the user navigates.
const route = reactive({ fullPath: "/CTS/Home" })

vi.mock("vue-router", () => ({
    useRoute: () => route,
    useRouter: () => ({ push: () => Promise.resolve() }),
}))

function withBase(base: string, run: () => void): void {
    vi.stubEnv("VITE_VIPER_HOME", base)
    try {
        run()
    } finally {
        vi.unstubAllEnvs()
    }
}

describe("getLoginUrl()", () => {
    it("builds a login URL that returns to the current route", () => {
        route.fullPath = "/CTS/Home"

        withBase("/", () => {
            expect(getLoginUrl().value).toBe("/login?ReturnUrl=%2FCTS%2FHome")
        })
    })

    it("prefixes the application base, which fullPath omits", () => {
        route.fullPath = "/CTS/Home"

        withBase("/2/", () => {
            expect(getLoginUrl().value).toBe("/2/login?ReturnUrl=%2F2%2FCTS%2FHome")
        })
    })

    it("recomputes when the route changes", () => {
        // The regression this guards: the return path used to be read through an always-true
        // condition that never re-evaluated, so the link kept pointing at the first route seen.
        route.fullPath = "/CTS/Home"

        withBase("/", () => {
            const loginUrl = getLoginUrl()
            expect(loginUrl.value).toBe("/login?ReturnUrl=%2FCTS%2FHome")

            route.fullPath = "/CTS/MyAssessments?tab=epa"

            expect(loginUrl.value).toBe("/login?ReturnUrl=%2FCTS%2FMyAssessments%3Ftab%3Depa")
        })
    })

    it("falls back to the app root when the current route is not a valid internal path", () => {
        route.fullPath = "//evil.example/steal"

        withBase("/", () => {
            expect(getLoginUrl().value).toBe("/login?ReturnUrl=%2F")
        })
    })
})
