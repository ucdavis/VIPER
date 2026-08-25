import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import SVMFrequentNumberTable from "../components/SVMFrequentNumberTable.vue"
import type { SVMFrequentNumberRecord } from "../types/svm-phone-types"

const numbers: SVMFrequentNumberRecord[] = [{ label: "Front Desk", phone: "530-555-1000", entryId: 1 }]

function mountTable(editRecords: boolean) {
    return mount(SVMFrequentNumberTable, {
        props: { frequentNumbers: numbers, loading: false, editRecords, search: "" },
        global: { plugins: [Quasar] },
    })
}

function hasAddButton(wrapper: ReturnType<typeof mountTable>): boolean {
    return wrapper.findAllComponents({ name: "QBtn" }).some((btn) => btn.props("icon") === "add")
}

describe("sVMFrequentNumberTable.vue - editRecords gating", () => {
    it("shows the add button and edit/delete action buttons when editRecords is true", () => {
        expect.hasAssertions()
        const wrapper = mountTable(true)

        expect(hasAddButton(wrapper)).toBeTruthy()
        expect(wrapper.findAllComponents({ name: "RecordActionButton" })).toHaveLength(2)
    })

    it("hides the add button and edit/delete action buttons when editRecords is false", () => {
        expect.hasAssertions()
        const wrapper = mountTable(false)

        expect(hasAddButton(wrapper)).toBeFalsy()
        expect(wrapper.findAllComponents({ name: "RecordActionButton" })).toHaveLength(0)
    })
})
