import { mount, flushPromises } from "@vue/test-utils"
import { Quasar, Notify } from "quasar"
import { createRouter, createMemoryHistory } from "vue-router"
import PhoneList from "../pages/PhoneList.vue"
import PhoneListMaintain from "../pages/PhoneListMaintain.vue"
import { getPhoneListData } from "../composables/phone-list-data-fetch"
import { phoneListModifiedDateService } from "../services/phone-list-modified-date-service.ts"
import { phoneListService } from "../services/phone-list-service.ts"

/**
 * Every unit list renders through one :code route, so Vue Router reuses the page component when
 * the code changes instead of remounting it - a mounted hook fires only for the first list. These
 * tests drive a real router rather than a stubbed useRoute, because that reuse is exactly the
 * behaviour at issue: a stubbed route cannot reproduce it.
 *
 * On the maintain page the stale-code case is worse than stale display, since listCode is what
 * scopes every write the page sends.
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
vi.mock("../services/phone-list-unit-service.ts", () => ({
    phoneListUnitService: { deleteUnitPersonData: vi.fn<(...args: unknown[]) => unknown>() },
}))
vi.mock("@/composables/use-confirm-dialog", () => ({
    useConfirmDialog: () => ({ confirmAction: vi.fn<(...args: unknown[]) => unknown>() }),
}))

function stubList(code: string) {
    vi.mocked(phoneListService.getPhoneListInfo).mockResolvedValue({
        phoneListId: 1,
        code,
        name: `${code} Phone List`,
        canMaintain: true,
        canViewDirectPhone: true,
    })
}

/** Mounts the page behind a real router so param-only navigation reuses the component. */
async function mountUnderRouter(component: unknown) {
    const router = createRouter({
        history: createMemoryHistory(),
        routes: [
            { path: "/Personnel/PhoneList/:code", component: component as never, name: "PhoneList" },
            { path: "/Personnel/", component: { template: "<div />" }, name: "PersonnelHome" },
        ],
    })
    const wrapper = mount(
        { template: "<router-view />" },
        { global: { plugins: [[Quasar, { plugins: { Notify } }], router] } },
    )
    await router.push("/Personnel/PhoneList/VMDO")
    await router.isReady()
    await flushPromises()
    return { router, wrapper }
}

describe("phone list pages - changing the route code", () => {
    it("refetches the newly named list when the code changes", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        stubList("VMDO")
        vi.mocked(phoneListModifiedDateService.getModifiedDate).mockResolvedValue(null)
        vi.mocked(getPhoneListData).mockResolvedValue([])

        const { router } = await mountUnderRouter(PhoneList)
        expect(phoneListService.getPhoneListInfo).toHaveBeenCalledWith("VMDO")

        stubList("OTHER")
        await router.push("/Personnel/PhoneList/OTHER")
        await flushPromises()

        expect(phoneListService.getPhoneListInfo).toHaveBeenLastCalledWith("OTHER")
        expect(getPhoneListData).toHaveBeenLastCalledWith("OTHER", false, true)
    })

    it("shows the new list's name rather than the previous one", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        stubList("VMDO")
        vi.mocked(phoneListModifiedDateService.getModifiedDate).mockResolvedValue(null)
        vi.mocked(getPhoneListData).mockResolvedValue([])

        const { router, wrapper } = await mountUnderRouter(PhoneList)
        expect(wrapper.find("h1").text()).toBe("VMDO Phone List")

        stubList("OTHER")
        await router.push("/Personnel/PhoneList/OTHER")
        await flushPromises()

        expect(wrapper.find("h1").text()).toBe("OTHER Phone List")
    })

    it("rescopes the maintain page to the new list, so edits go to the right one", async () => {
        expect.hasAssertions()
        vi.clearAllMocks()
        stubList("VMDO")
        vi.mocked(getPhoneListData).mockResolvedValue([])

        const { router, wrapper } = await mountUnderRouter(PhoneListMaintain)

        stubList("OTHER")
        await router.push("/Personnel/PhoneList/OTHER")
        await flushPromises()

        // The dialog receives listCode and passes it to every save. A stale value here would
        // write the edits for the new list into the previous one.
        const dialog = wrapper.findComponent({ name: "PhoneListAddRecordDialog" })
        expect(dialog.props("listCode")).toBe("OTHER")
        expect(getPhoneListData).toHaveBeenLastCalledWith("OTHER", true, true)
    })
})
