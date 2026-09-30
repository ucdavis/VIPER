import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import PhoneListFilter from "../components/PhoneListFilter.vue"

function mountFilter(search = "", noMatches?: boolean) {
    return mount(PhoneListFilter, {
        props: { modelValue: search, noMatches },
        global: { plugins: [Quasar] },
    })
}

describe("phoneListFilter.vue", () => {
    it("reports what the user types back to the page", async () => {
        expect.hasAssertions()
        const wrapper = mountFilter()

        await wrapper.findComponent({ name: "QInput" }).setValue("pharmacy")

        expect(wrapper.emitted("update:modelValue")?.at(-1)).toStrictEqual(["pharmacy"])
    })

    it("names the field with a real label rather than a placeholder", () => {
        expect.hasAssertions()
        const input = mountFilter().findComponent({ name: "QInput" })

        // QInput copies the label prop to the native input's aria-label, so this is the
        // accessible name as well as the visible one. A placeholder would be neither reliably.
        expect(input.props("label")).toBe("Filter Results")
        expect(input.props("placeholder")).toBeUndefined()
    })

    it("keeps the debounce, so filtering does not run on every keystroke", () => {
        expect.hasAssertions()

        expect(mountFilter().findComponent({ name: "QInput" }).props("debounce")).toBe("300")
    })

    it("carries the class the sticky rule is keyed to", () => {
        expect.hasAssertions()

        // The pinning itself is a media query, which jsdom does not apply; this only guards the
        // hook it is attached to. Whether it looks right on a phone still needs a real browser.
        expect(mountFilter().find(".phone-list-filter").exists()).toBeTruthy()
    })

    it("publishes its own height, which the scroll-margin offset is calculated from", () => {
        expect.hasAssertions()
        document.documentElement.style.removeProperty("--phone-list-filter-height")

        mountFilter()

        // Jsdom reports 0 for every measurement, so this guards that the variable is published at
        // all, not what it comes to. The value itself is only meaningful in a real browser.
        expect(document.documentElement.style.getPropertyValue("--phone-list-filter-height")).toBe("0px")
    })
})

describe("phoneListFilter.vue - no matches line", () => {
    it("names the search that matched nothing", () => {
        expect.hasAssertions()

        expect(mountFilter("smith", true).find("[role='status']").text()).toBe('No records match "smith".')
    })

    it("says nothing while the search has matches", () => {
        expect.hasAssertions()

        expect(mountFilter("smith", false).find("[role='status']").text()).toBe("")
    })

    it("says nothing when the page does not set it, as the maintain pages do not", () => {
        expect.hasAssertions()

        expect(mountFilter("smith").find("[role='status']").text()).toBe("")
    })

    it("keeps the live region rendered while it is silent, so its first message is announced", () => {
        expect.hasAssertions()

        expect(mountFilter("smith", false).find("[role='status']").exists()).toBeTruthy()
    })
})
