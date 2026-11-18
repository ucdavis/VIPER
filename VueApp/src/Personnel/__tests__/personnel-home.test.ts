import { mount } from "@vue/test-utils"
import Home from "../pages/Home.vue"

/** The Personnel home page shows its CMS content; Personnel reports are in the Report Builder. */

describe("personnel home page", () => {
    it("shows the CMS content", () => {
        expect.hasAssertions()

        const wrapper = mount(Home, { global: { stubs: { CmsContent: true } } })

        expect(wrapper.findComponent({ name: "CmsContent" }).props("contentName")).toBe("personnel-home")
    })
})
