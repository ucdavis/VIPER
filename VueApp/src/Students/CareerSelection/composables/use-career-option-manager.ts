import { ref } from "vue"
import type { CareerOptionSaveResult, CareerOptionType, CareerSelectionOption } from "../types"
import { careerSelectionService } from "../services/career-selection-service"

type CareerOptionTypeConfig = {
    title: string
    // Singular name used in dialog titles, e.g. "Add Species Option".
    singular: string
    description: string
    maxLength: number
}

const CAREER_OPTION_TYPES: Record<CareerOptionType, CareerOptionTypeConfig> = {
    career: {
        title: "Career Direction",
        singular: "Career Direction",
        description: "Choices for the Career Direction dropdown.",
        maxLength: 100,
    },
    species: {
        title: "Species Focus",
        singular: "Species",
        description: "Choices shared by the Primary and Secondary Species Focus dropdowns.",
        maxLength: 100,
    },
    postGrad: {
        title: "Post-Graduation Plans",
        singular: "Post-Graduation Plan",
        description: "Choices for the Post-Graduation Plans dropdown.",
        maxLength: 200,
    },
}

function normalizeLabel(label: string): string {
    return label.trim().toLocaleLowerCase()
}

type OptionLabelContext = {
    existing: CareerSelectionOption[]
    editingId: number | null // The option being renamed, or null when adding.
    maxLength: number
}

/**
 * Client-side check of a proposed option label. The server is the authority on duplicates,
 * but this catches obvious issues. Returns an error message, or null.
 */
function validateOptionLabel(label: string, { existing, editingId, maxLength }: OptionLabelContext): string | null {
    const trimmed = label.trim()
    if (!trimmed) {
        return "Please enter a name."
    }
    if (trimmed.length > maxLength) {
        return `Name must be ${maxLength} characters or fewer.`
    }
    const normalized = normalizeLabel(trimmed)
    // Excluding the option being edited lets an unchanged name, or a change of case only, save.
    const duplicate = existing.some((o) => o.id !== editingId && normalizeLabel(o.label) === normalized)
    return duplicate ? "An option with this name already exists." : null
}

function useCareerOptionManager(type: CareerOptionType) {
    const options = ref<CareerSelectionOption[]>([])
    const loading = ref(false)
    const loadFailed = ref(false)
    const deletingId = ref<number | null>(null)

    async function load(): Promise<void> {
        loading.value = true
        const result = await careerSelectionService.getOptions(type)
        loadFailed.value = result === null
        options.value = result ?? []
        loading.value = false
    }

    async function save(id: number | null, label: string): Promise<CareerOptionSaveResult> {
        const trimmed = label.trim()
        const result =
            id === null
                ? await careerSelectionService.createOption(type, trimmed)
                : await careerSelectionService.updateOption(type, id, trimmed)
        if (result.success) {
            await load()
        }
        return result
    }

    async function remove(id: number): Promise<CareerOptionSaveResult> {
        deletingId.value = id
        const result = await careerSelectionService.deleteOption(type, id)
        await load()
        deletingId.value = null
        return result
    }

    return { options, loading, loadFailed, deletingId, load, save, remove }
}

export { CAREER_OPTION_TYPES, useCareerOptionManager, validateOptionLabel }
export type { CareerOptionTypeConfig }
