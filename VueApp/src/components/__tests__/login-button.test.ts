import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import { reactive } from "vue"
import LoginButton from "@/components/LoginButton.vue"

/**
 * LoginButton exists so every re-authentication prompt renders the same action. These tests pin the
 * properties that used to drift between the four call sites: the label and its casing, the colour,
 * the same-tab behaviour, and the href coming from the shared builder rather than a hand-rolled URL.
 */
const route = reactive({ fullPath: "/CTS/Home" })

vi.mock("vue-router", () => ({
    useRoute: () => route,
    useRouter: () => ({ push: () => Promise.resolve() }),
}))

function mountButton(props: Record<string, unknown> = {}) {
    return mount(LoginButton, { props, global: { plugins: [[Quasar, {}]] } })
}

function withBase(base: string, run: () => void): void {
    vi.stubEnv("VITE_VIPER_HOME", base)
    try {
        run()
    } finally {
        vi.unstubAllEnvs()
    }
}

describe("LoginButton", () => {
    it("renders the label as written rather than upper-cased", () => {
        // Quasar upper-cases labels through CSS, so the text reads "Log In" either way and only
        // the class distinguishes them. The session card used to render "LOG IN" for want of it.
        withBase("/", () => {
            const wrapper = mountButton()

            expect(wrapper.text()).toBe("Log In")
            expect(wrapper.get("a").classes()).toContain("q-btn--no-uppercase")
        })
    })

    it("uses the primary colour", () => {
        withBase("/", () => {
            expect(mountButton().find(".bg-primary").exists()).toBeTruthy()
        })
    })

    it("takes its href from the shared login-URL builder", () => {
        route.fullPath = "/CTS/Home"

        withBase("/", () => {
            expect(mountButton().get("a").attributes("href")).toBe("/login?ReturnUrl=%2FCTS%2FHome")
        })
    })

    it("prefixes the application base under the subpath deployment", () => {
        route.fullPath = "/CTS/Home"

        withBase("/2/", () => {
            expect(mountButton().get("a").attributes("href")).toBe("/2/login?ReturnUrl=%2F2%2FCTS%2FHome")
        })
    })

    it("never opens in a new tab", () => {
        // Re-authenticating in a new tab leaves the user signed in elsewhere while this tab
        // goes stale, which is what the old error-banner link did.
        withBase("/", () => {
            expect(mountButton().get("a").attributes("target")).toBeUndefined()
        })
    })

    it("passes caller attributes through to the button", () => {
        withBase("/", () => {
            const wrapper = mountButton({ dense: true, class: "q-px-md" })

            expect(wrapper.get("a").classes()).toContain("q-px-md")
            expect(wrapper.get("a").classes()).toContain("q-btn--dense")
        })
    })
})
