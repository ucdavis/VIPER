import type { Ref, ComputedRef } from "vue"
import { ref, computed } from "vue"
import type { PercentAssignTypeDto, UnitDto } from "../types"

// Constants for validation
const MIN_PERCENT = 0
const MAX_PERCENT = 100

// Year range for dropdown (1998 is oldest data in system)
const MIN_YEAR = 1998
const YEAR_RANGE_FUTURE = 1

/**
 * Type option for grouped dropdown display
 */
export type TypeOption = {
    label: string
    value?: number
    isHeader?: boolean
    disable?: boolean
}

/**
 * Form state for percentage assignment
 */
export type PercentageFormState = {
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

/**
 * Validation errors for percentage form
 */
export type PercentageFormErrors = {
    type: string
    unit: string
    startDate: string
    endDate: string
    percent: string
}

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
 * Composable for shared percentage form logic between Add and Edit dialogs
 */
export function usePercentageForm(
    percentAssignTypes: Ref<PercentAssignTypeDto[]> | ComputedRef<PercentAssignTypeDto[]>,
    units: Ref<UnitDto[]> | ComputedRef<UnitDto[]>,
) {
    // Form state
    const form = ref<PercentageFormState>({
        percentAssignTypeId: null,
        unitId: null,
        modifier: null,
        startMonth: null,
        startYear: null,
        endMonth: null,
        endYear: null,
        percentageValue: 0,
        comment: "",
        compensated: false,
    })

    // Validation errors
    const errors = ref<PercentageFormErrors>({
        type: "",
        unit: "",
        startDate: "",
        endDate: "",
        percent: "",
    })

    // Group types by class for dropdown
    const groupedTypeOptions = computed<TypeOption[]>(() => {
        const groups: Record<string, TypeOption[]> = {}

        for (const type of percentAssignTypes.value) {
            if (!type.isActive) {
                continue
            }
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
    })

    // Unit options
    const unitOptions = computed(() =>
        units.value
            .filter((u) => u.isActive)
            .map((u) => ({
                label: u.name,
                value: u.id,
            })),
    )

    // Modifier options
    const modifierOptions = [
        { label: "None", value: null },
        { label: "Acting", value: "Acting" },
        { label: "Interim", value: "Interim" },
    ]

    // Month options
    const monthOptions = [
        { label: "January", value: 1 },
        { label: "February", value: 2 },
        { label: "March", value: 3 },
        { label: "April", value: 4 },
        { label: "May", value: 5 },
        { label: "June", value: 6 },
        { label: "July", value: 7 },
        { label: "August", value: 8 },
        { label: "September", value: 9 },
        { label: "October", value: 10 },
        { label: "November", value: 11 },
        { label: "December", value: 12 },
    ]

    // Year options (descending order, most recent first)
    const yearOptions = computed(() => {
        const currentYear = new Date().getFullYear()
        const years: { label: string; value: number }[] = []
        for (let y = currentYear + YEAR_RANGE_FUTURE; y >= MIN_YEAR; y -= 1) {
            years.push({ label: y.toString(), value: y })
        }
        return years
    })

    // Check if there are validation errors
    function hasValidationErrors(): boolean {
        return Object.values(errors.value).some((e) => e !== "")
    }

    // Form validation
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

    // Validate form and populate errors
    function validateForm(): boolean {
        errors.value = {
            type: "",
            unit: "",
            startDate: "",
            endDate: "",
            percent: "",
        }

        if (form.value.percentAssignTypeId === null) {
            errors.value.type = "Type is required"
        }

        if (form.value.unitId === null) {
            errors.value.unit = "Unit is required"
        }

        if (form.value.startMonth === null || form.value.startYear === null) {
            errors.value.startDate = "Start date is required"
        }

        if (form.value.percentageValue < MIN_PERCENT || form.value.percentageValue > MAX_PERCENT) {
            errors.value.percent = "Percent must be between 0 and 100"
        }

        // Validate end date is after start date if provided
        if (form.value.endMonth !== null && form.value.endYear !== null) {
            if (form.value.startYear !== null && form.value.startMonth !== null) {
                const startDate = new Date(form.value.startYear, form.value.startMonth - 1)
                const endDate = new Date(form.value.endYear, form.value.endMonth - 1)
                if (endDate < startDate) {
                    errors.value.endDate = "End date must be after start date"
                }
            }
        } else if (
            (form.value.endMonth !== null && form.value.endYear === null) ||
            (form.value.endMonth === null && form.value.endYear !== null)
        ) {
            errors.value.endDate = "Both month and year are required for end date"
        }

        return !hasValidationErrors()
    }

    // Reset form to default values
    function resetForm() {
        const currentDate = new Date()
        form.value = {
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
        resetErrors()
    }

    // Reset errors
    function resetErrors() {
        errors.value = {
            type: "",
            unit: "",
            startDate: "",
            endDate: "",
            percent: "",
        }
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
