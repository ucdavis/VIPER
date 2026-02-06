import { describe, it, expect, vi, beforeEach } from "vitest"

/**
 * Tests for Dashboard service.
 *
 * These tests validate the dashboard service methods and error handling
 * for the Effort staff dashboard feature.
 */

// Mock the ViperFetch composable
const mockGet = vi.fn()
const mockPost = vi.fn()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        post: (...args: unknown[]) => mockPost(...args),
    }),
}))

// Import service after mocking
import { dashboardService } from "../services/dashboard-service"

// Test data - term codes use YYYYXX format (no numeric separators)
// oxlint-disable-next-line unicorn/numeric-separators-style
const TEST_TERM_CODE = 202410

describe("DashboardService", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    describe("getStats", () => {
        it("should return stats on successful response", async () => {
            const mockStats = {
                totalInstructors: 100,
                verifiedInstructors: 80,
                totalCourses: 50,
                coursesWithInstructors: 45,
                totalRecords: 200,
                hygieneSummary: {
                    activeAlerts: 5,
                    resolvedAlerts: 10,
                    ignoredAlerts: 2,
                },
            }
            mockGet.mockResolvedValue({ success: true, result: mockStats })

            const result = await dashboardService.getStats(TEST_TERM_CODE)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/202410/stats"))
            expect(result).toEqual(mockStats)
        })

        it("should return null on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await dashboardService.getStats(TEST_TERM_CODE)

            expect(result).toBeNull()
        })

        it("should return null when result is undefined", async () => {
            mockGet.mockResolvedValue({ success: true, result: undefined })

            const result = await dashboardService.getStats(TEST_TERM_CODE)

            expect(result).toBeNull()
        })
    })

    describe("getDepartmentVerification", () => {
        it("should return departments on successful response", async () => {
            const mockDepts = [
                { departmentCode: "VME", departmentName: "Medicine", totalInstructors: 20, verifiedInstructors: 18 },
                { departmentCode: "APC", departmentName: "Anatomy", totalInstructors: 15, verifiedInstructors: 10 },
            ]
            mockGet.mockResolvedValue({ success: true, result: mockDepts })

            const result = await dashboardService.getDepartmentVerification(TEST_TERM_CODE)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/202410/departments?threshold=80"))
            expect(result).toEqual(mockDepts)
        })

        it("should use custom threshold when provided", async () => {
            mockGet.mockResolvedValue({ success: true, result: [] })

            await dashboardService.getDepartmentVerification(TEST_TERM_CODE, 90)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("threshold=90"))
        })

        it("should return empty array on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await dashboardService.getDepartmentVerification(TEST_TERM_CODE)

            expect(result).toEqual([])
        })

        it("should return empty array when result is not an array", async () => {
            mockGet.mockResolvedValue({ success: true, result: "invalid" })

            const result = await dashboardService.getDepartmentVerification(TEST_TERM_CODE)

            expect(result).toEqual([])
        })
    })

    describe("getDataHygieneAlerts", () => {
        it("should return alerts on successful response", async () => {
            const mockAlerts = [
                { alertType: "NoRecords", entityId: "123", entityName: "John Doe", severity: "Medium" },
                { alertType: "ZeroHours", entityId: "456", entityName: "Jane Smith", severity: "Medium" },
            ]
            mockGet.mockResolvedValue({ success: true, result: mockAlerts })

            const result = await dashboardService.getDataHygieneAlerts(TEST_TERM_CODE)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/202410/hygiene"))
            expect(result).toEqual(mockAlerts)
        })

        it("should return empty array on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await dashboardService.getDataHygieneAlerts(TEST_TERM_CODE)

            expect(result).toEqual([])
        })
    })

    describe("getAllAlerts", () => {
        it("should return alerts excluding ignored by default", async () => {
            const mockAlerts = [
                { alertType: "NoRecords", status: "Active" },
                { alertType: "ZeroHours", status: "Active" },
            ]
            mockGet.mockResolvedValue({ success: true, result: mockAlerts })

            const result = await dashboardService.getAllAlerts(TEST_TERM_CODE)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("includeIgnored=false"))
            expect(result).toEqual(mockAlerts)
        })

        it("should include ignored alerts when requested", async () => {
            const mockAlerts = [
                { alertType: "NoRecords", status: "Active" },
                { alertType: "ZeroHours", status: "Ignored" },
            ]
            mockGet.mockResolvedValue({ success: true, result: mockAlerts })

            const result = await dashboardService.getAllAlerts(TEST_TERM_CODE, true)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("includeIgnored=true"))
            expect(result).toEqual(mockAlerts)
        })

        it("should return empty array on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await dashboardService.getAllAlerts(TEST_TERM_CODE)

            expect(result).toEqual([])
        })
    })

    describe("ignoreAlert", () => {
        it("should return true on successful ignore", async () => {
            mockPost.mockResolvedValue({ success: true })

            const result = await dashboardService.ignoreAlert(TEST_TERM_CODE, "NoRecords", "123")

            expect(mockPost).toHaveBeenCalledWith(expect.stringContaining("/202410/alerts/ignore"), {
                alertType: "NoRecords",
                entityId: "123",
            })
            expect(result).toBeTruthy()
        })

        it("should return false on failed ignore", async () => {
            mockPost.mockResolvedValue({ success: false })

            const result = await dashboardService.ignoreAlert(TEST_TERM_CODE, "NoRecords", "123")

            expect(result).toBeFalsy()
        })
    })

    describe("getRecentChanges", () => {
        it("should return recent changes on successful response", async () => {
            const mockChanges = [
                { id: 1, action: "CreateEffortRecord", changedByName: "John Doe" },
                { id: 2, action: "UpdateEffortRecord", changedByName: "Jane Smith" },
            ]
            mockGet.mockResolvedValue({ success: true, result: mockChanges })

            const result = await dashboardService.getRecentChanges(TEST_TERM_CODE)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/202410/recent-changes?limit=10"))
            expect(result).toEqual(mockChanges)
        })

        it("should use custom limit when provided", async () => {
            mockGet.mockResolvedValue({ success: true, result: [] })

            await dashboardService.getRecentChanges(TEST_TERM_CODE, 5)

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("limit=5"))
        })

        it("should return empty array on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await dashboardService.getRecentChanges(TEST_TERM_CODE)

            expect(result).toEqual([])
        })

        it("should return empty array when result is not an array", async () => {
            mockGet.mockResolvedValue({ success: true, result: "not-an-array" })

            const result = await dashboardService.getRecentChanges(TEST_TERM_CODE)

            expect(result).toEqual([])
        })
    })

    describe("getStats - hasAuditAccess flag", () => {
        it("should pass through hasAuditAccess=true from API response", async () => {
            const mockStats = {
                totalInstructors: 10,
                verifiedInstructors: 8,
                hasAuditAccess: true,
                hygieneSummary: { activeAlerts: 0, resolvedAlerts: 0, ignoredAlerts: 0 },
            }
            mockGet.mockResolvedValue({ success: true, result: mockStats })

            const result = await dashboardService.getStats(TEST_TERM_CODE)

            expect(result).not.toBeNull()
            expect(result!.hasAuditAccess).toBe(true)
        })

        it("should pass through hasAuditAccess=false from API response", async () => {
            const mockStats = {
                totalInstructors: 10,
                verifiedInstructors: 8,
                hasAuditAccess: false,
                hygieneSummary: { activeAlerts: 0, resolvedAlerts: 0, ignoredAlerts: 0 },
            }
            mockGet.mockResolvedValue({ success: true, result: mockStats })

            const result = await dashboardService.getStats(TEST_TERM_CODE)

            expect(result).not.toBeNull()
            expect(result!.hasAuditAccess).toBe(false)
        })
    })

    describe("conditional loading based on hasAuditAccess", () => {
        it("should only call getRecentChanges when hasAuditAccess is true", async () => {
            // Simulate the loadDashboard pattern from StaffDashboard.vue
            const statsWithAccess = { hasAuditAccess: true }
            mockGet
                .mockResolvedValueOnce({ success: true, result: statsWithAccess }) // getStats
                .mockResolvedValueOnce({ success: true, result: [] }) // getDepartmentVerification
                .mockResolvedValueOnce({ success: true, result: [] }) // getAllAlerts
                .mockResolvedValueOnce({ success: true, result: [] }) // getRecentChanges

            // Replicate loadDashboard logic
            const [statsData] = await Promise.all([
                dashboardService.getStats(TEST_TERM_CODE),
                dashboardService.getDepartmentVerification(TEST_TERM_CODE),
                dashboardService.getAllAlerts(TEST_TERM_CODE, true),
            ])

            let recentChanges: unknown[] = []
            if (statsData?.hasAuditAccess) {
                recentChanges = await dashboardService.getRecentChanges(TEST_TERM_CODE, 10)
            }

            // 4 API calls: stats + departments + alerts + recentChanges
            expect(mockGet).toHaveBeenCalledTimes(4)
            expect(recentChanges).toEqual([])
        })

        it("should skip getRecentChanges when hasAuditAccess is false", async () => {
            const statsNoAccess = { hasAuditAccess: false }
            mockGet
                .mockResolvedValueOnce({ success: true, result: statsNoAccess }) // getStats
                .mockResolvedValueOnce({ success: true, result: [] }) // getDepartmentVerification
                .mockResolvedValueOnce({ success: true, result: [] }) // getAllAlerts

            const [statsData] = await Promise.all([
                dashboardService.getStats(TEST_TERM_CODE),
                dashboardService.getDepartmentVerification(TEST_TERM_CODE),
                dashboardService.getAllAlerts(TEST_TERM_CODE, true),
            ])

            let recentChanges: unknown[] = []
            if (statsData?.hasAuditAccess) {
                recentChanges = await dashboardService.getRecentChanges(TEST_TERM_CODE, 10)
            }

            // Only 3 API calls: stats + departments + alerts (no recentChanges)
            expect(mockGet).toHaveBeenCalledTimes(3)
            expect(recentChanges).toEqual([])
        })
    })
})
