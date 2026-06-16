import LinkCollections from "@/CMS/components/LinkCollections.vue"
import type { Link, LinkCollection } from "@/CMS/types"
import { mountCms, flushPromises } from "./test-utils"

/**
 * LinkCollections is the most logic-dense CMS display component: it loads a collection and
 * its links, derives tag-category filters, then filters (search across title/description/tags,
 * case-insensitive; plus per-category tag select) and optionally groups links by a category.
 * These tests mock ViperFetch to seed deterministic data and assert the filter/group output.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        createUrlSearchParams: (obj: Record<string, string | number | null | undefined>) => {
            const params = new URLSearchParams()
            for (const [k, v] of Object.entries(obj)) {
                if (v !== null && v !== undefined) {
                    params.append(k, v.toString())
                }
            }
            return params
        },
    }),
}))

const TYPE_CATEGORY_ID = 1
const SUBJECT_CATEGORY_ID = 2

const collection: LinkCollection = {
    linkCollectionId: 7,
    linkCollection: "Resources",
    linkCollectionTagCategories: [
        { linkCollectionTagCategoryId: TYPE_CATEGORY_ID, linkCollectionTagCategory: "Type", sortOrder: 1 },
        { linkCollectionTagCategoryId: SUBJECT_CATEGORY_ID, linkCollectionTagCategory: "Subject", sortOrder: 2 },
    ],
}

function tag(categoryId: number, value: string) {
    return {
        linkTagId: Math.floor(Math.random() * 100_000),
        linkId: 0,
        linkCollectionTagCategoryId: categoryId,
        sortOrder: 1,
        value,
    }
}

const links: Link[] = [
    {
        linkId: 1,
        url: "https://a.example.com",
        title: "Anatomy Guide",
        description: "Bones and muscles",
        order: 1,
        linkTags: [tag(TYPE_CATEGORY_ID, "PDF"), tag(SUBJECT_CATEGORY_ID, "Anatomy")],
    },
    {
        linkId: 2,
        url: "https://b.example.com",
        title: "Pharmacology Notes",
        description: "Drug interactions",
        order: 2,
        linkTags: [tag(TYPE_CATEGORY_ID, "Doc"), tag(SUBJECT_CATEGORY_ID, "Pharmacology")],
    },
    {
        linkId: 3,
        url: "https://c.example.com",
        title: "Anatomy Atlas",
        description: "Detailed plates",
        order: 3,
        linkTags: [tag(TYPE_CATEGORY_ID, "PDF"), tag(SUBJECT_CATEGORY_ID, "Anatomy")],
    },
]

// First get() returns the collection wrapped in an array (the component reads result[0]);
// the second returns the link list.
function primeFetch(linkList: Link[] = links) {
    mockGet.mockReset()
    mockGet
        .mockResolvedValueOnce({ success: true, result: [collection] })
        .mockResolvedValueOnce({ success: true, result: linkList })
}

async function mountLoaded(props: { linkCollectionName: string; groupByTagCategory?: string | null }) {
    const wrapper = mountCms(LinkCollections, { props })
    await flushPromises()
    await flushPromises()
    return wrapper
}

function linkCardTitles(wrapper: ReturnType<typeof mountCms>): string[] {
    return wrapper.findAllComponents({ name: "Link" }).map((c) => (c.props("link") as Link).title)
}

describe("LinkCollections.vue - loading", () => {
    beforeEach(() => primeFetch())

    it("requests the collection by name then loads its links", async () => {
        await mountLoaded({ linkCollectionName: "Resources" })
        expect(mockGet).toHaveBeenCalledTimes(2)
        expect(mockGet.mock.calls[0]![0]).toContain("linkCollectionName=Resources")
        expect(mockGet.mock.calls[1]![0]).toContain(`/${collection.linkCollectionId}/links`)
    })

    it("renders one Link card per loaded link when no filter is active", async () => {
        const wrapper = await mountLoaded({ linkCollectionName: "Resources" })
        expect(linkCardTitles(wrapper)).toHaveLength(3)
    })
})

describe("LinkCollections.vue - search filter", () => {
    beforeEach(() => primeFetch())

    it("filters by title case-insensitively", async () => {
        const wrapper = await mountLoaded({ linkCollectionName: "Resources" })
        const vm = wrapper.vm as unknown as { search: string }
        vm.search = "anatomy"
        await flushPromises()
        expect(linkCardTitles(wrapper).toSorted()).toEqual(["Anatomy Atlas", "Anatomy Guide"])
    })

    it("matches on description text", async () => {
        const wrapper = await mountLoaded({ linkCollectionName: "Resources" })
        const vm = wrapper.vm as unknown as { search: string }
        vm.search = "interactions"
        await flushPromises()
        expect(linkCardTitles(wrapper)).toEqual(["Pharmacology Notes"])
    })

    it("matches on a tag value", async () => {
        const wrapper = await mountLoaded({ linkCollectionName: "Resources" })
        const vm = wrapper.vm as unknown as { search: string }
        vm.search = "pharmacology"
        await flushPromises()
        // "Pharmacology" matches both the title and the Subject tag of link 2 only.
        expect(linkCardTitles(wrapper)).toEqual(["Pharmacology Notes"])
    })

    it("shows the empty banner when nothing matches", async () => {
        const wrapper = await mountLoaded({ linkCollectionName: "Resources" })
        const vm = wrapper.vm as unknown as { search: string }
        vm.search = "zzz-no-match"
        await flushPromises()
        expect(linkCardTitles(wrapper)).toHaveLength(0)
        expect(wrapper.text()).toContain("No links found.")
    })
})

describe("LinkCollections.vue - tag category filter", () => {
    beforeEach(() => primeFetch())

    it("filters links to those carrying the selected tag value", async () => {
        const wrapper = await mountLoaded({ linkCollectionName: "Resources" })
        const vm = wrapper.vm as unknown as {
            tagFilters: { linkCollectionTagCategoryId: number; selected: string | null }[]
        }
        const subjectFilter = vm.tagFilters.find((t) => t.linkCollectionTagCategoryId === SUBJECT_CATEGORY_ID)!
        subjectFilter.selected = "Anatomy"
        await flushPromises()
        expect(linkCardTitles(wrapper).toSorted()).toEqual(["Anatomy Atlas", "Anatomy Guide"])
    })

    it("builds de-duplicated, sorted options per category from the link tags", async () => {
        const wrapper = await mountLoaded({ linkCollectionName: "Resources" })
        const vm = wrapper.vm as unknown as {
            tagFilters: { linkCollectionTagCategoryId: number; options: string[] }[]
        }
        const subject = vm.tagFilters.find((t) => t.linkCollectionTagCategoryId === SUBJECT_CATEGORY_ID)!
        // Anatomy appears twice in the data but must be de-duplicated.
        expect(subject.options).toEqual(["Anatomy", "Pharmacology"])
    })
})

describe("LinkCollections.vue - group by tag category", () => {
    beforeEach(() => primeFetch())

    it("getInGroup returns only links whose group tag matches the group value", async () => {
        const wrapper = await mountLoaded({ linkCollectionName: "Resources", groupByTagCategory: "Subject" })
        const vm = wrapper.vm as unknown as {
            getInGroup: (links: Link[], group: string) => Link[]
            filteredLinks: Link[]
        }
        const anatomy = vm.getInGroup(vm.filteredLinks, "Anatomy").map((l) => l.title)
        expect(anatomy.toSorted()).toEqual(["Anatomy Atlas", "Anatomy Guide"])
        const pharm = vm.getInGroup(vm.filteredLinks, "Pharmacology").map((l) => l.title)
        expect(pharm).toEqual(["Pharmacology Notes"])
    })

    it("getInGroup returns all links unchanged when no group category is set", async () => {
        primeFetch()
        const wrapper = await mountLoaded({ linkCollectionName: "Resources" })
        const vm = wrapper.vm as unknown as {
            getInGroup: (links: Link[], group: string) => Link[]
            filteredLinks: Link[]
        }
        expect(vm.getInGroup(vm.filteredLinks, "anything")).toHaveLength(3)
    })

    it("computes the sorted set of group header values", async () => {
        const wrapper = await mountLoaded({ linkCollectionName: "Resources", groupByTagCategory: "Subject" })
        const vm = wrapper.vm as unknown as { groupByValues: string[] }
        expect(vm.groupByValues).toEqual(["Anatomy", "Pharmacology"])
    })
})
