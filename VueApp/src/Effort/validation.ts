/**
 * Shared validation rules for the Effort module.
 * These rules are used by Quasar form components (:rules prop).
 */

/**
 * Validation rules for effort value input fields.
 * Ensures the value is a required, positive integer.
 */
export const effortValueRules = [
    (val: number | null) => val !== null || "Effort value is required",
    (val: number | null) => val === null || Number.isFinite(val) || "Effort value must be a valid number",
    (val: number | null) => val === null || Number.isInteger(val) || "Effort value must be a whole number",
    (val: number | null) => val === null || val > 0 || "Effort value must be greater than zero",
]
