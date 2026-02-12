import type { Ref, ComputedRef } from "vue"
import { ref, computed } from "vue"
import type { PercentAssignTypeDto, UnitDto } from "../types"

// Year range for dropdown (1998 is oldest data in system)
const MIN_YEAR = 1998
const YEAR_RANGE_FUTURE = 1

// Static options that don't depend on reactive data
const modifierOptions = [
    { label: "None", value: null },
    { label: "Acting", value: "Acting" },
    { label: "Interim", value: "Interim" },
]

const monthFormatter = new Intl.DateTimeFormat("en-US", { month: "long" })
const monthOptions = Array.from({ length: 12 }, (_, i) => ({
    label: monthFormatter.format(new Date(0, i)),
    value: i + 1,
}))

/**
 * Format month/year to ISO date string (first of month)
 */
function formatToIsoDate(month: number, year: number): string {
    const monthStr = month.toString().padStart(2, "0")
    return `${year}-${monthStr}-01`
}

/**
 * Parse date string to month/year
 */
function parseDateToMonthYear(dateStr: string | null): { month: number | null; year: number | null } {
    if (!dateStr) {
        return { month: null, year: null }
    }
    const date = new Date(dateStr)
    return {
        month: date.getMonth() + 1,
        year: date.getFullYear(),
    }
}

/**
 * Create default form state
 */
function createDefaultFormState(): PercentageFormState {
    const currentDate = new Date()
    return {
        percentAssignTypeId: null,
        unitId: null,
        modifier: null,
        startMonth: currentDate.getMonth() + 1,
        startYear: currentDate.getFullYear(),
        endMonth: null,
        endYear: null,
        percentageValue: 0,
        comment: "",
        compensated: false,
    }
}

/**
 * Build grouped type options from percent assign types
 */
function buildGroupedTypeOptions(types: PercentAssignTypeDto[]): TypeOption[] {
    const groups: Record<string, TypeOption[]> = {}
    const activeTypes = types.filter((t) => t.isActive)

    for (const type of activeTypes) {
        const groupName = type.class || "Other"
        const groupArray = groups[groupName] ?? (groups[groupName] = [])
        groupArray.push({
            label: type.name,
            value: type.id,
        })
    }

    const result: TypeOption[] = []
    const orderedClasses = ["Admin", "Clinical", "Other"]

    for (const className of orderedClasses) {
        if (groups[className]) {
            result.push({ label: className, isHeader: true, disable: true })
            result.push(...groups[className])
        }
    }

    // Add any remaining classes not in the ordered list
    for (const [className, items] of Object.entries(groups)) {
        if (!orderedClasses.includes(className)) {
            result.push({ label: className, isHeader: true, disable: true })
            result.push(...items)
        }
    }

    return result
}

/**
 * Composable for shared percentage form logic between Add and Edit dialogs
 */
function usePercentageForm(
    percentAssignTypes: Ref<PercentAssignTypeDto[]> | ComputedRef<PercentAssignTypeDto[]>,
    units: Ref<UnitDto[]> | ComputedRef<UnitDto[]>,
) {
    const form = ref<PercentageFormState>(createDefaultFormState())

    const groupedTypeOptions = computed<TypeOption[]>(() => buildGroupedTypeOptions(percentAssignTypes.value))

    const unitOptions = computed(() =>
        units.value
            .filter((u) => u.isActive)
            .map((u) => ({
                label: u.name,
                value: u.id,
            })),
    )

    const yearOptions = computed(() => {
        const currentYear = new Date().getFullYear()
        const years: { label: string; value: number }[] = []
        for (let y = currentYear + YEAR_RANGE_FUTURE; y >= MIN_YEAR; y -= 1) {
            years.push({ label: y.toString(), value: y })
        }
        return years
    })

    // Cross-field validation rules for end date fields.
    // Using computed so Quasar detects the rules array change when dependencies update.
    const endMonthRules = computed(() => [
        () => {
            if (form.value.endYear && !form.value.endMonth) {
                return "End month is required when end year is set"
            }
            return true
        },
    ])

    const endYearRules = computed(() => [
        () => {
            if (form.value.endMonth && !form.value.endYear) {
                return "End year is required when end month is set"
            }
            if (form.value.endMonth && form.value.endYear && form.value.startMonth && form.value.startYear) {
                const start = new Date(form.value.startYear, form.value.startMonth - 1)
                const end = new Date(form.value.endYear, form.value.endMonth - 1)
                if (end < start) {
                    return "End date must be after start date"
                }
            }
            return true
        },
    ])

    function resetForm() {
        form.value = createDefaultFormState()
    }

    return {
        form,
        groupedTypeOptions,
        unitOptions,
        modifierOptions,
        monthOptions,
        yearOptions,
        endMonthRules,
        endYearRules,
        formatToIsoDate,
        parseDateToMonthYear,
        resetForm,
    }
}

// Type exports
type TypeOption = {
    label: string
    value?: number
    isHeader?: boolean
    disable?: boolean
}

type PercentageFormState = {
    percentAssignTypeId: number | null
    unitId: number | null
    modifier: string | null
    startMonth: number | null
    startYear: number | null
    endMonth: number | null
    endYear: number | null
    percentageValue: number
    comment: string
    compensated: boolean
}


export { usePercentageForm, formatToIsoDate, parseDateToMonthYear }
export type { TypeOption, PercentageFormState }
