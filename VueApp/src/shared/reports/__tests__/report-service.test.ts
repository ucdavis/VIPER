import { reportService } from "../report-service"
import type { ReportDefinitionMetadata, ReportResult } from "../report-types"

/**
 * The service returns null or an outcome instead of throwing, so pages can tell "failed" apart
 * from "empty": ReportCatalog shows an error instead of hiding, and the report page shows the
 * server's validation messages.
 */

const { mockGet, mockPost, mockPostForBlob, mockDownloadBlob } = vi.hoisted(() => ({
    mockGet: vi.fn<(...args: unknown[]) => unknown>(),
    mockPost: vi.fn<(...args: unknown[]) => unknown>(),
    mockPostForBlob: vi.fn<(...args: unknown[]) => unknown>(),
    mockDownloadBlob: vi.fn<(...args: unknown[]) => unknown>(),
}))
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        post: (...args: unknown[]) => mockPost(...args),
    }),
    postForBlob: (...args: unknown[]) => mockPostForBlob(...args),
    downloadBlob: (...args: unknown[]) => mockDownloadBlob(...args),
}))

const catalog = [
    {
        key: "personnel.faculty-profile",
        title: "Faculty Profile",
        area: "Personnel",
        description: "Faculty by department.",
        available: false,
    },
]

const definition: ReportDefinitionMetadata = {
    key: "personnel.employees-on-leave",
    title: "Employees on Leave",
    area: "Personnel",
    description: "Faculty and staff on leave.",
    parameters: [],
    columns: [],
}

const result = { key: definition.key, title: definition.title, rowCount: 0 } as ReportResult

describe("reportService.getCatalog()", () => {
    it("returns the catalog for the area", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: true, result: catalog })

        const catalogResult = await reportService.getCatalog("Personnel")

        expect(mockGet).toHaveBeenCalledWith(expect.stringMatching(/reports\?area=Personnel$/u))
        expect(catalogResult).toStrictEqual(catalog)
    })

    it("percent-encodes the area so it stays one query value", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: true, result: [] })

        await reportService.getCatalog("a&b c")

        expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("?area=a%26b%20c"))
    })

    it("returns an empty list when the user may run none of the area's reports", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: true, result: [] })

        await expect(reportService.getCatalog("Personnel")).resolves.toStrictEqual([])
    })

    it("returns null when the request fails", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: false, result: null })

        await expect(reportService.getCatalog("Personnel")).resolves.toBeNull()
    })

    it("returns null when the response is not a list", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: true, result: { key: "personnel.faculty-profile" } })

        await expect(reportService.getCatalog("Personnel")).resolves.toBeNull()
    })
})

describe("reportService.getDefinition()", () => {
    it("returns the report's definition", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: true, result: definition })

        await expect(reportService.getDefinition("personnel.employees-on-leave")).resolves.toStrictEqual(definition)
        expect(mockGet).toHaveBeenCalledWith(expect.stringMatching(/reports\/personnel\.employees-on-leave$/u))
    })

    it("percent-encodes the key so it stays one path segment", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: true, result: definition })

        await reportService.getDefinition("a/b")

        expect(mockGet).toHaveBeenCalledWith(expect.stringMatching(/reports\/a%2Fb$/u))
    })

    it("returns null for an unknown, planned or forbidden report", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: false, result: null, errors: [""] })

        await expect(reportService.getDefinition("personnel.visa-audit")).resolves.toBeNull()
    })
})

describe("reportService.run()", () => {
    it("posts the parameters and returns the result", async () => {
        expect.hasAssertions()
        mockPost.mockResolvedValue({ success: true, result, errors: [] })

        const outcome = await reportService.run(definition.key, { startDate: "2026-07-01" })

        expect(mockPost).toHaveBeenCalledWith(expect.stringMatching(/employees-on-leave\/run$/u), {
            startDate: "2026-07-01",
        })
        expect(outcome).toStrictEqual({ result, errors: [] })
    })

    it("returns the server's validation messages", async () => {
        expect.hasAssertions()
        mockPost.mockResolvedValue({ success: false, result: null, errors: ["Start date is required."] })

        await expect(reportService.run(definition.key, {})).resolves.toStrictEqual({
            result: null,
            errors: ["Start date is required."],
        })
    })

    it("falls back to a general message when the server gives none", async () => {
        expect.hasAssertions()
        mockPost.mockResolvedValue({ success: false, result: null, errors: [""] })

        await expect(reportService.run(definition.key, {})).resolves.toStrictEqual({
            result: null,
            errors: ["The report could not be run."],
        })
    })
})

describe("reportService.exportReport()", () => {
    it("downloads the file under the server's filename", async () => {
        expect.hasAssertions()
        const blob = new Blob(["a,b"])
        mockPostForBlob.mockResolvedValue({ blob, filename: "Employees on Leave.csv" })

        const exported = await reportService.exportReport(definition.key, "csv", { search: "smith" })

        expect(exported).toBeTruthy()
        expect(mockPostForBlob).toHaveBeenCalledWith(expect.stringMatching(/employees-on-leave\/export\/csv$/u), {
            search: "smith",
        })
        expect(mockDownloadBlob).toHaveBeenCalledWith(blob, "Employees on Leave.csv")
    })

    it("names the file after the report when the server sends no filename", async () => {
        expect.hasAssertions()
        const blob = new Blob(["%PDF"])
        mockPostForBlob.mockResolvedValue({ blob, filename: null })

        await reportService.exportReport(definition.key, "pdf", {})

        expect(mockDownloadBlob).toHaveBeenCalledWith(blob, "personnel.employees-on-leave.pdf")
    })

    it("returns false when the export fails", async () => {
        expect.hasAssertions()
        mockDownloadBlob.mockClear()
        mockPostForBlob.mockRejectedValue(new Error("Bad Request"))

        await expect(reportService.exportReport(definition.key, "xlsx", {})).resolves.toBeFalsy()
        expect(mockDownloadBlob).not.toHaveBeenCalled()
    })
})
