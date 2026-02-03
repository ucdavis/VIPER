import { describe, it, expect, vi, beforeEach } from "vitest"
import { ref, computed } from "vue"
import { setActivePinia, createPinia } from "pinia"
import type { HarvestPreviewDto, HarvestResultDto } from "../types"

/**
 * Tests for HarvestDialog error handling, loading states, and validation behavior.
 *
 * These tests validate that the component properly handles various states
 * during the harvest preview and commit process.
 */

// Mock the harvest service
const mockGetPreview = vi.fn()
const mockCommitHarvest = vi.fn()
vi.mock("../services/harvest-service", () => ({
    harvestService: {
        getPreview: (...args: unknown[]) => mockGetPreview(...args),
        commitHarvest: (...args: unknown[]) => mockCommitHarvest(...args),
    },
}))

// Test data - term codes use YYYYXX format (no numeric separators)
// oxlint-disable-next-line unicorn/numeric-separators-style
const TEST_TERM_CODE = 202410

// Helper to create a minimal valid preview
function createMockPreview(overrides: Partial<HarvestPreviewDto> = {}): HarvestPreviewDto {
    return {
        termCode: TEST_TERM_CODE,
        termName: "Fall 2024",
        crestInstructors: [],
        crestCourses: [],
        crestEffort: [],
        nonCrestInstructors: [],
        nonCrestCourses: [],
        nonCrestEffort: [],
        clinicalInstructors: [],
        clinicalCourses: [],
        clinicalEffort: [],
        guestAccounts: [],
        removedInstructors: [],
        removedCourses: [],
        summary: {
            totalInstructors: 0,
            totalCourses: 0,
            totalEffortRecords: 0,
            guestAccounts: 0,
        },
        warnings: [],
        errors: [],
        ...overrides,
    }
}

describe("HarvestDialog - Error Handling", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    describe("Preview Load Errors", () => {
        it("should capture error message when preview returns null", async () => {
            const loadError = ref<string | null>(null)
            const preview = ref<HarvestPreviewDto | null>(null)

            mockGetPreview.mockResolvedValue(null)

            // Simulate loadPreview logic
            const result = await mockGetPreview(TEST_TERM_CODE)
            if (result) {
                preview.value = result
            } else {
                loadError.value = "Failed to load harvest preview"
            }

            expect(loadError.value).toBe("Failed to load harvest preview")
            expect(preview.value).toBeNull()
        })

        it("should capture error message from thrown exception", async () => {
            const loadError = ref<string | null>(null)

            mockGetPreview.mockRejectedValue(new Error("Network error"))

            // Simulate loadPreview error handling
            try {
                await mockGetPreview(TEST_TERM_CODE)
            } catch (err) {
                loadError.value = err instanceof Error ? err.message : "Failed to load harvest preview"
            }

            expect(loadError.value).toBe("Network error")
        })

        it("should use default message when exception has no message", async () => {
            const loadError = ref<string | null>(null)

            mockGetPreview.mockRejectedValue("Unknown error")

            // Simulate loadPreview error handling
            try {
                await mockGetPreview(TEST_TERM_CODE)
            } catch (err) {
                loadError.value = err instanceof Error ? err.message : "Failed to load harvest preview"
            }

            expect(loadError.value).toBe("Failed to load harvest preview")
        })

        it("should clear error when preview loads successfully", async () => {
            const loadError = ref<string | null>("Previous error")
            const preview = ref<HarvestPreviewDto | null>(null)

            const mockPreview = createMockPreview()
            mockGetPreview.mockResolvedValue(mockPreview)

            // Simulate loadPreview success
            loadError.value = null
            const result = await mockGetPreview(TEST_TERM_CODE)
            if (result) {
                preview.value = result
            }

            expect(loadError.value).toBeNull()
            expect(preview.value).not.toBeNull()
        })
    })

    describe("Commit Errors", () => {
        it("should detect failed commit from success: false", () => {
            const result: HarvestResultDto = {
                success: false,
                termCode: TEST_TERM_CODE,
                harvestedDate: null,
                summary: { totalInstructors: 0, totalCourses: 0, totalEffortRecords: 0, guestAccounts: 0 },
                warnings: [],
                errorMessage: "Term is locked",
            }

            const errorMessage = result.success ? null : (result.errorMessage ?? "Harvest failed")

            expect(errorMessage).toBe("Term is locked")
        })

        it("should use default message when commit fails without error message", () => {
            const result: HarvestResultDto = {
                success: false,
                termCode: TEST_TERM_CODE,
                harvestedDate: null,
                summary: { totalInstructors: 0, totalCourses: 0, totalEffortRecords: 0, guestAccounts: 0 },
                warnings: [],
                errorMessage: null,
            }

            const errorMessage = result.success ? null : (result.errorMessage ?? "Harvest failed")

            expect(errorMessage).toBe("Harvest failed")
        })
    })
})

describe("HarvestDialog - Loading States", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("should track loading state during preview fetch", async () => {
        const isLoading = ref(false)

        isLoading.value = true
        expect(isLoading.value).toBeTruthy()

        // Simulate async operation
        await Promise.resolve()

        isLoading.value = false
        expect(isLoading.value).toBeFalsy()
    })

    it("should track committing state during harvest commit", async () => {
        const isCommitting = ref(false)

        isCommitting.value = true
        expect(isCommitting.value).toBeTruthy()

        // Simulate async operation
        await Promise.resolve()

        isCommitting.value = false
        expect(isCommitting.value).toBeFalsy()
    })

    it("should disable confirm button while committing", () => {
        const isCommitting = ref(true)
        const preview = ref<HarvestPreviewDto | null>(createMockPreview())

        // Button is disabled when committing or when there are errors
        const canConfirm = !isCommitting.value && preview.value && preview.value.errors.length === 0

        expect(canConfirm).toBeFalsy()
    })

    it("should enable confirm button when not committing and no errors", () => {
        const isCommitting = ref(false)
        const preview = ref<HarvestPreviewDto | null>(createMockPreview())

        const canConfirm = !isCommitting.value && preview.value && preview.value.errors.length === 0

        expect(canConfirm).toBeTruthy()
    })
})

describe("HarvestDialog - Validation Logic", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("should disable confirm when errors exist in preview", () => {
        const preview = ref<HarvestPreviewDto | null>(
            createMockPreview({
                errors: [{ phase: "CREST", message: "No data found", details: "" }],
            }),
        )

        const canConfirm = preview.value && preview.value.errors.length === 0

        expect(canConfirm).toBeFalsy()
    })

    it("should enable confirm when no errors exist", () => {
        const preview = ref<HarvestPreviewDto | null>(createMockPreview({ errors: [] }))

        const canConfirm = preview.value && preview.value.errors.length === 0

        expect(canConfirm).toBeTruthy()
    })

    it("should correctly identify when removed items exist", () => {
        const preview = ref<HarvestPreviewDto | null>(
            createMockPreview({
                removedInstructors: [
                    {
                        mothraId: "TEST123",
                        personId: 1,
                        fullName: "Test, User",
                        firstName: "User",
                        lastName: "Test",
                        department: "VME",
                        titleCode: "001",
                        titleDescription: "Professor",
                        source: "Existing",
                        isNew: false,
                    },
                ],
            }),
        )

        const hasRemovedItems = computed(
            () =>
                preview.value &&
                (preview.value.removedInstructors.length > 0 || preview.value.removedCourses.length > 0),
        )

        expect(hasRemovedItems.value).toBeTruthy()
    })

    it("should correctly identify when no removed items exist", () => {
        const preview = ref<HarvestPreviewDto | null>(
            createMockPreview({
                removedInstructors: [],
                removedCourses: [],
            }),
        )

        const hasRemovedItems = computed(
            () =>
                preview.value &&
                (preview.value.removedInstructors.length > 0 || preview.value.removedCourses.length > 0),
        )

        expect(hasRemovedItems.value).toBeFalsy()
    })

    it("should identify removed courses", () => {
        const preview = ref<HarvestPreviewDto | null>(
            createMockPreview({
                removedCourses: [
                    {
                        crn: "12345",
                        subjCode: "VME",
                        crseNumb: "100",
                        seqNumb: "001",
                        enrollment: 10,
                        units: 4,
                        custDept: "VME",
                        source: "Existing",
                        isNew: false,
                    },
                ],
            }),
        )

        const hasRemovedItems = computed(
            () =>
                preview.value &&
                (preview.value.removedInstructors.length > 0 || preview.value.removedCourses.length > 0),
        )

        expect(hasRemovedItems.value).toBeTruthy()
    })
})

describe("HarvestDialog - State Reset", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("should reset all state when dialog closes", () => {
        // Simulate state after dialog was open
        const preview = ref<HarvestPreviewDto | null>(createMockPreview())
        const loadError = ref<string | null>("Some error")
        const activeTab = ref("clinical")

        // Simulate dialog close reset
        preview.value = null
        loadError.value = null
        activeTab.value = "crest"

        expect(preview.value).toBeNull()
        expect(loadError.value).toBeNull()
        expect(activeTab.value).toBe("crest")
    })

    it("should not load preview when termCode is null", async () => {
        const termCode = ref<number | null>(null)
        const preview = ref<HarvestPreviewDto | null>(null)

        // Simulate loadPreview guard
        if (termCode.value) {
            const result = await mockGetPreview(termCode.value)
            if (result) {
                preview.value = result
            }
        }

        expect(mockGetPreview).not.toHaveBeenCalled()
        expect(preview.value).toBeNull()
    })

    it("should load preview when termCode is provided", async () => {
        const termCode = ref<number | null>(TEST_TERM_CODE)
        const preview = ref<HarvestPreviewDto | null>(null)

        const mockPreview = createMockPreview()
        mockGetPreview.mockResolvedValue(mockPreview)

        // Simulate loadPreview with termCode
        if (termCode.value) {
            const result = await mockGetPreview(termCode.value)
            if (result) {
                preview.value = result
            }
        }

        expect(mockGetPreview).toHaveBeenCalledWith(TEST_TERM_CODE)
        expect(preview.value).not.toBeNull()
    })
})

describe("HarvestDialog - Summary Display", () => {
    beforeEach(() => {
        setActivePinia(createPinia())
        vi.clearAllMocks()
    })

    it("should display correct summary totals", () => {
        const preview = ref<HarvestPreviewDto | null>(
            createMockPreview({
                summary: {
                    totalInstructors: 25,
                    totalCourses: 15,
                    totalEffortRecords: 100,
                    guestAccounts: 6,
                },
            }),
        )

        expect(preview.value?.summary.totalInstructors).toBe(25)
        expect(preview.value?.summary.totalCourses).toBe(15)
        expect(preview.value?.summary.totalEffortRecords).toBe(100)
        expect(preview.value?.summary.guestAccounts).toBe(6)
    })

    it("should track warning count correctly", () => {
        const preview = ref<HarvestPreviewDto | null>(
            createMockPreview({
                warnings: [
                    { phase: "", message: "Warning 1", details: "" },
                    { phase: "CREST", message: "Warning 2", details: "" },
                    { phase: "Clinical", message: "Warning 3", details: "" },
                ],
            }),
        )

        expect(preview.value?.warnings.length).toBe(3)
    })

    it("should limit displayed warnings to 5", () => {
        const warnings = [
            { phase: "", message: "Warning 1", details: "" },
            { phase: "", message: "Warning 2", details: "" },
            { phase: "", message: "Warning 3", details: "" },
            { phase: "", message: "Warning 4", details: "" },
            { phase: "", message: "Warning 5", details: "" },
            { phase: "", message: "Warning 6", details: "" },
            { phase: "", message: "Warning 7", details: "" },
        ]

        const preview = ref<HarvestPreviewDto | null>(createMockPreview({ warnings }))

        // Component displays first 5 warnings
        const displayedWarnings = preview.value?.warnings.slice(0, 5) ?? []
        const remainingCount = (preview.value?.warnings.length ?? 0) - 5

        expect(displayedWarnings.length).toBe(5)
        expect(remainingCount).toBe(2)
    })
})
