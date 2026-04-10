import { describe, it, expect } from "vitest"
import { computed } from "vue"

/**
 * Tests for CompletenessIcon component logic.
 * Validates icon selection and color based on complete/total values.
 * Uses logic-only tests following the project's existing test patterns.
 */

function getIconState(complete: number, total: number) {
    const iconName = computed(() => {
        if (complete >= total) {
            return "check_circle"
        }
        if (complete === 0) {
            return "cancel"
        }
        return "warning"
    })

    const iconColor = computed(() => {
        if (complete >= total) {
            return "positive"
        }
        if (complete === 0) {
            return "negative"
        }
        return "warning"
    })

    const tooltipText = computed(() => `${complete} of ${total} complete`)

    return { iconName: iconName.value, iconColor: iconColor.value, tooltipText: tooltipText.value }
}

describe("CompletenessIcon - Icon Selection", () => {
    describe("complete state (complete === total)", () => {
        it("should show green check when all fields complete", () => {
            const { iconName, iconColor, tooltipText } = getIconState(6, 6)

            expect(iconName).toBe("check_circle")
            expect(iconColor).toBe("positive")
            expect(tooltipText).toBe("6 of 6 complete")
        })

        it("should show green check when 3 of 3 complete", () => {
            const { iconName, iconColor } = getIconState(3, 3)

            expect(iconName).toBe("check_circle")
            expect(iconColor).toBe("positive")
        })
    })

    describe("empty state (complete === 0)", () => {
        it("should show red X when no fields complete", () => {
            const { iconName, iconColor, tooltipText } = getIconState(0, 6)

            expect(iconName).toBe("cancel")
            expect(iconColor).toBe("negative")
            expect(tooltipText).toBe("0 of 6 complete")
        })

        it("should show red X for student info with 0 of 3", () => {
            const { iconName, iconColor } = getIconState(0, 3)

            expect(iconName).toBe("cancel")
            expect(iconColor).toBe("negative")
        })
    })

    describe("partial state (0 < complete < total)", () => {
        it("should show yellow warning when partially complete", () => {
            const { iconName, iconColor, tooltipText } = getIconState(3, 6)

            expect(iconName).toBe("warning")
            expect(iconColor).toBe("warning")
            expect(tooltipText).toBe("3 of 6 complete")
        })

        it("should show yellow warning for 1 of 6", () => {
            const { iconName, iconColor } = getIconState(1, 6)

            expect(iconName).toBe("warning")
            expect(iconColor).toBe("warning")
        })

        it("should show yellow warning for 5 of 6", () => {
            const { iconName, iconColor } = getIconState(5, 6)

            expect(iconName).toBe("warning")
            expect(iconColor).toBe("warning")
        })

        it("should show yellow warning for 1 of 3", () => {
            const { iconName, iconColor } = getIconState(1, 3)

            expect(iconName).toBe("warning")
            expect(iconColor).toBe("warning")
        })

        it("should show yellow warning for 2 of 3", () => {
            const { iconName, iconColor } = getIconState(2, 3)

            expect(iconName).toBe("warning")
            expect(iconColor).toBe("warning")
        })
    })

    describe("edge cases", () => {
        it("should handle complete > total as complete state", () => {
            const { iconName, iconColor } = getIconState(7, 6)

            expect(iconName).toBe("check_circle")
            expect(iconColor).toBe("positive")
        })

        it("should handle 0 of 0 as complete state", () => {
            const { iconName, iconColor } = getIconState(0, 0)

            // 0 >= 0 is true, so this is "complete"
            expect(iconName).toBe("check_circle")
            expect(iconColor).toBe("positive")
        })
    })
})
