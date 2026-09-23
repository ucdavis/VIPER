import type { CareerDropdownOption, StudentInfo } from "../types"

/**
 * A choice counts as answered unless it is the catch-all, which also needs its free text.
 * Mirrors IsSelectionComplete in CareerSelectionService.cs.
 */
function isSelectionComplete(option: CareerDropdownOption | null, otherText: string | null): boolean {
    return option !== null && (!option.isOther || Boolean(otherText?.trim()))
}

/**
 * The fields the form still wants an answer for, in the order they appear on the page.
 */
function missingFieldLabels(studentInfo: StudentInfo): string[] {
    const checks: [complete: boolean, label: string][] = [
        [isSelectionComplete(studentInfo.direction, studentInfo.directionOther), "Career Direction"],
        [isSelectionComplete(studentInfo.primaryFocus, studentInfo.primaryFocusOther), "Primary Focus"],
        // Optional field, but still flag in the warning.
        [isSelectionComplete(studentInfo.secondaryFocus, studentInfo.secondaryFocusOther), "Secondary Focus"],
        // Post-graduation plans lack their own Other field and use short term plans instead.
        [isSelectionComplete(studentInfo.postGrad, studentInfo.shortTermPlans), "Post-Graduation Plans"],
        [Boolean(studentInfo.shortTermPlans?.trim()), "Short Term Plans"],
        [Boolean(studentInfo.longTermPlans?.trim()), "Long Term Plans"],
    ]
    return checks.filter(([complete]) => !complete).map(([, label]) => label)
}

export { isSelectionComplete, missingFieldLabels }
