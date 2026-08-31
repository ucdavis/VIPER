import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import SectionJumpLinks from "../components/SectionJumpLinks.vue"
import type { JumpTarget } from "../components/SectionJumpLinks.vue"

const targets: JumpTarget[] = [
    { id: "phone-section-1", label: "Dean's Office" },
    { id: "phone-section-2", label: "Business Office" },
    { id: "phone-section-frequent-numbers", label: "Frequently Called Numbers" },
]

// Portals outlive their wrapper unless it is unmounted, and a leaked menu is indistinguishable from
// the one under test once queries run against the document rather than the wrapper.
const mounted: ReturnType<typeof mount>[] = []

function mountLinks(jumpTargets: JumpTarget[] = targets) {
    // Attached to the document, because the menu renders through a portal and so never appears
    // inside the wrapper.
    const wrapper = mount(SectionJumpLinks, {
        props: { targets: jumpTargets },
        global: { plugins: [Quasar] },
        attachTo: document.body,
    })
    mounted.push(wrapper)
    return wrapper
}

/** The links live behind the trigger, so every test that inspects them has to open it first. */
async function openMenu(jumpTargets: JumpTarget[] = targets) {
    const wrapper = mountLinks(jumpTargets)
    await wrapper.find("button").trigger("click")
    return wrapper
}

function menuLinks() {
    return [...document.querySelectorAll<HTMLAnchorElement>(".q-menu a")]
}

describe("sectionJumpLinks.vue", () => {
    afterEach(() => {
        for (const wrapper of mounted.splice(0)) {
            wrapper.unmount()
        }
        document.body.innerHTML = ""
    })

    it("offers a fragment link per target, in the order the sections appear", async () => {
        expect.hasAssertions()
        await openMenu()
        const links = menuLinks()

        expect(links.map((link) => link.getAttribute("href"))).toStrictEqual([
            "#phone-section-1",
            "#phone-section-2",
            "#phone-section-frequent-numbers",
        ])
        expect(links.map((link) => link.textContent?.trim())).toStrictEqual([
            "Dean's Office",
            "Business Office",
            "Frequently Called Numbers",
        ])
    })

    it("uses plain anchors, so the browser's own fragment navigation applies the scroll margin", async () => {
        expect.hasAssertions()
        const wrapper = await openMenu()

        // A router-link would position with window.scrollTo, which ignores scroll-margin-top and
        // would drop the heading under the sticky header and filter bar.
        expect(wrapper.findComponent({ name: "RouterLink" }).exists()).toBeFalsy()
    })

    it("closes itself once a link is followed, so it stops covering the section", async () => {
        expect.hasAssertions()
        await openMenu()

        expect(menuLinks()).not.toHaveLength(0)

        menuLinks()[0].click()

        // Open, the menu floats over the heading the browser is scrolling to. It is torn down on a
        // timer rather than during the click, which is what leaves the anchor connected long enough
        // for the browser to follow it.
        await vi.waitFor(() => {
            expect(menuLinks()).toHaveLength(0)
        })
    })

    it("keeps the menu out of the sticky bar's own flow, so the published height stays true", async () => {
        expect.hasAssertions()
        const wrapper = await openMenu()

        // The bar measures its own border box to publish --phone-list-filter-height. Anything the
        // menu rendered inside the component would be counted in that measurement and would change
        // it on open, which is what put jump targets under the bar in the first place.
        expect(wrapper.find(".q-menu").exists()).toBeFalsy()
        expect(document.querySelector(".q-menu")).not.toBeNull()
    })

    it("names the navigation landmark, since a page can hold more than one", async () => {
        expect.hasAssertions()
        await openMenu()

        expect(document.querySelector(".q-menu nav")?.getAttribute("aria-label")).toBe("Phone list sections")
    })

    it("renders nothing when a search has left only one section with results", () => {
        expect.hasAssertions()

        // Jumping to the only thing on screen is not navigation.
        expect(mountLinks([targets[0]]).find("button").exists()).toBeFalsy()
    })

    it("renders nothing when a search matches no section at all", () => {
        expect.hasAssertions()

        expect(mountLinks([]).find("button").exists()).toBeFalsy()
    })
})
