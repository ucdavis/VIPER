import { mount, flushPromises } from "@vue/test-utils"
import { Quasar } from "quasar"
import { ref } from "vue"
import CareerOptionManager from "../components/CareerOptionManager.vue"
import type { CareerOptionSaveResult, CareerSelectionOption } from "../types"

/**
 * Tests for the option manager: the admin table behind one dropdown's list. The composable it
 * drives is tested separately, so it is stubbed here and only the component's own rules are
 * exercised — which options may be edited or deleted, and what a refused delete reports.
 */

const mockNotify = vi.fn<(...args: unknown[]) => unknown>()
/** Captures the confirm dialog's handler so a test can accept the prompt. */
const dialogCallbacks: { onOk?: () => Promise<void> | void } = {}

vi.mock("quasar", async (importOriginal) => {
    const actual = await importOriginal<Record<string, unknown>>()
    return {
        ...actual,
        useQuasar: () => ({
            notify: (...args: unknown[]) => mockNotify(...args),
            dialog: () => {
                const chain = {
                    onOk(handler: () => Promise<void> | void) {
                        dialogCallbacks.onOk = handler
                        return chain
                    },
                }
                return chain
            },
        }),
    }
})

const managerState = {
    options: ref<CareerSelectionOption[]>([]),
    loading: ref(false),
    loadFailed: ref(false),
    deletingId: ref<number | null>(null),
    load: vi.fn<() => Promise<void>>(),
    save: vi.fn<() => Promise<CareerOptionSaveResult>>(),
    remove: vi.fn<(id: number) => Promise<CareerOptionSaveResult>>(),
}

vi.mock("../composables/use-career-option-manager", async (importOriginal) => {
    const actual = await importOriginal<Record<string, unknown>>()
    return { ...actual, useCareerOptionManager: () => managerState }
})

function option(id: number, label: string, overrides: Partial<CareerSelectionOption> = {}): CareerSelectionOption {
    return { id, label, isOther: false, usageCount: 0, ...overrides }
}

function mountManager(options: CareerSelectionOption[], loadFailed = false) {
    vi.clearAllMocks()
    delete dialogCallbacks.onOk
    managerState.options.value = options
    managerState.loadFailed.value = loadFailed
    managerState.deletingId.value = null
    managerState.remove.mockResolvedValue({ success: true, errors: [] })
    return mount(CareerOptionManager, {
        props: { type: "species" },
        global: {
            plugins: [[Quasar, {}]],
            stubs: {
                CareerOptionFormDialog: true,
                StatusBanner: { template: "<div class='banner'><slot /><slot name='action' /></div>" },
            },
        },
    })
}

function buttonWithLabel(wrapper: ReturnType<typeof mountManager>, label: string) {
    return wrapper.findAll("button").find((b) => b.attributes("aria-label") === label)
}

describe("career option manager", () => {
    it("wraps a long option name rather than pushing the actions off a narrow screen", () => {
        expect.hasAssertions()
        const wrapper = mountManager([option(1, "A very long species focus name ".repeat(3))])

        // jsdom does no layout, so this checks the wrapping rules rather than the fit itself.
        expect(wrapper.find(".q-table__container").classes()).not.toContain("q-table--no-wrap")
        const cells = wrapper.findAll("tbody td")
        expect(cells[0].classes()).not.toContain("text-no-wrap")
        // Breaks a single unbroken word too, which wrapping at spaces alone cannot.
        expect(cells[0].classes()).toContain("option-label")
        expect(cells[1].classes()).toContain("text-no-wrap")
        expect(cells[2].classes()).toContain("text-no-wrap")
    })

    it("heads the section with the list it manages", () => {
        expect.hasAssertions()
        const wrapper = mountManager([option(1, "Equine")])

        expect(wrapper.find("h2").text()).toBe("Species Focus")
        expect(wrapper.text()).toContain("Choices shared by the Primary and Secondary Species Focus dropdowns.")
    })

    it("counts how many students chose each option", () => {
        expect.hasAssertions()
        const wrapper = mountManager([option(1, "Equine", { usageCount: 2 }), option(2, "Bovine", { usageCount: 1 })])

        expect(wrapper.text()).toContain("2 students")
        expect(wrapper.text()).toContain("1 student")
    })

    it("offers no edit or delete for the catch-all", () => {
        expect.hasAssertions()
        // "Other" is what tells the form to collect free text, so it cannot be renamed or removed.
        const wrapper = mountManager([option(9, "Other", { isOther: true })])

        expect(buttonWithLabel(wrapper, "Edit Other")).toBeUndefined()
    })

    it("explains why an option in use cannot be deleted", async () => {
        expect.hasAssertions()
        const wrapper = mountManager([option(1, "Equine", { usageCount: 2 })])

        // aria-disabled, not disabled: a disabled button leaves the tab order and never opens
        // its tooltip, so a keyboard user could not reach the explanation.
        const deleteButton = buttonWithLabel(wrapper, "Cannot delete Equine: selected by 2 students")
        expect(deleteButton).toBeDefined()
        expect(deleteButton?.attributes("aria-disabled")).toBe("true")
        expect(deleteButton?.attributes("disabled")).toBeUndefined()

        // Still reachable, so the handler is what has to refuse it: no dialog is opened, which
        // shows as no onOk handler having been captured.
        await deleteButton?.trigger("click")
        expect(dialogCallbacks.onOk).toBeUndefined()
    })

    it("allows deleting an option nobody has chosen", () => {
        expect.hasAssertions()
        const wrapper = mountManager([option(1, "Equine")])

        const deleteButton = buttonWithLabel(wrapper, "Delete Equine")
        expect(deleteButton).toBeDefined()
        expect(deleteButton?.attributes("aria-disabled")).toBeUndefined()
    })

    it("deletes on confirmation and says so", async () => {
        expect.hasAssertions()
        const wrapper = mountManager([option(1, "Equine")])

        await buttonWithLabel(wrapper, "Delete Equine")?.trigger("click")
        await dialogCallbacks.onOk?.()
        await flushPromises()

        expect(managerState.remove).toHaveBeenCalledWith(1)
        expect(mockNotify).toHaveBeenCalledWith({ type: "positive", message: 'Deleted "Equine".' })
    })

    it("reports the server's reason when a delete is refused", async () => {
        expect.hasAssertions()
        const wrapper = mountManager([option(1, "Equine")])
        managerState.remove.mockResolvedValue({ success: false, errors: ["2 students have selected this option."] })

        await buttonWithLabel(wrapper, "Delete Equine")?.trigger("click")
        await dialogCallbacks.onOk?.()
        await flushPromises()

        expect(mockNotify).toHaveBeenCalledWith({
            type: "negative",
            message: "2 students have selected this option.",
        })
    })

    it("offers a retry when the options cannot be loaded", async () => {
        expect.hasAssertions()
        const wrapper = mountManager([], true)

        expect(wrapper.find(".banner").text()).toContain("Unable to load the Species Focus options.")
        await wrapper.find(".banner button").trigger("click")

        expect(managerState.load).toHaveBeenCalledWith()
    })

    it("does not offer to add an option it could not load the list for", () => {
        expect.hasAssertions()
        const wrapper = mountManager([], true)

        expect(buttonWithLabel(wrapper, "Add Species option")?.attributes("disabled")).toBeDefined()
    })
})
