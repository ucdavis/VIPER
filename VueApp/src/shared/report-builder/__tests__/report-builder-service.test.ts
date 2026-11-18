import { reportBuilderService } from "../report-builder-service"
import type { ReportRunRequest } from "../report-builder-types"
import { resultOf, roster } from "./report-builder-fixtures"

/**
 * The service returns null or an outcome instead of throwing, so pages can tell a failure apart
 * from an empty result and show the server's messages.
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

const request: ReportRunRequest = {
    parameters: { Dept: ["VME"] },
    page: 1,
    perPage: 25,
    sortBy: null,
    sortDescending: false,
    search: null,
}

describe("catalog", () => {
    it("returns the user's reports", async () => {
        expect.hasAssertions()
        const catalog = [{ slug: roster.slug, title: roster.title, description: null, area: "Personnel" }]
        mockGet.mockResolvedValue({ success: true, result: catalog })

        await expect(reportBuilderService.getCatalog()).resolves.toStrictEqual(catalog)
        expect(mockGet).toHaveBeenCalledWith(expect.stringMatching(/report-builder$/u))
    })

    it.each([
        { success: false, result: null },
        { success: true, result: { slug: "x" } },
    ])("returns null when the request fails or isn't a list (%j)", async (response) => {
        expect.hasAssertions()
        mockGet.mockResolvedValue(response)

        await expect(reportBuilderService.getCatalog()).resolves.toBeNull()
    })
})

describe("definition", () => {
    it("returns the report's definition", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: true, result: roster })

        await expect(reportBuilderService.getDefinition(roster.slug)).resolves.toStrictEqual(roster)
        expect(mockGet).toHaveBeenCalledWith(expect.stringMatching(new RegExp(`report-builder/${roster.slug}$`, "u")))
    })

    it("returns null when the report isn't available", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: false, result: null })

        await expect(reportBuilderService.getDefinition("x")).resolves.toBeNull()
    })
})

describe("run", () => {
    it("posts the request and returns the rows", async () => {
        expect.hasAssertions()
        const result = resultOf([{ Name: "Ada", Salary: 1 }])
        mockPost.mockResolvedValue({ success: true, result })

        await expect(reportBuilderService.run(roster.slug, request)).resolves.toStrictEqual({ result, errors: [] })
        expect(mockPost).toHaveBeenCalledWith(expect.stringMatching(/\/run$/u), request)
    })

    it("returns the server's messages", async () => {
        expect.hasAssertions()
        mockPost.mockResolvedValue({ success: false, result: null, errors: ["Department is required.", ""] })

        await expect(reportBuilderService.run(roster.slug, request)).resolves.toStrictEqual({
            result: null,
            errors: ["Department is required."],
        })
    })

    it("explains a failure that came without messages", async () => {
        expect.hasAssertions()
        mockPost.mockResolvedValue({ success: false, result: null, errors: null })

        const outcome = await reportBuilderService.run(roster.slug, request)

        expect(outcome.errors).toStrictEqual(["The report couldn't be run. Please try again later."])
    })
})

describe("download", () => {
    it("saves the file under the server's name", async () => {
        expect.hasAssertions()
        const blob = new Blob(["xlsx"])
        mockPostForBlob.mockResolvedValue({ blob, filename: "Faculty roster.xlsx" })

        await expect(reportBuilderService.exportReport(roster, "xlsx", request)).resolves.toBe("downloaded")
        expect(mockPostForBlob).toHaveBeenCalledWith(expect.stringMatching(/\/export\/xlsx$/u), request)
        expect(mockDownloadBlob).toHaveBeenCalledWith(blob, "Faculty roster.xlsx")
    })

    it("names the file after the report when the server doesn't", async () => {
        expect.hasAssertions()
        mockPostForBlob.mockResolvedValue({ blob: new Blob(["pdf"]), filename: null })

        await reportBuilderService.exportReport(roster, "pdf", request)

        expect(mockDownloadBlob).toHaveBeenCalledWith(expect.any(Blob), "Faculty roster.pdf")
    })

    it("saves nothing when the report has no rows", async () => {
        expect.hasAssertions()
        mockPostForBlob.mockResolvedValue({ blob: new Blob([]), filename: null })

        await expect(reportBuilderService.exportReport(roster, "pdf", request)).resolves.toBe("empty")
        expect(mockDownloadBlob).not.toHaveBeenCalled()
    })

    it("reports a failed download", async () => {
        expect.hasAssertions()
        mockPostForBlob.mockRejectedValue(new Error("Forbidden"))

        await expect(reportBuilderService.exportReport(roster, "pdf", request)).resolves.toBe("failed")
    })
})
