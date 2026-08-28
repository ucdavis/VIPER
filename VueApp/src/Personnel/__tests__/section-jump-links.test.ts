import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import SectionJumpLinks from "../components/SectionJumpLinks.vue"
import type { JumpTarget } from "../components/SectionJumpLinks.vue"

const targets: JumpTarget[] = [
    { id: "phone-section-1", label: "Dean's Office" },
    { id: "phone-section-2", label: "Business Office" },
    { id: "phone-section-frequent-numbers", label: "Frequently Called Numbers" },
]

function mountLinks(jumpTargets: JumpTarget[] = targets) {
    return mount(SectionJumpLinks, {
        props: { targets: jumpTargets },
        global: { plugins: [Quasar] },
    })
}

describe("sectionJumpLinks.vue", () => {
    it("offers a fragment link per target, in the order the sections appear", () => {
        expect.hasAssertions()
        const links = mountLinks().findAll("a")

        expect(links.map((link) => link.attributes("href"))).toStrictEqual([
            "#phone-section-1",
            "#phone-section-2",
            "#phone-section-frequent-numbers",
        ])
        expect(links.map((link) => link.text())).toStrictEqual([
            "Dean's Office",
            "Business Office",
            "Frequently Called Numbers",
        ])
    })

    it("uses plain anchors, so the browser's own fragment navigation applies the scroll margin", () => {
        expect.hasAssertions()

        // A router-link would position with window.scrollTo, which ignores scroll-margin-top and
        // would drop the heading under the sticky header and filter bar.
        expect(mountLinks().findComponent({ name: "RouterLink" }).exists()).toBeFalsy()
    })

    it("names the navigation landmark, since a page can hold more than one", () => {
        expect.hasAssertions()

        expect(mountLinks().find("nav").attributes("aria-label")).toBe("Phone list sections")
    })

    it("renders nothing when a search has left only one section with results", () => {
        expect.hasAssertions()

        // Jumping to the only thing on screen is not navigation.
        expect(mountLinks([targets[0]]).find("a").exists()).toBeFalsy()
    })

    it("renders nothing when a search matches no section at all", () => {
        expect.hasAssertions()

        expect(mountLinks([]).find("a").exists()).toBeFalsy()
    })
})
