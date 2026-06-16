// oxlint-disable no-script-url -- a javascript: URL is the unsafe input under test here
import LinkComponent from "@/CMS/components/Link.vue"
import type { Link, LinkCollection } from "@/CMS/types"
import { mountCms } from "./test-utils"

/**
 * Link.vue renders a single collection link. The two behaviors worth locking down are:
 * unsafe URLs degrade to plain text (no href), and tag badges color-cycle through a
 * fixed palette keyed by each tag category's 1-based sortOrder.
 */

function makeCollection(sortOrders: number[]): LinkCollection {
    return {
        linkCollectionId: 1,
        linkCollection: "Resources",
        linkCollectionTagCategories: sortOrders.map((sortOrder, i) => ({
            linkCollectionTagCategoryId: i + 1,
            linkCollectionTagCategory: `Category ${i + 1}`,
            sortOrder,
        })),
    }
}

function makeLink(overrides: Partial<Link> = {}): Link {
    return {
        linkId: 10,
        url: "https://example.com",
        title: "Example",
        description: "An example link",
        order: 1,
        linkTags: [],
        ...overrides,
    }
}

describe("Link.vue - URL safety", () => {
    it("renders an anchor with the safe href for an http(s) URL", () => {
        const wrapper = mountCms(LinkComponent, {
            props: { link: makeLink({ url: "https://example.com/page" }), linkCollection: makeCollection([]) },
        })
        const anchor = wrapper.find("a")
        expect(anchor.exists()).toBeTruthy()
        expect(anchor.attributes("href")).toBe("https://example.com/page")
        expect(anchor.attributes("target")).toBe("_blank")
        expect(anchor.attributes("rel")).toBe("noopener noreferrer")
    })

    it("renders the title as plain text (no anchor) for a javascript: URL", () => {
        const wrapper = mountCms(LinkComponent, {
            props: {
                link: makeLink({ url: "javascript:alert(1)", title: "Sketchy" }),
                linkCollection: makeCollection([]),
            },
        })
        expect(wrapper.find("a").exists()).toBeFalsy()
        expect(wrapper.text()).toContain("Sketchy")
    })

    it("renders plain text for an empty URL (safeHref returns '#')", () => {
        const wrapper = mountCms(LinkComponent, {
            props: { link: makeLink({ url: "" }), linkCollection: makeCollection([]) },
        })
        expect(wrapper.find("a").exists()).toBeFalsy()
    })
})

describe("Link.vue - tag badge color cycling", () => {
    // The palette has 6 entries; the component maps sortOrder n -> index (n-1) % 6.
    // sortOrder 1 -> warning/dark, 2 -> secondary/white, 7 -> wraps back to warning/dark.
    function mountWithTag(categorySortOrder: number) {
        const categoryId = 1
        const collection: LinkCollection = {
            linkCollectionId: 1,
            linkCollection: "Resources",
            linkCollectionTagCategories: [
                {
                    linkCollectionTagCategoryId: categoryId,
                    linkCollectionTagCategory: "Cat",
                    sortOrder: categorySortOrder,
                },
            ],
        }
        const link = makeLink({
            linkTags: [
                { linkTagId: 1, linkId: 10, linkCollectionTagCategoryId: categoryId, sortOrder: 1, value: "TagValue" },
            ],
        })
        return mountCms(LinkComponent, { props: { link, linkCollection: collection } })
    }

    it("uses the first palette entry (warning) for sortOrder 1", () => {
        const wrapper = mountWithTag(1)
        const badge = wrapper.findComponent({ name: "QBadge" })
        expect(badge.exists()).toBeTruthy()
        expect(badge.props("color")).toBe("warning")
        expect(badge.props("textColor")).toBe("dark")
        expect(badge.text()).toContain("TagValue")
    })

    it("uses the second palette entry (secondary) for sortOrder 2", () => {
        const badge = mountWithTag(2).findComponent({ name: "QBadge" })
        expect(badge.props("color")).toBe("secondary")
        expect(badge.props("textColor")).toBe("white")
    })

    it("wraps modulo the palette length so sortOrder 7 matches sortOrder 1", () => {
        const badge = mountWithTag(7).findComponent({ name: "QBadge" })
        expect(badge.props("color")).toBe("warning")
        expect(badge.props("textColor")).toBe("dark")
    })

    it("falls back to the first entry for a non-positive sortOrder", () => {
        const badge = mountWithTag(0).findComponent({ name: "QBadge" })
        expect(badge.props("color")).toBe("warning")
    })

    it("only renders badges for tags whose category matches the collection category", () => {
        const collection: LinkCollection = {
            linkCollectionId: 1,
            linkCollection: "Resources",
            linkCollectionTagCategories: [
                { linkCollectionTagCategoryId: 1, linkCollectionTagCategory: "Cat1", sortOrder: 1 },
            ],
        }
        const link = makeLink({
            linkTags: [
                { linkTagId: 1, linkId: 10, linkCollectionTagCategoryId: 1, sortOrder: 1, value: "Shown" },
                { linkTagId: 2, linkId: 10, linkCollectionTagCategoryId: 99, sortOrder: 1, value: "Hidden" },
            ],
        })
        const wrapper = mountCms(LinkComponent, { props: { link, linkCollection: collection } })
        const badges = wrapper.findAllComponents({ name: "QBadge" })
        expect(badges).toHaveLength(1)
        expect(badges[0]!.text()).toContain("Shown")
    })
})
