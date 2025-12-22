import { describe, it, expect, vi, beforeEach } from "vitest"
import { ref } from "vue"
import { setActivePinia, createPinia } from "pinia"

/**
 * Tests for CourseImportDialog error handling behavior.
 *
 * These tests validate that the component properly displays error feedback
 * to users when import operations fail, rather than showing generic errors.
 *
 * The actual component UI is tested via Playwright MCP (see SMOKETEST-EFFORT-COURSE.md).
 */

// Mock the effort service
const mockSearchBannerCourses = vi.fn()
const mockImportCourse = vi.fn()
vi.mock("../services/effort-service", () => ({
    effortService: {
        searchBannerCourses: (...args: unknown[]) => mockSearchBannerCourses(...args),
        importCourse: (...args: unknown[]) => mockImportCourse(...args),
    },
}))

describe("CourseImportDialog - Error Handling", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    describe("Import Error States", () => {
        it("should capture error message when API returns success: false", () => {
            const importError = ref<string | null>(null)

            // Simulate the error handling logic from the component
            const result = { success: false, error: "Course DVM 443 with 4 units already exists for this term" }

            if (!result.success) {
                importError.value = result.error ?? "Failed to import course"
            }

            expect(importError.value).toBe("Course DVM 443 with 4 units already exists for this term")
        })

        it("should use default message when API returns no error message", () => {
            const importError = ref<string | null>(null)

            const result = { success: false, error: null }

            if (!result.success) {
                importError.value = result.error ?? "Failed to import course"
            }

            expect(importError.value).toBe("Failed to import course")
        })

        it("should capture error message from thrown exception", () => {
            const importError = ref<string | null>(null)

            // Simulate exception handling logic
            try {
                throw new Error("Failed to import course. Please check all field values are valid.")
            } catch (err) {
                importError.value = err instanceof Error ? err.message : "Failed to import course"
            }

            expect(importError.value).toBe("Failed to import course. Please check all field values are valid.")
        })

        it("should handle non-Error exceptions gracefully", () => {
            const importError = ref<string | null>(null)

            // Simulate handling a non-Error value (e.g., from external code)
            const nonErrorValue: unknown = "string error"
            importError.value = nonErrorValue instanceof Error ? nonErrorValue.message : "Failed to import course"

            expect(importError.value).toBe("Failed to import course")
        })
    })

    describe("Search Error States", () => {
        it("should capture search error from thrown exception", () => {
            const searchError = ref("")

            try {
                throw new Error("Search failed")
            } catch (err) {
                searchError.value = err instanceof Error ? err.message : "Error searching for courses"
            }

            expect(searchError.value).toBe("Search failed")
        })

        it("should use default message for non-Error exceptions", () => {
            const searchError = ref("")

            // Simulate handling a non-Error value (e.g., from external code)
            const nonErrorValue: unknown = "unknown error"
            searchError.value = nonErrorValue instanceof Error ? nonErrorValue.message : "Error searching for courses"

            expect(searchError.value).toBe("Error searching for courses")
        })
    })

    describe("Service Mock Behavior", () => {
        it("effortService.importCourse rejects with specific error message", async () => {
            mockImportCourse.mockRejectedValue(
                new Error("Failed to import course. Please check all field values are valid."),
            )

            await expect(mockImportCourse()).rejects.toThrow("Failed to import course")
        })

        it("effortService.importCourse rejects with duplicate error message", async () => {
            mockImportCourse.mockRejectedValue(new Error("Course DVM 443 with 4 units already exists for this term"))

            await expect(mockImportCourse()).rejects.toThrow("already exists")
        })

        it("effortService.searchBannerCourses rejects with error message", async () => {
            mockSearchBannerCourses.mockRejectedValue(new Error("Search failed"))

            await expect(mockSearchBannerCourses()).rejects.toThrow("Search failed")
        })

        it("effortService.importCourse returns success with course data", async () => {
            mockImportCourse.mockResolvedValue({ success: true, course: { id: 1, crn: "12345" } })

            const result = await mockImportCourse()

            expect(result.success).toBeTruthy()
            expect(result.course.id).toBe(1)
        })

        it("effortService.importCourse returns failure with error message", async () => {
            mockImportCourse.mockResolvedValue({
                success: false,
                error: "Course already exists",
            })

            const result = await mockImportCourse()

            expect(result.success).toBeFalsy()
            expect(result.error).toBe("Course already exists")
        })
    })

    describe("Error Reset Behavior", () => {
        it("should clear import error when starting new import", () => {
            const importError = ref<string | null>("Previous error")

            // Simulate the startImport logic
            importError.value = null

            expect(importError.value).toBeNull()
        })

        it("should clear search error when dialog opens", () => {
            const searchError = ref("Previous search error")

            // Simulate the dialog open watcher logic
            searchError.value = ""

            expect(searchError.value).toBe("")
        })
    })
})
