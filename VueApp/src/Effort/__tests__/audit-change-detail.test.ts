import { mount } from "@vue/test-utils"
import AuditChangeDetail from "../components/AuditChangeDetail.vue"
import type { ChangeDetail } from "../types"

function mountDetail(name: string, detail: ChangeDetail) {
    return mount(AuditChangeDetail, { props: { name, detail } })
}

describe("auditChangeDetail", () => {
    it("shows a reference value once without diff styling", () => {
        expect.assertions(3)

        const wrapper = mountDetail("Course", { oldValue: "VET 410", newValue: "VET 410" })

        expect(wrapper.text()).toContain("Course:")
        expect(wrapper.text()).toContain("VET 410")
        expect(wrapper.find(".text-negative").exists()).toBeFalsy()
    })

    it("shows old and new values with diff styling when changed", () => {
        expect.assertions(3)

        const wrapper = mountDetail("Hours", { oldValue: "10", newValue: "12" })

        expect(wrapper.find(".text-negative").text()).toBe("10")
        expect(wrapper.find(".text-positive").text()).toBe("12")
        expect(wrapper.text()).toContain("→")
    })

    it("omits the old value and arrow when the value was added", () => {
        expect.assertions(3)

        const wrapper = mountDetail("Role", { oldValue: null, newValue: "Instructor" })

        expect(wrapper.find(".text-negative").exists()).toBeFalsy()
        expect(wrapper.find(".text-positive").text()).toBe("Instructor")
        expect(wrapper.text()).not.toContain("→")
    })

    it("omits the new value and arrow when the value was removed", () => {
        expect.assertions(3)

        const wrapper = mountDetail("Role", { oldValue: "Instructor", newValue: null })

        expect(wrapper.find(".text-negative").text()).toBe("Instructor")
        expect(wrapper.find(".text-positive").exists()).toBeFalsy()
        expect(wrapper.text()).not.toContain("→")
    })
})
