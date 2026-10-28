import { mount, flushPromises } from "@vue/test-utils"
import { Quasar, Notify } from "quasar"
import PhoneList from "../pages/PhoneList.vue"
import { getPhoneListData } from "../composables/phone-list-data-fetch"
import { phoneListModifiedDateService } from "../services/phone-list-modified-date-service.ts"
import { phoneListService } from "../services/phone-list-service.ts"
import type { PhoneListDisplayRecord, PhoneListUnit } from "../types/phone-list-phone-types"
import { linkTargets, openJumpLinks, trackAttached, unmountAttached } from "./test-utils"

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
        vi.mocked(phoneListService.getPhoneListInfo).mockResolvedValue(null)

        const wrapper = mountPage()
        await flushPromises()

        expect(wrapper.text()).toContain("could not be found")
        expect(getPhoneListData).not.toHaveBeenCalled()
    })
})

// Attached, so the jump links' portalled menu reaches the document.
function mountAttachedPage() {
    return trackAttached(
        mount(PhoneList, {
            global: { plugins: [[Quasar, { plugins: { Notify } }]] },
            attachTo: document.body,
        }),
    )
}

afterEach(unmountAttached)

const unitCols = [
    { name: "name", label: "Name", field: "name", align: "left" as const },
    { name: "office", label: "Office", field: "office", align: "left" as const },
]

// Only the fields the jump links read: the unit name, and enough of a row for the filter to match
// on. The rest of PhoneListDisplayRecord is irrelevant here.
function unit(id: number, name: string, row: { name: string; office?: string }): PhoneListUnit {
    const { office = "Room 100" } = row
    return {
        name,
        id,
        cols: unitCols,
        rows: [{ name: row.name, office, unitPersonId: id, employeeMailId: "" } as unknown as PhoneListDisplayRecord],
    }
}

describe("phoneList.vue - unit jump links", () => {
    async function mountWithUnits(units: PhoneListUnit[]) {
        stubListInfo(false)
        vi.mocked(getPhoneListData).mockResolvedValue(units)
        const wrapper = mountAttachedPage()
        await flushPromises()
        return wrapper
    }

    it("links to every unit", async () => {
        expect.hasAssertions()
        const wrapper = await mountWithUnits([
            unit(1, "Dean's Office", { name: "Smith, Amy" }),
            unit(2, "Business Office", { name: "Jones, Bo" }),
        ])

        await expect(linkTargets(wrapper)).resolves.toStrictEqual(["Dean's Office", "Business Office"])
    })

    it("points each link at the heading of its own unit", async () => {
        expect.hasAssertions()
        const wrapper = await mountWithUnits([
            unit(1, "Dean's Office", { name: "Smith, Amy" }),
            unit(2, "Business Office", { name: "Jones, Bo" }),
        ])

        // The links come from the portalled menu; the headings they point at stay in the page.
        const links = await openJumpLinks(wrapper)

        expect(links).not.toHaveLength(0)

        for (const link of links) {
            const id = link.getAttribute("href")!.slice(1)

            expect(wrapper.find(`h2#${id}`).exists()).toBeTruthy()
        }
    })

    it("drops a unit from the links once a search leaves it with no rows", async () => {
        expect.hasAssertions()
        const wrapper = await mountWithUnits([
            unit(1, "Dean's Office", { name: "Smith, Amy", office: "Room 100" }),
            unit(2, "Business Office", { name: "Jones, Bo", office: "Room 100" }),
            unit(3, "Teaching Office", { name: "Lee, Cy", office: "Room 200" }),
        ])

        // A unit the search empties is hidden, so a link to it would go nowhere.
        await wrapper.findComponent({ name: "QInput" }).setValue("room 100")

        await expect(linkTargets(wrapper)).resolves.toStrictEqual(["Dean's Office", "Business Office"])
    })

    it("says once, below the filter, when a search leaves every unit empty", async () => {
        expect.hasAssertions()
        const wrapper = await mountWithUnits([
            unit(1, "Dean's Office", { name: "Smith, Amy" }),
            unit(2, "Business Office", { name: "Jones, Bo" }),
        ])
        const status = () => wrapper.find("[role='status']").text()

        expect(status()).toBe("")

        await wrapper.findComponent({ name: "QInput" }).setValue("nobody")

        expect(status()).toBe('No records match "nobody".')

        await wrapper.findComponent({ name: "QInput" }).setValue("jones")

        expect(status()).toBe("")
    })

    it("offers no jump links for a list with a single unit", async () => {
        expect.hasAssertions()
        const wrapper = await mountWithUnits([unit(1, "Dean's Office", { name: "Smith, Amy" })])

        await expect(linkTargets(wrapper)).resolves.toStrictEqual([])
    })
})
