import ModifiedStamp from "@/CMS/components/ModifiedStamp.vue"
import { mountCms } from "./test-utils"

/**
 * ModifiedStamp shows a "MM/DD/YY by <user>" stamp. It renders a <q-td> in table mode
 * (cellProps given) or a plain <span> in card/grid mode (row given), sharing one date
 * format. A blank modifiedOn must produce no date text.
 */

describe("ModifiedStamp.vue", () => {
    it("formats modifiedOn as MM/DD/YY (en-US 2-digit)", () => {
        // Card mode (row prop) and table mode share the same `formatted` computed; assert the
        // format via the plainly-rendered span so we don't depend on QTable's slot internals.
        const wrapper = mountCms(ModifiedStamp, {
            props: { row: { modifiedOn: "2024-03-09T12:00:00", modifiedBy: "jdoe" } },
        })
        expect(wrapper.text()).toContain("03/09/24")
        expect(wrapper.text()).toContain("jdoe")
    })

    it("renders a q-td when given cellProps (table mode)", () => {
        const wrapper = mountCms(ModifiedStamp, {
            props: { cellProps: { row: { modifiedOn: "2024-03-09T12:00:00", modifiedBy: "jdoe" } } },
        })
        expect(wrapper.findComponent({ name: "QTd" }).exists()).toBeTruthy()
    })

    it("renders a plain span (no q-td) when given a row directly (card mode)", () => {
        const wrapper = mountCms(ModifiedStamp, {
            props: { row: { modifiedOn: "2024-03-09T12:00:00", modifiedBy: "asmith" } },
        })
        expect(wrapper.findComponent({ name: "QTd" }).exists()).toBeFalsy()
        expect(wrapper.find("span").exists()).toBeTruthy()
        expect(wrapper.text()).toContain("03/09/24")
        expect(wrapper.text()).toContain("asmith")
    })

    it("renders no date text when modifiedOn is empty", () => {
        const wrapper = mountCms(ModifiedStamp, {
            props: { row: { modifiedOn: "", modifiedBy: "nobody" } },
        })
        expect(wrapper.text().trim()).toBe("nobody")
        // No slashes from a formatted date.
        expect(wrapper.text()).not.toContain("/")
    })

    it("chooses the q-td (cellProps) branch when both cellProps and row are supplied", () => {
        // The template's v-if is on cellProps, so supplying both renders the q-td branch (and
        // the stamp computed prefers cellProps.row), never the plain-span row branch.
        const wrapper = mountCms(ModifiedStamp, {
            props: {
                cellProps: { row: { modifiedOn: "2024-03-09T12:00:00", modifiedBy: "fromCell" } },
                row: { modifiedOn: "2020-01-01T00:00:00", modifiedBy: "fromRow" },
            },
        })
        expect(wrapper.findComponent({ name: "QTd" }).exists()).toBeTruthy()
        // The span-only fallback branch must not render alongside the q-td.
        expect(wrapper.text()).not.toContain("fromRow")
    })
})
