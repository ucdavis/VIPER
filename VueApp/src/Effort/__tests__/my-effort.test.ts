import { describe, it, expect } from "vitest"
import type { EffortTypeOptionDto } from "../types"

/**
 * Tests for MyEffort page logic.
 *
 * These tests validate the restricted effort type detection logic used
 * when warning users about deleting effort records that cannot be re-added.
 */

// Type definitions matching the component's usage
interface CourseClassificationFlags {
    isDvm: boolean
    is199299: boolean
    isRCourse: boolean
}

interface MockRecord {
    effortType: string
    course: CourseClassificationFlags
}

/**
 * Pure function extracted from MyEffort.vue for testing.
 * Checks if the effort type is restricted for the given course classification.
 */
function isRestrictedEffortType(record: MockRecord, effortTypes: EffortTypeOptionDto[]): boolean {
    const effortType = effortTypes.find((et) => et.id === record.effortType)
    if (!effortType) {
        return false
    }

    const { isDvm, is199299, isRCourse } = record.course
    // Check each classification - if course has it AND type is not allowed, it's restricted
    if (isDvm && !effortType.allowedOnDvm) {
        return true
    }
    if (is199299 && !effortType.allowedOn199299) {
        return true
    }
    if (isRCourse && !effortType.allowedOnRCourses) {
        return true
    }
    return false
}

describe("MyEffort - Restricted Effort Type Detection", () => {
    // Default effort types for testing
    const effortTypes: EffortTypeOptionDto[] = [
        {
            id: "LEC",
            description: "Lecture",
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
            id: "NDV",
            description: "Non-DVM Only",
            usesWeeks: false,
            allowedOnDvm: false,
            allowedOn199299: true,
            allowedOnRCourses: true,
        },
        {
            id: "N19",
            description: "No 199/299",
            usesWeeks: false,
            allowedOnDvm: true,
            allowedOn199299: false,
            allowedOnRCourses: true,
        },
        {
            id: "NRC",
            description: "No R-Courses",
            usesWeeks: false,
            allowedOnDvm: true,
            allowedOn199299: true,
            allowedOnRCourses: false,
        },
    ]

    describe("Non-restricted cases", () => {
        it("returns false when effort type is allowed on all course types", () => {
            const record: MockRecord = {
                effortType: "LEC",
                course: { isDvm: true, is199299: true, isRCourse: true },
            }

            expect(isRestrictedEffortType(record, effortTypes)).toBeFalsy()
        })

        it("returns false when effort type is not found", () => {
            const record: MockRecord = {
                effortType: "UNKNOWN",
                course: { isDvm: true, is199299: false, isRCourse: false },
            }

            expect(isRestrictedEffortType(record, effortTypes)).toBeFalsy()
        })

        it("returns false for regular course with any effort type", () => {
            const record: MockRecord = {
                effortType: "NDV",
                course: { isDvm: false, is199299: false, isRCourse: false },
            }

            expect(isRestrictedEffortType(record, effortTypes)).toBeFalsy()
        })
    })

    describe("DVM course restrictions", () => {
        it("returns true when effort type is not allowed on DVM courses", () => {
            const record: MockRecord = {
                effortType: "NDV",
                course: { isDvm: true, is199299: false, isRCourse: false },
            }

            expect(isRestrictedEffortType(record, effortTypes)).toBeTruthy()
        })

        it("returns false when effort type is allowed on DVM courses", () => {
            const record: MockRecord = {
                effortType: "LEC",
                course: { isDvm: true, is199299: false, isRCourse: false },
            }

            expect(isRestrictedEffortType(record, effortTypes)).toBeFalsy()
        })
    })

    describe("199/299 course restrictions", () => {
        it("returns true when effort type is not allowed on 199/299 courses", () => {
            const record: MockRecord = {
                effortType: "N19",
                course: { isDvm: false, is199299: true, isRCourse: false },
            }

            expect(isRestrictedEffortType(record, effortTypes)).toBeTruthy()
        })

        it("returns true for CLI on 199/299 course", () => {
            const record: MockRecord = {
                effortType: "CLI",
                course: { isDvm: false, is199299: true, isRCourse: false },
            }

            expect(isRestrictedEffortType(record, effortTypes)).toBeTruthy()
        })

        it("returns false when effort type is allowed on 199/299 courses", () => {
            const record: MockRecord = {
                effortType: "LEC",
                course: { isDvm: false, is199299: true, isRCourse: false },
            }

            expect(isRestrictedEffortType(record, effortTypes)).toBeFalsy()
        })
    })

    describe("R-course restrictions", () => {
        it("returns true when effort type is not allowed on R courses", () => {
            const record: MockRecord = {
                effortType: "NRC",
                course: { isDvm: false, is199299: false, isRCourse: true },
            }

            expect(isRestrictedEffortType(record, effortTypes)).toBeTruthy()
        })

        it("returns true for CLI on R course", () => {
            const record: MockRecord = {
                effortType: "CLI",
                course: { isDvm: false, is199299: false, isRCourse: true },
            }

            expect(isRestrictedEffortType(record, effortTypes)).toBeTruthy()
        })

        it("returns false when effort type is allowed on R courses", () => {
            const record: MockRecord = {
                effortType: "LEC",
                course: { isDvm: false, is199299: false, isRCourse: true },
            }

            expect(isRestrictedEffortType(record, effortTypes)).toBeFalsy()
        })
    })

    describe("Multiple classification restrictions (AND logic)", () => {
        it("returns true when any classification is restricted - DVM + 199/299", () => {
            // NDV is not allowed on DVM, so should be restricted even if allowed on 199/299
            const record: MockRecord = {
                effortType: "NDV",
                course: { isDvm: true, is199299: true, isRCourse: false },
            }

            expect(isRestrictedEffortType(record, effortTypes)).toBeTruthy()
        })

        it("returns true when any classification is restricted - DVM + R-course", () => {
            // NDV is not allowed on DVM, so should be restricted
            const record: MockRecord = {
                effortType: "NDV",
                course: { isDvm: true, is199299: false, isRCourse: true },
            }

            expect(isRestrictedEffortType(record, effortTypes)).toBeTruthy()
        })

        it("returns true for 299R course with CLI (both 199/299 and R restricted)", () => {
            // CLI is not allowed on 199/299 OR R courses
            const record: MockRecord = {
                effortType: "CLI",
                course: { isDvm: false, is199299: true, isRCourse: true },
            }

            expect(isRestrictedEffortType(record, effortTypes)).toBeTruthy()
        })

        it("returns false when all applicable classifications allow the type", () => {
            // LEC is allowed on all course types
            const record: MockRecord = {
                effortType: "LEC",
                course: { isDvm: true, is199299: true, isRCourse: true },
            }

            expect(isRestrictedEffortType(record, effortTypes)).toBeFalsy()
        })
    })
})
