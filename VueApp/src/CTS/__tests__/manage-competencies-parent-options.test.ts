import { mount, flushPromises } from "@vue/test-utils"
import { Quasar } from "quasar"
import ManageCompetencies from "@/CTS/pages/ManageCompetencies.vue"
import type { Competency } from "@/CTS/types"

function comp(competencyId: number, parentId: number | null, children: Competency[] = []): Competency {
    return {
        competencyId,
        parentId,
        children,
        domainId: 1,
        name: `Competency ${competencyId}`,
        number: String(competencyId),
        description: null,
        canLinkToStudent: false,
        domain: null,
        type: "Competency",
    }
}

// 1 > 2 > 3, plus an unrelated root 4
const grandchild = comp(3, 2)
const child = comp(2, 1, [grandchild])
const root = comp(1, null, [child])
const unrelated = comp(4, null)

const responses: Record<string, unknown> = {
    "cts/domains": [],
    "cts/competencies": [comp(1, null), comp(2, 1), comp(3, 2), comp(4, null)],
    "cts/competencies/hierarchy": [root, unrelated],
}

vi.mock("@/composables/ViperFetch", () => ({
    useFetch: () => ({
        get: (url: string) => Promise.resolve({ success: true, result: responses[url] }),
    }),
}))

type PageVm = {
    editComp: (c: Competency) => void
    addChild: (c: Competency) => void
    parentOptions: Competency[]
}

async function mountPage() {
    const wrapper = mount(ManageCompetencies, {
        global: {
            plugins: [Quasar],
            provide: { apiURL: "" },
            stubs: { QDialog: true },
        },
    })
    await flushPromises()
    return wrapper.vm as unknown as PageVm
}

const optionIds = (vm: PageVm) => vm.parentOptions.map((c) => c.competencyId)

describe("manageCompetencies parent options", () => {
    it("excludes the competency and all its descendants when editing", async () => {
        expect.assertions(1)
        const vm = await mountPage()

        vm.editComp(root)

        expect(optionIds(vm)).toStrictEqual([4])
    })

    it("keeps ancestors available when editing a descendant", async () => {
        expect.assertions(1)
        const vm = await mountPage()

        vm.editComp(grandchild)

        expect(optionIds(vm)).toStrictEqual([1, 2, 4])
    })

    it("offers every competency when adding", async () => {
        expect.assertions(1)
        const vm = await mountPage()

        vm.addChild(root)

        expect(optionIds(vm)).toStrictEqual([1, 2, 3, 4])
    })

    it("offers nothing, without throwing, when the competencies request fails", async () => {
        expect.assertions(1)
        const saved = responses["cts/competencies"]
        responses["cts/competencies"] = undefined
        const vm = await mountPage()
        responses["cts/competencies"] = saved

        expect(optionIds(vm)).toStrictEqual([])
    })
})
