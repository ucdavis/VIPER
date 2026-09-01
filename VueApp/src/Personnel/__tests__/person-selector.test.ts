import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import PersonSelector from "../components/PersonSelector.vue"
import { searchPeopleOptions } from "../services/phone-person-options-service"
import type { AugmentedViperPerson } from "../types/phone-types"

/**
 * Personnel's PersonSelector is never mounted for real by the dialog tests (they stub it away
 * to isolate what those tests are about), so nothing else exercises its actual wiring: passing
 * listCode through to searchPeopleOptions, reflecting results into QSelect's options, and the
 * clear-to-sparse-fallback behavior on selection.
 */

vi.mock("../services/phone-person-options-service", () => ({
    searchPeopleOptions: vi.fn<(...args: unknown[]) => unknown>(),
}))

function makePerson(overrides: Partial<AugmentedViperPerson> = {}): AugmentedViperPerson {
    return {
        personId: 1,
        firstName: "Amy",
        lastName: "Smith",
        fullName: "Amy Smith",
        iamId: "asmith",
        currentEmployee: true,
        mailId: "asmith",
        phoneData: null,
        ...overrides,
    }
}

/** The sparse placeholder both dialogs seed a pristine form with. */
const noPersonSelected = { iamId: "", fullName: "" }

function mountSelector(listCode = "", modelValue: { iamId: string; fullName: string | null } = noPersonSelected) {
    return mount(PersonSelector, {
        props: { modelValue, label: "Employee", listCode },
        global: { plugins: [Quasar] },
    })
}

function applyUpdate(fn: () => void): void {
    fn()
}

async function triggerFilter(wrapper: ReturnType<typeof mountSelector>, value: string): Promise<void> {
    await wrapper.findComponent({ name: "QSelect" }).vm.$emit("filter", value, applyUpdate)
}

describe("personSelector.vue", () => {
    it("scopes the search to the given listCode", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(searchPeopleOptions).mockResolvedValue([])
        const wrapper = mountSelector("VMDO")

        await triggerFilter(wrapper, "smith")

        expect(searchPeopleOptions).toHaveBeenCalledWith("smith", "VMDO")
    })

    it("reflects the search results into the QSelect options", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        const people: AugmentedViperPerson[] = [
            {
                personId: 1,
                firstName: "Amy",
                lastName: "Smith",
                fullName: "Amy Smith",
                iamId: "asmith",
                currentEmployee: true,
                mailId: "asmith",
                phoneData: null,
            },
        ]
        vi.mocked(searchPeopleOptions).mockResolvedValue(people)
        const wrapper = mountSelector()

        await triggerFilter(wrapper, "smith")

        expect(wrapper.findComponent({ name: "QSelect" }).props("options")).toStrictEqual(people)
    })

    it("emits the selected person when one is chosen", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        const person: AugmentedViperPerson = {
            personId: 1,
            firstName: "Amy",
            lastName: "Smith",
            fullName: "Amy Smith",
            iamId: "asmith",
            currentEmployee: true,
            mailId: "asmith",
            phoneData: null,
        }
        const wrapper = mountSelector()

        await wrapper.findComponent({ name: "QSelect" }).vm.$emit("update:model-value", person)

        expect(wrapper.emitted("update:modelValue")).toStrictEqual([[person]])
    })

    it("emits a sparse fallback person when the selection is cleared", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        const wrapper = mountSelector()

        await wrapper.findComponent({ name: "QSelect" }).vm.$emit("update:model-value", null)

        const emitted = wrapper.emitted("update:modelValue")
        expect(emitted).toHaveLength(1)
        expect((emitted![0]![0] as AugmentedViperPerson).iamId).toBe("")
    })

    /**
     * An unset person is a sparse record rather than null, and QSelect wraps any non-null model
     * value into a single selected item, so the chip has to be suppressed explicitly.
     */
    it("shows no chip on a pristine form, where the person is a sparse placeholder", () => {
        expect.hasAssertions()
        vi.clearAllMocks()

        const wrapper = mountSelector()

        expect(wrapper.findAllComponents({ name: "QChip" })).toHaveLength(0)
    })

    it("shows a chip once a person is actually selected", () => {
        expect.hasAssertions()
        vi.clearAllMocks()

        const wrapper = mountSelector("", { iamId: "asmith", fullName: "Amy Smith" })

        const chip = wrapper.findComponent({ name: "QChip" })

        expect(chip.exists()).toBeTruthy()
        expect(chip.text()).toContain("Amy Smith")
    })

    it("captions each option with the person's email, so alike names can be told apart", () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        const wrapper = mountSelector()

        // Rendered straight from the slot rather than by opening the select: QSelect puts its
        // options in a QMenu portal, which needs layout measurement jsdom does not do.
        const optionSlot = wrapper.findComponent({ name: "QSelect" }).vm.$slots.option!
        const option = mount(
            {
                render: () =>
                    optionSlot({ itemProps: {}, opt: makePerson({ fullName: "Amy Smith", mailId: "asmith" }) }),
            },
            // The slot renders Quasar components, which need $q even in a bare harness.
            { global: { plugins: [Quasar] } },
        )

        expect(option.text()).toContain("Amy Smith")
        expect(option.text()).toContain("asmith@ucdavis.edu")
    })
})
