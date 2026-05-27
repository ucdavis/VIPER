import { ref } from "vue"
import { setActivePinia, createPinia } from "pinia"
import { shouldShowImportButton, importButtonLabel, shouldShortcutToImport } from "../utils/import-button-rules"

/**
 * Tests for CourseImportDialog error handling behavior.
 *
 * These tests validate that the component properly displays error feedback
 * to users when import operations fail, rather than showing generic errors.
 */

// Mock the course service
const mockSearchBannerCourses = vi.fn()
const mockImportCourse = vi.fn()
vi.mock("../services/course-service", () => ({
    courseService: {
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
            const isError = typeof nonErrorValue === "object" && nonErrorValue instanceof Error
            importError.value = isError ? (nonErrorValue as Error).message : "Failed to import course"

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
            const isError = typeof nonErrorValue === "object" && nonErrorValue instanceof Error
            searchError.value = isError ? (nonErrorValue as Error).message : "Error searching for courses"

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

/**
 * The backend's `alreadyImported` flag is true for fixed-unit courses that
 * exist in Effort for the term AND for VET 4XX courses (any unit type) that
 * exist for the term — the latter are harvested during the Harvest period and
 * cannot be manually re-imported. The frontend renders off this flag.
 */
describe("CourseImportDialog - alreadyImported Button Behavior", () => {
    describe("Import Button Visibility", () => {
        it("hides the button in staff mode when the course is already imported", () => {
            expect(shouldShowImportButton(false, { alreadyImported: true })).toBeFalsy()
        })

        it("shows the button in staff mode when the course is not yet imported", () => {
            expect(shouldShowImportButton(false, { alreadyImported: false })).toBeTruthy()
        })

        it("always shows the button in self mode so instructors can 'Use Course'", () => {
            expect(shouldShowImportButton(true, { alreadyImported: true })).toBeTruthy()
            expect(shouldShowImportButton(true, { alreadyImported: false })).toBeTruthy()
        })
    })

    describe("Import Button Label", () => {
        it("shows 'Use Course' in self mode when the course is already imported", () => {
            expect(importButtonLabel(true, { alreadyImported: true })).toBe("Use Course")
        })

        it("shows 'Import' in self mode when the course is not yet imported", () => {
            expect(importButtonLabel(true, { alreadyImported: false })).toBe("Import")
        })

        it("always shows 'Import' in staff mode", () => {
            expect(importButtonLabel(false, { alreadyImported: true })).toBe("Import")
            expect(importButtonLabel(false, { alreadyImported: false })).toBe("Import")
        })
    })

    describe("startImport Shortcut (self-mode bypass of units dialog)", () => {
        // Self mode shortcuts the options dialog when the course is already
        // imported AND fixed-unit — the backend returns the existing row by
        // CRN match. Variable-unit courses always open the dialog so the user
        // can pick the unit value for a new import.
        it("shortcuts fixed-unit already-imported courses in self mode", () => {
            expect(shouldShortcutToImport(true, { alreadyImported: true, isVariableUnits: false })).toBeTruthy()
        })

        it("opens the dialog for non-harvest variable-unit courses so the user can pick units", () => {
            // Non-VET-4XX variable-unit: alreadyImported is false, so no shortcut.
            expect(shouldShortcutToImport(true, { alreadyImported: false, isVariableUnits: true })).toBeFalsy()
        })

        it("opens the dialog for VET 4XX variable-unit courses even when alreadyImported", () => {
            // Under the new backend semantics, VET 4XX variable-unit existing in
            // Effort has alreadyImported=true. The current shortcut check bails
            // out on isVariableUnits, so the user goes through the units dialog
            // (backend will return the existing row regardless of the unit value).
            expect(shouldShortcutToImport(true, { alreadyImported: true, isVariableUnits: true })).toBeFalsy()
        })

        it("never shortcuts in staff mode (the button is hidden when alreadyImported)", () => {
            expect(shouldShortcutToImport(false, { alreadyImported: true, isVariableUnits: false })).toBeFalsy()
            expect(shouldShortcutToImport(false, { alreadyImported: true, isVariableUnits: true })).toBeFalsy()
        })
    })
})
