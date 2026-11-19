import { overlappingRCourses } from "../utils/r-course-overlap"
import type { InstructorEffortRecordDto } from "../types"

/**
 * Tests for overlappingRCourses: flags department R-courses with effort when the
 * generic RES 000R-001 also has effort.
 */

function record(
    courseCode: string,
    opts: { hours?: number | null; weeks?: number | null; generic?: boolean; rCourse?: boolean } = {},
): InstructorEffortRecordDto {
    return {
        hours: opts.hours ?? null,
        weeks: opts.weeks ?? null,
        course: {
            courseCode,
            seqNumb: "001",
            isGenericRCourse: opts.generic ?? false,
            isRCourse: opts.rCourse ?? opts.generic ?? false,
        },
    } as InstructorEffortRecordDto
}

const genericWithEffort = record("RES 000R", { hours: 5, generic: true })
const genericZero = record("RES 000R", { hours: 0, generic: true })
const deptR = record("VME 433R", { hours: 10, rCourse: true })

describe("overlappingRCourses util", () => {
    it("flags a department R-course when both have effort", () => {
        expect.hasAssertions()
        expect(overlappingRCourses([genericWithEffort, deptR])).toStrictEqual(["VME 433R-001"])
    })

    it("ignores the 0-hour generic placeholder", () => {
        expect.hasAssertions()
        expect(overlappingRCourses([genericZero, deptR])).toStrictEqual([])
    })

    it("ignores department R-courses with zero effort and non-R courses", () => {
        expect.hasAssertions()
        const zeroDept = record("VME 438R", { hours: 0, rCourse: true })
        const lecture = record("VME 400", { hours: 20 })
        expect(overlappingRCourses([genericWithEffort, zeroDept, lecture])).toStrictEqual([])
    })

    it("counts weeks as effort and lists each course once", () => {
        expect.hasAssertions()
        const weeksDept = record("VME 433R", { weeks: 2, rCourse: true })
        expect(overlappingRCourses([genericWithEffort, deptR, weeksDept])).toStrictEqual(["VME 433R-001"])
    })
})
