const LOCAL_DIGITS = 7
const FULL_DIGITS = 10
const AREA_CODE_END = 3
const EXCHANGE_END = 6

/** Strip all non-digit characters from a string. */
function stripToDigits(value: string): string {
    return value.replaceAll(/\D/g, "")
}

/**
 * Format a digit string for display.
 * 7 digits → "123-4567", 10 digits → "(123) 456-7890", else returns as-is.
 */
function formatPhone(digits: string): string {
    if (digits.length === LOCAL_DIGITS) {
        return `${digits.slice(0, AREA_CODE_END)}-${digits.slice(AREA_CODE_END)}`
    }
    if (digits.length === FULL_DIGITS) {
        return `(${digits.slice(0, AREA_CODE_END)}) ${digits.slice(AREA_CODE_END, EXCHANGE_END)}-${digits.slice(EXCHANGE_END)}`
    }
    return digits
}

/** Returns true for empty, 7-digit, or 10-digit strings. */
function isValidPhone(digits: string): boolean {
    return digits.length === 0 || digits.length === LOCAL_DIGITS || digits.length === FULL_DIGITS
}

export { stripToDigits, formatPhone, isValidPhone }
