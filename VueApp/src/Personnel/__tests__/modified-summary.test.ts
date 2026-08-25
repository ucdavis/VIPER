import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import ModifiedSummary from "../components/ModifiedSummary.vue"

/**
 * ModifiedSummary renders the "<label> Modified <date> by <person>" footer both record dialogs
 * show in edit mode. The two absent cases are what it exists to get right: a record nobody has
 * touched, and one whose author was never recorded.
 */

function mountSummary(props: { label: string; date: Date | string | null; by: string | null }) {
    return mount(ModifiedSummary, {
        props,
        global: { plugins: [Quasar] },
    })
}

describe("modifiedSummary.vue", () => {
    it("shows the formatted date and the person who made the change", () => {
        expect.hasAssertions()

        const wrapper = mountSummary({
            label: "Dean/Director",
            date: new Date(2026, 5, 1),
            by: "jdoe",
        })

        expect(wrapper.text()).toContain("Dean/Director Modified")
        expect(wrapper.text()).toContain("by jdoe")
        expect(wrapper.text()).not.toContain("Never")
    })

    it("accepts the ISO string form the API actually delivers", () => {
        expect.hasAssertions()
        // The record models type these as Date, but JSON hands over a string, so the component
        // has to render the same either way.
        const wrapper = mountSummary({
            label: "Dean/Director",
            date: "2026-06-01T12:30:00",
            by: "jdoe",
        })

        expect(wrapper.text()).not.toContain("Never")
    })

    it("reads as Never when the record has no modified date", () => {
        expect.hasAssertions()

        const wrapper = mountSummary({ label: "Admin Staff", date: null, by: "jdoe" })

        expect(wrapper.text()).toContain("Admin Staff Modified Never")
    })

    it("omits the by clause when no author was recorded", () => {
        expect.hasAssertions()
        // Legacy rows carry a date but no author, and "by" on its own would read as an
        // unfinished sentence.
        const wrapper = mountSummary({ label: "Admin Staff", date: new Date(2026, 5, 1), by: null })

        expect(wrapper.text()).not.toContain("by")
    })
})
