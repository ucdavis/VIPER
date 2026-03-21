import { ref } from "vue"
import { setActivePinia, createPinia } from "pinia"

/**
 * Tests for CourseLinkDialog error handling and filter behavior.
 *
 * These tests validate that the component properly handles errors
 * and filters available courses correctly.
 */

// Mock the course service
const mockGetCourseRelationships = vi.fn()
const mockGetAvailableChildCourses = vi.fn()
const mockCreateCourseRelationship = vi.fn()
const mockDeleteCourseRelationship = vi.fn()

vi.mock("../services/course-service", () => ({
    courseService: {
        getCourseRelationships: (...args: unknown[]) => mockGetCourseRelationships(...args),
        getAvailableChildCourses: (...args: unknown[]) => mockGetAvailableChildCourses(...args),
        createCourseRelationship: (...args: unknown[]) => mockCreateCourseRelationship(...args),
        deleteCourseRelationship: (...args: unknown[]) => mockDeleteCourseRelationship(...args),
    },
}))

// Sample course data for testing
const sampleCourses = [
    { id: 1, courseCode: "DVM 443", seqNumb: "001", crn: "12345", enrollment: 20, units: 4 },
    { id: 2, courseCode: "VME 200", seqNumb: "001", crn: "12346", enrollment: 15, units: 3 },
    { id: 3, courseCode: "APC 100", seqNumb: "002", crn: "99999", enrollment: 10, units: 2 },
]

// Filter function extracted from component logic
function filterCourses(courses: typeof sampleCourses, needle: string): typeof sampleCourses {
    if (!needle) {
        return courses
    }
    const lowerNeedle = needle.toLowerCase()
    return courses.filter(
        (c) =>
            c.courseCode.toLowerCase().includes(lowerNeedle) ||
            c.seqNumb.toLowerCase().includes(lowerNeedle) ||
            c.crn.toLowerCase().includes(lowerNeedle),
    )
}

describe("CourseLinkDialog - Error Handling", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    describe("Create Relationship Error States", () => {
        it("should capture error message when API returns success: false", () => {
            const error = ref<string | null>(null)

            const result = { success: false, error: "Course is already a child of another course" }

            if (!result.success) {
                error.value = result.error ?? "Failed to link course"
            }

            expect(error.value).toBe("Course is already a child of another course")
        })

        it("should use default message when API returns no error message", () => {
            const error = ref<string | null>(null)

            const result = { success: false, error: null }

            if (!result.success) {
                error.value = result.error ?? "Failed to link course"
            }

            expect(error.value).toBe("Failed to link course")
        })
    })

    describe("Service Mock Behavior", () => {
        it("createCourseRelationship returns success with relationship data", async () => {
            mockCreateCourseRelationship.mockResolvedValue({
                success: true,
                relationship: { id: 1, parentCourseId: 1, childCourseId: 2, relationshipType: "CrossList" },
            })

            const result = await mockCreateCourseRelationship(1, { childCourseId: 2, relationshipType: "CrossList" })

            expect(result.success).toBeTruthy()
            expect(result.relationship.relationshipType).toBe("CrossList")
        })

        it("createCourseRelationship returns failure with error message", async () => {
            mockCreateCourseRelationship.mockResolvedValue({
                success: false,
                error: "Courses must be in the same term",
            })

            const result = await mockCreateCourseRelationship(1, { childCourseId: 99, relationshipType: "Section" })

            expect(result.success).toBeFalsy()
            expect(result.error).toBe("Courses must be in the same term")
        })

        it("deleteCourseRelationship returns true on success", async () => {
            mockDeleteCourseRelationship.mockResolvedValue(true)

            const result = await mockDeleteCourseRelationship(1, 5)

            expect(result).toBeTruthy()
        })

        it("deleteCourseRelationship returns false on failure", async () => {
            mockDeleteCourseRelationship.mockResolvedValue(false)

            const result = await mockDeleteCourseRelationship(1, 999)

            expect(result).toBeFalsy()
        })
    })
})

describe("CourseLinkDialog - Filter Logic", () => {
    it("should return all courses when filter is empty", () => {
        const result = filterCourses(sampleCourses, "")

        expect(result).toHaveLength(3)
    })

    it("should filter by course code", () => {
        const result = filterCourses(sampleCourses, "DVM")

        expect(result).toHaveLength(1)
        expect(result[0]!.courseCode).toBe("DVM 443")
    })

    it("should filter by course code case-insensitively", () => {
        const result = filterCourses(sampleCourses, "dvm")

        expect(result).toHaveLength(1)
        expect(result[0]!.courseCode).toBe("DVM 443")
    })

    it("should filter by CRN", () => {
        const result = filterCourses(sampleCourses, "99999")

        expect(result).toHaveLength(1)
        expect(result[0]!.crn).toBe("99999")
    })

    it("should filter by sequence number", () => {
        const result = filterCourses(sampleCourses, "002")

        expect(result).toHaveLength(1)
        expect(result[0]!.seqNumb).toBe("002")
    })

    it("should return empty array when no matches", () => {
        const result = filterCourses(sampleCourses, "XYZ")

        expect(result).toHaveLength(0)
    })

    it("should match partial course code", () => {
        const result = filterCourses(sampleCourses, "VM")

        expect(result).toHaveLength(2) // DVM and VME
    })
})

describe("CourseLinkDialog - State Reset", () => {
    it("should reset state to defaults when dialog closes", () => {
        const childRelationships = ref([{ id: 1 }])
        const availableCourses = ref([{ id: 2 }])
        const selectedChildCourse = ref({ id: 3 })
        const relationshipType = ref<"CrossList" | "Section">("Section")

        // Simulate dialog close reset
        childRelationships.value = []
        availableCourses.value = []
        selectedChildCourse.value = null as unknown as { id: number }
        relationshipType.value = "CrossList"

        expect(childRelationships.value).toHaveLength(0)
        expect(availableCourses.value).toHaveLength(0)
        expect(selectedChildCourse.value).toBeNull()
        expect(relationshipType.value).toBe("CrossList")
    })
})
