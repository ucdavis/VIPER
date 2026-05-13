import { mount } from "@vue/test-utils"
import { Quasar } from "quasar"

import AssessmentBubble from "../components/AssessmentBubble.vue"

/**
 * Tests for AssessmentBubble — the rating dot rendered on CTS assessment lists.
 *
 * Focus areas:
 * 1. Privacy: aria-label must surface the descriptive rating label, never the
 *    numeric value. Students should not hear "Rating 1 of 5" from a screen
 *    reader when they are low-rated.
 * 2. Class mapping: value/maxValue drive the bubbleClass contract consumed
 *    by cts.css.
 * 3. Click contract: clickable variant (id prop set) emits bubble-click with
 *    the id; non-clickable variant renders as a non-interactive span.
 */

function createWrapper(props: Record<string, unknown>) {
    return mount(AssessmentBubble, {
        props: props as never,
        global: {
            plugins: [[Quasar, {}]],
        },
    })
}

describe(AssessmentBubble, () => {
    describe("aria-label privacy", () => {
        it("uses levelName on the clickable button and does not expose the numeric value", () => {
            const wrapper = createWrapper({
                maxValue: 5,
                value: 1,
                levelName: "Trust with indirect supervision",
                id: 42,
            })

            const label = wrapper.get("button").attributes("aria-label")!
            expect(label).toContain("Trust with indirect supervision")
            expect(label).not.toMatch(/\b1 of 5\b/i)
            expect(label).not.toMatch(/rating\s+\d/i)
        })

        it("uses levelName on the standalone span and does not expose the numeric value", () => {
            const wrapper = createWrapper({
                maxValue: 5,
                value: 2,
                levelName: "Trust with direct supervision",
            })

            const label = wrapper.get('span[role="img"]').attributes("aria-label")!
            expect(label).toBe("Trust with direct supervision")
            expect(label).not.toMatch(/\b2 of 5\b/i)
        })

        it("renders the standalone span as aria-hidden when levelName is empty", () => {
            const wrapper = createWrapper({
                maxValue: 5,
                value: 2,
            })

            expect(wrapper.find('span[role="img"]').exists()).toBeFalsy()
            const decorative = wrapper.get('span[aria-hidden="true"]')
            expect(decorative.attributes("aria-label")).toBeUndefined()
        })

        it("appends open-details hint on the clickable variant", () => {
            const wrapper = createWrapper({
                maxValue: 5,
                value: 3,
                levelName: "Independent remote supervision",
                id: 7,
            })

            expect(wrapper.get("button").attributes("aria-label")).toBe(
                "Independent remote supervision, open assessment details",
            )
        })

        it("falls back to a generic hint when levelName is missing on a clickable bubble", () => {
            const wrapper = createWrapper({
                maxValue: 5,
                value: 3,
                id: 7,
            })

            expect(wrapper.get("button").attributes("aria-label")).toBe("Open assessment details")
        })
    })

    describe("bubbleClass contract", () => {
        it.each([
            [1, "assessmentBubble5_1"],
            [2, "assessmentBubble5_2"],
            [3, "assessmentBubble5_3"],
            [4, "assessmentBubble5_4"],
            [5, "assessmentBubble5_5"],
        ])("maps value=%i to %s", (value, expected) => {
            const wrapper = createWrapper({
                maxValue: 5,
                value,
                levelName: "Label",
            })

            expect(wrapper.get('span[role="img"]').classes()).toContain(expected)
        })

        it.each([0, 6])("yields no level class for out-of-range value=%i", (value) => {
            const wrapper = createWrapper({
                maxValue: 5,
                value,
                levelName: "Label",
            })

            const classes = wrapper.get('span[role="img"]').classes()
            expect(classes.some((c) => c.startsWith("assessmentBubble5_"))).toBeFalsy()
        })

        it("yields no level class when maxValue is not 5", () => {
            const wrapper = createWrapper({
                maxValue: 3,
                value: 2,
                levelName: "Label",
            })

            const classes = wrapper.get('span[role="img"]').classes()
            expect(classes.some((c) => c.startsWith("assessmentBubble5_"))).toBeFalsy()
        })
    })

    describe("click behaviour", () => {
        it("renders a button and emits bubble-click with the id when clicked", async () => {
            const wrapper = createWrapper({
                maxValue: 5,
                value: 3,
                levelName: "Label",
                id: 99,
            })

            await wrapper.get("button").trigger("click")

            expect(wrapper.emitted("bubble-click")).toEqual([[99]])
        })

        it("renders a non-interactive span and does not emit when id is omitted", () => {
            const wrapper = createWrapper({
                maxValue: 5,
                value: 3,
                levelName: "Label",
            })

            expect(wrapper.find("button").exists()).toBeFalsy()
            expect(wrapper.find('span[role="img"]').exists()).toBeTruthy()
            expect(wrapper.emitted("bubble-click")).toBeUndefined()
        })
    })

    describe("bubble content", () => {
        it("does not render the numeric value inside the bubble", () => {
            const wrapper = createWrapper({
                maxValue: 5,
                value: 4,
                levelName: "Label",
                id: 1,
            })

            const bubble = wrapper.get("span.assessmentBubble")
            expect(bubble.text()).toBe("")
        })
    })
})
