import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import SVMPhoneSectionTable from "../components/SVMPhoneSectionTable.vue"
import type { SVMPhoneSection } from "../types/svm-phone-types"

const cols = [{ name: "unitName", label: "Unit", field: "unitName", align: "left" as const }]

function makeSection(): SVMPhoneSection {
    return { title: "VMDO", id: 1, cols, rows: [] }
}

function mountTable(isModify: boolean) {
    return mount(SVMPhoneSectionTable, {
        props: { section: makeSection(), loading: false, isModify, search: "" },
        global: { plugins: [Quasar] },
    })
}

function hasAddButton(wrapper: ReturnType<typeof mountTable>): boolean {
    return wrapper.findAllComponents({ name: "QBtn" }).some((btn) => btn.props("icon") === "add")
}

describe("sVMPhoneSectionTable.vue - isModify gating", () => {
    it("shows the add button when isModify is true", () => {
        expect.hasAssertions()
        const wrapper = mountTable(true)

        expect(hasAddButton(wrapper)).toBeTruthy()
    })

    it("hides the add button when isModify is false", () => {
        expect.hasAssertions()
        const wrapper = mountTable(false)

        expect(hasAddButton(wrapper)).toBeFalsy()
    })
})
