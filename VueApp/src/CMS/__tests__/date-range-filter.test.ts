import DateRangeFilter from "@/CMS/components/DateRangeFilter.vue"
import { mountCms } from "./test-utils"

/**
 * DateRangeFilter is a small shared component (FileAuditLog + ContentBlockHistory both
 * use it). It exposes v-model:from / v-model:to and emits "change" whenever either input
 * updates, so the parent can reload. Lock down the binding and the change emission.
 */

describe("DateRangeFilter.vue", () => {
    it("renders From and To date inputs bound to the models", () => {
        const wrapper = mountCms(DateRangeFilter, {
            props: { from: "2024-01-01", to: "2024-02-01" },
        })
        const inputs = wrapper.findAll("input[type='date']")
        expect(inputs).toHaveLength(2)
        expect((inputs[0]!.element as HTMLInputElement).value).toBe("2024-01-01")
        expect((inputs[1]!.element as HTMLInputElement).value).toBe("2024-02-01")
    })

    it("emits update:from and change when the From input changes", async () => {
        const wrapper = mountCms(DateRangeFilter, {
            props: { from: "", to: "" },
        })
        const fromInput = wrapper.findAll("input[type='date']")[0]!
        await fromInput.setValue("2024-05-10")

        expect(wrapper.emitted("update:from")?.at(-1)).toEqual(["2024-05-10"])
        expect(wrapper.emitted("change")).toBeTruthy()
    })

    it("emits update:to and change when the To input changes", async () => {
        const wrapper = mountCms(DateRangeFilter, {
            props: { from: "", to: "" },
        })
        const toInput = wrapper.findAll("input[type='date']")[1]!
        await toInput.setValue("2024-06-15")

        expect(wrapper.emitted("update:to")?.at(-1)).toEqual(["2024-06-15"])
        expect(wrapper.emitted("change")).toBeTruthy()
    })

    it("emits change separately for each input edit", async () => {
        const wrapper = mountCms(DateRangeFilter, {
            props: { from: "", to: "" },
        })
        const [fromInput, toInput] = wrapper.findAll("input[type='date']")
        await fromInput!.setValue("2024-01-01")
        await toInput!.setValue("2024-12-31")
        expect(wrapper.emitted("change")).toHaveLength(2)
    })
})
