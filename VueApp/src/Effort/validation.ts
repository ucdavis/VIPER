/**
 * Shared validation rules for the Effort module.
 * These rules are used by Quasar form components (:rules prop).
 */

const MAX_PERCENT = 100

/**
 * Validation rules for effort value input fields.
 * Ensures the value is a required, positive integer.
 */
const effortValueRules = [
    (val: number | null) => val !== null || "Effort value is required",
    (val: number | null) => val === null || Number.isFinite(val) || "Effort value must be a valid number",
    (val: number | null) => val === null || Number.isInteger(val) || "Effort value must be a whole number",
    (val: number | null) => val === null || val > 0 || "Effort value must be greater than zero",
]

// Generic required rule for selects/inputs
const requiredRule = (label: string) => (v: any) =>
    (v !== null && v !== undefined && v !== "") || `${label} is required`

// Non-negative number
const nonNegativeRule = (label: string) => (v: number) => v >= 0 || `${label} must be non-negative`

// Whole number
const wholeNumberRule = (label: string) => (v: number) => Number.isInteger(v) || `${label} must be a whole number`

// Percent 0-100 (guards against blank/null from coercion)
const percentRule = (v: number | string | null | undefined) => {
    const n = typeof v === "number" ? v : Number(v)
    return (Number.isFinite(n) && n >= 0 && n <= MAX_PERCENT) || "Percent must be between 0 and 100"
}

// Hint shown when notes textarea is at maxlength
const NOTES_MAX_LENGTH = 500
const notesMaxHint = (val: string | null) =>
    val?.length === NOTES_MAX_LENGTH ? "Notes were truncated at 500 characters — please verify your text" : ""

export { effortValueRules, requiredRule, nonNegativeRule, wholeNumberRule, percentRule, notesMaxHint }
