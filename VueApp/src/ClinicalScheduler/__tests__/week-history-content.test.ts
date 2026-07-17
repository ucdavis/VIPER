import WeekHistoryContent from "../components/WeekHistoryContent.vue"
import { createTestWrapper } from "./test-utils"
import { useDateFunctions } from "@/composables/DateFunctions"
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

    it("labels the week navigation with the current week and date", () => {
        expect.assertions(2)

        const { formatDate } = useDateFunctions()
        const wrapper = mountContent({ weekNumber: 3, weekDateStart: "2026-01-05" })

        const label = wrapper.find(".week-history__nav-label").text()
        expect(label).toContain("Week 3")
        expect(label).toContain(formatDate("2026-01-05"))
    })

    it("emits prev/next when the enabled nav controls are clicked", async () => {
        expect.assertions(2)

        const wrapper = mountContent({ canPrev: true, canNext: true })

        await wrapper.find("[aria-label='Previous week']").trigger("click")
        await wrapper.find("[aria-label='Next week']").trigger("click")

        expect(wrapper.emitted("prev")).toHaveLength(1)
        expect(wrapper.emitted("next")).toHaveLength(1)
    })

    it("disables nav controls at the schedule ends", () => {
        expect.assertions(2)

        const wrapper = mountContent({ canPrev: false, canNext: false })

        expect(wrapper.find("[aria-label='Previous week']").classes()).toContain("disabled")
        expect(wrapper.find("[aria-label='Next week']").classes()).toContain("disabled")
    })

    it("shows skeleton rows only on the initial load (no entries yet)", () => {
        expect.assertions(2)

        const wrapper = mountContent({ isLoading: true, entries: [] })

        expect(wrapper.findAll(".week-history__skeleton-row")).toHaveLength(3)
        expect(wrapper.find(".week-history__list").exists()).toBeFalsy()
    })

    it("keeps the current rows (dimmed) instead of a skeleton while paging weeks", () => {
        expect.assertions(3)

        // Entries already present while loading = paging to another week
        const wrapper = mountContent({ isLoading: true, entries: [makeEntry()] })

        expect(wrapper.findAll(".week-history__skeleton-row")).toHaveLength(0)
        const list = wrapper.find(".week-history__list")
        expect(list.exists()).toBeTruthy()
        expect(list.classes()).toContain("week-history__list--loading")
    })

    it("shows the loading progress bar only while a fetch is in flight", () => {
        expect.assertions(2)

        const loading = mountContent({ isLoading: true, entries: [makeEntry()] })
        const idle = mountContent({ isLoading: false, entries: [makeEntry()] })

        expect(loading.find(".week-history__progress").exists()).toBeTruthy()
        expect(idle.find(".week-history__progress").exists()).toBeFalsy()
    })
})
