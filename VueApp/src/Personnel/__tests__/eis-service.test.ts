import { eisService } from "../services/eis-service"

/**
 * Every EIS request returns null on failure, which the pages show as "not available". A refused
 * request (another unit's employee) looks the same as a missing one on purpose.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
const mockPut = vi.fn<(...args: unknown[]) => unknown>()
const mockDel = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        put: (...args: unknown[]) => mockPut(...args),
        del: (...args: unknown[]) => mockDel(...args),
    }),
}))

describe("eis API client", () => {
    it("requests each page for the employee and returns its result", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValue({ success: true, result: { ok: true } })

        await expect(eisService.getPeople()).resolves.toStrictEqual({ ok: true })
        await eisService.getHeader("10123456")
        await eisService.getAppointments("10123456")
        await eisService.getHistory("10123456")
        await eisService.getAddress("10123456")
        await eisService.getAcademics("10123456")
        await eisService.getCategories("10123456")

        const urls = mockGet.mock.calls.map((call) => String(call[0]))
        expect(urls[0]).toMatch(/personnel\/eis\/people$/u)
        expect(urls[1]).toMatch(/people\/10123456$/u)
        expect(urls.slice(2)).toStrictEqual([
            expect.stringMatching(/10123456\/appointments$/u),
            expect.stringMatching(/10123456\/history$/u),
            expect.stringMatching(/10123456\/address$/u),
            expect.stringMatching(/10123456\/academics$/u),
            expect.stringMatching(/10123456\/categories$/u),
        ])
    })

    it("returns null when a request fails or has no result", async () => {
        expect.hasAssertions()
        mockGet.mockResolvedValueOnce({ success: false, result: null })
        mockGet.mockResolvedValueOnce({ success: true, result: null })

        await expect(eisService.getHeader("10123456")).resolves.toBeNull()
        await expect(eisService.getPeople()).resolves.toBeNull()
    })

    it("sets a flag with PUT and clears it with DELETE, returning the updated categories", async () => {
        expect.hasAssertions()
        const updated = { categories: [], academicYear: "2026-2027", canEditFlags: true }
        mockPut.mockResolvedValue({ success: true, result: updated })
        mockDel.mockResolvedValue({ success: false, result: null })

        await expect(eisService.setFlag("10123456", 4, true)).resolves.toStrictEqual(updated)
        await expect(eisService.setFlag("10123456", 4, false)).resolves.toBeNull()

        expect(String(mockPut.mock.calls[0]?.[0])).toMatch(/people\/10123456\/flags\/4$/u)
        expect(String(mockDel.mock.calls[0]?.[0])).toMatch(/people\/10123456\/flags\/4$/u)
    })

    it("encodes the employee ID in paths, including the photo", () => {
        expect.hasAssertions()
        expect(eisService.photoUrl("a/b")).toMatch(/people\/a%2Fb\/photo$/u)
    })
})
