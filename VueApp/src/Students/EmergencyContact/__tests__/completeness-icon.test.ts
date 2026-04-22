import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import CompletenessIcon from "../components/CompletenessIcon.vue"

interface Props {
    complete: string | number
    total: string | number
    missing?: string[]
    label?: string
}

function mountIcon(props: Props) {
    const wrapper = mount(CompletenessIcon, {
        props,
        global: { plugins: [[Quasar, {}]] },
    })
    const icon = wrapper.findComponent({ name: "QIcon" })
    return {
        iconName: icon.props("name") as string,
        iconColor: icon.props("color") as string,
        tooltipText: icon.attributes("aria-label") as string,
    }
}

describe("CompletenessIcon - Icon Selection", () => {
    describe("complete state (complete === total)", () => {
        it("should show green check when all fields complete", () => {
            const { iconName, iconColor, tooltipText } = mountIcon({ complete: 6, total: 6 })
            expect(iconName).toBe("check_circle")
            expect(iconColor).toBe("positive")
            expect(tooltipText).toBe("Complete")
        })

        it("should show green check when 3 of 3 complete", () => {
            const { iconName, iconColor } = mountIcon({ complete: 3, total: 3 })
            expect(iconName).toBe("check_circle")
            expect(iconColor).toBe("positive")
        })
    })

    describe("empty state (complete === 0)", () => {
        it("should show red X with unlabeled Missing tooltip when no label prop", () => {
            const { iconName, iconColor, tooltipText } = mountIcon({ complete: 0, total: 6 })
            expect(iconName).toBe("cancel")
            expect(iconColor).toBe("negative")
            expect(tooltipText).toBe("Missing")
        })

        it("should include the label in the Missing tooltip when provided", () => {
            const { tooltipText } = mountIcon({ complete: 0, total: 3, label: "Student Info" })
            expect(tooltipText).toBe("Missing Student Info")
        })
    })

    describe("partial state (0 < complete < total)", () => {
        it("should show yellow warning with generic tooltip by default", () => {
            const { iconName, iconColor, tooltipText } = mountIcon({ complete: 3, total: 6 })
            expect(iconName).toBe("warning")
            expect(iconColor).toBe("warning")
            expect(tooltipText).toBe("3 of 6 complete")
        })

        it("should list missing field names when missing prop is populated", () => {
            const { tooltipText } = mountIcon({
                complete: 3,
                total: 6,
                missing: ["Work Phone", "Email"],
            })
            expect(tooltipText).toBe("Missing: Work Phone, Email")
        })

        it("should fall back to generic tooltip when missing array is empty", () => {
            const { tooltipText } = mountIcon({ complete: 1, total: 6, missing: [] })
            expect(tooltipText).toBe("1 of 6 complete")
        })

        it("should show yellow warning for 5 of 6", () => {
            const { iconName, iconColor } = mountIcon({ complete: 5, total: 6 })
            expect(iconName).toBe("warning")
            expect(iconColor).toBe("warning")
        })
    })

    describe("edge cases", () => {
        it("should treat complete > total as complete state", () => {
            const { iconName, iconColor, tooltipText } = mountIcon({ complete: 7, total: 6 })
            expect(iconName).toBe("check_circle")
            expect(iconColor).toBe("positive")
            expect(tooltipText).toBe("Complete")
        })

        it("should treat 0 of 0 as complete state", () => {
            const { iconName, iconColor, tooltipText } = mountIcon({ complete: 0, total: 0 })
            expect(iconName).toBe("check_circle")
            expect(iconColor).toBe("positive")
            expect(tooltipText).toBe("Complete")
        })

        it("should coerce string complete/total props to numbers", () => {
            const { iconName, tooltipText } = mountIcon({ complete: "3", total: "6" })
            expect(iconName).toBe("warning")
            expect(tooltipText).toBe("3 of 6 complete")
        })
    })
})
