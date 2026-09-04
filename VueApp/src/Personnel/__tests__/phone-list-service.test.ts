import { phoneListService } from "../services/phone-list-service"

/**
 * The "null on failure" convention is what PhoneList.vue and PhoneListMaintain.vue branch on to
 * decide whether to show a not-found banner, so a regression here would surface as a silently
 * blank page rather than a caught error. Lookup is by the list's stable code, so a list renamed
 * for display keeps resolving.
 */

const mockGet = vi.fn<(...args: unknown[]) => unknown>()
vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (...args: unknown[]) => mockGet(...args),
    }),
}))

const listInfo = {
    phoneListId: 5,
    code: "VMDO",
    name: "Dean's Office",
    canMaintain: true,
    canViewDirectPhone: true,
}

describe("phoneListService()", () => {
    it("returns the list info on success", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: true, result: listInfo })

        const result = await phoneListService.getPhoneListInfo("VMDO")

        expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("phonelist/VMDO"))
        expect(result).toStrictEqual(listInfo)
    })

    it("percent-encodes a code so it cannot break out of the path segment", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: true, result: listInfo })

        await phoneListService.getPhoneListInfo("a/b c")

        expect(mockGet).toHaveBeenCalledWith(expect.stringContaining("phonelist/a%2Fb%20c"))
    })

    it("returns null when the request fails", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: false, result: null })

        const result = await phoneListService.getPhoneListInfo("VMDO")

        expect(result).toBeNull()
    })

    it("returns null when the request succeeds but finds no matching list", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        mockGet.mockResolvedValue({ success: true, result: null })

        const result = await phoneListService.getPhoneListInfo("Nonexistent")

        expect(result).toBeNull()
    })
})
