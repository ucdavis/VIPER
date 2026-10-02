import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"
import CompletenessIcon from "../components/CompletenessIcon.vue"

/**
 * Tests for the career selection CompletenessIcon: one field is either answered or not, unlike
 * the emergency contact icon, which counts fields and has a partial state.
 */

interface Props {
    complete: boolean
    label?: string
    showMissing?: boolean
}

function mountIcon(props: Props) {
    const wrapper = mount(CompletenessIcon, {
        props,
        global: { plugins: [[Quasar, {}]] },
    })
    const icon = wrapper.findComponent({ name: "QIcon" })
    // Read the name off the sr-only text, not the icon: Quasar sets aria-hidden on every
    // q-icon, so anything named there is dropped from the accessibility tree.
    const srOnly = wrapper.find(".sr-only")
    return {
        rendered: icon.exists(),
        iconName: icon.exists() ? (icon.props("name") as string) : "",
        iconColor: icon.exists() ? (icon.props("color") as string) : "",
        tooltipText: srOnly.exists() ? srOnly.text() : undefined,
        srOnlyHidden: srOnly.exists() ? srOnly.attributes("aria-hidden") : undefined,
    }
}

describe("completeness icon", () => {
    describe("answered", () => {
        it("shows a green check", () => {
            expect.hasAssertions()
            const { iconName, iconColor, tooltipText, srOnlyHidden } = mountIcon({ complete: true })
            expect(iconName).toBe("check_circle")
            expect(iconColor).toBe("positive")
            expect(tooltipText).toBe("Complete")
            // The whole point of the sibling: it must not inherit the icon's aria-hidden.
            expect(srOnlyHidden).toBeUndefined()
        })

        it("keeps the tooltip generic even when a label is given", () => {
            expect.hasAssertions()
            const { tooltipText } = mountIcon({ complete: true, label: "Direction" })
            expect(tooltipText).toBe("Complete")
        })
    })

    describe("unanswered", () => {
        it("shows a red cross", () => {
            expect.hasAssertions()
            const { iconName, iconColor, tooltipText } = mountIcon({ complete: false })
            expect(iconName).toBe("cancel")
            expect(iconColor).toBe("negative")
            expect(tooltipText).toBe("Missing")
        })

        it("names the field in the tooltip when a label is given", () => {
            expect.hasAssertions()
            const { tooltipText } = mountIcon({ complete: false, label: "Direction" })
            expect(tooltipText).toBe("Missing Direction")
        })
    })

    describe("optional fields (showMissing false)", () => {
        it("renders nothing when unanswered", () => {
            expect.hasAssertions()
            // A second species focus is optional, so an unset one is left blank rather than flagged.
            const { rendered } = mountIcon({ complete: false, showMissing: false })
            expect(rendered).toBeFalsy()
        })

        it("still shows the green check when answered", () => {
            expect.hasAssertions()
            const { iconName, iconColor } = mountIcon({ complete: true, showMissing: false })
            expect(iconName).toBe("check_circle")
            expect(iconColor).toBe("positive")
        })
    })
})
