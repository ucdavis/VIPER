import { mount, flushPromises } from "@vue/test-utils"
import { Quasar, Loading } from "quasar"
import { createPinia, setActivePinia } from "pinia"
import { createRouter, createMemoryHistory } from "vue-router"
import CtsHome from "@/CTS/pages/CtsHome.vue"

/**
 * A sendBackTo round trip lands back on /CTS/ and redirects from there. Both /CTS/ and /CTS/Home
 * render this component and router-view carries no key, so a target on either one reuses the
 * instance rather than remounting it. These pin that the page still renders, since relying on
 * onMounted firing a second time left it blank.
 */
const loggedIn = {
    success: true,
    result: {
        firstName: "Abigail",
        lastName: "Hsu",
        mailId: "abh225",
        loginId: "abh225",
        mothraId: "1",
        userId: 42,
        token: "",
        emulating: false,
        permissions: ["SVMSecure.CTS.Students"],
    },
}

vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({ get: vi.fn<(...args: unknown[]) => unknown>().mockResolvedValue(loggedIn) }),
}))

const HOME_HEADING = "Competency Tracking System"

async function landOn(url: string) {
    const router = createRouter({
        history: createMemoryHistory(),
        routes: [
            { path: "/CTS/", name: "CtsAuth", component: CtsHome },
            { path: "/CTS/Home", name: "CtsHome", component: CtsHome },
            { path: "/CTS/EPA", name: "Epa", component: { template: "<div>EPA page</div>" } },
        ],
    })
    await router.push(url)
    await router.isReady()

    const wrapper = mount(
        { template: "<router-view />" },
        {
            global: {
                plugins: [router, [Quasar, { plugins: { Loading } }], createPinia()],
                provide: { apiURL: "/api/" },
            },
        },
    )
    // The mount starts the logged-in lookup, the redirect follows it, and the target page then
    // runs a lookup of its own, so the DOM needs several turns to settle.
    await flushPromises()
    await flushPromises()
    await flushPromises()
    await flushPromises()

    return { wrapper, router }
}

describe("sendBackTo landing on the CTS page", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.stubEnv("VITE_VIPER_HOME", "/")
    })

    afterEach(() => vi.unstubAllEnvs())

    it("renders the page when there is nothing to redirect to", async () => {
        const { wrapper, router } = await landOn("/CTS/")

        expect(router.currentRoute.value.fullPath).toBe("/CTS/")
        expect(wrapper.text()).toContain(HOME_HEADING)
    })

    it("renders the page when the target is this same route", async () => {
        const { wrapper, router } = await landOn("/CTS/?sendBackTo=/CTS/")

        expect(router.currentRoute.value.fullPath).toBe("/CTS/")
        expect(wrapper.text()).toContain(HOME_HEADING)
    })

    it("renders the page when the target is the other route sharing this component", async () => {
        const { wrapper, router } = await landOn("/CTS/?sendBackTo=/CTS/Home")

        expect(router.currentRoute.value.fullPath).toBe("/CTS/Home")
        expect(wrapper.text()).toContain(HOME_HEADING)
    })

    it("hands off to a target that renders a different component", async () => {
        const { wrapper, router } = await landOn("/CTS/?sendBackTo=/CTS/EPA")

        expect(router.currentRoute.value.fullPath).toBe("/CTS/EPA")
        expect(wrapper.text()).toContain("EPA page")
        expect(wrapper.text()).not.toContain(HOME_HEADING)
    })

    it("carries the target's own query through the redirect", async () => {
        const { router } = await landOn("/CTS/?sendBackTo=/CTS/EPA%3Fid%3D7")

        expect(router.currentRoute.value.fullPath).toBe("/CTS/EPA?id=7")
    })
})
