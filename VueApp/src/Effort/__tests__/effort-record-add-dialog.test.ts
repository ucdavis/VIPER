import { describe, it, expect } from "vitest"
import type { EffortTypeOptionDto } from "../types"

/**
 * Tests for EffortRecordAddDialog component logic.
 *
 * These tests validate the effort type filtering, course option handling,
 * and form validation logic used when adding effort records.
 */

// Type for course classification flags
interface CourseClassification {
    isDvm: boolean
    is199299: boolean
    isRCourse: boolean
}

/**
 * Filters effort types based on course classification.
 * Extracted from EffortRecordAddDialog.vue filteredEffortTypes computed.
 */
function filterEffortTypesForCourse(
    effortTypes: EffortTypeOptionDto[],
    course: CourseClassification,
    usedEffortTypesOnCourse: Set<string>,
): Array<EffortTypeOptionDto & { disable: boolean; alreadyUsed: boolean }> {
    return effortTypes
        .filter((et) => {
            if (course.isDvm && !et.allowedOnDvm) {return false}
            if (course.is199299 && !et.allowedOn199299) {return false}
            if (course.isRCourse && !et.allowedOnRCourses) {return false}
            return true
        })
        .map((et) => ({
            ...et,
            disable: usedEffortTypesOnCourse.has(et.id),
            alreadyUsed: usedEffortTypesOnCourse.has(et.id),
        }))
        .toSorted((a, b) => a.id.localeCompare(b.id))
}

/**
 * Gets the set of effort types already used on a specific course.
 * Extracted from EffortRecordAddDialog.vue usedEffortTypesOnCourse computed.
 */
function getUsedEffortTypesOnCourse(
    courseId: number | null,
    existingRecords: Array<{ courseId: number; effortType: string }>,
): Set<string> {
    if (!courseId || !existingRecords) {return new Set<string>()}
    return new Set(existingRecords.filter((r) => r.courseId === courseId).map((r) => r.effortType))
}

/**
 * Determines the effort label (Hours vs Weeks) based on effort type and term.
 * Extracted from EffortRecordAddDialog.vue effortLabel computed.
 */
function getEffortLabel(effortTypeId: string | null, effortTypes: EffortTypeOptionDto[], termCode: number): string {
    if (!effortTypeId) {return "Hours *"}
    const effortType = effortTypes.find((et) => et.id === effortTypeId)
    // CLI uses weeks starting at term 201604
    if (effortType?.usesWeeks && termCode >= 201_604) {
        return "Weeks *"
    }
    return "Hours *"
}

/**
 * Validates the effort value input.
 * Extracted from EffortRecordAddDialog.vue effortValueRules.
 */
function validateEffortValue(val: number | null): string | true {
    if (val === null) {return "Effort value is required"}
    if (!Number.isFinite(val)) {return "Effort value must be a valid number"}
    if (!Number.isInteger(val)) {return "Effort value must be a whole number"}
    if (val <= 0) {return "Effort value must be greater than zero"}
    return true
}

/**
 * Checks if the form is valid for submission.
 * Extracted from EffortRecordAddDialog.vue isFormValid computed.
 */
function isFormValid(
    selectedCourse: number | null,
    selectedEffortType: string | null,
    selectedRole: number | null,
    effortValue: number | null,
): boolean {
    const isValidEffortValue =
        effortValue !== null && Number.isFinite(effortValue) && Number.isInteger(effortValue) && effortValue > 0

    return selectedCourse !== null && selectedEffortType !== null && selectedRole !== null && isValidEffortValue
}

describe("EffortRecordAddDialog - Effort Type Filtering", () => {
    const effortTypes: EffortTypeOptionDto[] = [
        {
            id: "ADM",
            description: "Admin",
            usesWeeks: false,
            allowedOnDvm: true,
            allowedOn199299: true,
            allowedOnRCourses: true,
        },
        {
            id: "CLI",
            description: "Clinical",
            usesWeeks: true,
            allowedOnDvm: true,
            allowedOn199299: false,
            allowedOnRCourses: false,
        },
        {
            id: "LAB",
            description: "Laboratory",
            usesWeeks: false,
            allowedOnDvm: true,
            allowedOn199299: true,
            allowedOnRCourses: true,
        },
        {
            id: "LEC",
            description: "Lecture",
            usesWeeks: false,
            allowedOnDvm: true,
            allowedOn199299: true,
            allowedOnRCourses: true,
        },
        {
            id: "NDV",
            description: "Non-DVM Only",
            usesWeeks: false,
            allowedOnDvm: false,
            allowedOn199299: true,
            allowedOnRCourses: true,
        },
    ]

    describe("Regular course (no restrictions)", () => {
        it("returns all effort types for regular course", () => {
            const course: CourseClassification = { isDvm: false, is199299: false, isRCourse: false }
            const result = filterEffortTypesForCourse(effortTypes, course, new Set())

            expect(result).toHaveLength(5)
            expect(result.map((r) => r.id)).toEqual(["ADM", "CLI", "LAB", "LEC", "NDV"])
        })
    })

    describe("DVM course restrictions", () => {
        it("excludes effort types not allowed on DVM courses", () => {
            const course: CourseClassification = { isDvm: true, is199299: false, isRCourse: false }
            const result = filterEffortTypesForCourse(effortTypes, course, new Set())

            expect(result.map((r) => r.id)).not.toContain("NDV")
            expect(result).toHaveLength(4)
        })

        it("includes CLI on DVM courses", () => {
            const course: CourseClassification = { isDvm: true, is199299: false, isRCourse: false }
            const result = filterEffortTypesForCourse(effortTypes, course, new Set())

            expect(result.map((r) => r.id)).toContain("CLI")
        })
    })

    describe("199/299 course restrictions", () => {
        it("excludes CLI from 199/299 courses", () => {
            const course: CourseClassification = { isDvm: false, is199299: true, isRCourse: false }
            const result = filterEffortTypesForCourse(effortTypes, course, new Set())

            expect(result.map((r) => r.id)).not.toContain("CLI")
        })

        it("includes NDV on 199/299 courses", () => {
            const course: CourseClassification = { isDvm: false, is199299: true, isRCourse: false }
            const result = filterEffortTypesForCourse(effortTypes, course, new Set())

            expect(result.map((r) => r.id)).toContain("NDV")
        })
    })

    describe("R-course restrictions", () => {
        it("excludes CLI from R-courses", () => {
            const course: CourseClassification = { isDvm: false, is199299: false, isRCourse: true }
            const result = filterEffortTypesForCourse(effortTypes, course, new Set())

            expect(result.map((r) => r.id)).not.toContain("CLI")
        })

        it("includes ADM on R-courses", () => {
            const course: CourseClassification = { isDvm: false, is199299: false, isRCourse: true }
            const result = filterEffortTypesForCourse(effortTypes, course, new Set())

            expect(result.map((r) => r.id)).toContain("ADM")
        })
    })

    describe("Combined restrictions (AND logic)", () => {
        it("applies all restrictions for DVM 199 course", () => {
            const course: CourseClassification = { isDvm: true, is199299: true, isRCourse: false }
            const result = filterEffortTypesForCourse(effortTypes, course, new Set())

            // Should exclude NDV (not allowed on DVM) and CLI (not allowed on 199/299)
            expect(result.map((r) => r.id)).not.toContain("NDV")
            expect(result.map((r) => r.id)).not.toContain("CLI")
            expect(result).toHaveLength(3) // ADM, LAB, LEC
        })

        it("applies all restrictions for 299R course", () => {
            const course: CourseClassification = { isDvm: false, is199299: true, isRCourse: true }
            const result = filterEffortTypesForCourse(effortTypes, course, new Set())

            // CLI is not allowed on 199/299 OR R-courses (fails both)
            expect(result.map((r) => r.id)).not.toContain("CLI")
        })
    })

    describe("Already used effort types", () => {
        it("marks effort types already used on the course as disabled", () => {
            const course: CourseClassification = { isDvm: false, is199299: false, isRCourse: false }
            const usedTypes = new Set(["LEC", "LAB"])
            const result = filterEffortTypesForCourse(effortTypes, course, usedTypes)

            const lecType = result.find((r) => r.id === "LEC")
            const labType = result.find((r) => r.id === "LAB")
            const admType = result.find((r) => r.id === "ADM")

            expect(lecType?.disable).toBeTruthy()
            expect(lecType?.alreadyUsed).toBeTruthy()
            expect(labType?.disable).toBeTruthy()
            expect(admType?.disable).toBeFalsy()
        })
    })

    describe("Sorting", () => {
        it("returns effort types sorted alphabetically by ID", () => {
            const course: CourseClassification = { isDvm: false, is199299: false, isRCourse: false }
            const result = filterEffortTypesForCourse(effortTypes, course, new Set())

            const ids = result.map((r) => r.id)
            const sortedIds = [...ids].toSorted()
            expect(ids).toEqual(sortedIds)
        })
    })
})

describe("EffortRecordAddDialog - Used Effort Types Detection", () => {
    const existingRecords = [
        { courseId: 1, effortType: "LEC" },
        { courseId: 1, effortType: "LAB" },
        { courseId: 2, effortType: "CLI" },
        { courseId: 2, effortType: "LEC" },
    ]

    it("returns empty set when courseId is null", () => {
        const result = getUsedEffortTypesOnCourse(null, existingRecords)
        expect(result.size).toBe(0)
    })

    it("returns empty set when existingRecords is empty", () => {
        const result = getUsedEffortTypesOnCourse(1, [])
        expect(result.size).toBe(0)
    })

    it("returns effort types used on specific course", () => {
        const result = getUsedEffortTypesOnCourse(1, existingRecords)

        expect(result.has("LEC")).toBeTruthy()
        expect(result.has("LAB")).toBeTruthy()
        expect(result.has("CLI")).toBeFalsy()
        expect(result.size).toBe(2)
    })

    it("returns different effort types for different courses", () => {
        const result = getUsedEffortTypesOnCourse(2, existingRecords)

        expect(result.has("CLI")).toBeTruthy()
        expect(result.has("LEC")).toBeTruthy()
        expect(result.has("LAB")).toBeFalsy()
    })

    it("returns empty set for course with no records", () => {
        const result = getUsedEffortTypesOnCourse(999, existingRecords)
        expect(result.size).toBe(0)
    })
})

describe("EffortRecordAddDialog - Effort Label", () => {
    const effortTypes: EffortTypeOptionDto[] = [
        {
            id: "CLI",
            description: "Clinical",
            usesWeeks: true,
            allowedOnDvm: true,
            allowedOn199299: false,
            allowedOnRCourses: false,
        },
        {
            id: "LEC",
            description: "Lecture",
            usesWeeks: false,
            allowedOnDvm: true,
            allowedOn199299: true,
            allowedOnRCourses: true,
        },
    ]

    it("returns 'Hours *' when no effort type selected", () => {
        expect(getEffortLabel(null, effortTypes, 202_410)).toBe("Hours *")
    })

    it("returns 'Hours *' for non-clinical effort types", () => {
        expect(getEffortLabel("LEC", effortTypes, 202_410)).toBe("Hours *")
    })

    it("returns 'Weeks *' for CLI after term 201604", () => {
        expect(getEffortLabel("CLI", effortTypes, 201_604)).toBe("Weeks *")
        expect(getEffortLabel("CLI", effortTypes, 202_410)).toBe("Weeks *")
    })

    it("returns 'Hours *' for CLI before term 201604", () => {
        expect(getEffortLabel("CLI", effortTypes, 201_603)).toBe("Hours *")
        expect(getEffortLabel("CLI", effortTypes, 201_510)).toBe("Hours *")
    })

    it("returns 'Hours *' for unknown effort type", () => {
        expect(getEffortLabel("UNKNOWN", effortTypes, 202_410)).toBe("Hours *")
    })
})

describe("EffortRecordAddDialog - Effort Value Validation", () => {
    it("fails when value is null", () => {
        expect(validateEffortValue(null)).toBe("Effort value is required")
    })

    it("fails for non-finite values", () => {
        expect(validateEffortValue(Infinity)).toBe("Effort value must be a valid number")
        expect(validateEffortValue(Number.NaN)).toBe("Effort value must be a valid number")
    })

    it("fails for non-integer values", () => {
        expect(validateEffortValue(10.5)).toBe("Effort value must be a whole number")
        expect(validateEffortValue(0.1)).toBe("Effort value must be a whole number")
    })

    it("fails for zero or negative values", () => {
        expect(validateEffortValue(0)).toBe("Effort value must be greater than zero")
        expect(validateEffortValue(-1)).toBe("Effort value must be greater than zero")
        expect(validateEffortValue(-10)).toBe("Effort value must be greater than zero")
    })

    it("passes for positive integers", () => {
        expect(validateEffortValue(1)).toBeTruthy()
        expect(validateEffortValue(10)).toBeTruthy()
        expect(validateEffortValue(100)).toBeTruthy()
    })
})

describe("EffortRecordAddDialog - Form Validation", () => {
    it("returns false when course is not selected", () => {
        expect(isFormValid(null, "LEC", 1, 10)).toBeFalsy()
    })

    it("returns false when effort type is not selected", () => {
        expect(isFormValid(1, null, 1, 10)).toBeFalsy()
    })

    it("returns false when role is not selected", () => {
        expect(isFormValid(1, "LEC", null, 10)).toBeFalsy()
    })

    it("returns false when effort value is null", () => {
        expect(isFormValid(1, "LEC", 1, null)).toBeFalsy()
    })

    it("returns false when effort value is zero", () => {
        expect(isFormValid(1, "LEC", 1, 0)).toBeFalsy()
    })

    it("returns false when effort value is negative", () => {
        expect(isFormValid(1, "LEC", 1, -5)).toBeFalsy()
    })

    it("returns false when effort value is not an integer", () => {
        expect(isFormValid(1, "LEC", 1, 10.5)).toBeFalsy()
    })

    it("returns true when all fields are valid", () => {
        expect(isFormValid(1, "LEC", 1, 10)).toBeTruthy()
        expect(isFormValid(2, "CLI", 2, 1)).toBeTruthy()
    })
})
