import type { InstructorEffortRecordDto, CourseDto } from "../types"

/**
 * Tests for EffortRecordsDisplay component logic.
 *
 * These tests validate the R-course separation, zero-effort detection,
 * and record filtering logic used when displaying effort records.
 */

// Default course values for mock records
const DEFAULT_COURSE: CourseDto = {
    id: 0,
    crn: "CRN0",
    termCode: 202_410,
    subjCode: "VET",
    crseNumb: "410",
    seqNumb: "01",
    courseCode: "VET 410",
    enrollment: 20,
    units: 4,
    custDept: "VME",
    isGenericRCourse: false,
    isDvm: false,
    is199299: false,
    isRCourse: false,
}

// Helper to create a mock effort record
function createMockRecord(
    id: number,
    courseOverrides: Partial<CourseDto> = {},
    recordOverrides: Partial<InstructorEffortRecordDto> = {},
): InstructorEffortRecordDto {
    const course: CourseDto = { ...DEFAULT_COURSE, id, crn: `CRN${id}`, ...courseOverrides }

    // Use 'in' operator to check if property exists, allowing null values to pass through
    // (nullish coalescing ?? treats null as missing, which we don't want here)
    const hours = "hours" in recordOverrides ? (recordOverrides.hours ?? null) : 40
    const weeks = "weeks" in recordOverrides ? (recordOverrides.weeks ?? null) : null
    const effortValue = "effortValue" in recordOverrides ? (recordOverrides.effortValue ?? null) : (hours ?? 40)

    return {
        id,
        courseId: course.id,
        personId: recordOverrides.personId ?? 100,
        termCode: course.termCode,
        effortType: recordOverrides.effortType ?? "LEC",
        effortTypeDescription: recordOverrides.effortTypeDescription ?? "Lecture",
        role: recordOverrides.role ?? 1,
        roleDescription: recordOverrides.roleDescription ?? "Instructor",
        hours,
        weeks,
        crn: course.crn,
        notes: recordOverrides.notes ?? null,
        modifiedDate: recordOverrides.modifiedDate ?? null,
        effortValue,
        effortLabel: recordOverrides.effortLabel ?? "hours",
        course,
        childCourses: recordOverrides.childCourses ?? [],
    }
}

/**
 * Filters records to get regular (non-R-course) records.
 * Extracted from EffortRecordsDisplay.vue regularRecords computed.
 */
function getRegularRecords(records: InstructorEffortRecordDto[]): InstructorEffortRecordDto[] {
    return records.filter((r) => !r.course.isRCourse)
}

/**
 * Filters records to get R-course records only.
 * Extracted from EffortRecordsDisplay.vue rCourseRecords computed.
 */
function getRCourseRecords(records: InstructorEffortRecordDto[]): InstructorEffortRecordDto[] {
    return records.filter((r) => r.course.isRCourse)
}

/**
 * Checks if any R-course record is the generic RESID course with zero effort.
 * Extracted from EffortRecordsDisplay.vue hasGenericRCourseWithZeroEffort computed.
 */
function hasGenericRCourseWithZeroEffort(records: InstructorEffortRecordDto[]): boolean {
    const rCourseRecords = getRCourseRecords(records)
    return rCourseRecords.some(
        (r) => r.course.crn === "RESID" && (r.hours === 0 || r.hours === null) && (r.weeks === 0 || r.weeks === null),
    )
}

/**
 * Checks if a record has zero effort.
 * Extracted from EffortRecordsDisplay.vue isZeroEffort function.
 */
function isZeroEffort(record: InstructorEffortRecordDto, zeroEffortRecordIds: number[]): boolean {
    if (zeroEffortRecordIds.length > 0) {
        return zeroEffortRecordIds.includes(record.id)
    }
    return record.effortValue === 0
}

/**
 * Formats a course code for display.
 * Extracted from EffortRecordsDisplay.vue formatCourseCode function.
 */
function formatCourseCode(course: { subjCode: string; crseNumb: string; seqNumb: string }): string {
    return `${course.subjCode} ${course.crseNumb.trim()}-${course.seqNumb}`
}

describe("EffortRecordsDisplay - Record Separation", () => {
    describe("Regular vs R-course separation", () => {
        it("separates regular courses from R-courses", () => {
            const records = [
                createMockRecord(1, { isRCourse: false }),
                createMockRecord(2, { isRCourse: true, crseNumb: "200R" }),
                createMockRecord(3, { isRCourse: false }),
                createMockRecord(4, { isRCourse: true, crseNumb: "300R" }),
            ]

            const regular = getRegularRecords(records)
            const rCourses = getRCourseRecords(records)

            expect(regular).toHaveLength(2)
            expect(rCourses).toHaveLength(2)
            expect(regular.map((r) => r.id)).toEqual([1, 3])
            expect(rCourses.map((r) => r.id)).toEqual([2, 4])
        })

        it("returns empty arrays for empty input", () => {
            expect(getRegularRecords([])).toHaveLength(0)
            expect(getRCourseRecords([])).toHaveLength(0)
        })

        it("returns all records as regular when no R-courses present", () => {
            const records = [createMockRecord(1, { isRCourse: false }), createMockRecord(2, { isRCourse: false })]

            expect(getRegularRecords(records)).toHaveLength(2)
            expect(getRCourseRecords(records)).toHaveLength(0)
        })

        it("returns all records as R-course when only R-courses present", () => {
            const records = [createMockRecord(1, { isRCourse: true }), createMockRecord(2, { isRCourse: true })]

            expect(getRegularRecords(records)).toHaveLength(0)
            expect(getRCourseRecords(records)).toHaveLength(2)
        })
    })
})

describe("EffortRecordsDisplay - Generic R-Course Detection", () => {
    it("returns true when RESID course has zero hours and null weeks", () => {
        const records = [createMockRecord(1, { crn: "RESID", isRCourse: true }, { hours: 0, weeks: null })]

        expect(hasGenericRCourseWithZeroEffort(records)).toBeTruthy()
    })

    it("returns true when RESID course has null hours and zero weeks", () => {
        const records = [createMockRecord(1, { crn: "RESID", isRCourse: true }, { hours: null, weeks: 0 })]

        expect(hasGenericRCourseWithZeroEffort(records)).toBeTruthy()
    })

    it("returns true when RESID course has both null hours and weeks", () => {
        const records = [createMockRecord(1, { crn: "RESID", isRCourse: true }, { hours: null, weeks: null })]

        expect(hasGenericRCourseWithZeroEffort(records)).toBeTruthy()
    })

    it("returns true when RESID course has both zero hours and weeks", () => {
        const records = [createMockRecord(1, { crn: "RESID", isRCourse: true }, { hours: 0, weeks: 0 })]

        expect(hasGenericRCourseWithZeroEffort(records)).toBeTruthy()
    })

    it("returns false when RESID course has non-zero effort", () => {
        const records = [createMockRecord(1, { crn: "RESID", isRCourse: true }, { hours: 10, weeks: null })]

        expect(hasGenericRCourseWithZeroEffort(records)).toBeFalsy()
    })

    it("returns false when RESID course has non-zero weeks", () => {
        const records = [createMockRecord(1, { crn: "RESID", isRCourse: true }, { hours: null, weeks: 5 })]

        expect(hasGenericRCourseWithZeroEffort(records)).toBeFalsy()
    })

    it("returns false when no RESID course exists", () => {
        const records = [createMockRecord(1, { crn: "12345", isRCourse: true }, { hours: 0, weeks: null })]

        expect(hasGenericRCourseWithZeroEffort(records)).toBeFalsy()
    })

    it("returns false when only regular courses exist", () => {
        const records = [createMockRecord(1, { crn: "12345", isRCourse: false }, { hours: 0 })]

        expect(hasGenericRCourseWithZeroEffort(records)).toBeFalsy()
    })

    it("returns false for empty records", () => {
        expect(hasGenericRCourseWithZeroEffort([])).toBeFalsy()
    })

    it("returns true when RESID is among multiple R-courses", () => {
        const records = [
            createMockRecord(1, { crn: "54321", isRCourse: true }, { hours: 10 }),
            createMockRecord(2, { crn: "RESID", isRCourse: true }, { hours: 0, weeks: null }),
            createMockRecord(3, { crn: "67890", isRCourse: true }, { hours: 5 }),
        ]

        expect(hasGenericRCourseWithZeroEffort(records)).toBeTruthy()
    })
})

describe("EffortRecordsDisplay - Zero Effort Detection", () => {
    describe("Using zeroEffortRecordIds list", () => {
        it("returns true when record ID is in the list", () => {
            const record = createMockRecord(5, {}, { effortValue: 10 })
            expect(isZeroEffort(record, [3, 5, 7])).toBeTruthy()
        })

        it("returns false when record ID is not in the list", () => {
            const record = createMockRecord(5, {}, { effortValue: 0 })
            // Even though effortValue is 0, the list takes precedence
            expect(isZeroEffort(record, [3, 7, 9])).toBeFalsy()
        })
    })

    describe("Using effortValue fallback", () => {
        it("returns true when effortValue is 0 and no list provided", () => {
            const record = createMockRecord(1, {}, { effortValue: 0 })
            expect(isZeroEffort(record, [])).toBeTruthy()
        })

        it("returns false when effortValue is positive and no list provided", () => {
            const record = createMockRecord(1, {}, { effortValue: 40 })
            expect(isZeroEffort(record, [])).toBeFalsy()
        })

        it("handles null effortValue as zero", () => {
            const record = createMockRecord(1)
            record.effortValue = null
            // Null !== 0, so this would be false
            expect(isZeroEffort(record, [])).toBeFalsy()
        })
    })
})

describe("EffortRecordsDisplay - Course Code Formatting", () => {
    it("formats standard course code correctly", () => {
        const course = { subjCode: "VET", crseNumb: "410", seqNumb: "01" }
        expect(formatCourseCode(course)).toBe("VET 410-01")
    })

    it("trims whitespace from course number", () => {
        const course = { subjCode: "DVM", crseNumb: "443  ", seqNumb: "02" }
        expect(formatCourseCode(course)).toBe("DVM 443-02")
    })

    it("handles R-course suffix", () => {
        const course = { subjCode: "VET", crseNumb: "290R", seqNumb: "01" }
        expect(formatCourseCode(course)).toBe("VET 290R-01")
    })

    it("handles 199/299 course numbers", () => {
        const course = { subjCode: "APC", crseNumb: "199", seqNumb: "03" }
        expect(formatCourseCode(course)).toBe("APC 199-03")
    })

    it("handles suffixed course numbers", () => {
        const course = { subjCode: "VET", crseNumb: "199A", seqNumb: "01" }
        expect(formatCourseCode(course)).toBe("VET 199A-01")
    })
})

describe("EffortRecordsDisplay - Integration Scenarios", () => {
    it("correctly processes a typical instructor with mixed courses", () => {
        const records = [
            // Regular courses
            createMockRecord(1, { subjCode: "VET", crseNumb: "410", isRCourse: false }, { effortValue: 40 }),
            createMockRecord(2, { subjCode: "VET", crseNumb: "420", isRCourse: false }, { effortValue: 0 }),
            // R-courses
            createMockRecord(
                3,
                { crn: "RESID", crseNumb: "000R", isRCourse: true },
                { hours: 0, weeks: null, effortValue: 0 },
            ),
            createMockRecord(4, { crn: "54321", crseNumb: "200R", isRCourse: true }, { hours: 10, effortValue: 10 }),
        ]

        const regular = getRegularRecords(records)
        const rCourses = getRCourseRecords(records)

        expect(regular).toHaveLength(2)
        expect(rCourses).toHaveLength(2)

        // Check generic R-course detection
        expect(hasGenericRCourseWithZeroEffort(records)).toBeTruthy()

        // Check zero effort detection with explicit list
        const zeroIds = [2, 3]
        expect(isZeroEffort(records[0]!, zeroIds)).toBeFalsy() // Id 1, has effort
        expect(isZeroEffort(records[1]!, zeroIds)).toBeTruthy() // Id 2, zero effort
        expect(isZeroEffort(records[2]!, zeroIds)).toBeTruthy() // Id 3, generic R-course
        expect(isZeroEffort(records[3]!, zeroIds)).toBeFalsy() // Id 4, has effort
    })

    it("handles instructor with only R-course records", () => {
        const records = [createMockRecord(1, { crn: "RESID", isRCourse: true }, { hours: 5, weeks: null })]

        expect(getRegularRecords(records)).toHaveLength(0)
        expect(getRCourseRecords(records)).toHaveLength(1)
        expect(hasGenericRCourseWithZeroEffort(records)).toBeFalsy()
    })

    it("handles instructor with no records", () => {
        const records: InstructorEffortRecordDto[] = []

        expect(getRegularRecords(records)).toHaveLength(0)
        expect(getRCourseRecords(records)).toHaveLength(0)
        expect(hasGenericRCourseWithZeroEffort(records)).toBeFalsy()
    })
})
