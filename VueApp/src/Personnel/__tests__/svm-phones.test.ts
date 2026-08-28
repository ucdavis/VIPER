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

function mountPage() {
    return mount(SVMPhones, {
        global: { plugins: [Quasar] },
    })
}

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

function linkTargets(wrapper: ReturnType<typeof mountPage>): string[] {
    return wrapper.findAll("a[href^='#']").map((link) => link.text())
}

describe("sVMPhones.vue - section jump links", () => {
    const sectionCols = [
        { name: "unitName", label: "Unit", field: "unitName", align: "left" as const },
        { name: "location", label: "Location", field: "officeLocation", align: "left" as const },
    ]

    // Only the fields the jump links read: the section title, and enough of a row for the filter
    // to match on. The rest of SVMPhoneDisplayRecord is irrelevant here.
    function section(id: number, title: string, row: { unitName: string; officeLocation?: string }): SVMPhoneSection {
        const { unitName, officeLocation = "Room 100" } = row
        return {
            title,
            id,
            cols: sectionCols,
            rows: [{ unitName, officeLocation, entryId: id } as unknown as SVMPhoneDisplayRecord],
        }
    }

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

        expect(linkTargets(wrapper)).toStrictEqual(["VMDO", "VMTH", "Frequently Called Numbers"])
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

        // Every link resolves to a heading that is actually on the page.
        for (const link of wrapper.findAll("a[href^='#']")) {
            const id = link.attributes("href")!.slice(1)

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

        expect(linkTargets(wrapper)).toStrictEqual(["VMDO", "VMTH"])
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

        expect(linkTargets(wrapper)).toStrictEqual(["VMDO", "Frequently Called Numbers"])
    })
})
