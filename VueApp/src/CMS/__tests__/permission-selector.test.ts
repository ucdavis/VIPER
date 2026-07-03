import PermissionSelector from "@/CMS/components/PermissionSelector.vue"
import { mountCms, flushPromises } from "./test-utils"

/**
 * PermissionSelector lazily loads the full permission list on first filter, then filters it
 * client-side (case-insensitive substring). Subsequent filters must not re-fetch. Driving the
 * QSelect's filter() method exercises the component's @filter handler with a real done callback.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
    }),
}))

const ALL_PERMISSIONS = ["SVMSecure.CMS", "SVMSecure.CMS.Files", "SVMSecure.Effort", "CTS.Manage"]

function mountSelector(modelValue: string[] = []) {
    return mountCms(PermissionSelector, { props: { modelValue } })
}

// Quasar's @filter handler is (val, update, abort); the component only uses (val, update), so
// runUpdate (which runs the setter synchronously, as Quasar would) is all we need to drive it.
const runUpdate = (fn: () => void) => fn()

// Invoke the component's @filter handler directly via the QSelect's "filter" event.
async function filter(wrapper: ReturnType<typeof mountSelector>, value: string) {
    wrapper.findComponent({ name: "QSelect" }).vm.$emit("filter", value, runUpdate)
    await flushPromises()
    await flushPromises()
}

function currentOptions(wrapper: ReturnType<typeof mountSelector>): string[] {
    return (wrapper.findComponent({ name: "QSelect" }).props("options") as string[]) ?? []
}

describe("PermissionSelector.vue", () => {
    beforeEach(() => {
        mockGet.mockReset()
        mockGet.mockResolvedValue({ success: true, result: ALL_PERMISSIONS })
    })

    it("does not fetch permissions until the first filter", () => {
        mountSelector()
        expect(mockGet).not.toHaveBeenCalled()
    })

    it("lazy-loads the permission list on first filter and shows all for an empty needle", async () => {
        const wrapper = mountSelector()
        await filter(wrapper, "")
        expect(mockGet).toHaveBeenCalledOnce()
        expect(mockGet.mock.calls[0]![0]).toContain("cms/options/permissions")
        expect(currentOptions(wrapper)).toEqual(ALL_PERMISSIONS)
    })

    it("filters case-insensitively by substring", async () => {
        const wrapper = mountSelector()
        await filter(wrapper, "cms")
        expect(currentOptions(wrapper)).toEqual(["SVMSecure.CMS", "SVMSecure.CMS.Files"])
    })

    it("does not re-fetch on a second filter (list is cached after first load)", async () => {
        const wrapper = mountSelector()
        await filter(wrapper, "cms")
        await filter(wrapper, "effort")
        expect(mockGet).toHaveBeenCalledOnce()
        expect(currentOptions(wrapper)).toEqual(["SVMSecure.Effort"])
    })

    it("emits the selected permissions array on update", async () => {
        const wrapper = mountSelector(["SVMSecure.CMS"])
        const select = wrapper.findComponent({ name: "QSelect" })
        select.vm.$emit("update:modelValue", ["SVMSecure.CMS", "SVMSecure.Effort"])
        await flushPromises()
        expect(wrapper.emitted("update:modelValue")?.at(-1)).toEqual([["SVMSecure.CMS", "SVMSecure.Effort"]])
    })

    it("coerces a null clear to an empty array on update", async () => {
        const wrapper = mountSelector(["SVMSecure.CMS"])
        const select = wrapper.findComponent({ name: "QSelect" })
        select.vm.$emit("update:modelValue", null)
        await flushPromises()
        expect(wrapper.emitted("update:modelValue")?.at(-1)).toEqual([[]])
    })
})
