import { careerSelectionService } from "../services/career-selection-service"
import type { StudentInfo } from "../types"

/**
 * Tests for the career selection service beyond its option endpoints: the roster, report, record
 * and mentor lookups, and the app access and export calls it inherits from StudentAppService.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockPost = vi.fn<(...args: unknown[]) => unknown>()
const mockPut = vi.fn<(...args: unknown[]) => unknown>()
const mockDel = vi.fn<(...args: unknown[]) => unknown>()
const mockPostForBlob = vi.fn<(...args: unknown[]) => unknown>()
const mockDownloadBlob = vi.fn<(...args: unknown[]) => unknown>()

vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        post: (...args: unknown[]) => mockPost(...args),
        put: (...args: unknown[]) => mockPut(...args),
        del: (...args: unknown[]) => mockDel(...args),
        createUrlSearchParams: (params: Record<string, string>) => new URLSearchParams(params).toString(),
    }),
    postForBlob: (...args: unknown[]) => mockPostForBlob(...args),
    downloadBlob: (...args: unknown[]) => mockDownloadBlob(...args),
}))

function emptyStudentInfo(): StudentInfo {
    return {
        direction: null,
        directionOther: "",
        primaryFocus: null,
        primaryFocusOther: "",
        secondaryFocus: null,
        secondaryFocusOther: "",
        postGrad: null,
        shortTermPlans: "",
        longTermPlans: "",
    }
}

describe("roster and report", () => {
    it("reads the roster from the app's base url", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: true, result: [{ personId: 100 }] })

        const result = await careerSelectionService.getList()

        expect(mockGet).toHaveBeenCalledWith(expect.stringMatching(/\/students\/career-selection$/u))
        expect(result).toHaveLength(1)
    })

    it("returns an empty roster when the request fails", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: false, result: null })

        await expect(careerSelectionService.getList()).resolves.toStrictEqual([])
    })

    it("reads the report from its own endpoint", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: true, result: [] })

        await careerSelectionService.getReport()

        expect(mockGet).toHaveBeenCalledWith(expect.stringMatching(/\/career-selection\/report$/u))
    })
})

describe("one student's record", () => {
    it("requests the record by person id", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: true, result: { personId: 100, studentInfo: emptyStudentInfo() } })

        await careerSelectionService.getDetail(100)

        expect(mockGet).toHaveBeenCalledWith(expect.stringMatching(/\/career-selection\/100$/u))
    })

    it("returns null when the record cannot be read", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: false, result: null })

        await expect(careerSelectionService.getDetail(100)).resolves.toBeNull()
    })

    it("turns absent plans into empty strings for the form", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        // The textareas bind to strings; null would render as "null" and count as a change.
        mockGet.mockResolvedValue({
            success: true,
            result: {
                personId: 100,
                studentInfo: { ...emptyStudentInfo(), shortTermPlans: null, longTermPlans: null },
            },
        })

        const detail = await careerSelectionService.getDetail(100)

        expect(detail?.studentInfo.shortTermPlans).toBe("")
        expect(detail?.studentInfo.longTermPlans).toBe("")
    })

    it("leaves written plans alone", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({
            success: true,
            result: {
                personId: 100,
                studentInfo: { ...emptyStudentInfo(), shortTermPlans: "Internship", longTermPlans: "Ownership" },
            },
        })

        const detail = await careerSelectionService.getDetail(100)

        expect(detail?.studentInfo.shortTermPlans).toBe("Internship")
    })
})

describe("saving a record", () => {
    it("puts the form to the student's endpoint and returns the refreshed record", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        const info = emptyStudentInfo()
        mockPut.mockResolvedValue({ success: true, result: { personId: 100, studentInfo: info }, errors: [] })

        const response = await careerSelectionService.updateCareerSelection(100, info)

        expect(mockPut).toHaveBeenCalledWith(expect.stringMatching(/\/career-selection\/100$/u), info)
        expect(response.success).toBeTruthy()
        expect(response.result?.personId).toBe(100)
    })

    it("reports the server's errors and no record when the save is refused", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockPut.mockResolvedValue({ success: false, result: null, errors: ["Select a career direction."] })

        const response = await careerSelectionService.updateCareerSelection(100, emptyStudentInfo())

        expect(response.result).toBeNull()
        expect(response.errors).toStrictEqual(["Select a career direction."])
    })

    it("reports an empty error list when the server sends none", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockPut.mockResolvedValue({ success: false, result: null })

        const response = await careerSelectionService.updateCareerSelection(100, emptyStudentInfo())

        expect(response.errors).toStrictEqual([])
    })
})

describe("mentor search", () => {
    it("passes the search text as a query parameter", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: true, result: [] })

        await careerSelectionService.searchMentors("smith")

        expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("/career-selection/mentors?search=smith"))
    })

    it("returns null on a failed search so the picker can tell it from no matches", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: false, result: null })

        await expect(careerSelectionService.searchMentors("smith")).resolves.toBeNull()
    })
})

describe("app access", () => {
    it("reads whether the app is open", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: true, result: true })

        const result = await careerSelectionService.getAccessStatus()

        expect(mockGet).toHaveBeenCalledWith(expect.stringMatching(/\/career-selection\/access\/status$/u))
        expect(result).toBeTruthy()
    })

    it("reports a closed app as false, not as a failed read", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        // A bare boolean status makes "closed" falsy, so only null may mean failure.
        mockGet.mockResolvedValue({ success: true, result: false })

        await expect(careerSelectionService.getAccessStatus()).resolves.toBeFalsy()
        await expect(careerSelectionService.getAccessStatus()).resolves.not.toBeNull()
    })

    it("returns null when the status cannot be read", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: false, result: null })

        await expect(careerSelectionService.getAccessStatus()).resolves.toBeNull()
    })

    it("posts the toggle and returns the new state", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockPost.mockResolvedValue({ success: true, result: false })

        const result = await careerSelectionService.toggleAppAccess()

        expect(mockPost).toHaveBeenCalledWith(expect.stringMatching(/\/access\/toggle-app$/u))
        expect(result).toBeFalsy()
    })

    it("returns null when the toggle fails", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockPost.mockResolvedValue({ success: false })

        await expect(careerSelectionService.toggleAppAccess()).resolves.toBeNull()
    })
})

describe("exports", () => {
    it("downloads the overview workbook under a career selection filename", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockPostForBlob.mockResolvedValue({ blob: new Blob(["x"]), filename: null })

        const downloaded = await careerSelectionService.downloadOverviewExcel()

        expect(mockPostForBlob).toHaveBeenCalledWith(expect.stringMatching(/\/export\/overview\/excel$/u), {})
        expect(mockDownloadBlob).toHaveBeenCalledWith(expect.any(Blob), "career-selection-overview.xlsx")
        expect(downloaded).toBeTruthy()
    })

    it("prefers the filename the server sends", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockPostForBlob.mockResolvedValue({ blob: new Blob(["x"]), filename: "CareerSelection_20260417.xlsx" })

        await careerSelectionService.downloadExcel()

        expect(mockDownloadBlob).toHaveBeenCalledWith(expect.any(Blob), "CareerSelection_20260417.xlsx")
    })

    it("reports nothing to download when the export comes back empty", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockPostForBlob.mockResolvedValue({ blob: new Blob([]), filename: null })

        const downloaded = await careerSelectionService.downloadExcel()

        expect(downloaded).toBeFalsy()
        expect(mockDownloadBlob).not.toHaveBeenCalled()
    })

    it("opens each pdf in a new tab", () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        const open = vi.spyOn(globalThis, "open").mockReturnValue(null)

        careerSelectionService.openOverviewPdf()
        careerSelectionService.openPdf()

        expect(open).toHaveBeenNthCalledWith(
            1,
            expect.stringMatching(/\/export\/overview\/pdf$/u),
            "_blank",
            "noopener",
        )
        expect(open).toHaveBeenNthCalledWith(2, expect.stringMatching(/\/export\/pdf$/u), "_blank", "noopener")
        open.mockRestore()
    })
})
