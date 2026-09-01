import { mount, flushPromises } from "@vue/test-utils"
import { Quasar, Notify } from "quasar"
import PhoneList from "../pages/PhoneList.vue"
import { getPhoneListData } from "../composables/phone-list-data-fetch"
import { phoneListModifiedDateService } from "../services/phone-list-modified-date-service.ts"
import { phoneListService } from "../services/phone-list-service.ts"

/**
 * PhoneList is the read-only view of any unit list, resolved from the :code route param. It
 * hides its "Updated"/internal banner while the initial fetch is in flight (v-if="!loading"),
 * and within that block only shows the "FOR INTERNAL USE ONLY" notice for callers with
 * direct-phone access - the UI-level counterpart of the canViewDirectPhone flag already covered
 * at the composable/service layer.
 */

vi.mock("../composables/phone-list-data-fetch", () => ({
    getPhoneListData: vi.fn<(...args: unknown[]) => unknown>(),
}))
vi.mock("../services/phone-list-modified-date-service.ts", () => ({
    phoneListModifiedDateService: { getModifiedDate: vi.fn<(...args: unknown[]) => unknown>() },
}))
vi.mock("../services/phone-list-service.ts", () => ({
    phoneListService: { getPhoneListInfo: vi.fn<(...args: unknown[]) => unknown>() },
}))
vi.mock("vue-router", () => ({
    useRoute: () => ({ params: { code: "VMDO" } }),
}))

// Never actually resolves, to simulate a fetch that's still in flight.
function neverResolves<T>(): Promise<T> {
    // eslint-disable-next-line avoid-new, no-empty-function -- deliberately pending forever, to simulate an in-flight fetch
    return new Promise<T>(() => {})
}

function mountPage() {
    return mount(PhoneList, {
        global: { plugins: [[Quasar, { plugins: { Notify } }]] },
    })
}

function stubListInfo(canViewDirectPhone: boolean): void {
    vi.clearAllMocks()
    vi.mocked(phoneListService.getPhoneListInfo).mockResolvedValue({
        phoneListId: 1,
        code: "VMDO",
        name: "Dean's Office Phone List",
        canMaintain: false,
        canViewDirectPhone,
    })
    vi.mocked(phoneListModifiedDateService.getModifiedDate).mockResolvedValue(null)
}

describe("phoneList.vue - loading, naming, and internal-use banner", () => {
    it("hides the Updated/internal-use block while the initial fetch is in flight", async () => {
        expect.hasAssertions()
        stubListInfo(true)
        // Never resolves, so the fetch stays in flight: loading flips true (in onMounted) and
        // never flips back.
        vi.mocked(getPhoneListData).mockReturnValue(neverResolves())

        const wrapper = mountPage()
        await flushPromises()

        expect(wrapper.text()).not.toContain("Click on a name to send an email")
        expect(wrapper.text()).not.toContain("FOR INTERNAL USE ONLY")
    })

    it("shows the internal-use notice once loaded, for a caller with internal access", async () => {
        expect.hasAssertions()
        stubListInfo(true)
        vi.mocked(getPhoneListData).mockResolvedValue([])

        const wrapper = mountPage()
        await flushPromises()

        expect(wrapper.text()).toContain("Click on a name to send an email")
        expect(wrapper.text()).toContain("FOR INTERNAL USE ONLY")
    })

    it("hides the internal-use notice once loaded, for a caller without internal access", async () => {
        expect.hasAssertions()
        stubListInfo(false)
        vi.mocked(getPhoneListData).mockResolvedValue([])

        const wrapper = mountPage()
        await flushPromises()

        expect(wrapper.text()).toContain("Click on a name to send an email")
        expect(wrapper.text()).not.toContain("FOR INTERNAL USE ONLY")
    })

    it("takes its heading from the list rather than hard-coded page copy", async () => {
        expect.hasAssertions()
        stubListInfo(false)
        vi.mocked(getPhoneListData).mockResolvedValue([])

        const wrapper = mountPage()
        await flushPromises()

        expect(wrapper.find("h1").text()).toBe("Dean's Office Phone List")
    })

    it("fetches the list named by the route param", async () => {
        expect.hasAssertions()
        stubListInfo(false)
        vi.mocked(getPhoneListData).mockResolvedValue([])

        mountPage()
        await flushPromises()

        expect(phoneListService.getPhoneListInfo).toHaveBeenCalledWith("VMDO")
        expect(getPhoneListData).toHaveBeenCalledWith("VMDO", false, false)
    })

    it("reports an unknown list code instead of rendering an empty list", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        vi.mocked(phoneListService.getPhoneListInfo).mockResolvedValue(null)

        const wrapper = mountPage()
        await flushPromises()

        expect(wrapper.text()).toContain("could not be found")
        expect(getPhoneListData).not.toHaveBeenCalled()
    })
})
