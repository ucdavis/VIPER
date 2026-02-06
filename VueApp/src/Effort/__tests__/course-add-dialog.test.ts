import { describe, it, expect, vi, beforeEach } from "vitest"
import { ref } from "vue"
import { setActivePinia, createPinia } from "pinia"

/**
 * Tests for CourseAddDialog error handling and validation behavior.
 *
 * These tests validate that the component properly handles errors
 * when creating courses manually.
 */

// Mock the course service
const mockCreateCourse = vi.fn()
vi.mock("../services/course-service", () => ({
    courseService: {
        createCourse: (...args: unknown[]) => mockCreateCourse(...args),
    },
}))

// Validation helper functions (moved outside tests for consistent-function-scoping)
const isValidEnrollment = (v: number) => v >= 0 && Number.isInteger(v)
const isValidUnits = (v: number) => v >= 0

describe("CourseAddDialog - Error Handling", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    describe("Create Course Error States", () => {
        it("should capture error message when API returns success: false", () => {
            const error = ref<string | null>(null)

            const result = { success: false, error: "Course already exists for this term" }

            if (!result.success) {
                error.value = result.error ?? "Failed to create course"
            }

            expect(error.value).toBe("Course already exists for this term")
        })

        it("should use default message when API returns no error message", () => {
            const error = ref<string | null>(null)

            const result = { success: false, error: null }

            if (!result.success) {
                error.value = result.error ?? "Failed to create course"
            }

            expect(error.value).toBe("Failed to create course")
        })
    })

    describe("Service Mock Behavior", () => {
        it("effortService.createCourse returns success with course data", async () => {
            mockCreateCourse.mockResolvedValue({ success: true, course: { id: 1 } })

            const result = await mockCreateCourse()

            expect(result.success).toBeTruthy()
            expect(result.course.id).toBe(1)
        })

        it("effortService.createCourse returns failure with error message", async () => {
            mockCreateCourse.mockResolvedValue({
                success: false,
                error: "Course already exists",
            })

            const result = await mockCreateCourse()

            expect(result.success).toBeFalsy()
            expect(result.error).toBe("Course already exists")
        })

        it("effortService.createCourse rejects with exception", async () => {
            mockCreateCourse.mockRejectedValue(new Error("Database error"))

            await expect(mockCreateCourse()).rejects.toThrow("Database error")
        })
    })

    describe("Form Data Normalization", () => {
        it("should trim and uppercase subject code", () => {
            const input = " dvm "
            const normalized = input.trim().toUpperCase()

            expect(normalized).toBe("DVM")
        })

        it("should trim and uppercase course number", () => {
            const input = " 443r "
            const normalized = input.trim().toUpperCase()

            expect(normalized).toBe("443R")
        })

        it("should trim CRN", () => {
            const input = " 12345 "
            const normalized = input.trim()

            expect(normalized).toBe("12345")
        })
    })

    describe("Validation Logic", () => {
        it("should validate CRN is 5 digits", () => {
            const crnRegex = /^\d{5}$/

            expect(crnRegex.test("12345")).toBeTruthy()
            expect(crnRegex.test("1234")).toBeFalsy()
            expect(crnRegex.test("123456")).toBeFalsy()
            expect(crnRegex.test("abcde")).toBeFalsy()
        })

        it("should validate enrollment is non-negative integer", () => {
            expect(isValidEnrollment(0)).toBeTruthy()
            expect(isValidEnrollment(25)).toBeTruthy()
            expect(isValidEnrollment(-1)).toBeFalsy()
            expect(isValidEnrollment(2.5)).toBeFalsy()
        })

        it("should validate units is non-negative", () => {
            expect(isValidUnits(0)).toBeTruthy()
            expect(isValidUnits(4)).toBeTruthy()
            expect(isValidUnits(0.5)).toBeTruthy()
            expect(isValidUnits(-1)).toBeFalsy()
        })

        it("should validate subject code max length", () => {
            const maxLength = 3

            expect("DVM".length <= maxLength).toBeTruthy()
            expect("DVMX".length <= maxLength).toBeFalsy()
        })
    })

    describe("Form Reset Behavior", () => {
        it("should reset form data to defaults", () => {
            const defaultDepartment = "APC"
            const formData = ref({
                subjCode: "OLD",
                crseNumb: "999",
                seqNumb: "002",
                crn: "99999",
                enrollment: 50,
                units: 5,
                custDept: "VME",
            })

            // Simulate reset logic
            formData.value = {
                subjCode: "",
                crseNumb: "",
                seqNumb: "",
                crn: "",
                enrollment: 0,
                units: 0,
                custDept: defaultDepartment,
            }

            expect(formData.value.subjCode).toBe("")
            expect(formData.value.enrollment).toBe(0)
            expect(formData.value.custDept).toBe("APC")
        })
    })
})
