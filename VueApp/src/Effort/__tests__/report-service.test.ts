import { describe, it, expect, vi, beforeEach } from "vitest"

/**
 * Tests for Report service.
 *
 * These tests validate the report service methods and error handling
 * for the Effort teaching activity reports feature.
 */

// Mock the ViperFetch composable
const mockGet = vi.fn()
const mockPostForBlob = vi.fn()
const mockDownloadBlob = vi.fn()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
    }),
    postForBlob: (...args: unknown[]) => mockPostForBlob(...args),
    downloadBlob: (...args: unknown[]) => mockDownloadBlob(...args),
}))

// Import service after mocking
import { reportService } from "../services/report-service"

// Test data - term codes use YYYYXX format (no numeric separators)
// oxlint-disable-next-line unicorn/numeric-separators-style
const TEST_TERM_CODE = 202410

const mockReport = {
    termCode: TEST_TERM_CODE,
    termName: "Fall Quarter 2024",
    filterDepartment: null,
    filterPerson: null,
    filterRole: null,
    filterTitle: null,
    effortTypes: ["CLI", "LEC"],
    departments: [
        {
            department: "VME",
            instructors: [
                {
                    mothraId: "A12345678",
                    instructor: "Smith, John",
                    jobGroupId: "REG",
                    courses: [
                        {
                            courseId: 1,
                            course: "VME 400-001",
                            crn: "40076",
                            units: 4.0,
                            enrollment: 25,
                            roleId: "Instructor",
                            effortByType: { LEC: 30.0, CLI: 10.0 },
                        },
                    ],
                    instructorTotals: { LEC: 30.0, CLI: 10.0 },
                },
            ],
            departmentTotals: { LEC: 30.0, CLI: 10.0 },
        },
    ],
}

describe("ReportService", () => {
    beforeEach(() => {
        vi.clearAllMocks()
    })

    describe("getTeachingActivityGrouped", () => {
        it("should return report on successful response", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            const result = await reportService.getTeachingActivityGrouped({ termCode: TEST_TERM_CODE })

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("teaching/grouped"))
            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining(`termCode=${TEST_TERM_CODE}`))
            expect(result).toEqual(mockReport)
        })

        it("should return null on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await reportService.getTeachingActivityGrouped({ termCode: TEST_TERM_CODE })

            expect(result).toBeNull()
        })

        it("should return null when result is undefined", async () => {
            mockGet.mockResolvedValue({ success: true, result: undefined })

            const result = await reportService.getTeachingActivityGrouped({ termCode: TEST_TERM_CODE })

            expect(result).toBeNull()
        })

        it("should include department filter in URL when provided", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            await reportService.getTeachingActivityGrouped({ termCode: TEST_TERM_CODE, department: "VME" })

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("department=VME"))
        })

        it("should include personId filter in URL when provided", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            await reportService.getTeachingActivityGrouped({ termCode: TEST_TERM_CODE, personId: 123 })

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("personId=123"))
        })

        it("should include role filter in URL when provided", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            await reportService.getTeachingActivityGrouped({ termCode: TEST_TERM_CODE, role: "I" })

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("role=I"))
        })

        it("should include title filter in URL when provided", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            await reportService.getTeachingActivityGrouped({ termCode: TEST_TERM_CODE, title: "REG" })

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("title=REG"))
        })

        it("should include all filters when provided", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            await reportService.getTeachingActivityGrouped({
                termCode: TEST_TERM_CODE,
                department: "VME",
                personId: 123,
                role: "I",
                title: "REG",
            })

            const calledUrl = mockGet.mock.calls[0]![0] as string
            expect(calledUrl).toContain(`termCode=${TEST_TERM_CODE}`)
            expect(calledUrl).toContain("department=VME")
            expect(calledUrl).toContain("personId=123")
            expect(calledUrl).toContain("role=I")
            expect(calledUrl).toContain("title=REG")
        })

        it("should not include optional filters when not provided", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            await reportService.getTeachingActivityGrouped({ termCode: TEST_TERM_CODE })

            const calledUrl = mockGet.mock.calls[0]![0] as string
            expect(calledUrl).toContain(`termCode=${TEST_TERM_CODE}`)
            expect(calledUrl).not.toContain("department=")
            expect(calledUrl).not.toContain("personId=")
            expect(calledUrl).not.toContain("role=")
            expect(calledUrl).not.toContain("title=")
        })
    })

    describe("getTeachingActivityIndividual", () => {
        it("should return report on successful response", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            const result = await reportService.getTeachingActivityIndividual({ termCode: TEST_TERM_CODE })

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("teaching/individual"))
            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining(`termCode=${TEST_TERM_CODE}`))
            expect(result).toEqual(mockReport)
        })

        it("should return null on failed response", async () => {
            mockGet.mockResolvedValue({ success: false, result: null })

            const result = await reportService.getTeachingActivityIndividual({ termCode: TEST_TERM_CODE })

            expect(result).toBeNull()
        })

        it("should include department filter in URL when provided", async () => {
            mockGet.mockResolvedValue({ success: true, result: mockReport })

            await reportService.getTeachingActivityIndividual({ termCode: TEST_TERM_CODE, department: "APC" })

            expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("department=APC"))
        })
    })

    describe("downloadExcel", () => {
        it("should return true and call downloadBlob when blob has data", async () => {
            mockPostForBlob.mockResolvedValue({
                blob: new Blob(["data"], { type: "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet" }),
                filename: "teaching-activity.xlsx",
            })

            const result = await reportService.downloadExcel("teaching/grouped/excel", { termCode: TEST_TERM_CODE })

            expect(result).toBe(true)
            expect(mockDownloadBlob).toHaveBeenCalledWith(expect.any(Blob), "teaching-activity.xlsx")
        })

        it("should return false when blob is empty", async () => {
            mockPostForBlob.mockResolvedValue({ blob: new Blob([]), filename: null })

            const result = await reportService.downloadExcel("teaching/grouped/excel", { termCode: TEST_TERM_CODE })

            expect(result).toBe(false)
            expect(mockDownloadBlob).not.toHaveBeenCalled()
        })

        it("should use fallback filename when server provides none", async () => {
            mockPostForBlob.mockResolvedValue({
                blob: new Blob(["data"]),
                filename: null,
            })

            await reportService.downloadExcel("teaching/grouped/excel", { termCode: TEST_TERM_CODE })

            expect(mockDownloadBlob).toHaveBeenCalledWith(expect.any(Blob), "report.xlsx")
        })
    })

    describe("downloadClinicalEffortExcel", () => {
        it("should return true and download when blob has data", async () => {
            mockPostForBlob.mockResolvedValue({
                blob: new Blob(["data"]),
                filename: "clinical-effort.xlsx",
            })

            const result = await reportService.downloadClinicalEffortExcel("2024-2025", 1)

            expect(result).toBe(true)
            expect(mockPostForBlob).toHaveBeenCalledWith(
                expect.stringContaining("merit/clinical/excel"),
                expect.objectContaining({ academicYear: "2024-2025", clinicalType: 1 }),
            )
            expect(mockDownloadBlob).toHaveBeenCalledWith(expect.any(Blob), "clinical-effort.xlsx")
        })

        it("should return false when blob is empty", async () => {
            mockPostForBlob.mockResolvedValue({ blob: new Blob([]), filename: null })

            const result = await reportService.downloadClinicalEffortExcel("2024-2025", 1)

            expect(result).toBe(false)
            expect(mockDownloadBlob).not.toHaveBeenCalled()
        })
    })

    describe("downloadMultiYearExcel", () => {
        it("should return true and download when blob has data", async () => {
            mockPostForBlob.mockResolvedValue({
                blob: new Blob(["data"]),
                filename: "multi-year.xlsx",
            })

            const result = await reportService.downloadMultiYearExcel({
                personId: 123,
                startYear: 2020,
                endYear: 2024,
            })

            expect(result).toBe(true)
            expect(mockPostForBlob).toHaveBeenCalledWith(
                expect.stringContaining("merit/multiyear/excel"),
                expect.objectContaining({ personId: 123, startYear: 2020, endYear: 2024 }),
            )
            expect(mockDownloadBlob).toHaveBeenCalledWith(expect.any(Blob), "multi-year.xlsx")
        })

        it("should return false when blob is empty", async () => {
            mockPostForBlob.mockResolvedValue({ blob: new Blob([]), filename: null })

            const result = await reportService.downloadMultiYearExcel({
                personId: 123,
                startYear: 2020,
                endYear: 2024,
            })

            expect(result).toBe(false)
            expect(mockDownloadBlob).not.toHaveBeenCalled()
        })
    })
})
