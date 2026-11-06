import { flushPromises, mount } from "@vue/test-utils"
import { Quasar, QSelect } from "quasar"
import EisSelectPerson from "../pages/EisSelectPerson.vue"
import { eisService } from "../services/eis-service"

/**
 * The person picker lists whoever the server says this user may see, filters as the user types,
 * and opens the chosen employee's summary.
 */

const { mockPush } = vi.hoisted(() => ({ mockPush: vi.fn<(...args: unknown[]) => unknown>() }))
vi.mock("vue-router", () => ({ useRouter: () => ({ push: mockPush }) }))
vi.mock("../services/eis-service", () => ({
    eisService: { getPeople: vi.fn<(...args: unknown[]) => unknown>() },
}))

const people = [
    { employeeId: "10123456", name: "Lovelace, Ada" },
    { employeeId: "10999999", name: "Byron, Bo" },
]

async function mountPicker(result: unknown) {
    vi.mocked(eisService.getPeople).mockResolvedValue(result as never)
    const wrapper = mount(EisSelectPerson, { global: { plugins: [Quasar] } })
    await flushPromises()
    return wrapper
}

describe("eisSelectPerson.vue", () => {
    it("filters the people and opens the chosen employee", async () => {
        expect.hasAssertions()
        const wrapper = await mountPicker(people)
        const select = wrapper.findComponent(QSelect)

        select.vm.$emit("filter", "bo", (apply: () => void) => apply())
        await flushPromises()
        expect(select.props("options")).toStrictEqual([people[1]])

        select.vm.$emit("update:model-value", "10999999")
        await flushPromises()
        expect(mockPush).toHaveBeenCalledWith({ name: "EisSummary", params: { employeeId: "10999999" } })
    })

    it("does nothing when the selection is cleared", async () => {
        expect.hasAssertions()
        const wrapper = await mountPicker(people)

        wrapper.findComponent(QSelect).vm.$emit("update:model-value", null)
        await flushPromises()

        expect(mockPush).not.toHaveBeenCalled()
    })

    it("explains a failed load and an empty list", async () => {
        expect.hasAssertions()
        const failed = await mountPicker(null)
        const empty = await mountPicker([])

        expect(failed.text()).toContain("could not be loaded")
        expect(empty.text()).toContain("No employees are available to you")
    })
})
