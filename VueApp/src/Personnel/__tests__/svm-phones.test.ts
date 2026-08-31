import { mount, flushPromises } from "@vue/test-utils"
import { Quasar } from "quasar"
import SVMPhones from "../pages/SVMPhones.vue"
import { getFrequentlyCalledNumbers, getSVMData } from "../composables/svm-data-fetch"
import { svmModifiedDateService } from "../services/svm-modified-date-service.ts"
import type { SVMFrequentNumberRecord, SVMPhoneDisplayRecord, SVMPhoneSection } from "../types/svm-phone-types"

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

// The jump links render through a portal, so they only reach the document when the page is
// attached to it, and they outlive the wrapper unless it is unmounted.
const mounted: ReturnType<typeof mount>[] = []

function mountPage() {
    const wrapper = mount(SVMPhones, {
        global: { plugins: [Quasar] },
        attachTo: document.body,
    })
    mounted.push(wrapper)
    return wrapper
}

afterEach(() => {
    for (const wrapper of mounted.splice(0)) {
        wrapper.unmount()
    }
    document.body.innerHTML = ""
})

function stubDataServices(): void {
    vi.clearAllMocks()
    vi.mocked(getFrequentlyCalledNumbers).mockResolvedValue({ rows: [], error: null })
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
            error: null,
        })

        const wrapper = mountPage()
        await flushPromises()

        expect(wrapper.text()).toContain("Updated")
    })
})

/**
 * The links sit behind a menu trigger, out of the sticky bar's flow, so they have to be opened
 * before they exist anywhere to query. No trigger means too few sections to navigate between.
 */
async function openJumpLinks(wrapper: ReturnType<typeof mountPage>): Promise<HTMLAnchorElement[]> {
    const trigger = wrapper.findComponent({ name: "SectionJumpLinks" }).find("button")
    if (trigger.exists()) {
        await trigger.trigger("click")
    }
    return [...document.querySelectorAll<HTMLAnchorElement>(".q-menu a[href^='#']")]
}

async function linkTargets(wrapper: ReturnType<typeof mountPage>): Promise<string[]> {
    const links = await openJumpLinks(wrapper)
    return links.map((link) => link.textContent?.trim() ?? "")
}

const sectionCols = [
    { name: "unitName", label: "Unit", field: "unitName", align: "left" as const },
    { name: "location", label: "Location", field: "officeLocation", align: "left" as const },
]

// Only the fields the jump links read: the section title, and enough of a row for the filter to
// match on. The rest of SVMPhoneDisplayRecord is irrelevant here.
function section(id: number, title: string, row: { unitName: string; officeLocation?: string }): SVMPhoneSection {
    const { unitName, officeLocation = "Room 100" } = row
    return {
        title,
        id,
        cols: sectionCols,
        rows: [{ unitName, officeLocation, entryId: id } as unknown as SVMPhoneDisplayRecord],
    }
}

describe("sVMPhones.vue - section jump links", () => {
    async function mountWithData(data: { sections: SVMPhoneSection[]; frequentNumbers?: SVMFrequentNumberRecord[] }) {
        stubDataServices()
        vi.mocked(getSVMData).mockResolvedValue({
            newSections: data.sections,
            newUnitOptions: [],
            newUnitFaxNumbers: [],
            newUnitAdminStaff: [],
            error: null,
        })
        vi.mocked(getFrequentlyCalledNumbers).mockResolvedValue({ rows: data.frequentNumbers ?? [], error: null })
        const wrapper = mountPage()
        await flushPromises()
        return wrapper
    }

    it("links to every section, and to the frequently called numbers alongside them", async () => {
        expect.hasAssertions()
        const wrapper = await mountWithData({
            sections: [
                section(1, "VMDO", { unitName: "Dean's Office" }),
                section(2, "VMTH", { unitName: "Business Office" }),
            ],
            frequentNumbers: [{ label: "Front Desk", phone: "530-555-1000", entryId: 7 }],
        })

        await expect(linkTargets(wrapper)).resolves.toStrictEqual(["VMDO", "VMTH", "Frequently Called Numbers"])
    })

    it("points each link at the heading of its own section", async () => {
        expect.hasAssertions()
        const wrapper = await mountWithData({
            sections: [
                section(1, "VMDO", { unitName: "Dean's Office" }),
                section(2, "VMTH", { unitName: "Business Office" }),
            ],
            frequentNumbers: [{ label: "Front Desk", phone: "530-555-1000", entryId: 7 }],
        })

        // Every link resolves to a heading that is actually on the page. The links come from the
        // portalled menu; the headings they point at stay in the page itself.
        const links = await openJumpLinks(wrapper)

        expect(links).not.toHaveLength(0)

        for (const link of links) {
            const id = link.getAttribute("href")!.slice(1)

            expect(wrapper.find(`h2#${id}`).exists()).toBeTruthy()
        }
    })

    it("drops a section from the links once a search leaves it with no rows", async () => {
        expect.hasAssertions()
        const wrapper = await mountWithData({
            sections: [
                section(1, "VMDO", { unitName: "Dean's Office", officeLocation: "Room 100" }),
                section(2, "VMTH", { unitName: "Business Office", officeLocation: "Room 100" }),
                section(3, "VMCVM", { unitName: "Teaching Office", officeLocation: "Room 200" }),
            ],
        })

        // The filtered-out sections still render, carrying their "no records" line, so a link to
        // one would send the reader somewhere empty.
        await wrapper.findComponent({ name: "QInput" }).setValue("room 100")

        await expect(linkTargets(wrapper)).resolves.toStrictEqual(["VMDO", "VMTH"])
    })

    it("keeps the frequently called numbers link when the search matches one of its numbers", async () => {
        expect.hasAssertions()
        const wrapper = await mountWithData({
            sections: [
                section(1, "VMDO", { unitName: "Dean's Office" }),
                section(2, "VMTH", { unitName: "Business Office" }),
            ],
            frequentNumbers: [{ label: "Dean's Hotline", phone: "530-555-9999", entryId: 7 }],
        })

        // "dean" appears in a section's row and in a frequent number, but in neither section
        // title, so this only passes if both were matched on their own rows.
        await wrapper.findComponent({ name: "QInput" }).setValue("dean")

        await expect(linkTargets(wrapper)).resolves.toStrictEqual(["VMDO", "Frequently Called Numbers"])
    })
})

describe("sVMPhones.vue - partial load failure", () => {
    // GetSVMData reports this when either its sections or its units read fails, and
    // getFrequentlyCalledNumbers when its own does. See LOAD_ERROR_MESSAGE.
    const loadError = "The phone list could not be loaded. Please refresh to try again."

    /** Sections arrive; the frequent numbers do not. The page still has plenty to show. */
    async function mountWithFailedFrequentNumbers() {
        stubDataServices()
        vi.mocked(getSVMData).mockResolvedValue({
            newSections: [
                section(1, "VMDO", { unitName: "Dean's Office" }),
                section(2, "VMTH", { unitName: "Business Office" }),
            ],
            newUnitOptions: [],
            newUnitFaxNumbers: [],
            newUnitAdminStaff: [],
            error: null,
        })
        vi.mocked(getFrequentlyCalledNumbers).mockResolvedValue({ rows: [], error: loadError })
        const wrapper = mountPage()
        await flushPromises()
        return wrapper
    }

    it("reports the failure", async () => {
        expect.hasAssertions()
        const wrapper = await mountWithFailedFrequentNumbers()

        expect(wrapper.text()).toContain(loadError)
    })

    it("keeps the filter over the sections that did arrive", async () => {
        expect.hasAssertions()
        const wrapper = await mountWithFailedFrequentNumbers()

        // The fetch layer deliberately returns whatever loaded alongside the error rather than
        // blanking the page, so gating the filter on the error would leave a full page of sections
        // with no way to search them.
        expect(wrapper.findComponent({ name: "PhoneListFilter" }).exists()).toBeTruthy()
    })

    it("keeps the jump links over the sections that did arrive", async () => {
        expect.hasAssertions()
        const wrapper = await mountWithFailedFrequentNumbers()
        const links = await openJumpLinks(wrapper)

        expect(links.map((link) => link.textContent?.trim())).toStrictEqual(["VMDO", "VMTH"])
    })
})
