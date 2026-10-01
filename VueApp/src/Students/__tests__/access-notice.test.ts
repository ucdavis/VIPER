import { enableAutoUnmount, mount, flushPromises } from "@vue/test-utils"
import { Quasar } from "quasar"
import { createPinia, setActivePinia } from "pinia"
import { createMemoryHistory, createRouter, RouterView } from "vue-router"
import { useUserStore } from "@/store/UserStore"
import { careerSelectionGuards } from "@/Students/CareerSelection/router/career-selection-guards"
import StudentsHome from "../pages/StudentsHome.vue"
import { redeliverAccessNotice, setAccessNotice, useAccessNotice } from "../composables/use-access-notice"

/**
 * Tests for the notice a Students route guard leaves when it turns a user away, and for Students
 * Home, where a user with no access to an app lands.
 */

const NOTICE = "You do not have permission to access Career Selection. Contact your administrator if you need access."

function mountHome() {
    return mount(StudentsHome, { global: { plugins: [[Quasar, {}]] } })
}

// A page left mounted keeps watching for redelivered notices and would take one meant for a
// later test's page; in the app, leaving a page unmounts it.
enableAutoUnmount(afterEach)

// The notice is held between navigations; start each test with none pending.
beforeEach(() => {
    useAccessNotice()
})

describe("access notice", () => {
    it("hands a notice to the first page that takes it", () => {
        expect.hasAssertions()
        setAccessNotice(NOTICE)

        expect(useAccessNotice().value).toBe(NOTICE)
    })

    it("hands it over only once, so the next page does not repeat it", () => {
        expect.hasAssertions()
        setAccessNotice(NOTICE)
        useAccessNotice()

        expect(useAccessNotice().value).toBeNull()
    })

    it("has nothing to hand over when no guard left a notice", () => {
        expect.hasAssertions()
        expect(useAccessNotice().value).toBeNull()
    })
})

describe("students home", () => {
    it("says why the user was sent here", () => {
        expect.hasAssertions()
        setAccessNotice(NOTICE)

        const wrapper = mountHome()

        expect(wrapper.text()).toContain(NOTICE)
    })

    it("shows no notice when the user came here on their own", () => {
        expect.hasAssertions()
        const wrapper = mountHome()

        expect(wrapper.find('[role="status"]').exists()).toBeFalsy()
    })

    it("does not show the notice again on the next visit", () => {
        expect.hasAssertions()
        setAccessNotice(NOTICE)
        mountHome().unmount()

        expect(mountHome().text()).not.toContain(NOTICE)
    })
})

describe("turned away while already on students home", () => {
    // A guard redirecting to the page already on screen ends the navigation as a duplicate, so
    // the page does not set up again; the router's afterEach hook has to hand it the notice.
    async function mountRouterOnHome() {
        setActivePinia(createPinia())
        const userStore = useUserStore()
        userStore.userInfo.userId = 100
        // Students access, but none to Career Selection.
        userStore.setPermissions(["SVMSecure.Students"])

        const router = createRouter({
            history: createMemoryHistory(),
            routes: [
                { path: "/Students/", name: "StudentsHome", component: StudentsHome },
                {
                    path: "/Students/CareerSelection/",
                    name: "CareerSelectionList",
                    beforeEnter: careerSelectionGuards.list,
                    component: { template: "<div>Career Selection</div>" },
                },
            ],
        })
        router.afterEach(redeliverAccessNotice)

        await router.push("/Students/")
        const wrapper = mount(RouterView, { global: { plugins: [router, [Quasar, {}]] } })
        await flushPromises()
        return { router, wrapper }
    }

    it("shows the notice on the page already on screen", async () => {
        expect.hasAssertions()
        const { router, wrapper } = await mountRouterOnHome()
        expect(wrapper.text()).not.toContain(NOTICE)

        await router.push("/Students/CareerSelection/")
        await flushPromises()

        expect(router.currentRoute.value.name).toBe("StudentsHome")
        expect(wrapper.text()).toContain(NOTICE)
    })

    it("leaves nothing pending for a later visit", async () => {
        expect.hasAssertions()
        const { router } = await mountRouterOnHome()

        await router.push("/Students/CareerSelection/")
        await flushPromises()

        expect(useAccessNotice().value).toBeNull()
    })
})
