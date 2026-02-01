import type { Ref, ComputedRef } from "vue"
import { ref, computed } from "vue"
import type { PercentAssignTypeDto, UnitDto } from "../types"

// Constants for validation
const MIN_PERCENT = 0
const MAX_PERCENT = 100

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
 * Create empty errors state
 */
function createEmptyErrors(): PercentageFormErrors {
    return {
        type: "",
        unit: "",
        startDate: "",
        endDate: "",
        percent: "",
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
 * Validate the form and return errors
 */
function validateFormState(formState: PercentageFormState): PercentageFormErrors {
    const formErrors = createEmptyErrors()

    if (formState.percentAssignTypeId === null) {
        formErrors.type = "Type is required"
    }

    if (formState.unitId === null) {
        formErrors.unit = "Unit is required"
    }

    if (formState.startMonth === null || formState.startYear === null) {
        formErrors.startDate = "Start date is required"
    }

    if (formState.percentageValue < MIN_PERCENT || formState.percentageValue > MAX_PERCENT) {
        formErrors.percent = "Percent must be between 0 and 100"
    }

    // Validate end date is after start date if provided
    if (formState.endMonth !== null && formState.endYear !== null) {
        if (formState.startYear !== null && formState.startMonth !== null) {
            const startDate = new Date(formState.startYear, formState.startMonth - 1)
            const endDate = new Date(formState.endYear, formState.endMonth - 1)
            if (endDate < startDate) {
                formErrors.endDate = "End date must be after start date"
            }
        }
    } else if (
        (formState.endMonth !== null && formState.endYear === null) ||
        (formState.endMonth === null && formState.endYear !== null)
    ) {
        formErrors.endDate = "Both month and year are required for end date"
    }

    return formErrors
}

/**
 * Composable for shared percentage form logic between Add and Edit dialogs
 */
function usePercentageForm(
    percentAssignTypes: Ref<PercentAssignTypeDto[]> | ComputedRef<PercentAssignTypeDto[]>,
    units: Ref<UnitDto[]> | ComputedRef<UnitDto[]>,
) {
    const form = ref<PercentageFormState>(createDefaultFormState())
    const errors = ref<PercentageFormErrors>(createEmptyErrors())

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

    function hasValidationErrors(): boolean {
        return Object.values(errors.value).some((e) => e !== "")
    }

    const isFormValid = computed(
        () =>
            form.value.percentAssignTypeId !== null &&
            form.value.unitId !== null &&
            form.value.startMonth !== null &&
            form.value.startYear !== null &&
            form.value.percentageValue >= MIN_PERCENT &&
            form.value.percentageValue <= MAX_PERCENT &&
            !hasValidationErrors(),
    )

    function validateForm(): boolean {
        errors.value = validateFormState(form.value)
        return !hasValidationErrors()
    }

    function resetForm() {
        form.value = createDefaultFormState()
        errors.value = createEmptyErrors()
    }

    function resetErrors() {
        errors.value = createEmptyErrors()
    }

    return {
        form,
        errors,
        groupedTypeOptions,
        unitOptions,
        modifierOptions,
        monthOptions,
        yearOptions,
        isFormValid,
        hasValidationErrors,
        validateForm,
        formatToIsoDate,
        parseDateToMonthYear,
        resetForm,
        resetErrors,
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

type PercentageFormErrors = {
    type: string
    unit: string
    startDate: string
    endDate: string
    percent: string
}

export { usePercentageForm, formatToIsoDate, parseDateToMonthYear }
export type { TypeOption, PercentageFormState, PercentageFormErrors }
