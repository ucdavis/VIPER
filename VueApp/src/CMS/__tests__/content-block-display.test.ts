import ContentBlock from "@/CMS/components/ContentBlock.vue"
import ContentBlockView from "@/CMS/pages/ContentBlockView.vue"
import { createTestRouter, mountCms, flushPromises } from "./test-utils"

/**
 * ContentBlock is the display component the whole site embeds, and (through ContentBlockView) the
 * target of the content-block list's View link. It addresses a block by friendly name or, for a
 * block that has none, by id; anything the endpoint refuses (missing block, or one this viewer may
 * not see) renders the `empty` slot rather than a blank area.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({ get: (...args: unknown[]) => mockGet(...args) }),
}))

function requestedUrl(call = 0): string {
    return mockGet.mock.calls[call][0] as string
}

beforeEach(() => {
    mockGet.mockReset()
    mockGet.mockResolvedValue({ success: true, result: { content: "<p>hello</p>" } })
})

describe("contentBlock.vue - which block it asks for", () => {
    it("addresses a named block through the fn route and renders the sanitized content", async () => {
        const wrapper = mountCms(ContentBlock, { props: { contentBlockName: "welcome" } })
        await flushPromises()

        expect(requestedUrl()).toContain("cms/content/fn/welcome")
        expect(wrapper.find(".content-block").html()).toContain("<p>hello</p>")
    })

    it("addresses a block with no friendly name through the id route", async () => {
        mountCms(ContentBlock, { props: { contentBlockId: 42 } })
        await flushPromises()

        expect(requestedUrl()).toContain("cms/content/id/42")
    })

    it("prefers the id when both are given, so the fn route is never asked for a nameless block", async () => {
        mountCms(ContentBlock, { props: { contentBlockName: "welcome", contentBlockId: 42 } })
        await flushPromises()

        expect(requestedUrl()).toContain("cms/content/id/42")
    })

    it("reloads once when the block it is showing changes", async () => {
        const wrapper = mountCms(ContentBlock, { props: { contentBlockName: "welcome" } })
        await flushPromises()

        await wrapper.setProps({ contentBlockName: "hours" })
        await flushPromises()

        expect(mockGet).toHaveBeenCalledTimes(2)
        expect(requestedUrl(1)).toContain("cms/content/fn/hours")
    })

    it("ignores a slow response for the block that is no longer being shown", async () => {
        // Without the in-flight guard the first (stale) response wins and the wrong block is left
        // on screen for good, since nothing re-fetches afterwards. The two requests go out in
        // mount-then-setProps order, so queueing the responses is enough to invert their arrival.
        // Not Promise.withResolvers: the project's TS lib target predates it, so tsc rejects it
        // even though the test runtime has it.
        let resolveStale!: (value: unknown) => void
        mockGet.mockReturnValueOnce(
            new Promise((resolve) => {
                resolveStale = resolve
            }),
        )
        mockGet.mockResolvedValueOnce({ success: true, result: { content: "<p>current</p>" } })

        const wrapper = mountCms(ContentBlock, { props: { contentBlockName: "welcome" } })
        await wrapper.setProps({ contentBlockName: "hours" })
        await flushPromises()
        resolveStale({ success: true, result: { content: "<p>stale</p>" } })
        await flushPromises()

        expect(wrapper.find(".content-block").html()).toContain("<p>current</p>")
    })
})

describe("contentBlock.vue - nothing to show", () => {
    it("stays silent until the first response, then renders the empty slot", async () => {
        mockGet.mockResolvedValue({ success: false, result: null })
        const wrapper = mountCms(ContentBlock, {
            props: { contentBlockName: "welcome" },
            slots: { empty: "<p class='no-block'>No block</p>" },
        })

        expect(wrapper.find(".no-block").exists()).toBeFalsy()

        await flushPromises()

        expect(wrapper.find(".content-block").exists()).toBeFalsy()
        expect(wrapper.find(".no-block").exists()).toBeTruthy()
    })

    it("renders nothing at all for an embedded use, which passes no empty slot", async () => {
        mockGet.mockResolvedValue({ success: false, result: null })
        const wrapper = mountCms(ContentBlock, { props: { contentBlockName: "welcome" } })
        await flushPromises()

        expect(wrapper.text()).toBe("")
    })
})

async function mountView(location: string) {
    const router = createTestRouter()
    await router.push(location)
    await router.isReady()
    const wrapper = mountCms(ContentBlockView, {}, router)
    await flushPromises()
    return wrapper
}

describe("contentBlockView.vue - standalone display page", () => {
    it("shows the block named in the fn route", async () => {
        const wrapper = await mountView("/CMS/Content/welcome")

        expect(requestedUrl()).toContain("cms/content/fn/welcome")
        expect(wrapper.html()).toContain("<p>hello</p>")
    })

    it("shows the block identified by the id route", async () => {
        await mountView("/CMS/Content/id/42")

        expect(requestedUrl()).toContain("cms/content/id/42")
    })

    it("explains a block it cannot show, rather than leaving the page blank", async () => {
        mockGet.mockResolvedValue({ success: false, result: null })
        const wrapper = await mountView("/CMS/Content/secret")

        expect(wrapper.text()).toContain("does not exist, or you do not have permission")
    })
})
