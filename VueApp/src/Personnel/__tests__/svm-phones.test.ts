import { mount, flushPromises } from "@vue/test-utils"
import { Quasar } from "quasar"
import SVMPhones from "../pages/SVMPhones.vue"
import { getFrequentlyCalledNumbers, getSVMData } from "../composables/svm-data-fetch"
import { svmModifiedDateService } from "../services/svm-modified-date-service.ts"

/**
 * SVMPhones hides its "Updated" line while the initial fetch is in flight (v-if="!loading").
 * Unlike PhoneList, the SVM list has no isInternal-gated content: DirectPhone is always
 * blanked for SVM data (see PhoneSVMUnitService), so there's no internal-use banner here.
 */

vi.mock("../composables/svm-data-fetch", () => ({
    getSVMData: vi.fn<(...args: unknown[]) => unknown>(),
    getFrequentlyCalledNumbers: vi.fn<(...args: unknown[]) => unknown>(),
}))
vi.mock("../services/svm-modified-date-service.ts", () => ({
    svmModifiedDateService: { getModifiedDate: vi.fn<(...args: unknown[]) => unknown>() },
}))

// Never actually resolves, to simulate a fetch that's still in flight.
function neverResolves<T>(): Promise<T> {
    // eslint-disable-next-line avoid-new, no-empty-function -- deliberately pending forever, to simulate an in-flight fetch
    return new Promise<T>(() => {})
}

function mountPage() {
    return mount(SVMPhones, {
        global: { plugins: [Quasar] },
    })
}

function stubDataServices(): void {
    vi.clearAllMocks()
    vi.mocked(getFrequentlyCalledNumbers).mockResolvedValue([])
    vi.mocked(svmModifiedDateService.getModifiedDate).mockResolvedValue(null)
}

describe("sVMPhones.vue - loading", () => {
    it("hides the Updated line while the initial fetch is in flight", async () => {
        expect.hasAssertions()
        stubDataServices()
        vi.mocked(getSVMData).mockReturnValue(neverResolves())

        const wrapper = mountPage()
        await flushPromises()

        expect(wrapper.text()).not.toContain("Updated")
    })

    it("shows the Updated line once the initial fetch resolves", async () => {
        expect.hasAssertions()
        stubDataServices()
        vi.mocked(getSVMData).mockResolvedValue({
            newSections: [],
            newUnitOptions: [],
            newUnitFaxNumbers: [],
            newUnitAdminStaff: [],
        })

        const wrapper = mountPage()
        await flushPromises()

        expect(wrapper.text()).toContain("Updated")
    })
})
