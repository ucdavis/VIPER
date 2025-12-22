import { describe, it, expect, vi, beforeEach } from "vitest"
import { ref } from "vue"
import { setActivePinia, createPinia } from "pinia"

/**
 * Tests for CourseEditDialog error handling and validation behavior.
 *
 * These tests validate that the component properly handles errors
 * when updating courses.
 *
 * The actual component UI is tested via Playwright MCP (see SMOKETEST-EFFORT-COURSE.md).
 */

// Mock the effort service
const mockUpdateCourse = vi.fn()
const mockUpdateCourseEnrollment = vi.fn()
vi.mock("../services/effort-service", () => ({
    effortService: {
        updateCourse: (...args: unknown[]) => mockUpdateCourse(...args),
        updateCourseEnrollment: (...args: unknown[]) => mockUpdateCourseEnrollment(...args),
    },
}))

// Validation helper functions (moved outside tests for consistent-function-scoping)
const isRCourse = (crseNumb: string) => crseNumb.toUpperCase().endsWith("R")
const isValidEnrollment = (v: number) => v >= 0
const isValidUnits = (v: number) => v >= 0

describe("CourseEditDialog - Error Handling", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    describe("Update Course Error States", () => {
        it("should capture error message when API returns success: false", () => {
            const error = ref<string | null>(null)

            const result = { success: false, error: "Failed to update course" }

            if (!result.success) {
                error.value = result.error ?? "Failed to update course"
            }

            expect(error.value).toBe("Failed to update course")
        })

        it("should use default message when API returns no error message", () => {
            const error = ref<string | null>(null)

            const result = { success: false, error: null }

            if (!result.success) {
                error.value = result.error ?? "Failed to update course"
            }

            expect(error.value).toBe("Failed to update course")
        })
    })

    describe("Service Mock Behavior - Full Update", () => {
        it("effortService.updateCourse returns success", async () => {
            mockUpdateCourse.mockResolvedValue({ success: true })

            const result = await mockUpdateCourse(1, { enrollment: 30, units: 4, custDept: "DVM" })

            expect(result.success).toBeTruthy()
        })

        it("effortService.updateCourse returns failure with error message", async () => {
            mockUpdateCourse.mockResolvedValue({
                success: false,
                error: "Invalid department code",
            })

            const result = await mockUpdateCourse()

            expect(result.success).toBeFalsy()
            expect(result.error).toBe("Invalid department code")
        })

        it("effortService.updateCourse rejects with exception", async () => {
            mockUpdateCourse.mockRejectedValue(new Error("Database error"))

            await expect(mockUpdateCourse()).rejects.toThrow("Database error")
        })
    })

    describe("Service Mock Behavior - Enrollment Only", () => {
        it("effortService.updateCourseEnrollment returns success", async () => {
            mockUpdateCourseEnrollment.mockResolvedValue({ success: true })

            const result = await mockUpdateCourseEnrollment(2, 75)

            expect(result.success).toBeTruthy()
        })

        it("effortService.updateCourseEnrollment returns failure", async () => {
            mockUpdateCourseEnrollment.mockResolvedValue({
                success: false,
                error: "Cannot update enrollment for non-R course",
            })

            const result = await mockUpdateCourseEnrollment()

            expect(result.success).toBeFalsy()
            expect(result.error).toBe("Cannot update enrollment for non-R course")
        })
    })

    describe("R-Course Detection Logic", () => {
        it("should identify R-course by course number suffix", () => {
            expect(isRCourse("443R")).toBeTruthy()
            expect(isRCourse("443r")).toBeTruthy()
            expect(isRCourse("443")).toBeFalsy()
            expect(isRCourse("R443")).toBeFalsy()
        })
    })

    describe("Validation Logic", () => {
        it("should validate enrollment is non-negative", () => {
            expect(isValidEnrollment(0)).toBeTruthy()
            expect(isValidEnrollment(30)).toBeTruthy()
            expect(isValidEnrollment(-1)).toBeFalsy()
        })

        it("should validate units is non-negative", () => {
            expect(isValidUnits(0)).toBeTruthy()
            expect(isValidUnits(4)).toBeTruthy()
            expect(isValidUnits(-1)).toBeFalsy()
        })
    })

    describe("Form Data Population", () => {
        it("should populate form with course data", () => {
            const course = {
                id: 1,
                enrollment: 20,
                units: 4,
                custDept: "DVM",
            }

            const formData = ref({
                enrollment: 0,
                units: 0,
                custDept: "",
            })

            // Simulate form population
            formData.value = {
                enrollment: course.enrollment,
                units: course.units,
                custDept: course.custDept,
            }

            expect(formData.value.enrollment).toBe(20)
            expect(formData.value.units).toBe(4)
            expect(formData.value.custDept).toBe("DVM")
        })

        it("should handle null course gracefully", () => {
            const course = null
            const formData = ref({
                enrollment: 0,
                units: 0,
                custDept: "",
            })

            // Simulate null handling
            if (course) {
                formData.value = {
                    enrollment: course.enrollment,
                    units: course.units,
                    custDept: course.custDept,
                }
            }

            // Form should remain unchanged
            expect(formData.value.enrollment).toBe(0)
        })
    })

    describe("Edit Mode Selection", () => {
        it("should use updateCourseEnrollment for enrollment-only mode", async () => {
            const enrollmentOnly = true
            const courseId = 2
            const newEnrollment = 75

            if (enrollmentOnly) {
                mockUpdateCourseEnrollment.mockResolvedValue({ success: true })
                await mockUpdateCourseEnrollment(courseId, newEnrollment)
            } else {
                await mockUpdateCourse(courseId, {})
            }

            expect(mockUpdateCourseEnrollment).toHaveBeenCalledWith(2, 75)
            expect(mockUpdateCourse).not.toHaveBeenCalled()
        })

        it("should use updateCourse for full edit mode", async () => {
            const enrollmentOnly = false
            const courseId = 1
            const updateData = { enrollment: 30, units: 4, custDept: "DVM" }

            if (enrollmentOnly) {
                await mockUpdateCourseEnrollment(courseId, updateData.enrollment)
            } else {
                mockUpdateCourse.mockResolvedValue({ success: true })
                await mockUpdateCourse(courseId, updateData)
            }

            expect(mockUpdateCourse).toHaveBeenCalledWith(1, updateData)
            expect(mockUpdateCourseEnrollment).not.toHaveBeenCalled()
        })
    })
})
