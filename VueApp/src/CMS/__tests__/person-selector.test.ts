import PersonSelector from "@/CMS/components/PersonSelector.vue"
import type { CmsPersonOption } from "@/CMS/types"
import { mountCms, flushPromises } from "./test-utils"

/**
 * PersonSelector searches people server-side, but only once the query reaches 2+ characters
 * (shorter queries clear options without a request). An out-of-order guard (searchSeq) drops
 * stale responses. These tests drive the QSelect filter() and assert the min-chars gate and
 * the request query param.
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

const PEOPLE: CmsPersonOption[] = [
    { iamId: "iam1", name: "Jane Doe", loginId: "jdoe", mailId: "jdoe@x.com" },
    { iamId: "iam2", name: "John Smith", loginId: "jsmith", mailId: "jsmith@x.com" },
]

function mountSelector(modelValue: { iamId: string; name: string | null }[] = []) {
    return mountCms(PersonSelector, { props: { modelValue } })
}

// Quasar's @filter handler is (val, update, abort); the component only uses (val, update), so
// runUpdate (which runs the setter synchronously) is all we need to drive it.
const runUpdate = (fn: () => void) => fn()

async function filter(wrapper: ReturnType<typeof mountSelector>, value: string) {
    wrapper.findComponent({ name: "QSelect" }).vm.$emit("filter", value, runUpdate)
    await flushPromises()
    await flushPromises()
}

function currentOptions(wrapper: ReturnType<typeof mountSelector>): CmsPersonOption[] {
    return (wrapper.findComponent({ name: "QSelect" }).props("options") as CmsPersonOption[]) ?? []
}

describe("PersonSelector.vue", () => {
    beforeEach(() => {
        mockGet.mockReset()
        mockGet.mockResolvedValue({ success: true, result: PEOPLE })
    })

    it("does not search for a single-character query", async () => {
        const wrapper = mountSelector()
        await filter(wrapper, "j")
        expect(mockGet).not.toHaveBeenCalled()
        expect(currentOptions(wrapper)).toEqual([])
    })

    it("does not search for a whitespace-padded query under 2 chars", async () => {
        const wrapper = mountSelector()
        await filter(wrapper, " a ")
        expect(mockGet).not.toHaveBeenCalled()
    })

    it("searches once the query reaches 2 characters, passing a trimmed search param", async () => {
        const wrapper = mountSelector()
        await filter(wrapper, "  ja  ")
        expect(mockGet).toHaveBeenCalledOnce()
        const url = mockGet.mock.calls[0]![0] as string
        expect(url).toContain("cms/options/people")
        expect(url).toContain("search=ja")
        expect(currentOptions(wrapper)).toEqual(PEOPLE)
    })

    it("clears options when the search fails", async () => {
        mockGet.mockResolvedValue({ success: false, result: null })
        const wrapper = mountSelector()
        await filter(wrapper, "jane")
        expect(currentOptions(wrapper)).toEqual([])
    })

    it("ignores a stale response that resolves after a newer search (out-of-order guard)", async () => {
        const wrapper = mountSelector()
        let resolveFirst!: (v: unknown) => void
        let resolveSecond!: (v: unknown) => void
        mockGet
            .mockImplementationOnce(
                () =>
                    new Promise((r) => {
                        resolveFirst = r
                    }),
            )
            .mockImplementationOnce(
                () =>
                    new Promise((r) => {
                        resolveSecond = r
                    }),
            )

        const select = wrapper.findComponent({ name: "QSelect" })
        select.vm.$emit("filter", "jane", runUpdate)
        select.vm.$emit("filter", "janet", runUpdate)

        // The newer search answers first; the older one lands afterwards and must be dropped.
        resolveSecond({ success: true, result: [PEOPLE[1]] })
        await flushPromises()
        resolveFirst({ success: true, result: [PEOPLE[0]] })
        await flushPromises()

        expect(currentOptions(wrapper)).toEqual([PEOPLE[1]])
    })

    it("emits the selected people array on update and coerces null to []", async () => {
        const wrapper = mountSelector()
        const select = wrapper.findComponent({ name: "QSelect" })
        select.vm.$emit("update:modelValue", [{ iamId: "iam1", name: "Jane Doe" }])
        await flushPromises()
        expect(wrapper.emitted("update:modelValue")?.at(-1)).toEqual([[{ iamId: "iam1", name: "Jane Doe" }]])

        select.vm.$emit("update:modelValue", null)
        await flushPromises()
        expect(wrapper.emitted("update:modelValue")?.at(-1)).toEqual([[]])
    })
})
