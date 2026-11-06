import { personCollectorService } from "../services/person-collector-service"
import type { PersonCollectorRequest } from "../types/person-collector-types"

/**
 * The Person Collector client returns null on any failure so the page can say so, and downloads
 * the Excel export under the server's filename.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockPost = vi.fn<(...args: unknown[]) => unknown>()
const mockPostForBlob = vi.fn<(...args: unknown[]) => unknown>()
const mockDownloadBlob = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        post: (...args: unknown[]) => mockPost(...args),
    }),
    postForBlob: (...args: unknown[]) => mockPostForBlob(...args),
    downloadBlob: (...args: unknown[]) => mockDownloadBlob(...args),
}))

const request: PersonCollectorRequest = {
    departments: ["VME"],
    fullDepartmentList: false,
    senateGroups: [],
    senateEmeriti: true,
    federationGroups: [],
    staffMsp: false,
    staffPss: false,
    staffVeterinarians: false,
    studentClasses: [],
    studentMpvm: false,
}

describe("person collector API client", () => {
    it("loads the form and posts the request for results", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: true, result: { departments: [] } })
        mockPost.mockResolvedValue({ success: true, result: { sections: [] } })

        await expect(personCollectorService.getForm()).resolves.toStrictEqual({ departments: [] })
        await expect(personCollectorService.getResults(request)).resolves.toStrictEqual({ sections: [] })

        expect(String(mockGet.mock.calls[0]?.[0])).toMatch(/personnel\/person-collector\/form$/u)
        expect(mockPost.mock.calls[0]).toStrictEqual([expect.stringMatching(/person-collector\/results$/u), request])
    })

    it("returns null when a request fails or has no result", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: false, result: null })
        mockPost.mockResolvedValue({ success: true, result: undefined })

        await expect(personCollectorService.getForm()).resolves.toBeNull()
        await expect(personCollectorService.getResults(request)).resolves.toBeNull()
    })

    it("downloads the Excel export, falling back to a default filename", async () => {
        expect.hasAssertions()
        const blob = new Blob(["x"])
        mockPostForBlob.mockResolvedValueOnce({ blob, filename: "PersonCollector-1.xlsx" })
        mockPostForBlob.mockResolvedValueOnce({ blob, filename: null })

        await personCollectorService.exportExcel(request)
        await personCollectorService.exportExcel(request)

        expect(mockPostForBlob.mock.calls[0]).toStrictEqual([
            expect.stringMatching(/person-collector\/export$/u),
            request,
        ])
        expect(mockDownloadBlob.mock.calls).toStrictEqual([
            [blob, "PersonCollector-1.xlsx"],
            [blob, "PersonCollector.xlsx"],
        ])
    })
})
