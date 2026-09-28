import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import StudentsHome from "../pages/StudentsHome.vue"
import { setAccessNotice, useAccessNotice } from "../composables/use-access-notice"

/**
 * Tests for the notice a Students route guard leaves when it turns a user away, and for Students
 * Home, where a user with no access to an app lands.
 */

const NOTICE = "You do not have permission to access Career Selection. Contact your administrator if you need access."

function mountHome() {
    return mount(StudentsHome, { global: { plugins: [[Quasar, {}]] } })
}

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
