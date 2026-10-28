import { mount, flushPromises } from "@vue/test-utils"
import { Quasar } from "quasar"
import CareerOptionFormDialog from "../components/CareerOptionFormDialog.vue"
import { CAREER_OPTION_TYPES } from "../composables/use-career-option-manager"
import type { CareerOptionSaveResult, CareerSelectionOption } from "../types"

/**
 * Tests for the add/edit option dialog: the title it takes from the list it manages, the label
 * rule it applies before saving, and what it does with a refused save.
 */

function option(id: number, label: string, overrides: Partial<CareerSelectionOption> = {}): CareerSelectionOption {
    return { id, label, isOther: false, usageCount: 0, ...overrides }
}

function mountDialog(editing: CareerSelectionOption | null, saveResult: CareerOptionSaveResult) {
    const saveOption = vi.fn<(id: number | null, label: string) => Promise<CareerOptionSaveResult>>()
    saveOption.mockResolvedValue(saveResult)
    const wrapper = mount(CareerOptionFormDialog, {
        props: {
            modelValue: true,
            titleId: "career-options-species",
            config: CAREER_OPTION_TYPES.species,
            option: editing,
            existingOptions: [option(1, "Equine"), option(9, "Other", { isOther: true })],
            saveOption,
        },
        global: {
            plugins: [[Quasar, {}]],
            // The dialog shell renders in a portal; its contents are what these tests are about.
            stubs: { RecordFormDialog: { template: "<div><slot /></div>" } },
        },
    })
    return { wrapper, saveOption }
}

/** Runs the dialog's submit handler, as its save button does. */
async function submit(wrapper: ReturnType<typeof mountDialog>["wrapper"]): Promise<void> {
    await (wrapper.vm as unknown as { submit: () => Promise<void> }).submit()
    await flushPromises()
}

describe("career option form dialog", () => {
    it("titles itself for adding to the list it manages", () => {
        expect.hasAssertions()
        const { wrapper } = mountDialog(null, { success: true, errors: [] })

        expect((wrapper.vm as unknown as { title: string }).title).toBe("Add Species Option")
    })

    it("titles itself for editing an existing option", () => {
        expect.hasAssertions()
        const { wrapper } = mountDialog(option(1, "Equine"), { success: true, errors: [] })

        expect((wrapper.vm as unknown as { title: string }).title).toBe("Edit Species Option")
    })

    it("saves the trimmed label and reports it", async () => {
        expect.hasAssertions()
        const { wrapper, saveOption } = mountDialog(null, { success: true, errors: [] })
        ;(wrapper.vm as unknown as { form: { label: string } }).form.label = "  Exotics  "

        await submit(wrapper)

        expect(saveOption).toHaveBeenCalledWith(null, "Exotics")
        expect(wrapper.emitted("saved")).toStrictEqual([["Exotics"]])
        expect(wrapper.emitted("update:modelValue")).toStrictEqual([[false]])
    })

    it("saves an edit against the option's id", async () => {
        expect.hasAssertions()
        const { wrapper, saveOption } = mountDialog(option(1, "Equine"), { success: true, errors: [] })
        ;(wrapper.vm as unknown as { form: { label: string } }).form.label = "Equine and camelid"

        await submit(wrapper)

        expect(saveOption).toHaveBeenCalledWith(1, "Equine and camelid")
    })

    it("shows the server's refusal and stays open", async () => {
        expect.hasAssertions()
        const { wrapper } = mountDialog(null, { success: false, errors: ["That option already exists."] })
        ;(wrapper.vm as unknown as { form: { label: string } }).form.label = "Equine"

        await submit(wrapper)

        expect((wrapper.vm as unknown as { formError: string }).formError).toBe("That option already exists.")
        expect(wrapper.emitted("update:modelValue")).toBeUndefined()
    })

    it("falls back to a general message when the server sends no reason", async () => {
        expect.hasAssertions()
        const { wrapper } = mountDialog(null, { success: false, errors: [] })
        ;(wrapper.vm as unknown as { form: { label: string } }).form.label = "Exotics"

        await submit(wrapper)

        expect((wrapper.vm as unknown as { formError: string }).formError).toBe(
            "Unable to save the option. Please try again.",
        )
    })

    it("refuses a duplicate name before asking the server", () => {
        expect.hasAssertions()
        const { wrapper } = mountDialog(null, { success: true, errors: [] })
        const rule = (wrapper.vm as unknown as { labelRule: (v: string) => true | string }).labelRule

        expect(rule("equine")).toBeTypeOf("string")
        expect(rule("Exotics")).toBeTruthy()
    })

    it("lets an option keep its own name while being edited", () => {
        expect.hasAssertions()
        const { wrapper } = mountDialog(option(1, "Equine"), { success: true, errors: [] })
        const rule = (wrapper.vm as unknown as { labelRule: (v: string) => true | string }).labelRule

        expect(rule("Equine")).toBeTruthy()
    })
})
