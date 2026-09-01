import { searchPeopleOptions } from "../services/phone-person-options-service"

/**
 * SearchPeopleOptions deliberately returns null (not []) on a failed request, so
 * usePersonSearch's callers can fall back safely via `result ?? []` while still distinguishing
 * "no matches" from "the fetch failed" for anyone who cares to.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
        createUrlSearchParams: (obj: Record<string, string | number | null | undefined>) => {
            const params = new URLSearchParams()
            for (const [k, v] of Object.entries(obj)) {
                if (v !== null && v !== undefined) {
                    params.append(k, v.toString())
                }
            }
            return params
        },
    }),
}))

describe("searchPeopleOptions()", () => {
    it("returns the matching people, scoped to the given list code", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        const people = [
            {
                personId: 1,
                firstName: "Amy",
                lastName: "Smith",
                fullName: "Amy Smith",
                iamId: "asmith",
                currentEmployee: true,
                mailId: "asmith",
                phoneData: null,
            },
        ]
        mockGet.mockResolvedValue({ success: true, result: people })

        const result = await searchPeopleOptions("Smith", "VMDO")

        expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("search=Smith"))
        expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("listCode=VMDO"))
        expect(result).toStrictEqual(people)
    })

    it("returns an empty array (not null) when the search matches nobody", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: true, result: [] })

        const result = await searchPeopleOptions("Nonexistent")

        expect(result).toStrictEqual([])
    })

    it("returns null (not an empty array) when the request fails", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: false, result: null })

        const result = await searchPeopleOptions("Smith")

        expect(result).toBeNull()
    })
})
