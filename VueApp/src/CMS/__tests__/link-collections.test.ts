import LinkCollections from "@/CMS/components/LinkCollections.vue"
import ManageLinkCollections from "@/CMS/pages/ManageLinkCollections.vue"
import type { Link, LinkCollection } from "@/CMS/types"
import { mountCms, flushPromises } from "./test-utils"

/**
 * LinkCollections is the most logic-dense CMS display component: it loads a collection and
 * its links, derives tag-category filters, then filters (search across title/description/tags,
 * case-insensitive; plus per-category tag select) and optionally groups links by a category.
 * These tests mock ViperFetch to seed deterministic data and assert the filter/group output.
 * The ManageLinkCollections admin page (same domain) is also exercised here for its
 * unsaved-changes guard on the Edit Collection dialog.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockPut = vi.fn<(...args: unknown[]) => unknown>()
const mockPost = vi.fn<(...args: unknown[]) => unknown>()
const mockDel = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        put: (...args: unknown[]) => mockPut(...args),
        post: (...args: unknown[]) => mockPost(...args),
        del: (...args: unknown[]) => mockDel(...args),
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

    it("buckets links by their value in the group-by tag category", async () => {
        const wrapper = await mountLoaded({ linkCollectionName: "Resources", groupByTagCategory: "Subject" })
        const vm = wrapper.vm as unknown as { groupedLinks: Map<string, Link[]> }
        const anatomy = (vm.groupedLinks.get("Anatomy") ?? []).map((l) => l.title)
        expect(anatomy.toSorted()).toEqual(["Anatomy Atlas", "Anatomy Guide"])
        const pharm = (vm.groupedLinks.get("Pharmacology") ?? []).map((l) => l.title)
        expect(pharm).toEqual(["Pharmacology Notes"])
    })

    it("renders all links and builds no buckets when no group category is set", async () => {
        primeFetch()
        const wrapper = await mountLoaded({ linkCollectionName: "Resources" })
        expect(linkCardTitles(wrapper)).toHaveLength(3)
        const vm = wrapper.vm as unknown as { groupedLinks: Map<string, Link[]> }
        expect(vm.groupedLinks.size).toBe(0)
    })

    it("computes the sorted set of group header values", async () => {
        const wrapper = await mountLoaded({ linkCollectionName: "Resources", groupByTagCategory: "Subject" })
        const vm = wrapper.vm as unknown as { groupByValues: string[] }
        expect(vm.groupByValues).toEqual(["Anatomy", "Pharmacology"])
    })
})

// Quasar plugin dialogs teleport to document.body; click the LAST matching button since a
// dismissed dialog's portal can briefly linger mid-transition.
function clickBodyButton(label: string) {
    const btn = [...document.body.querySelectorAll("button")].filter((b) => b.textContent?.includes(label)).at(-1)
    expect(btn, `expected a "${label}" button`).toBeTruthy()
    btn!.click()
}

const MANAGE_COLLECTION = { linkCollectionId: 7, linkCollection: "Resources" }
const MANAGE_TAGS = [{ linkCollectionTagCategoryId: 1, linkCollectionTagCategory: "Type", sortOrder: 1 }]

// The manage page fans out to the collection list, then the selected collection's links + tags.
function primeManageFetch() {
    mockGet.mockReset()
    mockPut.mockReset()
    mockPost.mockReset()
    mockDel.mockReset()
    mockGet.mockImplementation((...args: unknown[]) => {
        const url = args[0] as string
        if (url.includes("/links")) return Promise.resolve({ success: true, result: [] })
        if (url.includes("/tags")) return Promise.resolve({ success: true, result: MANAGE_TAGS })
        return Promise.resolve({ success: true, result: [MANAGE_COLLECTION] })
    })
}

type ManageVm = {
    showCollectionDialog: boolean
    draftTags: { linkCollectionTagCategoryId: number; linkCollectionTagCategory: string; sortOrder: number }[]
}

describe("ManageLinkCollections.vue - Edit Collection dialog unsaved-changes guard", () => {
    beforeEach(() => primeManageFetch())

    async function mountManage() {
        const wrapper = mountCms(ManageLinkCollections)
        await flushPromises()
        await flushPromises()
        return wrapper
    }

    function closeCollectionDialog() {
        const closeBtn = document.body.querySelector<HTMLButtonElement>('button[aria-label="Close dialog"]')
        expect(closeBtn, "expected the collection dialog close (X) button").toBeTruthy()
        closeBtn!.click()
    }

    it("prompts before discarding staged tag edits, then discards on confirm", async () => {
        const wrapper = await mountManage()
        const vm = wrapper.vm as unknown as ManageVm

        vm.showCollectionDialog = true
        await flushPromises()
        // Stage a new tag category so the dialog is dirty.
        vm.draftTags.push({ linkCollectionTagCategoryId: -1, linkCollectionTagCategory: "New", sortOrder: 2 })
        await flushPromises()

        closeCollectionDialog()
        await flushPromises()
        // The guard intercepts the close instead of silently discarding the staged edits.
        expect(document.body.textContent).toContain("Unsaved Changes")
        expect(vm.showCollectionDialog).toBe(true)

        clickBodyButton("Discard Changes")
        await flushPromises()
        await flushPromises()
        expect(vm.showCollectionDialog).toBe(false)
    })

    it("closes without prompting when nothing was edited", async () => {
        const wrapper = await mountManage()
        const vm = wrapper.vm as unknown as ManageVm

        vm.showCollectionDialog = true
        await flushPromises()

        closeCollectionDialog()
        await flushPromises()
        await flushPromises()
        // Clean dialog: confirmClose resolves immediately, so the dialog just closes.
        expect(vm.showCollectionDialog).toBe(false)
    })
})
