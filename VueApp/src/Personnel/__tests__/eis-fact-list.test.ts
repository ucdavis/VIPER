import { mount } from "@vue/test-utils"
import EisFactList from "../components/EisFactList.vue"

/**
 * The fact list starts each row of related facts on a new line, so a lone fact (such as Email)
 * leaves the second column empty instead of pulling the next pair up beside it.
 */

describe("eisFactList.vue", () => {
    it("marks the first fact of each row and links facts with an href", () => {
        expect.hasAssertions()
        const wrapper = mount(EisFactList, {
            props: {
                rows: [
                    [{ label: "Email", value: "ada@ucdavis.edu", href: "mailto:ada@ucdavis.edu" }],
                    [
                        { label: "Date of birth", value: "12/1815" },
                        { label: "Age", value: null },
                    ],
                ],
            },
        })

        const facts = wrapper.findAll(".eis-fact")
        expect(facts.map((fact) => fact.find("dt").text())).toStrictEqual(["Email", "Date of birth", "Age"])
        expect(facts.map((fact) => fact.classes("eis-fact--row-start"))).toStrictEqual([true, true, false])
        expect(wrapper.find("a[href='mailto:ada@ucdavis.edu']").text()).toBe("ada@ucdavis.edu")
        expect(facts[2]?.find("dd").text()).toBe("")
    })
})
