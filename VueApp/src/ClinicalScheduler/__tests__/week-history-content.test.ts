import WeekHistoryContent from "../components/WeekHistoryContent.vue"
import { createTestWrapper } from "./test-utils"
import type { AuditLogEntry } from "../types/audit-types"

function makeEntry(overrides: Partial<AuditLogEntry> = {}): AuditLogEntry {
    return {
        scheduleAuditId: 1,
        area: "Clinicians",
        mothraId: "abc123",
        personName: "Dr. Example",
        action: "Added to rotation",
        rotationId: 5,
        rotationName: "Cardiology",
        weekId: 10,
        weekNum: 3,
        weekStart: "2026-01-05",
        term: "Spring 2026",
        modifiedBy: "mgr456",
        modifiedByName: "Manager Person",
        timeStamp: "2026-01-06T08:30:00",
        ...overrides,
    }
}

function mountContent(props: Record<string, unknown> = {}) {
    return createTestWrapper({
        component: WeekHistoryContent,
        props: {
            titleId: "week-history-title-test",
            viewMode: "rotation",
            weekNumber: 3,
            weekDateStart: "2026-01-05",
            contextLabel: "Cardiology",
            entries: [],
            isLoading: false,
            error: null,
            ...props,
        },
    })
}

describe("weekHistoryContent", () => {
    it("shows skeleton rows while loading", () => {
        expect.assertions(2)

        const wrapper = mountContent({ isLoading: true })

        expect(wrapper.findAll(".week-history__skeleton-row")).toHaveLength(3)
        expect(wrapper.attributes("aria-busy")).toBe("true")
    })

    it("shows the error state and emits retry", async () => {
        expect.assertions(2)

        const wrapper = mountContent({ error: "Failed to load the audit trail" })

        expect(wrapper.find("[role='alert']").text()).toContain("Failed to load the audit trail")

        await wrapper.find("[role='alert'] button").trigger("click")

        expect(wrapper.emitted("retry")).toHaveLength(1)
    })

    it("shows an empty state when there are no entries", () => {
        expect.assertions(1)

        const wrapper = mountContent({ entries: [] })

        expect(wrapper.text()).toContain("No audit entries for this week.")
    })

    it("lists entries with the clinician as subject in rotation view", () => {
        expect.assertions(3)

        const wrapper = mountContent({ viewMode: "rotation", entries: [makeEntry()] })

        expect(wrapper.find(".week-history__subject").text()).toBe("Dr. Example")
        expect(wrapper.text()).toContain("Added to rotation")
        expect(wrapper.text()).toContain("Manager Person")
    })

    it("lists entries with the rotation as subject in clinician view", () => {
        expect.assertions(1)

        const wrapper = mountContent({ viewMode: "clinician", entries: [makeEntry()] })

        expect(wrapper.find(".week-history__subject").text()).toBe("Cardiology")
    })
})
